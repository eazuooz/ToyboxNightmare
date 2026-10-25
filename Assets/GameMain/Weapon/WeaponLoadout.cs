using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어가 장착한 무기 묶음. 장착 요청·전환·발사 입력·전역 쿨다운을 한곳에서 맡는다.
    ///
    /// <b>무기는 엔티티다</b>(<see cref="WeaponLogicBase"/>). 이 로드아웃은 무기를 만들지 않는다 —
    /// <see cref="Equip"/> 이 <c>ShowEntity</c> 로 요청하면 무기가 스스로 플레이어에게 붙고,
    /// 플레이어가 <c>OnAttached</c>/<c>OnDetached</c> 에서 <see cref="Attach"/>/<see cref="Detach"/> 로
    /// 알려 준다. 회수도 하지 않는다. 플레이어가 회수되면 코어가 붙어 있는 무기를 먼저 회수한다.
    ///
    /// 로드아웃 자체는 엔티티가 아니다. 소유 <see cref="PlayerLogic"/> 가 들고
    /// <see cref="PlayerLogic.TickAliveInput"/> 안에서 <see cref="OnUpdate"/> 를 직접 굴린다 —
    /// "조준점 계산 → 무기" 순서가 엔티티 갱신 순서에 좌우되지 않게 하기 위해서다.
    ///
    /// 원본 좀비토이의 <c>PlayerAttack</c> 자리에 해당한다 — 마우스 조준 + 수동 발사이고,
    /// 활성 무기는 항상 하나다.
    /// </summary>
    public class WeaponLoadout
    {
        /// <summary>
        /// 시작 무기 한 종의 명세. 어떤 로직을 어떤 프리팹에 붙여 띄울지와, 그 무기의 데이터를 만드는 법.
        /// 능력치(쿨다운·사거리·피해…)는 무기마다 다른 데이터가 표에서 채운다.
        /// </summary>
        private struct WeaponSpec
        {
            public Type   LogicType;
            public string AssetName;

            /// <summary>(엔티티 Id, 소유자 Id, 슬롯) → 그 무기의 데이터.</summary>
            public Func<int, int, int, WeaponData> CreateData;

            public WeaponSpec(Type logicType, string assetName, Func<int, int, int, WeaponData> createData)
            {
                LogicType  = logicType;
                AssetName  = assetName;
                CreateData = createData;
            }
        }

        /// <summary>
        /// 이 캐릭터가 들고 시작하는 무기. 배열 순서가 곧 슬롯 번호이자 Tab 순환 순서다.
        /// 레벨업 시스템이 붙으면 여기에 추가하는 형태가 된다.
        /// </summary>
        private static readonly WeaponSpec[] sStartingWeapons =
        {
            new WeaponSpec(typeof(LightningWeaponLogic), WeaponTable.LightningWeaponAsset,
                (id, ownerId, slot) => LightningWeaponData.Create(id, WeaponTypeId, ownerId, slot)),
            new WeaponSpec(typeof(FrostWeaponLogic),     WeaponTable.FrostWeaponAsset,
                (id, ownerId, slot) => FrostWeaponData.Create(id, WeaponTypeId, ownerId, slot)),
            new WeaponSpec(typeof(StinkWeaponLogic),     WeaponTable.StinkWeaponAsset,
                (id, ownerId, slot) => StinkWeaponData.Create(id, WeaponTypeId, ownerId, slot)),
            new WeaponSpec(typeof(SlimeWeaponLogic),     WeaponTable.SlimeWeaponAsset,
                (id, ownerId, slot) => SlimeWeaponData.Create(id, WeaponTypeId, ownerId, slot)),
        };

        /// <summary>무기 엔티티의 TypeId. 이 프로젝트는 TypeId 로 구분하지 않아 전부 1 이다.</summary>
        private const int WeaponTypeId = 1;

        /// <summary>이 로드아웃의 소유자. 무기는 이 엔티티에 붙는다.</summary>
        private readonly PlayerLogic mOwner = null;

        /// <summary>
        /// 슬롯별 무기. 무기는 비동기로 로드되므로 <b>아직 도착하지 않은 슬롯은 null 이다.</b>
        /// 이 중 <see cref="mActiveSlot"/> 하나만 활성이다.
        /// </summary>
        private readonly WeaponLogicBase[] mSlots = new WeaponLogicBase[sStartingWeapons.Length];

        private int mActiveSlot = 0;

        /// <summary>
        /// 무기를 전부 내려놓은 상태인가(사망). 이 뒤에 늦게 도착한 무기를 켜지 않기 위해 둔다.
        /// </summary>
        private bool mShutdown = false;

        // ─── 전역 쿨다운 ───

        /// <summary>
        /// 쿨다운은 <b>무기별이 아니라 전역 하나</b>다. 원본 <c>PlayerAttack</c> 이
        /// <c>attackCooldown</c>/<c>timeOfLastAttack</c> 한 쌍만 들고, 마지막으로 발사에 성공한
        /// 무기의 쿨다운 값을 거기 덮어쓰는 구조를 그대로 옮긴 것이다.
        ///
        /// 무기를 바꿔도 리셋하지 않는다 — 원본도 그렇고, 리셋하면 Tab 연타로 쿨다운을 지울 수 있다.
        /// </summary>
        private float mAttackCooldown = 0f;

        /// <summary>마지막 발사 시각. 원본과 같이 <see cref="Time.time"/> 기준이다.</summary>
        private float mTimeOfLastAttack = 0f;

        /// <summary>지금 발사할 수 있는지. 원본 <c>PlayerAttack.ReadyToAttack()</c> 과 같은 식이다.</summary>
        public bool IsReadyToAttack
        {
            get { return Time.time >= mTimeOfLastAttack + mAttackCooldown; }
        }

        public WeaponLoadout(PlayerLogic owner)
        {
            mOwner = owner;
        }

        /// <summary>지금 들고 있는 무기. 그 슬롯의 무기가 아직 도착 전이면 null.</summary>
        private WeaponLogicBase ActiveWeapon
        {
            get
            {
                GameAssert.InRange(mActiveSlot, mSlots.Length, "mActiveSlot");
                return mSlots[mActiveSlot];
            }
        }

        // ─── 장착 ───

        /// <summary>
        /// 시작 무기를 요청한다. 플레이어가 스폰(재스폰)될 때마다 한 번 부른다.
        ///
        /// 여기서는 <b>요청만</b> 한다. 무기는 로드가 끝나면 스스로 플레이어에게 붙고,
        /// 그때 <see cref="Attach"/> 가 불려 슬롯에 들어온다.
        /// </summary>
        public void Equip()
        {
            // 재스폰 때마다 첫 무기부터, 쿨다운 없이 시작한다.
            mShutdown   = false;
            mActiveSlot = 0;
            ClearCooldown();

            // 계약: 무기는 소유자 엔티티의 Id 로 붙는다. Entity 가 없으면 붙을 곳이 없다.
            GameAssert.IsTrue(mOwner != null && mOwner.Entity != null, "WeaponLoadout.Equip: 소유자 엔티티가 없다.");
            if (mOwner == null || mOwner.Entity == null) return;

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("WeaponLoadout: EntityComponent 를 찾지 못했다. 무기를 띄울 수 없다.");
                return;
            }

            int ownerId = mOwner.Entity.Id;
            for (int slot = 0; slot < sStartingWeapons.Length; slot++)
            {
                WeaponSpec spec = sStartingWeapons[slot];

                int id = EntitySerialId.Next();
                entityComponent.ShowEntity(
                    id,
                    spec.LogicType,
                    spec.AssetName,
                    WeaponTable.WeaponGroup,
                    spec.CreateData(id, ownerId, slot));
            }

            Log.Info("무기 {0}종 요청. 마우스 좌클릭 발사 / Tab 으로 전환.", sStartingWeapons.Length);
        }

        /// <summary>
        /// 무기가 플레이어에게 붙었다. <see cref="PlayerLogic.OnAttached"/> 가 부른다.
        /// 도착 순서와 무관하게 자기 슬롯에 들어간다.
        /// </summary>
        public void Attach(WeaponLogicBase weapon)
        {
            if (weapon == null) return;

            int slot = weapon.Slot;
            if (slot < 0 || slot >= mSlots.Length)
            {
                Log.Error("WeaponLoadout: 슬롯 {0} 은 범위 밖이다({1}). 이 무기는 쓰이지 않는다.", slot, weapon.GetType().Name);
                return;
            }

            mSlots[slot] = weapon;
            ApplySelection();
        }

        /// <summary>무기가 플레이어에게서 떨어졌다(회수 직전). <see cref="PlayerLogic.OnDetached"/> 가 부른다.</summary>
        public void Detach(WeaponLogicBase weapon)
        {
            // 슬롯 번호를 무기에게 다시 묻지 않고 참조로 찾는다. 떨어지는 시점에는
            // 무기 쪽 데이터가 이미 정리 중일 수 있다.
            for (int i = 0; i < mSlots.Length; i++)
            {
                if (mSlots[i] == weapon)
                {
                    mSlots[i] = null;
                }
            }
        }

        /// <summary>
        /// 활성 무기만 켠다. 꺼진 무기는 조준 갱신도 발사 입력도 받지 못하고
        /// 총구 VFX·레티클도 함께 꺼진다.
        /// </summary>
        private void ApplySelection()
        {
            for (int i = 0; i < mSlots.Length; i++)
            {
                if (mSlots[i] == null) continue;

                mSlots[i].SetActive(!mShutdown && i == mActiveSlot);
            }
        }

        // ─── 갱신 ───

        /// <summary>
        /// 활성 무기의 조준을 갱신하고 발사 입력을 읽는다.
        /// <see cref="PlayerLogic.TickAliveInput"/> 이 조준점 계산 뒤에 부르므로 조준이 한 프레임 밀리지 않는다.
        /// </summary>
        public void OnUpdate()
        {
            WeaponLogicBase weapon = ActiveWeapon;
            if (weapon == null) return;

            // 쿨다운 판정은 프레임당 한 번만 해서 조준 표시(레티클 색)와 실제 발사 가부가
            // 어긋나지 않게 한다.
            bool isReady = IsReadyToAttack;

            weapon.UpdateAim(isReady);
            ReadFireInput(weapon, isReady);
        }

        /// <summary>
        /// 원본 <c>PlayerInputPC</c> 의 Fire1 처리. 누르는 동안과 떼는 프레임이 나뉘어 있고
        /// 무기마다 어느 쪽에서 나가는지가 다르다(Lightning·Frost=누름 / Stink·Slime=뗌).
        /// </summary>
        private void ReadFireInput(WeaponLogicBase weapon, bool isReady)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                // 마우스가 사라져도(무선 절전, USB 분리) 지속 발사는 끊어야 한다.
                // 여기서 그냥 return 하면 "뗀 프레임" 이 영영 오지 않아 Frost 콘이 켜진 채
                // 굳고, 콘 안의 적이 매 프레임 접촉 갱신을 받아 영구 빙결된다.
                // StopFiring 은 4종 모두 멱등이라 매 프레임 불려도 안전하다.
                weapon.StopFiring();
                return;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                // StopFiring 은 쿨다운 검사 **밖**에서 항상 부른다.
                // 원본 PlayerAttack 은 StopFiring 까지 ReadyToAttack() 안에 넣어 두어,
                // 직전에 Stink(쿨다운 5초)를 쏘고 Frost 로 바꾸면 버튼을 떼도 콘이 안 꺼졌다.
                // 명백한 결함이므로 이식하지 않는다.
                weapon.StopFiring();

                if (isReady && weapon.TryFireReleased())
                {
                    BeginCooldown(weapon.Cooldown);
                }

                return;
            }

            if (!mouse.leftButton.isPressed) return;

            if (isReady && weapon.TryFireHeld())
            {
                BeginCooldown(weapon.Cooldown);
            }
        }

        /// <summary>발사에 성공한 무기의 쿨다운을 전역 타이머에 싣는다.</summary>
        private void BeginCooldown(float cooldown)
        {
            // 쿨다운이 없는 무기(Frost)는 0 을 준다. 음수를 실으면 다음 발사가
            // 과거로 밀려 다른 무기의 쿨다운까지 건너뛴다.
            mAttackCooldown   = Mathf.Max(0f, cooldown);
            mTimeOfLastAttack = Time.time;

            // 쿨다운이 0 인 무기(Frost)는 게이지를 건드리지 않는다 — 0 을 넘기면
            // HUD 게이지가 가득 찼다가 즉시 비는 깜빡임이 된다.
            if (mAttackCooldown <= 0f) return;

            EventComponent events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;

            events.Fire(this, WeaponCooldownStartedEventArgs.Create(mAttackCooldown));
        }

        /// <summary>전역 쿨다운을 지워 즉시 쏠 수 있게 한다. 장착(재스폰) 시점에만 쓴다.</summary>
        private void ClearCooldown()
        {
            mAttackCooldown   = 0f;
            mTimeOfLastAttack = 0f;
        }

        /// <summary>
        /// 무기를 전부 내려놓는다. 총구 VFX·레티클·Frost 콘이 여기서 꺼진다.
        /// 사망 시 부른다 — 죽으면 갱신이 멈춰 무기 스스로는 못 끈다.
        ///
        /// 내려놓을 뿐 회수하지 않는다. 무기 엔티티는 플레이어가 회수될 때 함께 회수된다.
        /// </summary>
        public void Shutdown()
        {
            mShutdown = true;
            ApplySelection();
        }

        /// <summary>Tab 무기 순환. 원본 <c>PlayerInputPC</c> 의 SwitchAttack 자리다.</summary>
        public void ReadSwitchInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;

            int nextSlot = FindNextEquippedSlot(mActiveSlot);
            if (nextSlot == mActiveSlot) return;

            // 전환 전에 이전 무기의 지속 발사를 반드시 끊는다. 버튼을 누른 채 Tab 을 치면
            // 그 무기는 "뗀 프레임" 을 영영 못 받는다 — Frost 콘이 켜진 채로 남는다.
            // 뒤따르는 SetActive(false) 가 연출을 끄더라도, 무기가 들고 있는
            // "발사 중" 상태까지 지우는 것은 StopFiring 뿐이므로 명시적으로 부른다.
            WeaponLogicBase previous = ActiveWeapon;
            if (previous != null)
            {
                previous.StopFiring();
            }

            mActiveSlot = nextSlot;
            ApplySelection();

            Log.Info("무기 전환 → {0} ({1}/{2})",
                mSlots[mActiveSlot].GetType().Name, mActiveSlot + 1, mSlots.Length);
        }

        /// <summary>
        /// from 다음부터 돌면서 무기가 도착해 있는 첫 슬롯. 아직 로드 중인 슬롯은 건너뛴다.
        /// 다른 무기가 하나도 없으면 from 을 그대로 돌려준다.
        /// </summary>
        private int FindNextEquippedSlot(int from)
        {
            for (int step = 1; step < mSlots.Length; step++)
            {
                int slot = (from + step) % mSlots.Length;
                if (mSlots[slot] != null) return slot;
            }

            return from;
        }
    }
}
