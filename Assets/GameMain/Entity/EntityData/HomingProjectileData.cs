using System;

namespace ToyBoxNightmare
{
    /// <summary>유도 투사체(Slime)용.</summary>
    [Serializable]
    public class HomingProjectileData : EntityData
    {
        public HomingProjectileData(int entityId, int typeId) : base(entityId, typeId)
        {
        }

        /// <summary>
        /// 추적 대상의 엔티티 Id. 참조가 아니라 Id 로 들고 있는 이유는,
        /// 대상이 회수되어 다른 적으로 재사용됐을 때 조용히 엉뚱한 적을 쫓지 않기 위해서다.
        /// </summary>
        public int TargetEntityId { get; set; }

        /// <summary>공격자 엔티티 Id. DoT 를 누가 넣었는지 추적용.</summary>
        public int AttackerEntityId { get; set; }

        public float Speed { get; set; } = 20f;

        /// <summary>명중 판정 거리. 콜라이더가 아니라 거리로 판정한다(원본과 동일).</summary>
        public float HitRadius { get; set; } = 1f;
    }
}
