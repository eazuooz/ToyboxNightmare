using System;

namespace ToyBoxNightmare
{
    /// <summary>수명이 지나면 스스로 회수되는 일회성 이펙트 엔티티용.</summary>
    [Serializable]
    public class EffectData : EntityData
    {
        public EffectData(int entityId, int typeId) : base(entityId, typeId)
        {
        }

        /// <summary>이 시간이 지나면 회수된다.</summary>
        public float Lifetime { get; set; } = 2f;
    }
}
