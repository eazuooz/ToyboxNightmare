using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>수명이 지나면 스스로 회수되는 일회성 이펙트 엔티티용.</summary>
    public class EffectData : EntityData
    {
        /// <summary>
        /// 호출부가 수명을 지정하지 않았을 때 쓸 값.
        /// 이펙트별 실제 수명은 WeaponTable 에 종류별로 따로 있고 SpawnEffect 가 항상 명시하므로,
        /// 이 값은 그 경로를 거치지 않는 스폰에만 적용된다.
        /// </summary>
        private const float DefaultLifetime = 2f;

        public static EffectData Create(int entityId, int typeId,
                                        Vector3 position, Quaternion rotation, float lifetime)
        {
            EffectData data = ReferencePool.Acquire<EffectData>();
            data.Fill(entityId, typeId);

            data.Position = position;
            data.Rotation = rotation;
            data.Lifetime = lifetime;

            return data;
        }

        /// <summary>이 시간이 지나면 회수된다.</summary>
        public float Lifetime { get; set; } = DefaultLifetime;

        public override void Clear()
        {
            base.Clear();

            // 필드 초기화식은 첫 new 때만 돈다. 기본값 복원도 여기서 해야 한다.
            Lifetime = DefaultLifetime;
        }
    }
}
