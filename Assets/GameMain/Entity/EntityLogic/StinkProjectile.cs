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

        private Vector3 mStart    = Vector3.zero;
        private Vector3 mPath     = Vector3.zero;
        private float   mTotal    = 0f;
        private float   mTraveled = 0f;
        private float   mSpeed    = WeaponTable.StinkSpeed;

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

            mStart    = data.Position;
            mSpeed    = Mathf.Max(0.1f, data.Speed);
            mPath     = data.ImpactPoint - mStart;
            mTotal    = mPath.magnitude;
            mTraveled = 0f;

            if (mTotal < 0.01f)
            {
                Explode();
                return;
            }

            MaxLifetime = mTotal / mSpeed + 0.5f;

            CachedTransform.position = mStart;
            CachedTransform.rotation = Quaternion.identity;

            PlayEffects();
        }

        protected override void OnFly(float elapseSeconds)
        {
            mTraveled += mSpeed * elapseSeconds;

            float t = Mathf.Clamp01(mTraveled / mTotal);
            Vector3 position = mStart + mPath.normalized * mTraveled;
            position.y += Arc.Evaluate(t);
            CachedTransform.position = position;

            if (mTraveled >= mTotal)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (IsHiding)
            {
                return;
            }

            Vector3 impact = mStart + mPath;
            impact.y = 0f;

            // 루트가 X -90° 로 저작돼 있다. identity 로 두면 가스 구름이 눕는다.
            SpawnEffect(typeof(StinkHit), WeaponTable.StinkHitAsset,
                impact, Quaternion.Euler(-90f, 0f, 0f), WeaponTable.StinkHitLifetime);

            SafeHide();
        }
    }
}
