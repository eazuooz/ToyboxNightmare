using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취 무기. Fire1 을 뗄 때 마우스 위치에 범위 폭발 데미지를 준다.
    /// 샘플의 StinkAttack 에 대응한다.
    /// </summary>
    public class StinkWeapon : WeaponBase
    {
        [SerializeField] private int   damage          = 25;
        [SerializeField] private float throwRange      = 5f;   // 투척 최대 거리
        [SerializeField] private float explosionRadius = 3f;   // 폭발 반경
        [SerializeField] private float cooldown        = 5f;

        private float mLastFireTime = -10f;

        public override void OnFireStop()
        {
            if (Owner == null || Owner.IsDead) return;
            if (Time.time < mLastFireTime + cooldown) return;

            Vector3 targetPos = GetMouseWorldPosition();
            float dist = Vector3.Distance(targetPos, Owner.CachedTransform.position);
            if (dist > throwRange) return;

            Explode(targetPos);
            mLastFireTime = Time.time;
        }

        private void Explode(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, explosionRadius);
            foreach (Collider col in hits)
            {
                Entity entity = col.GetComponent<Entity>();
                if (entity?.Logic is Enemy enemy && !enemy.IsDead)
                    enemy.ApplyDamage(Owner.Entity, damage);
            }
        }
    }
}
