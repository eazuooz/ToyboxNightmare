using System;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>포물선 투사체(Stink)용. 착탄점을 발사 시점에 확정한다.</summary>
    [Serializable]
    public class ArcProjectileData : EntityData
    {
        public ArcProjectileData(int entityId, int typeId) : base(entityId, typeId)
        {
        }

        /// <summary>지면 착탄점. 발사 순간에 고정된다 — 매 프레임 재조준하면 아치가 깨진다.</summary>
        public Vector3 ImpactPoint { get; set; }

        /// <summary>수평 진행 속도.</summary>
        public float Speed { get; set; } = 10f;
    }
}
