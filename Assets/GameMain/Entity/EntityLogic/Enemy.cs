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

        /// <summary>이동 속도 배율을 설정한다. 빙결 계열 디버프의 수신부(M4).</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            mSpeedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
        }

        // ─── 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mAgent        = GetComponent<NavMeshAgent>();
            mAnimator     = GetComponentInChildren<Animator>();
            mBodyCollider = GetComponent<CapsuleCollider>();

            // 적 프리팹 5종 모두 ParticleSystem 이 정확히 1개(HitParticles)뿐이다.
            mHitParticles = GetComponentInChildren<ParticleSystem>(true);

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
                    mAgent.SetDestination(playerPosition);
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

                if (GetPlanarDistance(playerCenter) <= stats.AttackRange)
                {
                    player.ApplyDamage(Entity, stats.AttackDamage);
                }
            }
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
