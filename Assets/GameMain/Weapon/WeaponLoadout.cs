using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어가 장착한 무기 묶음. 장착·전환·발사 입력·정리를 한곳에서 맡는다.
    ///
    /// <b>MonoBehaviour 가 아니다.</b> 소유 <see cref="Player"/> 가 인스턴스를 만들어 들고
    /// <see cref="Player.OnUpdate"/> 가 <see cref="OnUpdate"/> 를 직접 굴린다.
    /// 이유는 <see cref="WeaponBase"/> 주석에 적어 둔 셋과 같다.
    ///
    /// 수명은 소유 <see cref="Player"/> 로직과 같다. Player 로직은 판마다 통째로 재생성되는 것이
    /// 정상 경로이므로(<see cref="Equip"/> 주석) 이 로드아웃도 함께 새로 만들어진다.
    ///
    /// 원본 좀비토이의 <c>PlayerAttack</c> 자리에 해당한다 — 마우스 조준 + 수동 발사이고,
    /// 활성 무기는 항상 하나다.
    /// </summary>
    public class WeaponLoadout
    {
        /// <summary>이 로드아웃의 소유자. 무기 초기화에 그대로 넘긴다.</summary>
        private readonly Player mOwner = null;

        /// <summary>장착된 무기. 이 중 <see cref="mActiveWeaponIndex"/> 하나만 활성이다.</summary>
        private readonly List<WeaponBase> mWeapons = new List<WeaponBase>();

        private int mActiveWeaponIndex = 0;

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

        public WeaponLoadout(Player owner)
        {
            mOwner = owner;
        }

        /// <summary>지금 들고 있는 무기. 아직 장착 전이면 null.</summary>
        private WeaponBase ActiveWeapon
        {
            get
            {
                if (mWeapons.Count == 0) return null;

                GameAssert.InRange(mActiveWeaponIndex, mWeapons.Count, "mActiveWeaponIndex");
                return mWeapons[mActiveWeaponIndex];
            }
        }

        /// <summary>
        /// 이 캐릭터가 들고 시작하는 무기. 레벨업 시스템이 붙으면 여기에 추가하는 형태가 된다.
        ///
        /// 무기는 MonoBehaviour 가 아니라 이 로드아웃이 소유하는 순수 객체다. 로드아웃
        /// 인스턴스가 살아남는 재사용 경로에서는 무기도 그대로 재활용한다.
        ///
        /// 단, 그 경로에 기대면 안 된다 — <c>Entity.cs:86-96</c> 은 풀에서 꺼낸 인스턴스의
        /// 로직 타입이 다르면 컴포넌트를 파괴하고 새로 붙인다. 선택화면은
        /// <see cref="PlayerSelectLogic"/>, 플레이는 <see cref="Player"/> 라 <b>판마다
        /// 이 로직이 통째로 재생성되는 것이 정상 경로</b>다(로드아웃도 함께 새로 만들어진다).
        /// 무기가 캐릭터에 남긴 것은 <see cref="Dispose"/> 가 치운다.
        /// </summary>
        public void Equip()
        {
            if (mWeapons.Count == 0)
            {
                mWeapons.Add(new LightningWeapon());
                mWeapons.Add(new FrostWeapon());
                mWeapons.Add(new StinkWeapon());
                mWeapons.Add(new SlimeWeapon());
            }

            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].Initialize(mOwner);
            }

            // 재스폰 때마다 첫 무기부터, 쿨다운 없이 시작한다.
            mActiveWeaponIndex = 0;
            ApplySelection();
            ClearCooldown();

            if (mWeapons.Count > 0)
            {
                Log.Info("무기 {0}종 장착. 마우스 좌클릭 발사 / Tab 으로 전환. 현재 → {1}",
                    mWeapons.Count, mWeapons[mActiveWeaponIndex].GetType().Name);
            }
        }

        /// <summary>
        /// 활성 무기만 켠다. 꺼진 무기는 조준 갱신도 발사 입력도 받지 못하고
        /// 총구 VFX·레티클도 함께 꺼진다.
        /// </summary>
        private void ApplySelection()
        {
            if (mWeapons.Count > 0)
            {
                GameAssert.InRange(mActiveWeaponIndex, mWeapons.Count, "mActiveWeaponIndex");
            }

            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].SetActive(i == mActiveWeaponIndex);
            }
        }

        /// <summary>
        /// 활성 무기의 조준을 갱신하고 발사 입력을 읽는다.
        /// <see cref="Player.OnUpdate"/> 가 조준점 계산 뒤에 부르므로 조준이 한 프레임 밀리지 않는다.
        /// </summary>
        public void OnUpdate()
        {
            WeaponBase weapon = ActiveWeapon;
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
        private void ReadFireInput(WeaponBase weapon, bool isReady)
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
        /// 무기를 전부 끈다. 총구 VFX·레티클·Frost 콘이 여기서 정리된다.
        /// 사망과 회수 양쪽에서 부른다 — 죽으면 OnUpdate 가 멈춰 무기 스스로는 못 끈다.
        /// </summary>
        public void Shutdown()
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].SetActive(false);
            }
        }

        /// <summary>
        /// 무기가 캐릭터에 남긴 것을 전부 지운다(복제한 레티클, 꺼 놓은 VFX 루트).
        ///
        /// 이 캐릭터 GameObject 는 풀에 돌아가 <see cref="PlayerSelectLogic"/> 으로도 재사용된다.
        /// 정리를 안 하면 재시작마다 레티클이 쌓이고, 선택화면 캐릭터의 안테나 이펙트가 꺼진다.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].Dispose();
            }
        }

        /// <summary>Tab 무기 순환. 원본 <c>PlayerInputPC</c> 의 SwitchAttack 자리다.</summary>
        public void ReadSwitchInput()
        {
            if (mWeapons.Count <= 1) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;

            // 전환 전에 이전 무기의 지속 발사를 반드시 끊는다. 버튼을 누른 채 Tab 을 치면
            // 그 무기는 "뗀 프레임" 을 영영 못 받는다 — Frost 콘이 켜진 채로 남는다.
            // 뒤따르는 SetActive(false) 가 연출 GameObject 를 끄더라도, 무기가 들고 있는
            // "발사 중" 상태까지 지우는 것은 StopFiring 뿐이므로 명시적으로 부른다.
            WeaponBase previous = ActiveWeapon;
            if (previous != null)
            {
                previous.StopFiring();
            }

            mActiveWeaponIndex = (mActiveWeaponIndex + 1) % mWeapons.Count;
            ApplySelection();

            Log.Info("무기 전환 → {0} ({1}/{2})",
                mWeapons[mActiveWeaponIndex].GetType().Name,
                mActiveWeaponIndex + 1, mWeapons.Count);
        }
    }
}
