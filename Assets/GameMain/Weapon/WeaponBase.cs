using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 베이스.
    ///
    /// <b>MonoBehaviour 가 아니다.</b> 소유 <see cref="Player"/> 가 인스턴스를 만들어 들고,
    /// <see cref="Player.OnUpdate"/> 가 직접 <see cref="OnUpdate"/> 를 굴린다. MonoBehaviour 를
    /// 쓰지 않는 이유는 셋이다.
    /// <list type="number">
    /// <item>Unity 메시지는 virtual 이 아니라서, 파생 무기가 <c>OnEnable</c>/<c>OnDisable</c> 을
    ///       선언하면 베이스 쪽이 조용히 안 불린다(실제로 총구 VFX 버그를 냈다).</item>
    /// <item><c>MonoBehaviour.Update</c> 와 <c>EntityLogic.OnUpdate</c> 의 선후가 미보장이라
    ///       "플레이어가 이동·회전한 뒤에 조준" 이 성립하지 않는다.</item>
    /// <item>엔티티가 풀에서 재사용되면 컴포넌트가 남아 <c>AddComponent</c> 중복 방지 코드가 필요하다.</item>
    /// </list>
    ///
    /// 뱀서라이크 모델이라 조준·발사 입력이 없다. 각 무기가 자기 쿨다운을 돌리며
    /// 자동으로 <see cref="Attack"/> 을 호출한다.
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

        /// <summary>캐릭터 루트 트랜스폼. 무기 VFX 와 마커가 전부 이 아래에 있다.</summary>
        protected Transform Root { get; private set; }

        /// <summary>지금 이 무기가 돌고 있는지. <see cref="SetActive"/> 로만 바뀐다.</summary>
        public bool Active { get; private set; }

        private bool mActiveApplied = false;

        /// <summary>
        /// 쿨다운 하한. 0 이하를 허용하면 한 프레임에 여러 발이 나가고
        /// <see cref="RetryAfter"/> 도 재시도 간격을 잃는다.
        /// </summary>
        private const float MinAttackInterval = 0.05f;

        private float mAttackInterval = 1f;
        private float mAttackTimer    = 0f;

        /// <summary>발사(또는 재판정) 주기. 파생 무기가 <see cref="OnInitialize"/> 에서 정한다.</summary>
        protected float AttackInterval
        {
            get => mAttackInterval;
            set => mAttackInterval = Mathf.Max(MinAttackInterval, value);
        }

        // ─── 생명주기 ───

        /// <summary>
        /// 스폰(재스폰)마다 호출된다. 소유자 연결과 쿨다운 리셋을 겸한다.
        /// 무기 인스턴스 자체는 풀 재사용 사이에 살아남으므로 여기서 상태를 되돌린다.
        /// </summary>
        public void Initialize(Player owner)
        {
            // 계약: 소유자 없이는 초기화할 수 없다. Root 가 null 이면 파생 무기의
            // Root.Find(...) 가 그 자리에서 NRE 를 낸다 — 여기서 원인을 남겨 둔다.
            GameAssert.IsTrue(owner != null, "WeaponBase.Initialize: owner 가 null 이다.");

            Owner = owner;
            Root  = owner != null ? owner.CachedTransform : null;

            ResetCooldown();
            OnInitialize();
        }

        /// <summary>장착·전환 직후 첫 발이 즉시 나가게 쿨다운을 채운다.</summary>
        public void ResetCooldown()
        {
            mAttackTimer = mAttackInterval;
        }

        /// <summary>파생 무기가 자기 상태를 리셋할 훅.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 무기를 켜고 끈다. 끄면 총구 VFX 와 타겟 마커가 함께 꺼진다 —
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
                SetTargetRingVisible(false);
                SetVfxRootActive(false);
            }
        }

        protected virtual void OnWeaponEnabled() { }
        protected virtual void OnWeaponDisabled() { }

        /// <summary>
        /// 캐릭터에서 이 무기의 흔적을 지운다. <see cref="Player.OnHide"/> 가 부른다.
        ///
        /// 마커 링은 캐릭터 GameObject 의 자식으로 <b>복제</b>돼 있어 무기 객체보다 오래 산다.
        /// 그리고 무기 객체는 생각보다 자주 새로 만들어진다 — <c>Entity.cs:86-96</c> 이
        /// "풀에서 꺼낸 인스턴스의 로직 타입이 다르면 Destroy 후 새로 AddComponent" 하므로,
        /// 선택화면(<c>PlayerSelectLogic</c>) ↔ 플레이(<c>Player</c>) 를 오갈 때마다
        /// Player 로직이 통째로 재생성되고 무기도 함께 재생성된다.
        /// 링을 지우지 않으면 <b>재시작마다 캐릭터 밑에 죽은 링이 쌓인다</b>.
        ///
        /// 총구 VFX 루트도 프리팹 기본 상태로 되돌린다. 안 그러면 이 캐릭터가 다음 판
        /// 선택화면에 재사용될 때 안테나 이펙트가 꺼진 채로 등장한다
        /// (<c>Antenna/LightningAttack</c> 은 프리팹에서 활성이다).
        /// </summary>
        public void Dispose()
        {
            DestroyClonedRings();
            RestoreVfxRootToPrefabState();

            // 다음 Initialize 뒤의 첫 SetActive 가 무조건 적용되게 한다.
            mActiveApplied = false;
            Active         = false;

            Owner = null;
            Root  = null;
        }

        /// <summary>
        /// 무기를 굴릴 수 있는 상태인가. 소유자가 회수됐거나 죽었으면 거짓이다.
        /// (Owner 는 MonoBehaviour 파생이라 == null 이 "파괴됨" 까지 잡아 준다.)
        /// </summary>
        private bool IsOwnerAlive => Owner != null && Owner.Available && !Owner.IsDead;

        /// <summary>
        /// <see cref="Player.OnUpdate"/> 가 매 프레임 부른다.
        /// 플레이어의 입력 처리가 끝난 뒤에 호출되므로 조준이 한 프레임 밀리지 않는다.
        /// </summary>
        public void OnUpdate(float elapseSeconds)
        {
            if (!Active) return;

            if (!IsOwnerAlive)
            {
                // 소유자가 사라지면 스스로 꺼진다. 재스폰 시 Initialize → SetActive 로 되살아난다.
                SetActive(false);
                return;
            }

            OnAim(elapseSeconds);
            UpdateTargetRing();

            if (HasCooldownElapsed(elapseSeconds))
            {
                Attack();
            }
        }

        /// <summary>쿨다운을 elapseSeconds 만큼 진행시키고, 이번 프레임에 발사할 차례면 true.</summary>
        private bool HasCooldownElapsed(float elapseSeconds)
        {
            mAttackTimer += elapseSeconds;
            if (mAttackTimer < mAttackInterval) return false;

            // 위상을 유지한다. 프레임이 튀어도 발사 주기가 밀리지 않는다.
            mAttackTimer -= mAttackInterval;
            return true;
        }

        /// <summary>
        /// 발사 판정 전에 불린다. 조준 연출(Frost 콘 회전 등)을 여기서 돌린다.
        ///
        /// 예전에는 <c>LateUpdate</c> 였다. 플레이어 회전은 FixedUpdate 에서 일어나고
        /// 물리 보간까지 끝난 포즈가 Update 시점에 이미 반영돼 있으므로, 여기서 월드 회전을
        /// 덮어써도 결과가 같다.
        /// </summary>
        protected virtual void OnAim(float elapseSeconds) { }

        /// <summary>쿨다운마다 호출된다. 파생 무기가 여기서 발사한다.</summary>
        protected abstract void Attack();

        /// <summary>
        /// 이번 발사가 헛방이었을 때 파생 무기가 호출한다. seconds 뒤에 다시 시도한다.
        /// 대상이 없는데 매 프레임 OverlapSphere 를 도는 것을 막는다.
        /// </summary>
        protected void RetryAfter(float seconds)
        {
            mAttackTimer = mAttackInterval - Mathf.Max(0f, seconds);
        }

        // ─── 총구 VFX ───

        /// <summary>
        /// 이 무기가 소유한 총구 VFX 루트의 경로(캐릭터 기준). null 이면 없는 것으로 본다.
        /// 무기를 바꾸면 이 GameObject 가 함께 꺼진다 — 안 그러면 장착했던 무기의
        /// 총구 파티클이 전부 겹쳐서 남는다.
        /// </summary>
        protected virtual string VfxRootPath => null;

        private Transform mVfxRoot            = null;
        private bool      mVfxResolved        = false;
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

        // ─── 타겟 마커 ───

        /// <summary>
        /// 적 발밑에 깔 마커의 경로. null 이면 마커를 쓰지 않는다.
        ///
        /// 원본에서는 마우스 지면 조준점을 가리키고 사거리/쿨다운을 3색으로 알리던
        /// 물건이지만, 자동 조준으로 바뀌면서 그 역할이 사라졌다.
        /// "이 무기로 지금 때릴 수 있는 적인가" 를 보여주는 용도로 바꿔 쓴다.
        /// </summary>
        protected virtual string TargetRingPath => null;

        /// <summary>이 무기의 실제 공격 사거리. 마커 색 판정의 기준이다.</summary>
        protected virtual float AttackRadius => 0f;

        /// <summary>
        /// 마커를 띄울 탐색 반경. 사거리보다 넓어야 "아직 못 때린다(빨강)" 를 보여줄 수 있다.
        /// </summary>
        protected virtual float MarkerRadius => AttackRadius * 2f;

        /// <summary>범위 무기면 true — 반경 안 전원에게 마커를 띄운다. 단일 대상 무기는 최근접 1명.</summary>
        protected virtual bool MarkAllTargets => false;

        /// <summary>
        /// 마커를 띄울 후보를 채운다. 반환값은 개수.
        /// <b>사거리 안인지와 무관하다</b> — 사거리 밖 적도 빨강으로 보여주기 위함이다.
        /// </summary>
        protected virtual int CollectTargets(List<Enemy> results)
        {
            results.Clear();

            float radius = MarkerRadius;
            if (Owner == null || radius <= 0f) return 0;

            Vector3 origin = Owner.CachedTransform.position;

            if (MarkAllTargets) return WeaponUtil.FindEnemiesInSphere(origin, radius, results);

            Enemy nearest = WeaponUtil.FindNearestEnemy(origin, radius, Candidates);
            if (nearest != null)
            {
                results.Add(nearest);
            }

            return results.Count;
        }

        /// <summary>
        /// 이 적을 지금 때릴 수 있는지 판정한다 — 마커 색이 여기서 나온다.
        /// 기본은 순수 거리 판정. 시야·각도 제약이 있는 무기가 재정의한다.
        ///
        /// enemy 와 Owner 의 null 검사가 없는 이유: 호출 경로가
        /// <see cref="OnUpdate"/>(Owner 생존 확인) → <see cref="UpdateTargetRing"/>(enemy null 스킵)
        /// 하나뿐이라 여기 도달하면 둘 다 살아 있다. 새 호출부를 만들면 그 전제를 함께 옮길 것.
        /// </summary>
        protected virtual TargetState EvaluateTarget(Enemy enemy)
        {
            float distance = WeaponUtil.PlanarDistance(
                enemy.CachedTransform.position, Owner.CachedTransform.position);

            if (distance <= AttackRadius) return TargetState.InRange;

            return distance <= AttackRadius * WeaponUtil.NearRangeScale
                ? TargetState.Near
                : TargetState.OutOfRange;
        }

        /// <summary>지면과 겹쳐 z-fighting 나지 않게 살짝 띄운다.</summary>
        private const float TargetRingHeight = 0.06f;

        /// <summary>마커 최대 개수. Frost 부채꼴에 한꺼번에 들어올 수 있는 수를 감안한 값.</summary>
        private const int MaxTargetRings = 16;

        private Transform mRingTemplate       = null;
        private bool      mTargetRingResolved = false;

        private readonly List<Transform> mRings       = new List<Transform>();
        private readonly List<Enemy>     mRingTargets = new List<Enemy>(MaxTargetRings);

        private static MaterialPropertyBlock sRingBlock = null;

        /// <summary>
        /// 후보들의 발밑에 마커를 깔고, "지금 때릴 수 있나" 에 따라 색을 바꾼다.
        /// 대상 수가 링 수보다 많으면 링을 복제해 늘린다.
        /// </summary>
        private void UpdateTargetRing()
        {
            ResolveRingTemplate();
            if (mRingTemplate == null) return;

            int count = Mathf.Min(CollectTargets(mRingTargets), MaxTargetRings);

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = mRingTargets[i];
                if (enemy == null) continue;

                Transform ring = GetRing(i);
                if (ring == null)
                {
                    // 링을 더 만들 수 없다. 남은 대상도 마찬가지이므로 여기서 끊는다.
                    break;
                }

                ShowRingOn(ring, enemy);
            }

            HideRingsFrom(count);
        }

        /// <summary>링 하나를 적 발밑에 놓고 "지금 때릴 수 있나" 에 맞는 색을 입힌다.</summary>
        private void ShowRingOn(Transform ring, Enemy enemy)
        {
            if (!ring.gameObject.activeSelf)
            {
                ring.gameObject.SetActive(true);
            }

            Vector3 position = enemy.CachedTransform.position;
            position.y += TargetRingHeight;
            ring.position = position;

            // 부모(캐릭터)가 회전하므로 월드 회전을 고정해 항상 지면에 눕힌다.
            ring.rotation = Quaternion.Euler(-90f, 0f, 0f);

            ApplyRingColor(ring, WeaponUtil.GetTargetColor(EvaluateTarget(enemy)));
        }

        /// <summary>이번 프레임에 쓰지 않은 남는 링을 숨긴다.</summary>
        private void HideRingsFrom(int firstUnusedIndex)
        {
            for (int i = firstUnusedIndex; i < mRings.Count; i++)
            {
                if (mRings[i] != null && mRings[i].gameObject.activeSelf)
                {
                    mRings[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 복제해 둔 링을 전부 파괴하고 템플릿 캐시를 버린다.
        /// 링은 캐릭터의 자식이라 무기 객체보다 오래 살아남는다 — <see cref="Dispose"/> 주석 참조.
        /// </summary>
        private void DestroyClonedRings()
        {
            for (int i = 0; i < mRings.Count; i++)
            {
                if (mRings[i] != null)
                {
                    Object.Destroy(mRings[i].gameObject);
                }
            }

            mRings.Clear();
            mRingTargets.Clear();

            mRingTemplate       = null;
            mTargetRingResolved = false;
        }

        private void ResolveRingTemplate()
        {
            if (mTargetRingResolved) return;

            mTargetRingResolved = true;

            string path = TargetRingPath;
            if (string.IsNullOrEmpty(path) || Root == null) return;

            mRingTemplate = Root.Find(path);
            if (mRingTemplate == null)
            {
                Log.Warning("{0}: 타겟 마커 '{1}' 을 찾지 못했다.", GetType().Name, path);
                return;
            }

            // 템플릿은 무기 VFX 루트 아래에 있어 무기를 바꾸면 같이 꺼진다.
            // 실제로 쓰는 링은 항상 살아 있는 캐릭터 루트 밑에 복제해 둔다.
            mRingTemplate.gameObject.SetActive(false);
        }

        /// <summary>i 번째 링을 얻는다. 모자라면 템플릿을 복제한다. 더 못 만들면 null.</summary>
        private Transform GetRing(int index)
        {
            GameAssert.InRange(index, MaxTargetRings, "GetRing(index)");

            // 템플릿이 파괴됐을 수 있다(캐릭터 회수). Instantiate(null) 은 예외를 던진다.
            if (mRingTemplate == null) return null;

            while (mRings.Count <= index && mRings.Count < MaxTargetRings)
            {
                Transform clone = Object.Instantiate(mRingTemplate, Root);
                clone.name = "TargetRing_" + GetType().Name + "_" + mRings.Count;
                clone.gameObject.SetActive(false);
                mRings.Add(clone);
            }

            return index < mRings.Count ? mRings[index] : null;
        }

        /// <summary>레거시 파티클 셰이더의 색 프로퍼티.</summary>
        private const string LegacyTintColorProperty = "_TintColor";

        /// <summary>URP Lit/Unlit 의 색 프로퍼티. 마커 머티리얼을 URP 로 갈았을 때 대비다.</summary>
        private const string UrpBaseColorProperty = "_BaseColor";

        private static void ApplyRingColor(Transform ring, Color color)
        {
            // material 프로퍼티를 직접 건드리면 Renderer 마다 머티리얼 사본이 생겨 샌다.
            var renderer = ring.GetComponent<Renderer>();
            if (renderer == null)
            {
                // 링 프리팹 구성 문제지만 게임플레이는 계속된다. 매 프레임 도는 경로라 로그는 생략한다.
                return;
            }

            if (sRingBlock == null)
            {
                sRingBlock = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(sRingBlock);
            sRingBlock.SetColor(LegacyTintColorProperty, color);
            sRingBlock.SetColor(UrpBaseColorProperty, color);
            renderer.SetPropertyBlock(sRingBlock);
        }

        private void SetTargetRingVisible(bool visible)
        {
            for (int i = 0; i < mRings.Count; i++)
            {
                if (mRings[i] != null && mRings[i].gameObject.activeSelf != visible)
                {
                    mRings[i].gameObject.SetActive(visible);
                }
            }
        }

        // ─── 공통 유틸 ───

        /// <summary>
        /// Owner 주변 radius 안에서 가장 가까운 살아있는 적을 반환한다.
        /// Shootable 레이어만 훑으므로 바닥/환경 콜라이더를 건드리지 않는다.
        /// </summary>
        protected Enemy FindNearestEnemy(float radius)
        {
            if (Owner == null) return null;

            return WeaponUtil.FindNearestEnemy(Owner.CachedTransform.position, radius, Candidates);
        }
    }
}
