using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 점액 투사체. 완전 유도라 물리적으로 빗나가지 않는다(원본 주석이 "can't miss").
    /// 명중 판정은 콜라이더가 아니라 거리로 한다 — 프리팹에 콜라이더가 아예 없다.
    ///
    /// 대상을 참조가 아니라 <b>엔티티 Id</b> 로 들고 있다. 참조로 들면 대상이 회수되어
    /// 다른 적으로 재사용됐을 때 조용히 엉뚱한 적을 쫓는다.
    /// </summary>
    public class SlimeProjectileLogic : ProjectileLogicBase
    {
        /// <summary>적 트랜스폼 원점이 발밑이라 몸통 높이까지 올려 조준한다.</summary>
        private const float AimHeightOffset = 0.5f;

        /// <summary>속도가 0 이하로 들어오면 제자리에 뜬 채 수명을 다 쓴다.</summary>
        private const float MinSpeed = 0.1f;

        /// <summary>명중 반경이 0 이하로 들어오면 영원히 명중 판정에 걸리지 않는다.</summary>
        private const float MinHitRadius = 0.1f;

        /// <summary>LookAt 에 이보다 짧은 벡터를 넘기면 Unity 가 매 프레임 에러를 뱉는다.</summary>
        private const float MinLookSqrDistance = 1e-6f;

        // 데이터의 속도·명중 반경에 하한을 건 값. 발사 후 변하지 않는다.
        private float mSpeed     = 0f;
        private float mHitRadius = 0f;

        /// <summary>스폰 데이터. 대상·공격자 Id 와 붙었을 때의 지속 피해를 여기서 읽는다. 회수된 뒤에는 null.</summary>
        private SlimeProjectileData mData = null;

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mData = userData as SlimeProjectileData;
            if (mData == null)
            {
                Log.Error("SlimeProjectileLogic: SlimeProjectileData 가 아니다.");
                SafeHide();
                return;
            }

            mSpeed     = Mathf.Max(MinSpeed, mData.Speed);
            mHitRadius = Mathf.Max(MinHitRadius, mData.HitRadius);

            // 원본에는 수명도 최대 사거리도 없다. 유실 방지 상한을 반드시 둔다.
            MaxLifetime = mData.MaxFlightSeconds;

            CachedTransform.position = mData.Position;
            CachedTransform.rotation = mData.Rotation;

            PlayEffects();
        }

        protected override void OnFly(float elapseSeconds)
        {
            EnemyLogic target = ResolveTarget();
            if (target == null)
            {
                // 원본은 죽은 적을 계속 쫓아가 시체에 디버프를 붙였다. 그건 따라하지 않는다.
                SafeHide();
                return;
            }

            Vector3 aimPoint = GetAimPoint(target);

            TurnToward(aimPoint);
            CachedTransform.Translate(0f, 0f, mSpeed * elapseSeconds);

            bool hasReachedTarget = Vector3.Distance(CachedTransform.position, aimPoint) <= mHitRadius;
            if (hasReachedTarget)
            {
                Explode(target);
            }
        }

        private static Vector3 GetAimPoint(EnemyLogic target)
        {
            return target.CachedTransform.position + Vector3.up * AimHeightOffset;
        }

        /// <summary>
        /// 조준점을 바라본다. 이미 조준점에 겹쳐 있으면 회전을 건드리지 않는다 —
        /// 길이 0 벡터를 LookAt 에 넘기면 Unity 가 "viewing vector is zero" 에러를 내고
        /// 회전은 어차피 그대로 남기 때문에, 건너뛰는 것과 결과가 같다.
        /// </summary>
        private void TurnToward(Vector3 aimPoint)
        {
            Vector3 toAimPoint = aimPoint - CachedTransform.position;
            if (toAimPoint.sqrMagnitude < MinLookSqrDistance) return;

            CachedTransform.LookAt(aimPoint);
        }

        private EnemyLogic ResolveTarget()
        {
            if (mData == null) return null;

            var entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null || !entityComponent.HasEntity(mData.TargetEntityId)) return null;

            Entity entity = entityComponent.GetEntity(mData.TargetEntityId);
            if (entity == null) return null;

            EnemyLogic enemy = entity.Logic as EnemyLogic;
            // Available 을 IsDead 보다 먼저 본다. IsDead 는 스폰 데이터를 읽는데,
            // 회수된 엔티티의 데이터는 이미 풀로 반납된 뒤라 답을 신뢰할 수 없다.
            if (enemy == null || !enemy.Available || enemy.IsDead) return null;

            return enemy;
        }

        /// <summary>
        /// DoT 를 누가 넣었는지 추적하기 위한 공격자. 이미 회수됐으면 null 이다 —
        /// 회수된 엔티티를 EnemyLogic 쪽에 넘기면 다음 판까지 물고 간다.
        /// </summary>
        private Entity ResolveAttacker()
        {
            if (mData == null || mData.AttackerEntityId == 0) return null;

            var entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null || !entityComponent.HasEntity(mData.AttackerEntityId)) return null;

            return entityComponent.GetEntity(mData.AttackerEntityId);
        }

        private void Explode(EnemyLogic target)
        {
            if (IsHiding) return;

            GameAssert.IsTrue(target != null, "SlimeProjectileLogic.Explode 는 살아 있는 대상만 받는다.");
            if (target == null)
            {
                SafeHide();
                return;
            }

            SpawnEffect(typeof(HitEffectLogic), mData.HitEffectAsset,
                CachedTransform.position, Quaternion.identity, mData.HitEffectLifetime);
            GameSound.PlaySfx(SoundTable.SlimeHit, CachedTransform.position);

            target.ApplySlime(
                mData.Ticks,
                mData.TickInterval,
                mData.TickDamage,
                ResolveAttacker());

            SafeHide();
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            // 데이터는 base 체인 끝에서 참조 풀로 반납된다. 반납된 것을 들고 있지 않는다.
            mData = null;

            base.OnHide(isShutdown, userData);
        }
    }
}
