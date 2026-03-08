using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 범위형 무기. 플레이어 주변 적에게 범위 데미지를 준다.
    /// </summary>
    public class AreaWeapon : WeaponBase
    {
        [SerializeField] private int   damage = 15;
        [SerializeField] private float radius = 3f;

        protected override void Attack()
        {
            Collider[] hits = Physics.OverlapSphere(Owner.CachedTransform.position, radius);

            foreach (Collider col in hits)
            {
                Entity entity = col.GetComponent<Entity>();
                if (entity?.Logic is Enemy enemy && !enemy.IsDead)
                {
                    enemy.ApplyDamage(Owner.Entity, damage);
                }
            }
        }
    }
}
