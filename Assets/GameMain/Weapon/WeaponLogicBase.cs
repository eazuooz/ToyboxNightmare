using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 베이스. <b>무기도 엔티티다</b> — <see cref="WeaponLoadout"/> 이 <c>ShowEntity</c> 로 띄우고,
    /// 뜨자마자 <see cref="WeaponData.OwnerId"/> 의 플레이어에게 <c>AttachEntity</c> 로 붙는다.
    ///
    /// 엔티티로 두어서 얻는 것:
    /// <list type="bullet">
    /// <item>프리팹 로딩(Addressables)과 풀링을 프레임워크가 맡는다. 무기 외형이 캐릭터 프리팹에
    ///       박혀 있지 않으므로 캐릭터와 무기를 따로 늘릴 수 있다.</item>
    /// <item>수명이 소유자에 묶인다. 플레이어가 회수되면 코어가 붙어 있는 무기를 먼저 회수한다 —
    ///       무기가 캐릭터에 남긴 것을 손으로 되돌리는 코드가 필요 없다.</item>
    /// <item>훅이 전부 virtual 이다(<c>OnShow</c>/<c>OnHide</c>). Unity 메시지처럼 파생이 선언하면
    ///       베이스 쪽이 조용히 안 불리는 일이 없다.</item>
    /// </list>
    ///
    /// <b>무기는 스스로 발사하지 않는다.</b> 발사 시점은 <see cref="WeaponLoadout"/> 이 입력에서 뽑아
    /// <see cref="TryFireHeld"/>/<see cref="TryFireReleased"/>/<see cref="StopFiring"/> 로 내려 주고,
    /// 그 호출은 <see cref="PlayerLogic"/> 의 갱신 안에서 일어난다. 그래서 "플레이어가 조준점을 계산한
    /// 뒤에 무기가 쏜다" 는 순서가 엔티티 갱신 순서와 무관하게 보장된다.
    ///
    /// <b>수동 발사 모델이다</b>(원본 좀비토이와 같다). 조준은 플레이어가 마우스로 한다
    /// (<c>PlayerLogic.AimPoint</c>).
    ///
    /// <b>쿨다운은 무기가 아니라 로드아웃이 전역 하나로 관리한다</b> — 원본 <c>PlayerAttack</c> 이
    /// <c>attackCooldown</c>/<c>timeOfLastAttack</c> 한 쌍만 들고 "마지막으로 쏜 무기의 쿨다운"을
    /// 거기 넣는 구조와 같다. 무기는 자기 쿨다운 <b>값</b>(<see cref="Cooldown"/>)만 알려 줄 뿐
    /// 타이머를 돌리지 않는다.
    /// </summary>
    public abstract class WeaponLogicBase : EntityLogicBase
    {
        /// <summary>
        /// 소유자 프리팹에서 무기가 붙는 자리(소유자 루트 기준 경로).
        /// Girl/Boy 프리팹의 발사점이다(로컬 <c>(0.123, 0.948, 1.019)</c>). 무기 4종이 전부 여기서 쏜다.
        /// </summary>
        public const string AttachPath = "Antenna";

        /// <summary>Shootable(9). 적 탐색용.</summary>
        protected const int ShootableMask = WeaponUtil.ShootableMask;

        /// <summary>Shootable(9) | Blocking(10) | Environment(14) = 17920. 원본 Lightning 마스크.</summary>
        protected const int HitscanMask = WeaponUtil.HitscanMask;

        /// <summary>파생 무기가 재사용하는 스크래치 버퍼. 프레임 할당을 없앤다.</summary>
        protected readonly List<EnemyLogic> Candidates = new List<EnemyLogic>(32);

        private WeaponData mWeaponData = null;

        /// <summary>이 무기를 든 플레이어. 붙기 전과 떨어진 뒤에는 null 이다.</summary>
        protected PlayerLogic Owner { get; private set; }

        /// <summary>지금 이 무기가 손에 들려 있는지. <see cref="SetActive"/> 로만 바뀐다.</summary>
        public bool Active { get; private set; }

        private bool mActiveApplied = false;

        /// <summary>로드아웃에서의 자리. <see cref="WeaponData.Slot"/> 그대로다.</summary>
        public int Slot
        {
            get { return mWeaponData != null ? mWeaponData.Slot : 0; }
        }

        /// <summary>
        /// 이 무기가 발사에 성공했을 때 로드아웃이 걸 전역 쿨다운(초). 쿨다운이 없으면 0.
        ///
        /// 여기 값이 곧 "다음 발사까지 <b>모든</b> 무기가 잠기는 시간" 이다 — 무기별 타이머가 아니다.
        /// 값은 스폰 데이터(<see cref="WeaponData.Cooldown"/>)에서 온다.
        /// </summary>
        public float Cooldown
        {
            get { return mWeaponData != null ? mWeaponData.Cooldown : 0f; }
        }

        /// <summary>
        /// 파생 무기가 자기 데이터 타입으로 읽는다(<see cref="LightningWeaponData"/> 등). 능력치는 전부 여기 있다.
        /// 회수된 뒤이거나 다른 타입의 데이터가 왔으면 null 이다.
        /// </summary>
        protected T DataAs<T>() where T : WeaponData
        {
            return mWeaponData as T;
        }

        // ─── 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            // 인스턴스가 처음 만들어질 때 한 번. 자식 트랜스폼은 풀 재사용 사이에 바뀌지 않으므로
            // 여기서 찾아 두면 스폰마다 다시 찾지 않아도 된다.
            ResolveReticle();
            OnWeaponInit();
        }

        /// <summary>파생 무기가 자기 프리팹 안의 자식을 찾아 둘 훅. 인스턴스당 한 번 불린다.</summary>
        protected virtual void OnWeaponInit() { }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 풀에서 꺼낸 인스턴스는 이전 판의 상태를 물고 있다. 데이터 검사보다 먼저 되돌린다.
            Owner          = null;
            Active         = false;
            mActiveApplied = false;

            // 손에 들리기 전에는 보이지 않는다. 로드아웃이 SetActive 로 켠다.
            // (base.OnShow 가 방금 켰으므로 여기서 다시 꺼야 한다.)
            Visible = false;
            HideReticle();
            OnWeaponShow();

            mWeaponData = userData as WeaponData;
            if (mWeaponData == null)
            {
                Log.Error("{0}: Weapon data is invalid.", GetType().Name);
                return;
            }

            AttachToOwner();
        }

        /// <summary>파생 무기가 자기 상태를 리셋할 훅. 스폰(재사용)마다 불린다.</summary>
        protected virtual void OnWeaponShow() { }

        /// <summary>
        /// 소유자에게 붙는다. 무기는 비동기로 로드되므로, 뜨는 사이에 소유자가 이미 회수됐을 수 있다.
        /// 그때는 붙지 않고 그대로 둔다 — 다음 <see cref="OnUpdate"/> 가 주인 없는 무기를 회수한다.
        /// </summary>
        private void AttachToOwner()
        {
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null) return;

            // 없는 부모에 붙이면 코어가 예외를 던진다. 먼저 확인한다.
            if (!entityComponent.HasEntity(mWeaponData.OwnerId)) return;

            entityComponent.AttachEntity(Entity, mWeaponData.OwnerId, AttachPath);
        }

        protected internal override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);

            // base 는 부모만 바꾼다. 풀 루트에 있던 월드 좌표가 로컬로 남지 않게 붙는 자리에 맞춘다.
            CachedTransform.localPosition = Vector3.zero;
            CachedTransform.localRotation = Quaternion.identity;
            CachedTransform.localScale    = Vector3.one;

            Owner = parentEntity as PlayerLogic;
            if (Owner == null)
            {
                Log.Warning("{0}: 소유자가 PlayerLogic 이 아니다. 이 무기는 발사되지 않는다.", GetType().Name);
            }
        }

        protected internal override void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            Owner = null;

            base.OnDetachFrom(parentEntity, userData);
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            // 주인 없는 무기는 쓸 수 없다. 소유자가 먼저 사라진 뒤에 로드가 끝난 경우다.
            if (Owner == null)
            {
                SafeHide();
            }
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            // 켜 둔 연출을 끊고 프리팹 기본 상태로 되돌린다. 이 인스턴스는 풀에 돌아가
            // 다음 판에 그대로 재사용되므로, 여기서 안 끄면 Frost 콘이 켜진 채로 다시 나온다.
            OnWeaponDisabled();
            OnStopFiring();
            HideReticle();

            Active         = false;
            mActiveApplied = false;
            Owner          = null;

            // 데이터는 base 체인 끝(EntityLogicBase)에서 ReferencePool 로 반납된다.
            // 반납된 것을 계속 들고 있으면 다음 주인의 값을 읽게 되므로 참조를 놓는다.
            mWeaponData = null;

            base.OnHide(isShutdown, userData);
        }

        /// <summary>
        /// 무기를 손에 들거나 내려놓는다. 내려놓으면 무기 엔티티가 통째로 보이지 않게 되어
        /// 총구 VFX 와 레티클이 함께 꺼진다 — 이걸 빠뜨리면 Tab 으로 바꿔도 이전 무기의 연출이 남는다.
        ///
        /// 보이지 않을 뿐 엔티티는 살아 있다. 회수는 소유자가 회수될 때 프레임워크가 한다.
        /// </summary>
        public void SetActive(bool active)
        {
            if (mActiveApplied && Active == active) return;

            mActiveApplied = true;
            Active         = active;

            if (active)
            {
                Visible = true;
                OnWeaponEnabled();
            }
            else
            {
                // 정리를 숨기기보다 **먼저** 한다. 먼저 숨기면 그 아래 컴포넌트가
                // 비활성 상태가 되어, 스스로 멈춰야 하는 연출(LightningBolt 등)이
                // 정리 기회를 잃고 다음에 켤 때 그대로 되살아난다.
                OnWeaponDisabled();

                // 지속 연출(Frost 콘 등)도 여기서 확실히 끊는다. 버튼을 누른 채로 Tab 을 치면
                // 이 무기의 StopFiring 이 영영 안 불리기 때문이다.
                OnStopFiring();

                HideReticle();
                Visible = false;
            }
        }

        protected virtual void OnWeaponEnabled() { }
        protected virtual void OnWeaponDisabled() { }

        /// <summary>
        /// 무기를 굴릴 수 있는 상태인가. 소유자가 회수됐거나 죽었으면 거짓이다.
        /// (Owner 는 MonoBehaviour 파생이라 == null 이 "파괴됨" 까지 잡아 준다.)
        /// </summary>
        private bool IsOwnerAlive => Owner != null && Owner.Available && !Owner.IsDead;

        /// <summary>손에 들려 있고 소유자도 멀쩡한가. 발사 훅의 공통 전제다.</summary>
        private bool CanOperate => Active && IsOwnerAlive;

        // ─── 발사 ───

        /// <summary>
        /// 발사 버튼을 누르고 있는 동안 매 프레임 불린다.
        /// 쿨다운을 소모하는 발사를 했으면 true — 로드아웃이 그때만 전역 쿨다운을 건다.
        ///
        /// 원본에서 이 타이밍에 쏘는 것은 Lightning(즉발)과 Frost(콘 ON)뿐이다.
        /// </summary>
        public bool TryFireHeld()
        {
            if (!CanOperate) return false;

            return OnFireHeld();
        }

        /// <summary>기본은 "이 타이밍에 쏘지 않는다".</summary>
        protected virtual bool OnFireHeld()
        {
            return false;
        }

        /// <summary>
        /// 발사 버튼을 뗀 프레임에 불린다.
        /// 쿨다운을 소모하는 발사를 했으면 true — 로드아웃이 그때만 전역 쿨다운을 건다.
        ///
        /// 원본에서 이 타이밍에 쏘는 것은 Stink 와 Slime 이다.
        /// Slime 은 대상이 없으면 false 를 돌려 쿨다운도 걸지 않는다(원본과 같다).
        /// </summary>
        public bool TryFireReleased()
        {
            if (!CanOperate) return false;

            return OnFireReleased();
        }

        /// <summary>기본은 "이 타이밍에 쏘지 않는다".</summary>
        protected virtual bool OnFireReleased()
        {
            return false;
        }

        /// <summary>
        /// 발사 버튼을 뗐을 때 <b>쿨다운과 무관하게</b> 항상 불린다. 지속 연출을 끊는 용도다.
        ///
        /// 일부러 <see cref="CanOperate"/> 로 막지 않는다. 소유자가 그 프레임에 죽었더라도
        /// 켜 둔 연출(Frost 콘·루프 사운드)은 반드시 꺼야 한다.
        /// </summary>
        public void StopFiring()
        {
            OnStopFiring();
        }

        /// <summary>파생 무기가 지속 연출을 끊을 훅. 여러 번 불려도 안전해야 한다.</summary>
        protected virtual void OnStopFiring() { }

        // ─── 조준 ───

        /// <summary>
        /// 활성 무기에 대해 매 프레임 불린다. 레티클 위치와 색을 갱신한다.
        /// isReady 는 <b>전역</b> 쿨다운이 끝났는지다(이 무기만의 상태가 아니다).
        ///
        /// 플레이어의 입력 처리가 끝난 뒤에 호출되므로 조준이 한 프레임 밀리지 않는다.
        /// </summary>
        public void UpdateAim(bool isReady)
        {
            if (!Active) return;

            if (!IsOwnerAlive)
            {
                // 소유자가 죽으면 스스로 내려놓는다.
                SetActive(false);
                return;
            }

            OnUpdateAim(isReady);
        }

        /// <summary>
        /// 파생 무기가 레티클을 갱신할 훅. 기본은 아무것도 하지 않는다
        /// (Lightning·Frost 는 원본에도 레티클이 없다).
        /// </summary>
        protected virtual void OnUpdateAim(bool isReady) { }

        // ─── 총구(발사 원점) ───

        /// <summary>
        /// 발사 원점. 무기 엔티티가 소유자의 발사점(<see cref="AttachPath"/>)에 붙어 있으므로
        /// 무기 자신의 위치가 곧 총구다.
        /// </summary>
        protected Vector3 MuzzleOrigin
        {
            get { return CachedTransform.position; }
        }

        // ─── 레티클 ───

        /// <summary>
        /// 조준 지점에 깔 레티클의 경로(무기 프리팹 루트 기준). null 이면 레티클이 없는 무기다.
        ///
        /// 원본에도 레티클이 있는 무기는 Stink 와 Slime 뿐이다. Lightning·Frost 는 없다.
        /// 레티클은 무기 프리팹의 자식이라 무기를 내려놓으면 같이 꺼진다.
        /// </summary>
        protected virtual string ReticlePath => null;

        /// <summary>지면과 겹쳐 z-fighting 나지 않게 살짝 띄운다. 원본은 조준점에 정확히 붙였다.</summary>
        private const float ReticleGroundOffset = 0.06f;

        /// <summary>
        /// 부모(캐릭터·안테나)가 회전하므로 월드 회전을 고정해 항상 지면에 눕힌다.
        /// 이 값을 지역 변수로 매 프레임 만들지 않기 위해 static 으로 뽑아 둔다.
        /// </summary>
        private static readonly Quaternion ReticleGroundRotation = Quaternion.Euler(-90f, 0f, 0f);

        /// <summary>레거시 파티클 셰이더의 색 프로퍼티. 원본 레티클 머티리얼이 이걸 쓴다.</summary>
        private const string LegacyTintColorProperty = "_TintColor";

        /// <summary>URP Lit/Unlit 의 색 프로퍼티. 레티클 머티리얼을 URP 로 갈았을 때 대비다.</summary>
        private const string UrpBaseColorProperty = "_BaseColor";

        /// <summary>
        /// 색 적용용 공유 블록. 매 프레임 도는 경로라 인스턴스마다 만들면 그대로 GC 압력이 된다.
        /// 값을 세워 곧바로 <c>SetPropertyBlock</c> 으로 넘기고 끝이라 공유해도 섞이지 않는다.
        /// </summary>
        private static MaterialPropertyBlock sReticleBlock = null;

        private Transform mReticle         = null;
        private Renderer  mReticleRenderer = null;

        /// <summary>
        /// 레티클을 조준 지점에 놓고 상태에 맞는 색을 입힌다.
        /// 레티클이 없는 무기가 불러도 조용히 무시된다.
        /// </summary>
        protected void ShowReticle(Vector3 worldPosition, TargetState state)
        {
            if (mReticle == null) return;

            if (!mReticle.gameObject.activeSelf)
            {
                mReticle.gameObject.SetActive(true);
            }

            worldPosition.y  += ReticleGroundOffset;
            mReticle.position = worldPosition;
            mReticle.rotation = ReticleGroundRotation;

            ApplyReticleColor(WeaponUtil.GetTargetColor(state));
        }

        /// <summary>레티클을 숨긴다. 없는 무기가 불러도 조용히 무시된다.</summary>
        protected void HideReticle()
        {
            if (mReticle == null || !mReticle.gameObject.activeSelf) return;

            mReticle.gameObject.SetActive(false);
        }

        /// <summary>레티클과 그 렌더러를 찾아 둔다. 색 적용은 매 프레임이라 GetComponent 를 반복할 수 없다.</summary>
        private void ResolveReticle()
        {
            string path = ReticlePath;

            // 레티클을 쓰지 않는 무기다. 정상 경로이므로 로그를 남기지 않는다.
            if (string.IsNullOrEmpty(path)) return;

            mReticle = CachedTransform.Find(path);
            if (mReticle == null)
            {
                Log.Warning("{0}: 레티클 '{1}' 을 찾지 못했다.", GetType().Name, path);
                return;
            }

            mReticleRenderer = mReticle.GetComponent<Renderer>();
            if (mReticleRenderer == null)
            {
                // 원본 레티클 중에는 메시가 한 겹 아래에 있는 것이 있다. 꺼져 있어도 찾는다.
                mReticleRenderer = mReticle.GetComponentInChildren<Renderer>(true);
            }

            if (mReticleRenderer == null)
            {
                Log.Warning("{0}: 레티클 '{1}' 에 Renderer 가 없다. 위치만 움직이고 색은 바뀌지 않는다.",
                    GetType().Name, path);
            }
        }

        /// <summary>
        /// 레티클 색을 바꾼다. <c>renderer.material</c> 을 직접 건드리면 Renderer 마다
        /// 머티리얼 사본이 생겨 샌다 — 반드시 MaterialPropertyBlock 으로 간다.
        ///
        /// 프로퍼티 두 개를 다 세우는 이유는 머티리얼이 어느 셰이더인지에 따라 먹는 쪽이
        /// 다르기 때문이다. 없는 프로퍼티에 값을 넣는 것은 무해하다.
        /// </summary>
        private void ApplyReticleColor(Color color)
        {
            if (mReticleRenderer == null) return;

            if (sReticleBlock == null)
            {
                sReticleBlock = new MaterialPropertyBlock();
            }

            mReticleRenderer.GetPropertyBlock(sReticleBlock);
            sReticleBlock.SetColor(LegacyTintColorProperty, color);
            sReticleBlock.SetColor(UrpBaseColorProperty, color);
            mReticleRenderer.SetPropertyBlock(sReticleBlock);
        }
    }
}
