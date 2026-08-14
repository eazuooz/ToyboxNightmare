using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 투사체 엔티티. 방향으로 이동하다 적에 닿으면 데미지 후 소멸.
    /// </summary>
    public class Projectile : EntityLogicBase
    {
        /// <summary>forward 에 이보다 짧은 벡터를 넣으면 Unity 가 에러를 뱉는다.</summary>
        private const float MinDirectionSqrMagnitude = 1e-6f;

        private ProjectileData mData = null;
        private float mElapsed = 0f;

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mData = userData as ProjectileData;
            if (mData == null)
            {
                Log.Error("Projectile data is invalid.");
                return;
            }

            CachedTransform.position = mData.Position;
            AimForward(mData.Direction);
            mElapsed = 0f;
        }

        /// <summary>
        /// 진행 방향을 바라보게 한다. 길이 0 벡터는 넘기지 않는다 —
        /// 발사 순간 적과 완전히 겹치면 방향이 0 이 되는데, 그대로 넣으면
        /// Unity 가 에러를 내고 회전은 어차피 그대로 남는다(건너뛰는 것과 결과가 같다).
        /// </summary>
        private void AimForward(Vector3 direction)
        {
            if (direction.sqrMagnitude < MinDirectionSqrMagnitude) return;

            CachedTransform.forward = direction;
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (mData == null || IsHiding) return;

            mElapsed += elapseSeconds;

            bool hasExpired = mElapsed >= mData.Lifetime;
            if (hasExpired)
            {
                SafeHide();
                return;
            }

            CachedTransform.position += mData.Direction * mData.Speed * elapseSeconds;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 한 물리 스텝에 여러 콜라이더와 겹치면 이 콜백이 연속 호출된다.
            // 가드가 없으면 두 번째 적에게도 데미지가 들어가고 Hide 가 중복된다.
            if (Entity == null || mData == null || IsHiding) return;

            Enemy enemy = ResolveEnemy(other);
            if (enemy == null) return;

            enemy.ApplyDamage(Entity, mData.Damage);
            SafeHide();
        }

        /// <summary>부딪힌 콜라이더가 적 엔티티면 그 로직을, 아니면 null 을 돌려준다.</summary>
        private static Enemy ResolveEnemy(Collider other)
        {
            if (other == null) return null;

            Entity otherEntity = other.GetComponent<Entity>();
            if (otherEntity == null) return null;

            return otherEntity.Logic as Enemy;
        }
    }
}
