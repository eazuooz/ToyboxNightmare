using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>포물선 투사체(Stink)용. 착탄점을 발사 시점에 확정한다.</summary>
    public class ArcProjectileData : EntityData
    {
        public static ArcProjectileData Create(int entityId, int typeId,
                                               Vector3 position, Vector3 impactPoint, float speed)
        {
            ArcProjectileData data = ReferencePool.Acquire<ArcProjectileData>();
            data.Fill(entityId, typeId);

            data.Position    = position;
            data.ImpactPoint = impactPoint;
            data.Speed       = speed;

            return data;
        }

        /// <summary>지면 착탄점. 발사 순간에 고정된다 — 매 프레임 재조준하면 아치가 깨진다.</summary>
        public Vector3 ImpactPoint { get; set; }

        /// <summary>
        /// 수평 진행 속도(초당 거리). 기본값은 Stink 기본 속도와 같은 값이다.
        /// 0 이하가 들어와도 StinkProjectile 이 스스로 하한을 잡으므로 여기서는 막지 않는다.
        /// </summary>
        public float Speed { get; set; } = WeaponTable.StinkSpeed;

        public override void Clear()
        {
            base.Clear();

            ImpactPoint = Vector3.zero;

            // 필드 초기화식은 첫 new 때만 돈다. 기본값 복원도 여기서 해야 한다.
            Speed = WeaponTable.StinkSpeed;
        }
    }
}
