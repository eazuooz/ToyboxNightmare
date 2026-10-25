using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>수명이 지나면 스스로 회수되는 일회성 이펙트(<see cref="HitEffectLogic"/>)의 데이터.</summary>
    public class HitEffectData : EntityData
    {
        /// <summary>
        /// 호출부가 수명을 지정하지 않았을 때 쓸 값. 원본 파티클(0.5~0.75초)이 다 끝나고도 남는 길이다.
        /// 이펙트별 실제 수명은 무기 데이터가 정해 SpawnEffect 가 항상 명시하므로,
        /// 이 값은 그 경로를 거치지 않는 스폰에만 적용된다.
        /// </summary>
        public const float DefaultLifetime = 2f;

        public static HitEffectData Create(int entityId, int typeId,
                                           Vector3 position, Quaternion rotation, float lifetime)
        {
            HitEffectData data = ReferencePool.Acquire<HitEffectData>();
            data.FillEffect(entityId, typeId, position, rotation, lifetime);

            return data;
        }

        /// <summary>이 시간이 지나면 회수된다.</summary>
        public float Lifetime { get; private set; } = DefaultLifetime;

        /// <summary>파생 데이터의 Create 가 공통 부분을 채운다.</summary>
        protected void FillEffect(int entityId, int typeId, Vector3 position, Quaternion rotation, float lifetime)
        {
            Fill(entityId, typeId);

            Position = position;
            Rotation = rotation;
            Lifetime = lifetime;
        }

        public override void Clear()
        {
            base.Clear();

            // 필드 초기화식은 첫 new 때만 돈다. 기본값 복원도 여기서 해야 한다.
            Lifetime = DefaultLifetime;
        }
    }
}
