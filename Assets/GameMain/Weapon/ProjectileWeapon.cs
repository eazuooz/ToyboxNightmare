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

        [SerializeField] private int   damage   = 25;
        [SerializeField] private float speed    = 10f;
        [SerializeField] private float lifetime = 3f;

        public int   Damage   { get => damage;   set => damage   = value; }
        public float Speed    { get => speed;    set => speed    = value; }
        public float Lifetime { get => lifetime; set => lifetime = value; }

        protected override void Attack()
        {
            Enemy nearest = FindNearestEnemy();
            if (nearest == null) return;

            Vector3 dir = (nearest.CachedTransform.position - Owner.CachedTransform.position).normalized;

            int id = EntitySerialId.Next();
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
                    Speed     = speed,
                    Lifetime  = lifetime
                });
        }

        private Enemy FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(Owner.CachedTransform.position, DetectRadius);
            Enemy nearest = null;
            float minDist = float.MaxValue;

            foreach (Collider col in hits)
            {
                Entity entity = col.GetComponent<Entity>();
                if (entity?.Logic is Enemy enemy && !enemy.IsDead)
                {
                    float dist = Vector3.Distance(Owner.CachedTransform.position, enemy.CachedTransform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }
    }
}
