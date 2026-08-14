using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 적에게 걸리는 디버프 3종 — Frost(빙결) / Stink(도주) / Slime(공격봉인 DoT) — 의
    /// 상태와 전이. 추격·공격·사망 연출은 <see cref="Enemy"/> 쪽에 남아 있다.
    ///
    /// AttachEntity 대신 Enemy 내부 상태로 관리한다. 엔티티가 회수되면 상태도 함께
    /// 사라지므로 "주인 잃은 디버프" 가 원천적으로 생기지 않는다.
    ///
    /// <b>MonoBehaviour 가 아니다.</b> 소유 <see cref="Enemy"/> 가 인스턴스를 만들어 들고
    /// <c>Enemy.OnUpdate</c> 가 직접 <see cref="OnUpdate"/> 를 굴린다 — 이유는
    /// <see cref="WeaponBase"/> 클래스 주석과 같다(Unity 메시지 비-virtual, 갱신 순서 미보장,
    /// 풀 재사용과 불화).
    ///
    /// 이 객체 자체는 엔티티 풀 재사용 사이에 살아남는다. 그래서 <c>Enemy.OnShow</c> 가
    /// <see cref="ClearAll"/> 로 이전 판의 상태를 되돌린다.
    /// </summary>
    public class EnemyDebuffs
    {
        // ─── 튜닝 상수 ───

        private const float FrostContactGrace = 0.35f; // 재판정 틱(0.2)보다 커야 빙결이 깜빡이지 않는다
        private const float FrostFreezeDelay  = 1f;    // 원본 freezeDelay
        private const float FrostThawDuration = 2f;    // 원본 freezeDuration

        /// <summary>
        /// "한 번도 Frost 콘에 닿은 적 없음" 을 뜻하는 접촉 시각.
        /// Time.time 이 0 에 가까운 스폰 직후에도 grace 밖으로 판정되도록 충분히 과거여야 한다.
        /// </summary>
        private const float NoFrostContactTime = -999f;

        /// <summary>슬라임 틱 간격의 하한. 0 이하가 들어오면 매 프레임 틱이 나가 버린다.</summary>
        private const float MinSlimeTickInterval = 0.05f;

        /// <summary>
        /// "공격자를 모른다" 를 뜻하는 엔티티 Id. <see cref="EntitySerialId"/> 의 첫 발급이 1 이라
        /// 0 은 어떤 엔티티도 갖지 않는다.
        /// </summary>
        private const int NoAttackerId = 0;

        /// <summary>디버프가 걸리는 대상. 생성 시점에 정해지고 바뀌지 않는다.</summary>
        private readonly Enemy mOwner = null;

        // ─── 디버프 상태 ───

        // Frost
        private bool  mFrozen           = false;
        private float mFrostTimer       = 0f;
        private float mFrostRefreshTime = NoFrostContactTime;

        // 해동 대기가 시작됐는지. 원본은 콘에서 한 번 벗어나면 재진입해도 해동을 취소하지
        // 않는다(FrostDebuff.AttachToEnemy 의 `if (target != null) return;` 때문).
        // 그 동작을 그대로 재현한다 — 없으면 자동조준 특성상 무한 빙결이 된다.
        private bool mFrostThawing = false;

        // Stink
        private float mFleeTimer = 0f;

        // Slime. 아래 간격/데미지는 ApplySlime 이 매번 덮어쓰므로 초기값 자체에 의미는 없다.
        private bool  mAttackSuppressed  = false;
        private int   mSlimeTicksLeft    = 0;
        private float mSlimeTickTimer    = 0f;
        private float mSlimeTickInterval = 0.5f;
        private int   mSlimeTickDamage   = 20;

        /// <summary>
        /// DoT 를 넣은 공격자를 <b>참조가 아니라 Id 로</b> 들고 있다. DoT 는 최대 3초를 끄는데
        /// 그 사이 발사체 엔티티는 회수되고 같은 인스턴스가 다른 발사체로 재사용될 수 있다.
        /// 참조로 들면 마지막 틱이 무관한 엔티티를 공격자로 넘긴다.
        /// (SlimeProjectile 이 대상 추적에 같은 이유로 Id 를 쓴다.)
        /// </summary>
        private int mSlimeAttackerId = NoAttackerId;

        /// <summary>
        /// 소유자는 <c>Enemy.OnInit</c> 에서 한 번만 넘어온다. this 를 넘겨야 해서
        /// 필드 초기화자로는 만들 수 없다(C# 은 필드 초기화자에서 this 를 못 쓴다).
        /// </summary>
        public EnemyDebuffs(Enemy owner)
        {
            GameAssert.IsTrue(owner != null, "EnemyDebuffs 는 소유 Enemy 없이 만들 수 없다.");

            mOwner = owner;
        }

        // ─── 상태 질의 ───

        /// <summary>
        /// 무력화 상태 — 얼었거나, 도주 중이거나, 공격이 봉인됐다.
        /// 셋 중 하나라도 걸려 있으면 당장 플레이어를 때리지 못한다.
        /// </summary>
        public bool IsNeutralized => mFrozen || IsFleeing || mAttackSuppressed;

        /// <summary>Stink 를 맞고 달아나는 중인지.</summary>
        public bool IsFleeing => mFleeTimer > 0f;

        /// <summary>Slime 에 걸려 공격이 봉인됐는지. 이동과 타이머는 그대로 돈다.</summary>
        public bool IsAttackSuppressed => mAttackSuppressed;

        /// <summary>지금 Frost 콘 안에 있는지. 콘이 grace 안에 접촉을 갱신해 주는 동안만 참이다.</summary>
        private bool IsInFrostCone => (Time.time - mFrostRefreshTime) <= FrostContactGrace;

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
            if (ticks <= 0) return;

            GameAssert.IsTrue(tickInterval > 0f, "Slime 의 tickInterval 은 양수여야 한다.");
            GameAssert.IsTrue(damagePerTick >= 0, "Slime 의 damagePerTick 이 음수면 적을 회복시킨다.");

            mSlimeTicksLeft    = ticks;
            mSlimeTickInterval = Mathf.Max(MinSlimeTickInterval, tickInterval);
            mSlimeTickDamage   = damagePerTick;
            mSlimeTickTimer    = 0f;
            mAttackSuppressed  = true;

            // 참조가 아니라 Id 만 남긴다(mSlimeAttackerId 주석 참고).
            mSlimeAttackerId = IsUsableAttacker(attacker) ? attacker.Id : NoAttackerId;

            mOwner.SetSlimeFxActive(true);
        }

        /// <summary>
        /// 회수된 엔티티를 들고 있지 않는다. Available 은 Entity 가 아니라 Logic 쪽에 있다.
        /// </summary>
        private static bool IsUsableAttacker(Entity attacker)
        {
            return attacker != null && attacker.Logic != null && attacker.Logic.Available;
        }

        /// <summary>
        /// 틱 시점의 공격자. Id 로 다시 조회하므로 그 사이 회수된 엔티티를 넘기지 않는다.
        /// 못 찾으면 null 이다 — <b>데미지는 그대로 들어간다</b>. attacker 는 집계용 정보일 뿐이라
        /// 공격자를 잃었다고 DoT 를 끊으면 원본보다 약해진다.
        /// </summary>
        private Entity ResolveSlimeAttacker()
        {
            if (mSlimeAttackerId == NoAttackerId) return null;

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null || !entityComponent.HasEntity(mSlimeAttackerId)) return null;

            Entity attacker = entityComponent.GetEntity(mSlimeAttackerId);
            return IsUsableAttacker(attacker) ? attacker : null;
        }

        // ─── 갱신 ───

        /// <summary>
        /// <c>Enemy.OnUpdate</c> 가 매 프레임 부른다. 호출 위치(IsHiding 검사 뒤, 사망 연출 갱신 앞)의
        /// 근거는 그 호출부 주석에 있다.
        /// </summary>
        public void OnUpdate(float elapseSeconds)
        {
            if (mOwner.IsDying)
            {
                ClearAll();
                return;
            }

            UpdateFrost(elapseSeconds);
            UpdateFlee(elapseSeconds);
            UpdateSlime(elapseSeconds);
        }

        private void UpdateFrost(float elapseSeconds)
        {
            bool inCone = IsInFrostCone;

            if (!mFrozen)
            {
                UpdateFreezeCharge(inCone, elapseSeconds);
                return;
            }

            UpdateThaw(inCone, elapseSeconds);
        }

        /// <summary>빙결 전. 콘 안에 <b>연속으로</b> 머문 시간이 freezeDelay 를 넘어야 언다.</summary>
        private void UpdateFreezeCharge(bool inCone, float elapseSeconds)
        {
            if (!inCone)
            {
                // 벗어나면 누적이 0 으로 돌아간다 — 끊긴 노출은 합산하지 않는다.
                mFrostTimer = 0f;
                return;
            }

            mFrostTimer += elapseSeconds;
            if (mFrostTimer >= FrostFreezeDelay)
            {
                Freeze();
            }
        }

        /// <summary>
        /// 빙결 후. 콘 안에 머무는 동안은 계속 얼어 있고, 한 번 벗어나면 해동이 확정된다.
        /// 해동이 시작된 뒤로는 inCone 을 보지 않는다 — 재진입해도 예정대로 진행된다.
        /// (자세한 근거는 mFrostThawing 필드 주석 참고)
        /// </summary>
        private void UpdateThaw(bool inCone, float elapseSeconds)
        {
            if (!mFrostThawing)
            {
                // 콘 안에 머무는 동안은 계속 얼어 있다.
                if (inCone) return;

                // 벗어나는 순간 해동이 확정된다.
                mFrostThawing = true;
                mFrostTimer   = 0f;
            }

            mFrostTimer += elapseSeconds;
            if (mFrostTimer >= FrostThawDuration)
            {
                Unfreeze();
            }
        }

        private void Freeze()
        {
            mFrozen       = true;
            mFrostThawing = false;
            mFrostTimer   = 0f;

            mOwner.SetSpeedMultiplier(0f);
            mOwner.SetAnimatorEnabled(false);

            mOwner.SetFreezeFxActive(true);
        }

        private void Unfreeze()
        {
            mFrozen       = false;
            mFrostThawing = false;
            mFrostTimer   = 0f;

            mOwner.SetSpeedMultiplier(1f);
            mOwner.SetAnimatorEnabled(true);

            mOwner.SetFreezeFxActive(false);
        }

        /// <summary>Stink 도주 지속시간 소진. 0 이하로 내려가면 IsFleeing 이 자연히 꺼진다.</summary>
        private void UpdateFlee(float elapseSeconds)
        {
            if (!IsFleeing) return;

            mFleeTimer -= elapseSeconds;
        }

        private void UpdateSlime(float elapseSeconds)
        {
            if (mSlimeTicksLeft <= 0) return;

            mSlimeTickTimer -= elapseSeconds;
            if (mSlimeTickTimer > 0f) return;

            mSlimeTicksLeft--;
            mSlimeTickTimer = mSlimeTickInterval;

            // 이미 죽었으면 TargetableObject.ApplyDamage 의 IsDead 가드가 무시한다.
            // 이 한 방이 치명타면 OnDead → ClearAll → ClearSlime 으로 재진입해
            // mSlimeTicksLeft 를 0 으로 만든다. 아래 마무리는 그래도 안전하도록 멱등이다.
            mOwner.ApplyDamage(ResolveSlimeAttacker(), mSlimeTickDamage);

            if (mSlimeTicksLeft <= 0)
            {
                ClearSlime();
            }
        }

        private void ClearSlime()
        {
            mSlimeTicksLeft  = 0;
            mSlimeTickTimer  = 0f;
            mSlimeAttackerId = NoAttackerId;

            // 이걸 빼먹으면 한 번 걸린 적이 죽을 때까지 공격 불능이 된다(원본 버그).
            mAttackSuppressed = false;

            mOwner.SetSlimeFxActive(false);
        }

        /// <summary>
        /// 디버프 전량 해제. 풀 재사용(<c>Enemy.OnShow</c>)과 사망(<c>Enemy.OnDead</c>) 양쪽에서 부른다.
        /// </summary>
        public void ClearAll()
        {
            mFrozen           = false;
            mFrostThawing     = false;
            mFrostTimer       = 0f;
            mFrostRefreshTime = NoFrostContactTime;
            mFleeTimer        = 0f;

            mOwner.SetSpeedMultiplier(1f);
            mOwner.SetAnimatorEnabled(true);

            mOwner.SetFreezeFxActive(false);
            ClearSlime();
        }
    }
}
