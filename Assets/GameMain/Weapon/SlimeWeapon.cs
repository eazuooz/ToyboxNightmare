using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 슬라임 무기. Fire1 을 뗄 때 가장 가까운 적을 향해 투사체를 발사한다.
    /// 샘플의 SlimeAttack 에 대응한다.
    /// </summary>
    public class SlimeWeapon : WeaponBase
    {
        private const string ProjectileAssetPath = "Projectile";

        [SerializeField] private int   damage          = 30;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float lifetime        = 4f;
        [SerializeField] private float cooldown        = 3.5f;

        private float mLastFireTime = -10f;

        public override void OnFireStop()
        {
            if (Owner == null || Owner.IsDead) return;
            if (Time.time < mLastFireTime + cooldown) return;

            Enemy nearest = FindNearestEnemy(30f);
            if (nearest == null) return;

            Vector3 dir = (nearest.CachedTransform.position - Owner.CachedTransform.position).normalized;

            int id = EntitySerialId.Next();
            try
            {
                GameEntry.GetComponent<EntityComponent>().ShowEntity(
                    id,
                    typeof(Projectile),
                    ProjectileAssetPath,
                    "Projectile",
                    new ProjectileData(id, 1)
                    {
                        Position  = Owner.CachedTransform.position,
                        Direction = dir,
                        Damage    = damage,
                        Speed     = projectileSpeed,
                        Lifetime  = lifetime
                    });
            }
            catch (System.Exception ex)
            {
                Log.Warning("SlimeWeapon skipped (prefab not ready): {0}", ex.Message);
            }

            mLastFireTime = Time.time;
        }
    }
}
