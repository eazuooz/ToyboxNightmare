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

        private float mSpeedMultiplier = 1f;

        /// <summary>이동 속도 배율을 설정한다. 빙결 계열 디버프의 수신부.</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            mSpeedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
        }

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
            mSpeedMultiplier = 1f;
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
                CachedTransform.position += toPlayer.normalized * mEnemyData.MoveSpeed * mSpeedMultiplier * elapseSeconds;
                CachedTransform.forward = toPlayer.normalized;
            }
            else
            {
                // 공격 범위 안 - 공격 타이머
                mAttackTimer += elapseSeconds;
                if (mAttackTimer >= AttackInterval)
                {
                    mAttackTimer = 0f;
                    // 플레이어 피격은 전투 복원(M2) 시 여기에 구현한다.
                    // player.ApplyDamage(Entity, mEnemyData.AttackDamage);
                }
            }
        }

        // 주의: 사망 연출(Die 트리거 → 침하 → 제거)을 도입할 때는 base.OnDead 를
        // 호출하면 안 된다. base 가 즉시 SafeHide 하므로 연출이 생략되고,
        // 연출 종료 시점의 Hide 와 합쳐져 중복 호출이 된다.
        protected override void OnDead(Entity attacker)
        {
            base.OnDead(attacker);  // 현재는 즉시 SafeHide
        }
    }
}
