using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 투사체형 무기. 가장 가까운 적을 향해 투사체를 발사한다.
    /// </summary>
    public class ProjectileWeapon : WeaponBase
    {
        private const string ProjectileAssetPath = "Projectile";
        private const float DetectRadius = 20f;

        /// <summary>방향 벡터로 쓰기엔 너무 짧다고 볼 제곱 길이. 정규화하면 0 벡터가 나온다.</summary>
        private const float MinDirectionSqrMagnitude = 0.0001f;

        [SerializeField] private int   damage   = 25;
        [SerializeField] private float speed    = 10f;
        [SerializeField] private float lifetime = 3f;

        public int   Damage   { get => damage;   set => damage   = value; }
        public float Speed    { get => speed;    set => speed    = value; }
        public float Lifetime { get => lifetime; set => lifetime = value; }

        protected override void Attack()
        {
            Enemy nearest = FindNearestEnemy(DetectRadius);
            if (nearest == null) return;

            Vector3 toTarget = nearest.CachedTransform.position - Owner.CachedTransform.position;
            if (toTarget.sqrMagnitude < MinDirectionSqrMagnitude)
            {
                // 완전히 겹쳐 있다. 정규화해도 0 벡터라 제자리에 멈춘 투사체가 나온다.
                return;
            }

            Vector3 direction = toTarget.normalized;

            int id = EntitySerialId.Next();
            try
            {
                GameEntry.GetComponent<EntityComponent>().ShowEntity(
                    id,
                    typeof(Projectile),
                    ProjectileAssetPath,
                    WeaponTable.ProjectileGroup,
                    new ProjectileData(id, 1)
                    {
                        Position  = Owner.CachedTransform.position,
                        Direction = direction,
                        Damage    = damage,
                        Speed     = speed,
                        Lifetime  = lifetime
                    });
            }
            catch (System.Exception ex)
            {
                Log.Warning("ProjectileWeapon skipped (prefab not ready): {0}", ex.Message);
            }
        }
    }
}
