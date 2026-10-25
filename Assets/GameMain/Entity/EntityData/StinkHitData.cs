using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취 가스 구름(<see cref="StinkHitLogic"/>)의 데이터. 수명과 배치는 <see cref="HitEffectData"/> 그대로이고,
    /// 가스가 닿는 반경과 도주 시간을 더한다. 투사체(<see cref="StinkProjectileData"/>)가 받아 온 값을 넘긴다.
    /// </summary>
    public class StinkHitData : HitEffectData
    {
        public static StinkHitData Create(int entityId, int typeId, Vector3 position, Quaternion rotation,
                                          float lifetime, float blastRadius, float fleeDuration)
        {
            StinkHitData data = ReferencePool.Acquire<StinkHitData>();
            data.FillEffect(entityId, typeId, position, rotation, lifetime);

            data.BlastRadius  = blastRadius;
            data.FleeDuration = fleeDuration;

            return data;
        }

        /// <summary>가스가 닿는 반경.</summary>
        public float BlastRadius { get; private set; }

        /// <summary>맞은 적이 달아나는 시간(초).</summary>
        public float FleeDuration { get; private set; }

        public override void Clear()
        {
            base.Clear();

            BlastRadius  = 0f;
            FleeDuration = 0f;
        }
    }
}
