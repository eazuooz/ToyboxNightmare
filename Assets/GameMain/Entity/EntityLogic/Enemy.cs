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
        private EnemyData       mEnemyData    = null;
        private NavMeshAgent    mAgent        = null;
        private Animator        mAnimator     = null;
        private CapsuleCollider mBodyCollider = null;

        // 프리팹의 HitParticles 자식(흰 솜뭉치). 원본과 값까지 동일하게 보존돼 있다.
        private ParticleSystem  mHitParticles = null;

        private float mAttackTimer     = 0f;
        private float mRetargetTimer   = 0f;
        private float mSpeedMultiplier = 1f;

        // 프리팹 원본의 자식별 레이어. EntityLogic.OnHide 가 SetLayerRecursively 로
        // 계층 전체를 루트 레이어(9)로 평탄화해 버리므로, 재사용 전에 되돌려야
        // 2세대 인스턴스가 1세대와 다르게 동작하는 일을 막을 수 있다.
        private Transform[] mLayerTargets   = null;
        private int[]       mOriginalLayers = null;

        // 사망 연출 상태
        private bool  mDying      = false;
        private bool  mSinking    = false;
        private float mDeathTimer = 0f;

        // 플레이어 사망 연출을 한 번만 재생하기 위한 플래그
        private bool mPlayerDeadNotified = false;

        // ─── 디버프 상태 ───
        // AttachEntity 대신 Enemy 내부 상태로 관리한다. 엔티티가 회수되면 상태도 함께
        // 사라지므로 "주인 잃은 디버프" 가 원천적으로 생기지 않는다.

        private const float FrostContactGrace = 0.35f; // 재판정 틱(0.2)보다 커야 빙결이 깜빡이지 않는다
        private const float FrostFreezeDelay  = 1f;    // 원본 freezeDelay
        private const float FrostThawDuration = 2f;    // 원본 freezeDuration
        private const float FleeDistance      = 10f;   // 원본 runAwayDistance

        // Frost
        private bool  mFrozen           = false;
        private float mFrostTimer       = 0f;
        private float mFrostRefreshTime = -999f;

        // Stink
        private float mFleeTimer = 0f;

        // Slime
        private bool   mAttackSuppressed  = false;
        private int    mSlimeTicksLeft    = 0;
        private float  mSlimeTickTimer    = 0f;
        private float  mSlimeTickInterval = 0.5f;
        private int    mSlimeTickDamage   = 20;
        private Entity mSlimeAttacker     = null;

        // 디버프 VFX. 적 프리팹에 비활성 자식으로 구워 두면 자동으로 잡힌다(없어도 동작한다).
        private GameObject mFreezeFx = null;
        private GameObject mSlimeFx  = null;

        /// <summary>이동 속도 배율. 빙결이 0 으로 만든다.</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            mSpeedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
        }

        // ─── 디버프 수신 API ───

        /// <summary>Frost 콘 안에 있는 동안 매 재판정 틱마다 호출한다.</summary>
        public void RefreshFrostContact()
        {
            mFrostRefreshTime = Time.time;
        }

        /// <summary>Stink 착탄. 도주 지속시간(초). 중첩 시 더 긴 쪽으로 갱신한다.</summary>
        public void ApplyFlee(float duration)
        {
            mFleeTimer = Mathf.Max(mFleeTimer, duration);
        }

        /// <summary>
        /// Slime 명중. 스택하지 않고 리셋한다(원본은 동시 1마리뿐이라 중첩 규칙이 없었다).
        /// 첫 틱은 부착 즉시 나간다 — 원본 루프가 "데미지 → 대기" 순서다.
        /// </summary>
        public void ApplySlime(int ticks, float tickInterval, int damagePerTick, Entity attacker)
        {
            if (ticks <= 0)
            {
                return;
            }

            mSlimeTicksLeft    = ticks;
            mSlimeTickInterval = Mathf.Max(0.05f, tickInterval);
            mSlimeTickDamage   = damagePerTick;
            mSlimeTickTimer    = 0f;
            mAttackSuppressed  = true;

            // 회수된 엔티티를 들고 있지 않는다. Available 은 Entity 가 아니라 Logic 쪽에 있다.
            bool attackerAlive = attacker != null && attacker.Logic != null && attacker.Logic.Available;
            mSlimeAttacker = attackerAlive ? attacker : null;

            SetFxActive(mSlimeFx, true);
        }

        // ─── 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mAgent        = GetComponent<NavMeshAgent>();
            mAnimator     = GetComponentInChildren<Animator>();
            mBodyCollider = GetComponent<CapsuleCollider>();

            // 이름으로 찾는다. GetComponentInChildren<ParticleSystem> 로 잡으면
            // 디버프 VFX 자식을 추가하는 순간 계층 순서에 따라 엉뚱한 것이 잡힌다.
            Transform hit = CachedTransform.Find("HitParticles");
            mHitParticles = hit != null ? hit.GetComponent<ParticleSystem>() : null;
            if (mHitParticles == null)
            {
                mHitParticles = GetComponentInChildren<ParticleSystem>(true);
            }

            // 프리팹에 구워 두면 잡히고, 없으면 null 인 채로 게임플레이만 동작한다.
            Transform freeze = CachedTransform.Find("FreezeFx");
            mFreezeFx = freeze != null ? freeze.gameObject : null;

            Transform slime = CachedTransform.Find("SlimeFx");
            mSlimeFx = slime != null ? slime.gameObject : null;

            // OnInit 은 인스턴스당 1회, 첫 OnHide(레이어 평탄화)보다 먼저 돈다.
            // 따라서 여기서 뜬 스냅샷이 프리팹 원본 값이다.
            CacheChildLayers();
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
            if (mLayerTargets == null)
            {
                return;
            }

            for (int i = 0; i < mLayerTargets.Length; i++)
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

            // 풀에서 재사용되므로 이전 판의 상태를 전부 되돌린다.
            mAttackTimer        = 0f;
            mSpeedMultiplier    = 1f;
            mDying              = false;
            mSinking            = false;
            mDeathTimer         = 0f;
            mPlayerDeadNotified = false;

            // 디버프 전량 리셋. 하나라도 빠지면 재사용된 적이 얼어붙은 채 또는
            // 공격 불능인 채로 부활한다.
            ClearAllDebuffs();

            // 임계값으로 채워 둔다. 0 으로 두면 첫 SetDestination 이 0.5초 뒤에나
            // 나가서 스폰 직후 반 초 동안 제자리 달리기를 한다.
            mRetargetTimer = EnemyTable.RetargetInterval;

            RestoreChildLayers();

            // 원본은 SetActive 풀링이라 자동 초기화됐지만 GF 풀은 그렇지 않다.
            if (mHitParticles != null)
            {
                mHitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (mBodyCollider != null)
            {
                mBodyCollider.enabled = true;
            }

            if (mAnimator != null)
            {
                // 빙결이 꺼둔 채로 회수됐을 수 있다. 비활성 Animator 에 Rebind 를 걸면
                // 다음 판 적이 T 포즈로 굳는다.
                mAnimator.enabled = true;
                mAnimator.Rebind();
                mAnimator.Update(0f);
            }

            CachedTransform.rotation = mEnemyData.Rotation;
            PlaceOnNavMesh(mEnemyData.Position);
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            DisableAgent();
            base.OnHide(isShutdown, userData);
        }

        // ─── 이동 / 공격 ───

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (mEnemyData == null || IsHiding)
            {
                return;
            }

            // 사망 연출 중에도, 플레이어가 죽은 뒤에도 디버프 타이머는 돌아야 한다.
            // 아래 early-return 뒤에 두면 플레이어 사망 순간 빙결이 영구화된다.
            UpdateDebuffs(elapseSeconds);

            if (mDying)
            {
                UpdateDeath(elapseSeconds);
                return;
            }

            Player player = Player.Instance;
            if (player == null || !player.Available)
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

            // 추적 목적지는 발밑(트랜스폼 원점), 공격 판정은 콜라이더 중심 기준이다.
            Vector3 playerPosition = player.CachedTransform.position;
            Vector3 playerCenter   = player.CenterPosition;
            EnemyStats stats = mEnemyData.Stats;

            // 목적지는 0.5초 주기로만 갱신한다(경로 계산 비용 절감).
            // 원본 EnemyMovement 는 코루틴으로 같은 주기를 돌되 첫 SetDestination 은
            // 즉시 나가므로, OnShow 에서 타이머를 임계값으로 채워 그 의미를 맞췄다.
            if (mAgent != null && mAgent.enabled && mAgent.isOnNavMesh)
            {
                mAgent.isStopped = false;
                mAgent.speed = stats.MoveSpeed * mSpeedMultiplier;

                mRetargetTimer += elapseSeconds;
                if (mRetargetTimer >= EnemyTable.RetargetInterval)
                {
                    mRetargetTimer = 0f;
                    mAgent.SetDestination(GetDestination(playerPosition));
                }
            }

            // 원본 EnemyAttack 은 코루틴이 사거리와 무관하게 자유 진동하면서
            // 매 틱 "지금 사거리 안인가" 만 본다. 공격 성공 시에도 위상이 리셋되지 않는다.
            // 그래서 첫 타격은 접촉 후 0~간격 사이의 임의 시점에 나간다.
            // (타이머를 0 으로 리셋하면 접촉 즉시 확정 타격이 되어 원본보다 가혹해진다.)
            mAttackTimer += elapseSeconds;
            if (mAttackTimer >= stats.TimeBetweenAttacks)
            {
                mAttackTimer -= stats.TimeBetweenAttacks;

                // 슬라임에 걸리면 데미지만 막는다. 타이머는 계속 돌려야 한다 —
                // 멈추면 봉인 해제 직후 확정 타격이 나가 원본보다 가혹해진다.
                if (!mAttackSuppressed && GetPlanarDistance(playerCenter) <= stats.AttackRange)
                {
                    player.ApplyDamage(Entity, stats.AttackDamage);
                }
            }
        }

        // ─── 디버프 ───

        private void UpdateDebuffs(float elapseSeconds)
        {
            if (mDying)
            {
                ClearAllDebuffs();
                return;
            }

            UpdateFrost(elapseSeconds);

            if (mFleeTimer > 0f)
            {
                mFleeTimer -= elapseSeconds;
            }

            UpdateSlime(elapseSeconds);
        }

        private void UpdateFrost(float elapseSeconds)
        {
            bool inCone = (Time.time - mFrostRefreshTime) <= FrostContactGrace;

            if (!mFrozen)
            {
                mFrostTimer = inCone ? mFrostTimer + elapseSeconds : 0f;
                if (inCone && mFrostTimer >= FrostFreezeDelay)
                {
                    Freeze();
                }
                return;
            }

            if (inCone)
            {
                mFrostTimer = 0f;
                return;
            }

            mFrostTimer += elapseSeconds;
            if (mFrostTimer >= FrostThawDuration)
            {
                Unfreeze();
            }
        }

        private void Freeze()
        {
            mFrozen     = true;
            mFrostTimer = 0f;

            SetSpeedMultiplier(0f);
            if (mAnimator != null)
            {
                mAnimator.enabled = false;
            }

            SetFxActive(mFreezeFx, true);
        }

        private void Unfreeze()
        {
            mFrozen     = false;
            mFrostTimer = 0f;

            SetSpeedMultiplier(1f);
            if (mAnimator != null)
            {
                mAnimator.enabled = true;
            }

            SetFxActive(mFreezeFx, false);
        }

        private void UpdateSlime(float elapseSeconds)
        {
            if (mSlimeTicksLeft <= 0)
            {
                return;
            }

            mSlimeTickTimer -= elapseSeconds;
            if (mSlimeTickTimer > 0f)
            {
                return;
            }

            mSlimeTicksLeft--;
            mSlimeTickTimer = mSlimeTickInterval;

            // 이미 죽었으면 TargetableObject.ApplyDamage 의 IsDead 가드가 무시한다.
            ApplyDamage(mSlimeAttacker, mSlimeTickDamage);

            if (mSlimeTicksLeft <= 0)
            {
                ClearSlime();
            }
        }

        private void ClearSlime()
        {
            mSlimeTicksLeft = 0;
            mSlimeTickTimer = 0f;
            mSlimeAttacker  = null;

            // 이걸 빼먹으면 한 번 걸린 적이 죽을 때까지 공격 불능이 된다(원본 버그).
            mAttackSuppressed = false;

            SetFxActive(mSlimeFx, false);
        }

        private void ClearAllDebuffs()
        {
            mFrozen           = false;
            mFrostTimer       = 0f;
            mFrostRefreshTime = -999f;
            mFleeTimer        = 0f;
            mSpeedMultiplier  = 1f;

            if (mAnimator != null)
            {
                mAnimator.enabled = true;
            }

            SetFxActive(mFreezeFx, false);
            ClearSlime();
        }

        private static void SetFxActive(GameObject fx, bool active)
        {
            if (fx != null && fx.activeSelf != active)
            {
                fx.SetActive(active);
            }
        }

        /// <summary>
        /// 추적 목적지. 도주 중이면 플레이어 반대 방향으로 달아난다.
        ///
        /// 원본 EnemyMovement 는 정규화된 방향 벡터를 그대로 월드 목적지로 써서
        /// 모든 적이 월드 원점 근처로 몰려가는 버그가 있었다. 그건 옮기지 않는다.
        /// </summary>
        private Vector3 GetDestination(Vector3 playerPosition)
        {
            if (mFleeTimer <= 0f)
            {
                return playerPosition;
            }

            Vector3 away = CachedTransform.position - playerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = CachedTransform.forward;
            }

            Vector3 destination = CachedTransform.position + away.normalized * FleeDistance;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(destination, out navHit, 4f, NavMesh.AllAreas))
            {
                return navHit.position;
            }

            // NavMesh 밖이면 도주를 포기한다. 아무 데도 못 가는 것보다 낫다.
            return playerPosition;
        }

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
            if (mPlayerDeadNotified || mAnimator == null)
            {
                return;
            }

            mPlayerDeadNotified = true;
            mAnimator.SetTrigger("PlayerDead");
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

        // ─── 사망 ───

        /// <summary>
        /// 주의: <c>base.OnDead</c> 를 부르지 않는다. base 는 즉시 SafeHide 하므로
        /// 연출이 통째로 생략되고, 연출 종료 시점의 Hide 와 겹쳐 중복 호출이 된다.
        /// </summary>
        protected override void OnDead(Entity attacker)
        {
            if (mDying)
            {
                return;
            }

            mDying      = true;
            mSinking    = false;
            mDeathTimer = 0f;

            // 침하 중에 에이전트가 Y 를 NavMesh 높이로 되끌어올리므로 먼저 끈다.
            DisableAgent();

            if (mBodyCollider != null)
            {
                mBodyCollider.enabled = false;
            }

            // 빙결 중에 죽으면 Animator 가 꺼져 있어 사망 애니메이션이 통째로 안 나온다.
            ClearAllDebuffs();

            if (mAnimator != null)
            {
                mAnimator.SetTrigger("Dead");
            }

            GameEntry.GetComponent<EventComponent>().Fire(this, EnemyDiedEventArgs.Create(
                mEnemyData.AssetName, mEnemyData.ScoreValue, CachedTransform.position));
        }

        /// <summary>사망 애니메이션 이벤트에서 호출된다(ZomBear 를 제외한 4종의 FBX 에 baked).</summary>
        private void StartSinking()
        {
            mSinking = true;
        }

        private void UpdateDeath(float elapseSeconds)
        {
            mDeathTimer += elapseSeconds;

            // ZomBear 의 FBX 에는 StartSinking 이벤트가 없다. 폴백으로 절반 지점부터 가라앉힌다.
            if (!mSinking && mDeathTimer >= EnemyTable.DeathEffectTime * 0.5f)
            {
                mSinking = true;
            }

            if (mSinking)
            {
                CachedTransform.Translate(Vector3.down * (EnemyTable.SinkSpeed * elapseSeconds), Space.World);
            }

            if (mDeathTimer >= EnemyTable.DeathEffectTime)
            {
                SafeHide();
            }
        }

        // ─── NavMeshAgent 헬퍼 ───

        private void PlaceOnNavMesh(Vector3 position)
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

            EnemyStats stats = mEnemyData.Stats;
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
            if (mAgent != null && mAgent.enabled && mAgent.isOnNavMesh)
            {
                mAgent.isStopped = true;
            }
        }

        private void DisableAgent()
        {
            if (mAgent == null || !mAgent.enabled)
            {
                return;
            }

            if (mAgent.isOnNavMesh)
            {
                mAgent.isStopped = true;
                mAgent.ResetPath();
            }

            mAgent.enabled = false;
        }
    }
}
