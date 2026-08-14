using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>유도 투사체(Slime)용.</summary>
    public class HomingProjectileData : EntityData
    {
        public static HomingProjectileData Create(int entityId, int typeId,
                                                  Vector3 position, Quaternion rotation,
                                                  int targetEntityId, int attackerEntityId,
                                                  float speed, float hitRadius)
        {
            HomingProjectileData data = ReferencePool.Acquire<HomingProjectileData>();
            data.Fill(entityId, typeId);

            data.Position         = position;
            data.Rotation         = rotation;
            data.TargetEntityId   = targetEntityId;
            data.AttackerEntityId = attackerEntityId;
            data.Speed            = speed;
            data.HitRadius        = hitRadius;

            return data;
        }

        /// <summary>
        /// 추적 대상의 엔티티 Id. 참조가 아니라 Id 로 들고 있는 이유는,
        /// 대상이 회수되어 다른 적으로 재사용됐을 때 조용히 엉뚱한 적을 쫓지 않기 위해서다.
        /// </summary>
        public int TargetEntityId { get; set; }

        /// <summary>공격자 엔티티 Id. DoT 를 누가 넣었는지 추적용.</summary>
        public int AttackerEntityId { get; set; }

        /// <summary>초당 이동 거리. 기본값은 Slime 기본 속도와 같은 값이다.</summary>
        public float Speed { get; set; } = WeaponTable.SlimeSpeed;

        /// <summary>
        /// 명중 판정 거리. 콜라이더가 아니라 거리로 판정한다(원본과 동일).
        /// 기본값은 Slime 기본 판정 반경과 같은 값이다.
        /// </summary>
        public float HitRadius { get; set; } = WeaponTable.SlimeHitRadius;

        public override void Clear()
        {
            base.Clear();

            // Id 를 0 으로 되돌리지 않으면 다음 투사체가 이전 대상을 쫓는다.
            TargetEntityId   = 0;
            AttackerEntityId = 0;

            // 필드 초기화식은 첫 new 때만 돈다. 기본값 복원도 여기서 해야 한다.
            Speed     = WeaponTable.SlimeSpeed;
            HitRadius = WeaponTable.SlimeHitRadius;
        }
    }
}
