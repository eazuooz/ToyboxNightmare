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
    public class SlimeProjectile : ProjectileLogicBase
    {
        private int   mTargetId   = 0;
        private int   mAttackerId = 0;
        private float mSpeed      = WeaponTable.SlimeSpeed;
        private float mHitRadius  = WeaponTable.SlimeHitRadius;

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            var data = userData as HomingProjectileData;
            if (data == null)
            {
                Log.Error("SlimeProjectile data is invalid.");
                SafeHide();
                return;
            }

            mTargetId   = data.TargetEntityId;
            mAttackerId = data.AttackerEntityId;
            mSpeed      = Mathf.Max(0.1f, data.Speed);
            mHitRadius  = Mathf.Max(0.1f, data.HitRadius);

            // 원본에는 수명도 최대 사거리도 없다. 유실 방지 상한을 반드시 둔다.
            MaxLifetime = 3f;

            CachedTransform.position = data.Position;
            CachedTransform.rotation = data.Rotation;

            PlayEffects();
        }

        protected override void OnFly(float elapseSeconds)
        {
            Enemy target = ResolveTarget();
            if (target == null)
            {
                // 원본은 죽은 적을 계속 쫓아가 시체에 디버프를 붙였다. 그건 따라하지 않는다.
                SafeHide();
                return;
            }

            Vector3 aim = target.CachedTransform.position + Vector3.up * 0.5f;

            CachedTransform.LookAt(aim);
            CachedTransform.Translate(0f, 0f, mSpeed * elapseSeconds);

            if (Vector3.Distance(CachedTransform.position, aim) <= mHitRadius)
            {
                Explode(target);
            }
        }

        private Enemy ResolveTarget()
        {
            var entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null || !entityComponent.HasEntity(mTargetId))
            {
                return null;
            }

            Entity entity = entityComponent.GetEntity(mTargetId);
            if (entity == null)
            {
                return null;
            }

            Enemy enemy = entity.Logic as Enemy;
            if (enemy == null || enemy.IsDead || !enemy.Available)
            {
                return null;
            }

            return enemy;
        }

        private void Explode(Enemy target)
        {
            if (IsHiding)
            {
                return;
            }

            SpawnEffect(typeof(HitEffect), WeaponTable.SlimeHitAsset,
                CachedTransform.position, Quaternion.identity, WeaponTable.SlimeHitLifetime);

            Entity attacker = null;
            var entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent != null && mAttackerId != 0 && entityComponent.HasEntity(mAttackerId))
            {
                attacker = entityComponent.GetEntity(mAttackerId);
            }

            target.ApplySlime(
                WeaponTable.SlimeTicks,
                WeaponTable.SlimeTickInterval,
                WeaponTable.SlimeTickDamage,
                attacker);

            SafeHide();
        }
    }
}
