using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 베이스.
    ///
    /// <b>MonoBehaviour 가 아니다.</b> 소유 <see cref="Player"/> 가 인스턴스를 만들어 들고,
    /// <see cref="WeaponLoadout"/> 이 <see cref="Player.OnUpdate"/> 안에서 직접 굴린다.
    /// MonoBehaviour 를 쓰지 않는 이유는 셋이다.
    /// <list type="number">
    /// <item>Unity 메시지는 virtual 이 아니라서, 파생 무기가 <c>OnEnable</c>/<c>OnDisable</c> 을
    ///       선언하면 베이스 쪽이 조용히 안 불린다(실제로 총구 VFX 버그를 냈다).</item>
    /// <item><c>MonoBehaviour.Update</c> 와 <c>EntityLogic.OnUpdate</c> 의 선후가 미보장이라
    ///       "플레이어가 이동·회전한 뒤에 조준" 이 성립하지 않는다.</item>
    /// <item>엔티티가 풀에서 재사용되면 컴포넌트가 남아 <c>AddComponent</c> 중복 방지 코드가 필요하다.</item>
    /// </list>
    ///
    /// <b>수동 발사 모델이다</b>(원본 좀비토이와 같다). 조준은 플레이어가 마우스로 하고
    /// (<c>Player.AimPoint</c>), 발사 시점은 <see cref="WeaponLoadout"/> 이 입력에서 뽑아
    /// <see cref="TryFireHeld"/>/<see cref="TryFireReleased"/>/<see cref="StopFiring"/> 로 내려 준다.
    ///
    /// <b>쿨다운은 무기가 아니라 로드아웃이 전역 하나로 관리한다</b> — 원본 <c>PlayerAttack</c> 이
    /// <c>attackCooldown</c>/<c>timeOfLastAttack</c> 한 쌍만 들고 "마지막으로 쏜 무기의 쿨다운"을
    /// 거기 넣는 구조와 같다. 무기는 자기 쿨다운 <b>값</b>(<see cref="Cooldown"/>)만 알려 줄 뿐
    /// 타이머를 돌리지 않는다.
    /// </summary>
    public abstract class WeaponBase
    {
        /// <summary>Shootable(9). 적 탐색용.</summary>
        protected const int ShootableMask = WeaponUtil.ShootableMask;

        /// <summary>Shootable(9) | Blocking(10) | Environment(14) = 17920. 원본 Lightning 마스크.</summary>
        protected const int HitscanMask = WeaponUtil.HitscanMask;

        /// <summary>파생 무기가 재사용하는 스크래치 버퍼. 프레임 할당을 없앤다.</summary>
        protected readonly List<Enemy> Candidates = new List<Enemy>(32);

        protected Player Owner { get; private set; }

        /// <summary>캐릭터 루트 트랜스폼. 무기 VFX 와 레티클이 전부 이 아래에 있다.</summary>
        protected Transform Root { get; private set; }

        /// <summary>지금 이 무기가 손에 들려 있는지. <see cref="SetActive"/> 로만 바뀐다.</summary>
        public bool Active { get; private set; }

        private bool mActiveApplied = false;

        /// <summary>
        /// 이 무기가 발사에 성공했을 때 로드아웃이 걸 전역 쿨다운(초). 쿨다운이 없으면 0.
        ///
        /// 여기 값이 곧 "다음 발사까지 <b>모든</b> 무기가 잠기는 시간" 이다 — 무기별 타이머가 아니다.
        /// 원본과 같은 구조이므로 값도 원본 그대로 <see cref="WeaponTable"/> 에서 가져다 쓴다.
        /// </summary>
        public abstract float Cooldown { get; }

        // ─── 생명주기 ───

        /// <summary>
        /// 스폰(재스폰)마다 호출된다. 소유자 연결과 파생 상태 리셋을 겸한다.
        /// 무기 인스턴스 자체는 풀 재사용 사이에 살아남으므로 여기서 상태를 되돌린다.
        /// </summary>
        public void Initialize(Player owner)
        {
            // 계약: 소유자 없이는 초기화할 수 없다. Root 가 null 이면 파생 무기의
            // Root.Find(...) 가 그 자리에서 NRE 를 낸다 — 여기서 원인을 남겨 둔다.
            GameAssert.IsTrue(owner != null, "WeaponBase.Initialize: owner 가 null 이다.");

            Owner = owner;
            Root  = owner != null ? owner.CachedTransform : null;

            // 소유자가 없으면 OnInitialize 를 아예 건너뛴다. 이 한 줄이 실제 방어다 —
            // 위 GameAssert 는 [Conditional] 이라 릴리스에서 통째로 사라진다.
            //
            // 예전에는 owner 가 null 이어도 OnInitialize 를 불렀기 때문에 파생 무기 4종이
            // 전부 첫 줄에 똑같은 Root null 가드를 복붙해 두고 있었다. 그 중복의 근원이 여기다.
            // 이 가드를 되돌리면 그 복붙도 함께 되살려야 한다.
            if (owner == null) return;

            OnInitialize();
        }

        /// <summary>파생 무기가 자기 상태를 리셋할 훅.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 무기를 켜고 끈다. 끄면 총구 VFX 와 레티클이 함께 꺼진다 —
        /// 이걸 빠뜨리면 Tab 으로 바꿔도 이전 무기의 연출이 그대로 남는다.
        /// </summary>
        public void SetActive(bool active)
        {
            if (mActiveApplied && Active == active) return;

            mActiveApplied = true;
            Active         = active;

            if (active)
            {
                SetVfxRootActive(true);
                OnWeaponEnabled();
            }
            else
            {
                // 정리를 VFX 루트보다 **먼저** 한다. 루트를 먼저 끄면 그 아래 컴포넌트가
                // 비활성 상태가 되어, 스스로 멈춰야 하는 연출(LightningBolt 등)이
                // 정리 기회를 잃고 다음에 켤 때 그대로 되살아난다.
                OnWeaponDisabled();

                // 지속 연출(Frost 콘 등)도 여기서 확실히 끊는다. 버튼을 누른 채로 Tab 을 치면
                // 이 무기의 StopFiring 이 영영 안 불리기 때문이다.
                OnStopFiring();

                HideReticle();
                SetVfxRootActive(false);
            }
        }

        protected virtual void OnWeaponEnabled() { }
        protected virtual void OnWeaponDisabled() { }

        /// <summary>
        /// 캐릭터에서 이 무기의 흔적을 지운다. <see cref="Player.OnHide"/> 가 부른다.
        ///
        /// 무기 객체는 생각보다 자주 새로 만들어진다 — <c>Entity.cs:86-96</c> 이 "풀에서 꺼낸
        /// 인스턴스의 로직 타입이 다르면 Destroy 후 새로 AddComponent" 하므로,
        /// 선택화면(<c>PlayerSelectLogic</c>) ↔ 플레이(<c>Player</c>) 를 오갈 때마다
        /// Player 로직이 통째로 재생성되고 무기도 함께 재생성된다.
        ///
        /// 레티클과 총구 VFX 루트를 프리팹 기본 상태로 되돌린다. 안 그러면 이 캐릭터가 다음 판
        /// 선택화면에 재사용될 때 안테나 이펙트가 꺼진 채로 등장하거나, 죽은 레티클이
        /// 마지막 조준 지점에 그대로 떠 있는다.
        ///
        /// (예전에는 여기서 복제해 둔 타겟 마커 링도 파괴했다. 자동조준용 링 풀은 사라졌고
        ///  레티클은 프리팹의 기존 자식을 그대로 쓰므로 파괴할 것이 없다.)
        /// </summary>
        public void Dispose()
        {
            // 파생 캐시를 먼저 버린다. 아래 정리는 베이스가 들고 있는 참조만 쓰므로 순서에 영향이 없고,
            // 파생 쪽은 Root 가 아직 살아 있는 편이 다루기 쉽다.
            OnDispose();

            RestoreReticleToPrefabState();
            RestoreVfxRootToPrefabState();
            ClearMuzzleCache();

            // 다음 Initialize 뒤의 첫 SetActive 가 무조건 적용되게 한다.
            mActiveApplied = false;
            Active         = false;

            Owner = null;
            Root  = null;
        }

        /// <summary>
        /// 파생 무기가 캐릭터 밑에서 찾아 둔 캐시를 버릴 훅. <see cref="Dispose"/> 가 부른다.
        ///
        /// 파생은 대개 <c>if (mX == null)</c> 로만 다시 찾는다. 여기서 안 버리면 <b>다음
        /// Initialize 가 다른 캐릭터를 넘겨받아도 옛 트랜스폼을 그대로 물고 있는다</b> —
        /// Girl/Boy 를 바꿔 고르거나 선택화면 ↔ 플레이를 오갈 때 실제로 일어나는 경로다.
        /// </summary>
        protected virtual void OnDispose() { }

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
        /// 켜 둔 연출(Frost 콘·루프 사운드)은 반드시 꺼야 한다. 파생 쪽 캐시는 이미
        /// <see cref="OnDispose"/> 에서 null 로 비워지므로 여기서 NRE 가 나지 않는다.
        /// </summary>
        public void StopFiring()
        {
            OnStopFiring();
        }

        /// <summary>파생 무기가 지속 연출을 끊을 훅.</summary>
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
                // 소유자가 사라지면 스스로 꺼진다. 재스폰 시 Initialize → SetActive 로 되살아난다.
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
        /// 발사점으로 쓸 자식 트랜스폼의 경로(캐릭터 기준). null 이면 총구를 쓰지 않는다.
        ///
        /// 기본값 "Antenna" 는 Girl/Boy 프리팹에서 A6 때 보존해 둔 발사점이다
        /// (로컬 <c>(0.123, 0.948, 1.019)</c>). 무기 4종이 전부 여기서 쏘므로 베이스 기본값으로 둔다.
        /// </summary>
        protected virtual string MuzzlePath => "Antenna";

        private Transform mMuzzle         = null;
        private bool      mMuzzleResolved = false;

        /// <summary>
        /// 발사 원점. 총구를 못 찾았으면 캐릭터 원점에서 한 칸 띄운다 —
        /// 발밑에서 쏘면 레이도 투사체도 바로 바닥에 막힌다.
        /// </summary>
        protected Vector3 MuzzleOrigin
        {
            get
            {
                ResolveMuzzle();

                if (mMuzzle != null) return mMuzzle.position;

                // Owner 는 호출 경로(TryFire* → CanOperate 통과)상 항상 살아 있다.
                // 그래도 좌표 계산이 NRE 로 터지지는 않게 원점을 돌려준다.
                return Owner != null ? Owner.CachedTransform.position + Vector3.up : Vector3.zero;
            }
        }

        /// <summary>
        /// 총구를 딱 한 번 찾아 캐시한다. 못 찾아도 <see cref="mMuzzleResolved"/> 는 세워 둔다 —
        /// 매 발사마다 Find 를 반복하고 경고를 도배하지 않기 위해서다.
        /// </summary>
        private void ResolveMuzzle()
        {
            if (mMuzzleResolved) return;

            mMuzzleResolved = true;

            string path = MuzzlePath;
            if (string.IsNullOrEmpty(path) || Root == null)
            {
                // 총구를 쓰지 않는 무기이거나 아직 소유자가 없다. 정상 경로라 로그를 남기지 않는다.
                return;
            }

            mMuzzle = Root.Find(path);
            if (mMuzzle == null)
            {
                Log.Warning("{0}: 총구 '{1}' 을 찾지 못했다. 캐릭터 원점에서 발사한다.", GetType().Name, path);
            }
        }

        /// <summary>총구 캐시를 버린다. 다음 <see cref="Initialize"/> 가 새 캐릭터에서 다시 찾는다.</summary>
        private void ClearMuzzleCache()
        {
            mMuzzle         = null;
            mMuzzleResolved = false;
        }

        // ─── 총구 VFX ───

        /// <summary>
        /// 이 무기가 소유한 총구 VFX 루트의 경로(캐릭터 기준). null 이면 없는 것으로 본다.
        /// 무기를 바꾸면 이 GameObject 가 함께 꺼진다 — 안 그러면 장착했던 무기의
        /// 총구 파티클이 전부 겹쳐서 남는다.
        /// </summary>
        protected virtual string VfxRootPath => null;

        private Transform mVfxRoot              = null;
        private bool      mVfxResolved          = false;
        private bool      mVfxRootDefaultActive = true;

        private void SetVfxRootActive(bool active)
        {
            ResolveVfxRoot();

            if (mVfxRoot != null && mVfxRoot.gameObject.activeSelf != active)
            {
                mVfxRoot.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// VFX 루트를 딱 한 번 찾아 캐시한다. 못 찾아도 <see cref="mVfxResolved"/> 는 세워 둔다 —
        /// 매 프레임 Find 를 반복하고 경고를 도배하지 않기 위해서다.
        /// </summary>
        private void ResolveVfxRoot()
        {
            if (mVfxResolved) return;

            mVfxResolved = true;

            string path = VfxRootPath;
            if (string.IsNullOrEmpty(path) || Root == null)
            {
                // 총구 VFX 를 쓰지 않는 무기다. 정상 경로이므로 로그를 남기지 않는다.
                return;
            }

            mVfxRoot = Root.Find(path);
            if (mVfxRoot == null)
            {
                Log.Warning("{0}: VFX 루트 '{1}' 을 찾지 못했다.", GetType().Name, path);
                return;
            }

            // 아직 아무것도 바꾸기 전이라 프리팹 기본값이다. Dispose 에서 되돌린다.
            mVfxRootDefaultActive = mVfxRoot.gameObject.activeSelf;
        }

        /// <summary>
        /// 총구 VFX 루트를 프리팹 기본 활성 상태로 되돌리고 캐시를 버린다.
        /// 안 되돌리면 이 캐릭터가 다음 판 선택화면에 재사용될 때 이펙트가 꺼진 채로 등장한다.
        /// </summary>
        private void RestoreVfxRootToPrefabState()
        {
            if (mVfxRoot != null)
            {
                mVfxRoot.gameObject.SetActive(mVfxRootDefaultActive);
            }

            mVfxRoot     = null;
            mVfxResolved = false;
        }

        // ─── 레티클 ───

        /// <summary>
        /// 조준 지점에 깔 레티클의 경로(캐릭터 기준). null 이면 레티클이 없는 무기다.
        ///
        /// 원본에도 레티클이 있는 무기는 Stink 와 Slime 뿐이다. Lightning·Frost 는 없다.
        ///
        /// <b>복제하지 않는다.</b> 프리팹에 이미 있는 자식을 그대로 켜고 끄고 옮긴다.
        /// 레티클은 무기 VFX 루트 아래에 있어 무기를 바꾸면 같이 꺼지므로, 자동조준 시절의
        /// 링 풀처럼 "캐릭터 루트 밑에 복제본을 만들어 둘" 이유가 없다.
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

        private Transform mReticle              = null;
        private Renderer  mReticleRenderer      = null;
        private bool      mReticleResolved      = false;
        private bool      mReticleDefaultActive = false;

        /// <summary>
        /// 레티클을 조준 지점에 놓고 상태에 맞는 색을 입힌다.
        /// 레티클이 없는 무기가 불러도 조용히 무시된다.
        /// </summary>
        protected void ShowReticle(Vector3 worldPosition, TargetState state)
        {
            ResolveReticle();
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
            ResolveReticle();

            if (mReticle == null || !mReticle.gameObject.activeSelf) return;

            mReticle.gameObject.SetActive(false);
        }

        /// <summary>
        /// 레티클을 딱 한 번 찾아 캐시한다. 못 찾아도 <see cref="mReticleResolved"/> 는 세워 둔다 —
        /// 매 프레임 Find 를 반복하고 경고를 도배하지 않기 위해서다.
        /// </summary>
        private void ResolveReticle()
        {
            if (mReticleResolved) return;

            mReticleResolved = true;

            string path = ReticlePath;
            if (string.IsNullOrEmpty(path) || Root == null)
            {
                // 레티클을 쓰지 않는 무기다. 정상 경로이므로 로그를 남기지 않는다.
                return;
            }

            mReticle = Root.Find(path);
            if (mReticle == null)
            {
                Log.Warning("{0}: 레티클 '{1}' 을 찾지 못했다.", GetType().Name, path);
                return;
            }

            // 아직 아무것도 바꾸기 전이라 프리팹 기본값이다. Dispose 에서 되돌린다.
            mReticleDefaultActive = mReticle.gameObject.activeSelf;

            // 렌더러도 지금 잡아 둔다. 색 적용은 매 프레임이라 GetComponent 를 반복할 수 없다.
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

        /// <summary>
        /// 레티클을 프리팹 기본 활성 상태로 되돌리고 캐시를 버린다.
        /// 안 되돌리면 이 캐릭터가 다음 판에 재사용될 때 죽은 레티클이 마지막 조준 지점에 떠 있는다.
        /// </summary>
        private void RestoreReticleToPrefabState()
        {
            if (mReticle != null)
            {
                mReticle.gameObject.SetActive(mReticleDefaultActive);
            }

            mReticle              = null;
            mReticleRenderer      = null;
            mReticleResolved      = false;
            mReticleDefaultActive = false;
        }
    }
}
