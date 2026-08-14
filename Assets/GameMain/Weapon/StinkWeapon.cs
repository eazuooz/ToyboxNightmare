using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취. 포물선으로 던져 착탄 지점 주변의 적을 도주시킨다. <b>데미지는 0이다</b>.
    /// 원본은 마우스로 지면을 조준하고 레티클이 3색으로 바뀌었지만,
    /// 자동발사에서는 최근접 적의 발밑을 착탄점으로 잡는다.
    /// </summary>
    public class StinkWeapon : WeaponBase
    {
        /// <summary>
        /// 사거리 안에 아무도 없었을 때 다시 시도하기까지의 시간.
        /// 쿨다운을 통째로 소모하지 않고 짧게 되돌려, 적이 들어오는 즉시 던지게 한다.
        /// </summary>
        private const float MissRetryDelay = 0.5f;

        protected override string VfxRootPath => "Antenna/StinkAttack";

        // 마우스 조준 링을 "지금 노리는 적" 마커로 전용한다.
        protected override string TargetRingPath => "Antenna/StinkAttack/TargetRing";

        /// <summary>단일 대상 — 착탄점을 잡을 최근접 적 하나.</summary>
        protected override float AttackRadius => WeaponTable.StinkDetectRadius;

        /// <summary>
        /// 총구("Antenna") 해석과 폴백은 베이스의 <see cref="WeaponBase.MuzzleOrigin"/> 이 맡는다.
        /// Root null 가드도 <see cref="WeaponBase.Initialize"/> 가 처리하므로 여기엔 없다.
        /// </summary>
        protected override void OnInitialize()
        {
            AttackInterval = WeaponTable.StinkCooldown;
        }

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(WeaponTable.StinkDetectRadius);
            if (target == null)
            {
                RetryAfter(MissRetryDelay);
                return;
            }

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("StinkWeapon: EntityComponent 를 찾지 못했다. 투사체를 띄울 수 없다.");
                return;
            }

            Vector3 origin = MuzzleOrigin;

            // 착탄점은 발사 시점에 확정한다. 매 프레임 재조준하면 아치가 깨진다.
            Vector3 impact = target.CachedTransform.position;
            impact.y = 0f;

            int id = EntitySerialId.Next();
            entityComponent.ShowEntity(
                id,
                typeof(StinkProjectile),
                WeaponTable.StinkProjectileAsset,
                WeaponTable.ProjectileGroup,
                ArcProjectileData.Create(id, 1, origin, impact, WeaponTable.StinkSpeed));
        }
    }
}
