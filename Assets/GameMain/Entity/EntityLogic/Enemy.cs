using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public class Enemy : TargetableObject
    {
        private EnemyData mEnemyData = null;

        private float mAttackTimer = 0f;
        private const float AttackInterval = 1f;   // 초당 1회 공격
        private const float AttackRange = 1.5f;    // 공격 사거리

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mEnemyData = userData as EnemyData;
            if (mEnemyData == null)
            {
                Log.Error("Enemy data is invalid.");
                return;
            }

            CachedTransform.position = mEnemyData.Position;
            CachedTransform.rotation = mEnemyData.Rotation;
            mAttackTimer = 0f;
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (IsDead) return;

            Player player = Player.Instance;
            if (player == null || player.IsDead) return;

            Vector3 toPlayer = player.CachedTransform.position - CachedTransform.position;
            float distance = toPlayer.magnitude;

            if (distance > AttackRange)
            {
                // 플레이어 추적
                CachedTransform.position += toPlayer.normalized * mEnemyData.MoveSpeed * elapseSeconds;
                CachedTransform.forward = toPlayer.normalized;
            }
            else
            {
                // 공격 범위 안 - 공격 타이머
                mAttackTimer += elapseSeconds;
                if (mAttackTimer >= AttackInterval)
                {
                    mAttackTimer = 0f;
                    player.TakeDamage(Entity, mEnemyData.AttackDamage);
                }
            }
        }

        protected override void OnDead(Entity attacker)
        {
            SurvivalGame.Instance?.SpawnExpGem(CachedTransform.position, mEnemyData.ExpReward);
            base.OnDead(attacker);  // HideEntity 호출
        }
    }
}
