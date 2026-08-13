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
        private const string MuzzlePath = "Antenna";

        private Transform mMuzzle = null;

        protected override void OnInitialize()
        {
            AttackInterval = WeaponTable.StinkCooldown;

            if (mMuzzle == null)
            {
                mMuzzle = transform.Find(MuzzlePath);
            }
        }

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(WeaponTable.StinkDetectRadius);
            if (target == null)
            {
                RetryAfter(0.5f);
                return;
            }

            Vector3 origin = mMuzzle != null
                ? mMuzzle.position
                : Owner.CachedTransform.position + Vector3.up;

            // 착탄점은 발사 시점에 확정한다. 매 프레임 재조준하면 아치가 깨진다.
            Vector3 impact = target.CachedTransform.position;
            impact.y = 0f;

            int id = EntitySerialId.Next();
            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(StinkProjectile),
                WeaponTable.StinkProjectileAsset,
                WeaponTable.ProjectileGroup,
                new ArcProjectileData(id, 1)
                {
                    Position    = origin,
                    ImpactPoint = impact,
                    Speed       = WeaponTable.StinkSpeed,
                });
        }
    }
}
