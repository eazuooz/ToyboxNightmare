using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 서리 무기. Fire1 을 누른 채로 있는 동안 전방 콘 범위의 적을 지속적으로 둔화시키고 데미지를 준다.
    /// 샘플의 FrostAttack 에 대응한다.
    /// </summary>
    public class FrostWeapon : WeaponBase
    {
        [SerializeField] private float frostRange     = 8f;
        [SerializeField] private float frostAngle     = 90f;  // 전방 콘의 총 각도
        [SerializeField] private float slowMultiplier = 0.3f;
        [SerializeField] private int   damagePerSec   = 5;

        // Inspector 에서 선택적으로 연결
        [SerializeField] private GameObject frostConeEffect = null;

        private bool mIsFiring = false;
        private float mDamageTimer = 0f;
        private readonly List<Enemy> mFrostedEnemies = new List<Enemy>();

        private void Update()
        {
            if (!mIsFiring) return;

            UpdateFrostedEnemies();

            // 초당 데미지
            mDamageTimer += Time.deltaTime;
            if (mDamageTimer >= 1f)
            {
                mDamageTimer = 0f;
                foreach (var enemy in mFrostedEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                        enemy.ApplyDamage(Owner.Entity, damagePerSec);
                }
            }
        }

        public override void OnFireHeld()
        {
            if (Owner == null || Owner.IsDead) return;

            if (!mIsFiring)
            {
                mIsFiring = true;
                mDamageTimer = 0f;
                if (frostConeEffect != null) frostConeEffect.SetActive(true);
            }
        }

        public override void OnFireStop()
        {
            StopFrost();
        }

        private void OnDisable()
        {
            StopFrost();
        }

        private void UpdateFrostedEnemies()
        {
            var inCone = FindEnemiesInCone();

            // 콘 밖으로 나간 적 슬로우 해제
            for (int i = mFrostedEnemies.Count - 1; i >= 0; i--)
            {
                Enemy e = mFrostedEnemies[i];
                if (e == null || e.IsDead || !inCone.Contains(e))
                {
                    if (e != null && !e.IsDead) e.SetSpeedMultiplier(1f);
                    mFrostedEnemies.RemoveAt(i);
                }
            }

            // 콘 안에 새로 들어온 적 슬로우 적용
            foreach (Enemy e in inCone)
            {
                if (!mFrostedEnemies.Contains(e))
                {
                    e.SetSpeedMultiplier(slowMultiplier);
                    mFrostedEnemies.Add(e);
                }
            }
        }

        private void StopFrost()
        {
            mIsFiring = false;
            if (frostConeEffect != null) frostConeEffect.SetActive(false);

            foreach (Enemy e in mFrostedEnemies)
            {
                if (e != null && !e.IsDead) e.SetSpeedMultiplier(1f);
            }
            mFrostedEnemies.Clear();
        }

        private List<Enemy> FindEnemiesInCone()
        {
            var result = new List<Enemy>();
            if (Owner == null) return result;

            Collider[] hits = Physics.OverlapSphere(Owner.CachedTransform.position, frostRange);
            float halfAngle = frostAngle * 0.5f;

            foreach (Collider col in hits)
            {
                Entity entity = col.GetComponent<Entity>();
                if (entity?.Logic is Enemy enemy && !enemy.IsDead)
                {
                    Vector3 dir = (enemy.CachedTransform.position - Owner.CachedTransform.position).normalized;
                    if (Vector3.Angle(Owner.CachedTransform.forward, dir) <= halfAngle)
                        result.Add(enemy);
                }
            }

            return result;
        }
    }
}
