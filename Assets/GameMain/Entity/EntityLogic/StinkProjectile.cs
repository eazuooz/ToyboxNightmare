using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취 투사체. 물리를 쓰지 않는다 — 직선 등속으로 나아가면서
    /// 진행률에 따른 커브 값을 Y 에 더해 포물선처럼 보이게 한다(원본과 동일).
    ///
    /// 원본에는 "무엇에든 닿으면 터지는" 트리거가 있었지만 이식하지 않는다.
    /// 이식본은 플레이어 콜라이더가 총구 앞에 있어 발사 즉시 자폭한다.
    /// </summary>
    public class StinkProjectile : ProjectileLogicBase
    {
        /// <summary>원본 프리팹의 arc 커브. 최고점 1.0 유닛 @ 진행률 0.80.</summary>
        private static readonly AnimationCurve Arc = new AnimationCurve(
            new Keyframe(0.00000095f, 0.0000138f,  1.250883f,     1.250883f),
            new Keyframe(0.7994252f,  1f,         -0.060324907f, -0.060324907f),
            new Keyframe(0.8967965f,  0.85f,      -4.88826f,     -4.88826f),
            new Keyframe(0.9999976f,  0.0000331f, -8.236026f,    -8.236026f));

        /// <summary>속도가 0 이하로 들어오면 영원히 도착하지 못한다.</summary>
        private const float MinSpeed = 0.1f;

        /// <summary>
        /// 이보다 짧으면 방향 정규화도 진행률 계산도 의미가 없다(0 나눗셈). 즉시 착탄시킨다.
        /// </summary>
        private const float MinFlightDistance = 0.01f;

        /// <summary>비행 예정 시간에 얹는 여유. 이 시간이 지나면 베이스가 강제로 회수한다.</summary>
        private const float LifetimeMargin = 0.5f;

        /// <summary>
        /// 루트가 X -90° 로 저작돼 있다. identity 로 두면 가스 구름이 눕는다.
        /// </summary>
        private static readonly Quaternion HitRotation = Quaternion.Euler(-90f, 0f, 0f);

        private Vector3 mStart     = Vector3.zero;
        private Vector3 mPath      = Vector3.zero;
        /// <summary>mPath 의 단위 벡터. 발사 후 변하지 않으므로 매 프레임 정규화하지 않는다.</summary>
        private Vector3 mDirection = Vector3.forward;
        private float   mTotal     = 0f;
        private float   mTraveled  = 0f;
        private float   mSpeed     = WeaponTable.StinkSpeed;

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            var data = userData as ArcProjectileData;
            if (data == null)
            {
                Log.Error("StinkProjectile data is invalid.");
                SafeHide();
                return;
            }

            mStart     = data.Position;
            mSpeed     = Mathf.Max(MinSpeed, data.Speed);
            mPath      = data.ImpactPoint - mStart;
            mTotal     = mPath.magnitude;
            mDirection = Vector3.forward;
            mTraveled  = 0f;

            // 아래 계산이 전부 mTotal 로 나누므로, 0 나눗셈이 될 거리는 여기서 걸러낸다.
            if (mTotal < MinFlightDistance)
            {
                Explode();
                return;
            }

            mDirection  = mPath / mTotal;
            MaxLifetime = mTotal / mSpeed + LifetimeMargin;

            CachedTransform.position = mStart;
            CachedTransform.rotation = Quaternion.identity;

            PlayEffects();
        }

        protected override void OnFly(float elapseSeconds)
        {
            mTraveled += mSpeed * elapseSeconds;

            CachedTransform.position = GetArcPosition(mTraveled);

            bool hasArrived = mTraveled >= mTotal;
            if (hasArrived)
            {
                Explode();
            }
        }

        /// <summary>진행 거리에 대응하는 궤적 위의 한 점 — 직선 위치에 진행률 커브 높이를 얹은 것.</summary>
        private Vector3 GetArcPosition(float traveled)
        {
            float progress = Mathf.Clamp01(traveled / mTotal);

            Vector3 position = mStart + mDirection * traveled;
            position.y += Arc.Evaluate(progress);
            return position;
        }

        private void Explode()
        {
            if (IsHiding) return;

            SpawnEffect(typeof(StinkHit), WeaponTable.StinkHitAsset,
                GetImpactPoint(), HitRotation, WeaponTable.StinkHitLifetime);

            SafeHide();
        }

        /// <summary>가스 구름이 설 지면 좌표. 발사 시점에 확정된 착탄점을 Y=0 으로 눌러 쓴다.</summary>
        private Vector3 GetImpactPoint()
        {
            Vector3 impact = mStart + mPath;
            impact.y = 0f;
            return impact;
        }
    }
}
