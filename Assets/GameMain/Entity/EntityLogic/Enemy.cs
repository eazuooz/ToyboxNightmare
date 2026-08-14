using UnityEngine;
using UnityEngine.AI;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 적 엔티티. NavMeshAgent 로 플레이어를 추적하고, 사거리 안에서 주기적으로 공격한다.
    /// 사망하면 즉시 사라지지 않고 연출(정지 → 침하) 후 회수된다.
    /// </summary>
    public class Enemy : TargetableObject
    {
        /// <summary>
        /// 사망 연출 단계. Alive → Dying(쓰러지는 중) → Sinking(바닥으로 침하 중) 한 방향으로만 간다.
        /// bool 두 개(죽는 중/가라앉는 중)로 두면 "가라앉는데 죽지는 않은" 불가능한 조합이 표현된다.
        /// </summary>
        private enum DeathPhase
        {
            Alive,
            Dying,
            Sinking,
        }

        // ─── 튜닝 상수 ───

        private const float FleeDistance = 10f; // 원본 runAwayDistance

        /// <summary>도주 목적지를 NavMesh 폴리곤 위로 스냅할 때 허용하는 탐색 반경.</summary>
        private const float FleeSampleRadius = 4f;

        /// <summary>도주 방향을 정규화하기 전에 요구하는 최소 제곱 길이. 길이 0 벡터 정규화를 막는다.</summary>
        private const float MinFleeDirectionSqrMagnitude = 0.0001f;

        /// <summary>콜라이더를 못 찾았을 때 쓸 조준 높이(발밑 기준). 대략 몸통 중심이다.</summary>
        private const float FallbackAimHeight = 0.5f;

        /// <summary>
        /// StartSinking 애니메이션 이벤트가 없는 종(ZomBear)을 위한 폴백 시점.
        /// 사망 연출 전체 길이 중 이 비율만큼 지나면 강제로 침하를 시작한다.
        /// </summary>
        private const float SinkFallbackRatio = 0.5f;

        // 프리팹 구조 계약. 자식 이름이 바뀌면 조용히 null 이 되므로 한곳에 모아 둔다.
        private const string HitParticlesChildName = "HitParticles";
        private const string FreezeFxChildName     = "FreezeFx";
        private const string SlimeFxChildName      = "SlimeFx";

        // Animator 파라미터 이름.
        private const string DeadTriggerName       = "Dead";
        private const string PlayerDeadTriggerName = "PlayerDead";

        // ─── 캐시된 참조 ───

        private EnemyData    mEnemyData = null;
        private NavMeshAgent mAgent     = null;
        private Animator     mAnimator  = null;

        /// <summary>
        /// 루트의 콜라이더 전부. 적 프리팹은 CapsuleCollider(몸통)와 SphereCollider(트리거)를
        /// 둘 다 레이어 9(Shootable)로 달고 있어, 몸통만 꺼서는 시체가 히트스캔을 계속 막는다.
        /// </summary>
        private Collider[] mColliders = null;

        // 프리팹의 HitParticles 자식(흰 솜뭉치). 원본과 값까지 동일하게 보존돼 있다.
        private ParticleSystem  mHitParticles = null;

        // 디버프 VFX. 적 프리팹에 비활성 자식으로 구워 두면 자동으로 잡힌다(없어도 동작한다).
        private GameObject mFreezeFx = null;
        private GameObject mSlimeFx  = null;

        // 프리팹 원본의 자식별 레이어. EntityLogic.OnHide 가 SetLayerRecursively 로
        // 계층 전체를 루트 레이어(9)로 평탄화해 버리므로, 재사용 전에 되돌려야
        // 2세대 인스턴스가 1세대와 다르게 동작하는 일을 막을 수 있다.
        private Transform[] mLayerTargets   = null;
        private int[]       mOriginalLayers = null;

        // ─── 전투 / 연출 상태 ───

        private float mAttackTimer     = 0f;
        private float mRetargetTimer   = 0f;
        private float mSpeedMultiplier = 1f;

        // 사망 연출 상태
        private DeathPhase mDeathPhase = DeathPhase.Alive;
        private float      mDeathTimer = 0f;

        // 플레이어 사망 연출을 한 번만 재생하기 위한 플래그
        private bool mPlayerDeadNotified = false;

        // ─── 디버프 ───

        /// <summary>
        /// Frost/Stink/Slime 상태 전부. <see cref="OnInit"/> 에서 만들어 이 인스턴스와 수명을 같이한다
        /// (필드 초기화자에서는 this 를 넘길 수 없어 거기서 만들 수 없다).
        ///
        /// 순수 C# 객체라 엔티티 풀 재사용 사이에 살아남는다 — 이전 판의 상태는
        /// <see cref="ResetCombatState"/> 가 <c>ClearAll</c> 로 되돌린다.
        /// </summary>
        private EnemyDebuffs mDebuffs = null;

        // ─── 상태 질의 ───

        /// <summary>
        /// 무력화 상태 — 얼었거나, 도주 중이거나, 공격이 봉인됐다.
        /// 판정은 <see cref="EnemyDebuffs.IsNeutralized"/> 에 있다.
        /// </summary>
        public bool IsNeutralized => mDebuffs.IsNeutralized;

        /// <summary>이 적의 공격 사거리. 데이터가 없으면 0.</summary>
        public float AttackRange => mEnemyData != null ? mEnemyData.Stats.AttackRange : 0f;

        /// <summary>
        /// 히트스캔·유도탄이 노릴 몸통 지점. 트랜스폼 원점은 발밑이라 그대로 노리면 바닥에 막힌다.
        ///
        /// <c>bounds</c> 는 트랜스폼을 따라 움직이므로 <b>캐시할 것은 Collider 참조지 bounds 값이 아니다</b> —
        /// 값을 캐시하면 적이 움직이는 순간 조준점이 뒤처진다. 대신 콜라이더는 OnInit 에서
        /// 이미 잡아 두었으므로 매 프레임 GetComponent 를 돌지 않는다.
        /// </summary>
        public Vector3 AimPoint
        {
            get
            {
                Collider collider = AimCollider;
                return collider != null
                    ? collider.bounds.center
                    : CachedTransform.position + Vector3.up * FallbackAimHeight;
            }
        }

        /// <summary>
        /// 조준 기준이 될 콜라이더. 루트 콜라이더 중 첫 번째이며,
        /// 이는 <c>GetComponent&lt;Collider&gt;()</c> 가 돌려주던 것과 같다(순서가 같다).
        /// </summary>
        private Collider AimCollider
        {
            get
            {
                if (mColliders == null) return null;

                for (int i = 0; i < mColliders.Length; i++)
                {
                    if (mColliders[i] != null) return mColliders[i];
                }

                return null;
            }
        }

        /// <summary>
        /// 사망 연출에 들어갔는지. 쓰러지는 중과 침하 중을 모두 포함한다.
        /// <see cref="EnemyDebuffs"/> 가 "죽는 중이면 디버프 전량 해제" 판정에 쓰므로 internal 이다.
        /// </summary>
        internal bool IsDying => mDeathPhase != DeathPhase.Alive;

        /// <summary>
        /// NavMeshAgent 에 명령을 내려도 되는 상태인지.
        /// 셋 중 하나라도 어긋난 채로 SetDestination/isStopped 를 건드리면 Unity 가 에러를 뱉는다.
        /// </summary>
        private bool IsAgentReady => mAgent != null && mAgent.enabled && mAgent.isOnNavMesh;

        /// <summary>이동 속도 배율. 빙결이 0 으로 만든다.</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            mSpeedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
        }

        // ─── 디버프 수신 API ───
        // 무기 쪽(FrostWeapon / StinkHit / SlimeProjectile)이 부르는 창구다. 시그니처를 바꾸지 말 것.
        // 상태와 판정은 전부 EnemyDebuffs 에 있고 여기서는 위임만 한다.

        /// <summary>Frost 콘 안에 있는 동안 매 재판정 틱마다 호출한다.</summary>
        public void RefreshFrostContact()
        {
            mDebuffs.RefreshFrostContact();
        }

        /// <summary>Stink 착탄. 도주 지속시간(초). 중첩 시 더 긴 쪽으로 갱신한다.</summary>
        public void ApplyFlee(float duration)
        {
            mDebuffs.ApplyFlee(duration);
        }

        /// <summary>
        /// Slime 명중. 스택하지 않고 리셋한다(원본은 동시 1마리뿐이라 중첩 규칙이 없었다).
        /// 첫 틱은 부착 즉시 나간다 — 원본 루프가 "데미지 → 대기" 순서다.
        /// </summary>
        public void ApplySlime(int ticks, float tickInterval, int damagePerTick, Entity attacker)
        {
            mDebuffs.ApplySlime(ticks, tickInterval, damagePerTick, attacker);
        }

        // ─── 디버프 지원 (EnemyDebuffs 전용) ───
        // 디버프가 몸에 반영해야 하는 것만 연다. 상태 필드 자체는 열지 않는다.

        /// <summary>
        /// Animator 를 켜고 끈다. 빙결이 끄고 해동이 되돌린다.
        /// Animator 가 없는 프리팹도 있어(OnInit 이 경고만 남긴다) null 검사를 여기서 한다.
        /// </summary>
        internal void SetAnimatorEnabled(bool isEnabled)
        {
            if (mAnimator == null) return;

            mAnimator.enabled = isEnabled;
        }

        /// <summary>빙결 VFX 토글. 프리팹에 FreezeFx 자식이 없으면 조용히 무시된다.</summary>
        internal void SetFreezeFxActive(bool active)
        {
            SetFxActive(mFreezeFx, active);
        }

        /// <summary>슬라임 VFX 토글. 프리팹에 SlimeFx 자식이 없으면 조용히 무시된다.</summary>
        internal void SetSlimeFxActive(bool active)
        {
            SetFxActive(mSlimeFx, active);
        }

        private static void SetFxActive(GameObject fx, bool active)
        {
            if (fx != null && fx.activeSelf != active)
            {
                fx.SetActive(active);
            }
        }

        // ─── 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            // 인스턴스당 1회. OnShow/OnUpdate 보다 반드시 먼저 도므로 이후 경로에서는 항상 유효하다.
            mDebuffs = new EnemyDebuffs(this);

            mAgent     = GetComponent<NavMeshAgent>();
            // includeInactive: true 가 필수다. EntityLogic.OnHide 가 SetActive(false) 로 회수하는데
            // 다음 Show 에서는 OnInit 이 OnShow(= Visible=true → SetActive(true)) 보다 먼저 돈다.
            // 즉 풀에서 재사용된 인스턴스는 여기서 GameObject 가 꺼져 있고, 기본값(false)이면
            // 자기 자신과 자식을 훑지 않아 조용히 null 이 된다 — 2회차부터 사망 연출이 통째로 죽는다.
            mAnimator  = GetComponentInChildren<Animator>(true);
            mColliders = GetComponents<Collider>();

            WarnOnMissingPrefabParts();
            CacheVfxReferences();

            // OnInit 은 인스턴스당 1회, 첫 OnHide(레이어 평탄화)보다 먼저 돈다.
            // 따라서 여기서 뜬 스냅샷이 프리팹 원본 값이다.
            CacheChildLayers();
        }

        /// <summary>
        /// 프리팹 구성 실수를 스폰 시점에 드러낸다. 아래 둘은 없어도 예외는 안 나지만
        /// 적이 맞지 않거나 사망 연출이 통째로 생략돼 원인을 찾기 어렵다.
        /// </summary>
        private void WarnOnMissingPrefabParts()
        {
            if (mColliders == null || mColliders.Length == 0)
            {
                Log.Warning("Enemy '{0}' 루트에 Collider 가 없다. 히트스캔·트리거 판정이 전부 빗나간다.",
                    CachedTransform.name);
            }

            if (mAnimator == null)
            {
                Log.Warning("Enemy '{0}' 에 Animator 가 없다. 사망/빙결 연출이 재생되지 않는다.",
                    CachedTransform.name);
            }
        }

        private void CacheVfxReferences()
        {
            // 이름으로 찾는다. GetComponentInChildren<ParticleSystem> 로 잡으면
            // 디버프 VFX 자식을 추가하는 순간 계층 순서에 따라 엉뚱한 것이 잡힌다.
            Transform hit = CachedTransform.Find(HitParticlesChildName);
            mHitParticles = hit != null ? hit.GetComponent<ParticleSystem>() : null;
            if (mHitParticles == null)
            {
                mHitParticles = GetComponentInChildren<ParticleSystem>(true);
            }

            // 프리팹에 구워 두면 잡히고, 없으면 null 인 채로 게임플레이만 동작한다.
            mFreezeFx = FindChildObject(FreezeFxChildName);
            mSlimeFx  = FindChildObject(SlimeFxChildName);
        }

        private GameObject FindChildObject(string childName)
        {
            Transform child = CachedTransform.Find(childName);
            return child != null ? child.gameObject : null;
        }

        private void CacheChildLayers()
        {
            mLayerTargets   = GetComponentsInChildren<Transform>(true);
            mOriginalLayers = new int[mLayerTargets.Length];

            for (int i = 0; i < mLayerTargets.Length; i++)
            {
                mOriginalLayers[i] = mLayerTargets[i].gameObject.layer;
            }
        }

        private void RestoreChildLayers()
        {
            if (mLayerTargets == null || mOriginalLayers == null) return;

            GameAssert.IsTrue(mLayerTargets.Length == mOriginalLayers.Length,
                "레이어 스냅샷 길이가 대상 수와 다르다. CacheChildLayers 호출 시점을 확인할 것.");

            // 두 배열은 항상 같은 길이지만, 어긋나더라도 IndexOutOfRange 로 터지지는 않게 한다.
            int count = Mathf.Min(mLayerTargets.Length, mOriginalLayers.Length);
            for (int i = 0; i < count; i++)
            {
                if (mLayerTargets[i] != null)
                {
                    mLayerTargets[i].gameObject.layer = mOriginalLayers[i];
                }
            }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mEnemyData = userData as EnemyData;
            if (mEnemyData == null)
            {
                Log.Error("Enemy data is invalid.");
                return;
            }

            ResetCombatState();
            ResetPresentation();

            CachedTransform.rotation = mEnemyData.Rotation;
            PlaceOnNavMesh(mEnemyData.Position, mEnemyData.Stats);
        }

        /// <summary>풀에서 재사용되므로 이전 판의 상태를 전부 되돌린다.</summary>
        private void ResetCombatState()
        {
            mAttackTimer        = 0f;
            mSpeedMultiplier    = 1f;
            mDeathPhase         = DeathPhase.Alive;
            mDeathTimer         = 0f;
            mPlayerDeadNotified = false;

            // 디버프 전량 리셋. 하나라도 빠지면 재사용된 적이 얼어붙은 채 또는
            // 공격 불능인 채로 부활한다.
            mDebuffs.ClearAll();

            // 임계값으로 채워 둔다. 0 으로 두면 첫 SetDestination 이 0.5초 뒤에나
            // 나가서 스폰 직후 반 초 동안 제자리 달리기를 한다.
            mRetargetTimer = EnemyTable.RetargetInterval;
        }

        /// <summary>연출용 컴포넌트를 프리팹 초기 상태로 되돌린다.</summary>
        private void ResetPresentation()
        {
            RestoreChildLayers();

            // 원본은 SetActive 풀링이라 자동 초기화됐지만 GF 풀은 그렇지 않다.
            if (mHitParticles != null)
            {
                mHitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            SetCollidersEnabled(true);

            if (mAnimator != null)
            {
                // 빙결이 꺼둔 채로 회수됐을 수 있다. 비활성 Animator 에 Rebind 를 걸면
                // 다음 판 적이 T 포즈로 굳는다.
                mAnimator.enabled = true;
                mAnimator.Rebind();
                mAnimator.Update(0f);
            }
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            DisableAgent();

            // 데이터는 base 체인 끝(EntityLogicBase)에서 ReferencePool 로 반납된다.
            // 반납이 참조를 지워 주지 않으므로 여기서 놓지 않으면 AttackRange 같은 프로퍼티가
            // 풀에 들어간 객체를 계속 읽고, 그 인스턴스가 다음 적에게 재사용되면 남의 스탯을 돌려준다.
            mEnemyData = null;

            base.OnHide(isShutdown, userData);
        }

        // ─── 이동 / 공격 ───

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (mEnemyData == null || IsHiding) return;

            // 사망 연출 중에도, 플레이어가 죽은 뒤에도 디버프 타이머는 돌아야 한다.
            // 아래 early-return 뒤에 두면 플레이어 사망 순간 빙결이 영구화된다.
            mDebuffs.OnUpdate(elapseSeconds);

            if (IsDying)
            {
                UpdateDeath(elapseSeconds);
                return;
            }

            Player player = Player.Instance;

            bool isPlayerSpawned = player != null && player.Available;
            if (!isPlayerSpawned)
            {
                StopMoving();
                return;
            }

            if (player.IsDead)
            {
                StopMoving();
                NotifyPlayerDead();
                return;
            }

            UpdateChase(player, elapseSeconds);
            UpdateAttack(player, elapseSeconds);
        }

        /// <summary>
        /// 목적지는 0.5초 주기로만 갱신한다(경로 계산 비용 절감).
        /// 원본 EnemyMovement 는 코루틴으로 같은 주기를 돌되 첫 SetDestination 은
        /// 즉시 나가므로, OnShow 에서 타이머를 임계값으로 채워 그 의미를 맞췄다.
        /// </summary>
        private void UpdateChase(Player player, float elapseSeconds)
        {
            if (!IsAgentReady) return;

            mAgent.isStopped = false;
            mAgent.speed     = mEnemyData.Stats.MoveSpeed * mSpeedMultiplier;

            mRetargetTimer += elapseSeconds;
            if (mRetargetTimer < EnemyTable.RetargetInterval) return;

            mRetargetTimer = 0f;

            // 추적 목적지는 발밑(트랜스폼 원점)이다. 공격 판정만 콜라이더 중심을 쓴다.
            mAgent.SetDestination(GetDestination(player.CachedTransform.position));
        }

        /// <summary>
        /// 원본 EnemyAttack 은 코루틴이 사거리와 무관하게 자유 진동하면서
        /// 매 틱 "지금 사거리 안인가" 만 본다. 공격 성공 시에도 위상이 리셋되지 않는다.
        /// 그래서 첫 타격은 접촉 후 0~간격 사이의 임의 시점에 나간다.
        /// (타이머를 0 으로 리셋하면 접촉 즉시 확정 타격이 되어 원본보다 가혹해진다.)
        /// </summary>
        private void UpdateAttack(Player player, float elapseSeconds)
        {
            EnemyStats stats = mEnemyData.Stats;

            mAttackTimer += elapseSeconds;
            if (mAttackTimer < stats.TimeBetweenAttacks) return;

            mAttackTimer -= stats.TimeBetweenAttacks;

            // 슬라임에 걸리면 데미지만 막는다. 타이머는 계속 돌려야 한다 —
            // 멈추면 봉인 해제 직후 확정 타격이 나가 원본보다 가혹해진다.
            if (mDebuffs.IsAttackSuppressed) return;

            // 공격 판정은 콜라이더 중심 기준이다.
            bool isPlayerInAttackRange = GetPlanarDistance(player.CenterPosition) <= stats.AttackRange;
            if (!isPlayerInAttackRange) return;

            player.ApplyDamage(Entity, stats.AttackDamage);
        }

        /// <summary>
        /// 추적 목적지. 도주 중이면 플레이어 반대 방향으로 달아난다.
        ///
        /// 원본 EnemyMovement 는 정규화된 방향 벡터를 그대로 월드 목적지로 써서
        /// 모든 적이 월드 원점 근처로 몰려가는 버그가 있었다. 그건 옮기지 않는다.
        /// </summary>
        private Vector3 GetDestination(Vector3 playerPosition)
        {
            if (!mDebuffs.IsFleeing) return playerPosition;

            return GetFleeDestination(playerPosition);
        }

        /// <summary>플레이어 반대편으로 FleeDistance 만큼 떨어진, NavMesh 위의 지점.</summary>
        private Vector3 GetFleeDestination(Vector3 playerPosition)
        {
            Vector3 away = CachedTransform.position - playerPosition;
            away.y = 0f;

            // 플레이어와 거의 겹쳐 있으면 방향이 없다. 길이 0 벡터를 정규화하면 (0,0,0) 이 나와
            // 제자리를 목적지로 잡게 되므로, 그럴 때는 지금 보는 방향으로 달아난다.
            if (away.sqrMagnitude < MinFleeDirectionSqrMagnitude)
            {
                away = CachedTransform.forward;
            }

            Vector3 destination = CachedTransform.position + away.normalized * FleeDistance;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(destination, out navHit, FleeSampleRadius, NavMesh.AllAreas))
            {
                return navHit.position;
            }

            // NavMesh 밖이면 도주를 포기한다. 아무 데도 못 가는 것보다 낫다.
            return playerPosition;
        }

        /// <summary>
        /// 공격 사거리는 XZ 평면에서 잰다. 캐릭터 콜라이더의 중심이 Y 로 올라가 있어
        /// 3D 거리를 쓰면 접근 방향과 높이차에 따라 사거리가 흔들린다.
        /// </summary>
        private float GetPlanarDistance(Vector3 target)
        {
            Vector3 delta = target - CachedTransform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        // ─── 피격 / 통지 ───

        /// <summary>피격 파편. 원본 EnemyHealth.TakeDamage 가 매 피격 hitParticles.Play() 하는 것과 같다.</summary>
        protected override void OnDamaged(Entity attacker, int damageHitPoints)
        {
            if (mHitParticles != null)
            {
                mHitParticles.Play();
            }
        }

        /// <summary>플레이어가 죽으면 승리 연출로 전환한다. 적 애니메이터의 PlayerDead 트리거.</summary>
        private void NotifyPlayerDead()
        {
            if (mPlayerDeadNotified || mAnimator == null) return;

            mPlayerDeadNotified = true;
            mAnimator.SetTrigger(PlayerDeadTriggerName);
        }

        // ─── 사망 ───

        /// <summary>
        /// 주의: <c>base.OnDead</c> 를 부르지 않는다. base 는 즉시 SafeHide 하므로
        /// 연출이 통째로 생략되고, 연출 종료 시점의 Hide 와 겹쳐 중복 호출이 된다.
        /// </summary>
        protected override void OnDead(Entity attacker)
        {
            if (IsDying) return;

            mDeathPhase = DeathPhase.Dying;
            mDeathTimer = 0f;

            // 침하 중에 에이전트가 Y 를 NavMesh 높이로 되끌어올리므로 먼저 끈다.
            DisableAgent();

            // 트리거 구까지 전부 끈다. 몸통만 끄면 시체가 가라앉는 1.3~1.7초 동안
            // 트리거 구가 Lightning 레이를 계속 가로채, 뒤에 있는 적에게 한 발도 안 들어간다.
            SetCollidersEnabled(false);

            // 빙결 중에 죽으면 Animator 가 꺼져 있어 사망 애니메이션이 통째로 안 나온다.
            mDebuffs.ClearAll();

            if (mAnimator != null)
            {
                mAnimator.SetTrigger(DeadTriggerName);
            }

            FireEnemyDiedEvent();
        }

        /// <summary>사망 통지. 스코어 집계와 드롭 위치가 이 이벤트에 실린다.</summary>
        private void FireEnemyDiedEvent()
        {
            if (mEnemyData == null)
            {
                // OnShow 가 EnemyData 아닌 userData 를 받았을 때만 도달한다(설정 실수).
                Log.Error("Enemy data is invalid. EnemyDied 이벤트를 발행하지 못했다.");
                return;
            }

            EventComponent eventComponent = GameEntry.GetComponent<EventComponent>();
            if (eventComponent == null)
            {
                Log.Error("EventComponent 를 찾지 못했다. GameFramework.prefab 구성을 확인할 것.");
                return;
            }

            eventComponent.Fire(this, EnemyDiedEventArgs.Create(
                mEnemyData.AssetName, mEnemyData.ScoreValue, CachedTransform.position));
        }

        /// <summary>사망 애니메이션 이벤트에서 호출된다(ZomBear 를 제외한 4종의 FBX 에 baked).</summary>
        private void StartSinking()
        {
            // 사망 연출 중이 아닐 때 들어온 이벤트는 무시한다. 살아 있는 적을 바닥으로
            // 꺼뜨리지 않기 위한 방어이며, 정상 경로에서는 도달하지 않는다.
            if (mDeathPhase != DeathPhase.Dying) return;

            mDeathPhase = DeathPhase.Sinking;
        }

        private void UpdateDeath(float elapseSeconds)
        {
            mDeathTimer += elapseSeconds;

            // ZomBear 의 FBX 에는 StartSinking 이벤트가 없다. 폴백으로 절반 지점부터 가라앉힌다.
            bool needsSinkFallback = mDeathPhase == DeathPhase.Dying
                                     && mDeathTimer >= EnemyTable.DeathEffectTime * SinkFallbackRatio;
            if (needsSinkFallback)
            {
                mDeathPhase = DeathPhase.Sinking;
            }

            if (mDeathPhase == DeathPhase.Sinking)
            {
                CachedTransform.Translate(Vector3.down * (EnemyTable.SinkSpeed * elapseSeconds), Space.World);
            }

            if (mDeathTimer >= EnemyTable.DeathEffectTime)
            {
                SafeHide();
            }
        }

        // ─── NavMeshAgent 헬퍼 ───

        private void PlaceOnNavMesh(Vector3 position, EnemyStats stats)
        {
            if (mAgent == null)
            {
                CachedTransform.position = position;
                return;
            }

            // 에이전트가 켜진 상태로 transform 을 옮기면 경고가 나므로 끈 뒤 옮기고 다시 켠다.
            mAgent.enabled = false;
            CachedTransform.position = position;
            mAgent.enabled = true;

            // Warp 는 NavMesh 폴리곤 위로 스냅시킨다. 스폰 좌표가 살짝 떠 있어도 안전하다.
            mAgent.Warp(position);

            mAgent.speed            = stats.MoveSpeed;
            mAgent.stoppingDistance = stats.StoppingDistance;

            if (mAgent.isOnNavMesh)
            {
                mAgent.isStopped = false;
            }
            else
            {
                Log.Warning("Enemy '{0}' 가 NavMesh 위에 놓이지 못했다. 스폰 좌표 {1} 확인 필요.",
                    stats.AssetName, position);
            }
        }

        private void StopMoving()
        {
            if (IsAgentReady)
            {
                mAgent.isStopped = true;
            }
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            if (mColliders == null) return;

            for (int i = 0; i < mColliders.Length; i++)
            {
                if (mColliders[i] != null)
                {
                    mColliders[i].enabled = isEnabled;
                }
            }
        }

        private void DisableAgent()
        {
            if (mAgent == null || !mAgent.enabled) return;

            if (mAgent.isOnNavMesh)
            {
                mAgent.isStopped = true;
                mAgent.ResetPath();
            }

            mAgent.enabled = false;
        }
    }
}
