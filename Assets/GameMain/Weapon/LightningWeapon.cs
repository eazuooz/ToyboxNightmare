using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 번개 무기. Fire1 을 누르거나 누른 채로 있으면 쿨다운마다 전방으로 즉각 데미지를 준다.
    /// 샘플의 LightningAttack 에 대응한다.
    /// </summary>
    public class LightningWeapon : WeaponBase
    {
        [SerializeField] private int   damage   = 20;
        [SerializeField] private float range    = 20f;
        [SerializeField] private float cooldown = 1f;

        private float mLastFireTime = -10f;

        // Inspector 에서 선택적으로 연결 - 없으면 시각 효과만 없을 뿐 동작은 정상
        [SerializeField] private GameObject lightningEffect = null;

        public override void OnFireHeld()
        {
            if (Owner == null || Owner.IsDead) return;
            if (Time.time < mLastFireTime + cooldown) return;

            Fire();
        }

        private void Fire()
        {
            mLastFireTime = Time.time;

            // 전방 레이캐스트로 가장 먼저 맞는 적에게 즉시 데미지
            Ray ray = new Ray(Owner.CachedTransform.position, Owner.CachedTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                Entity entity = hit.collider.GetComponent<Entity>();
                if (entity?.Logic is Enemy enemy && !enemy.IsDead)
                    enemy.ApplyDamage(Owner.Entity, damage);
            }

            // 시각 효과
            if (lightningEffect != null)
            {
                lightningEffect.SetActive(true);
                Invoke(nameof(HideLightningEffect), 0.1f);
            }
        }

        private void HideLightningEffect()
        {
            if (lightningEffect != null)
                lightningEffect.SetActive(false);
        }
    }
}
