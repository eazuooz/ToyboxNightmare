using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 슬라임 유도탄(<see cref="SlimeProjectileLogic"/>)의 데이터.
    ///
    /// 붙었을 때의 지속 피해와 이펙트도 실려 온다. 무기(<see cref="SlimeWeaponData"/>)가 정한 값을
    /// 투사체가 받아 쓴다 — 투사체는 표를 보지 않는다.
    /// </summary>
    public class SlimeProjectileData : EntityData
    {
        // 풀에서 나온 객체는 필드 초기화식이 다시 돌지 않는다. Clear 가 이 상수로 되돌린다.
        /// <summary>유실 투사체 방지 상한(초). 원본에는 수명도 최대 사거리도 없다.</summary>
        private const float DefaultMaxFlightSeconds = 3f;

        public static SlimeProjectileData Create(int entityId, int typeId,
                                                 Vector3 position, Quaternion rotation,
                                                 int targetEntityId, int attackerEntityId,
                                                 SlimeWeaponData weapon)
        {
            SlimeProjectileData data = ReferencePool.Acquire<SlimeProjectileData>();
            data.Fill(entityId, typeId);

            data.Position          = position;
            data.Rotation          = rotation;
            data.TargetEntityId    = targetEntityId;
            data.AttackerEntityId  = attackerEntityId;
            data.Speed             = weapon.ProjectileSpeed;
            data.HitRadius         = weapon.HitRadius;
            data.HitEffectAsset    = weapon.HitEffectAsset;
            data.HitEffectLifetime = weapon.HitEffectLifetime;
            data.Ticks             = weapon.Ticks;
            data.TickInterval      = weapon.TickInterval;
            data.TickDamage        = weapon.TickDamage;

            return data;
        }

        /// <summary>쫓아갈 적의 엔티티 Id. 참조가 아니라 Id 로 든다 — 날아가는 사이에 회수될 수 있다.</summary>
        public int TargetEntityId { get; private set; }

        /// <summary>쏜 쪽(플레이어)의 엔티티 Id. 지속 피해의 공격자로 넘긴다.</summary>
        public int AttackerEntityId { get; private set; }

        /// <summary>
        /// 진행 속도와 명중 반경. 0 이하가 들어와도 SlimeProjectileLogic 이 하한을 잡는다.
        /// </summary>
        public float Speed { get; private set; }
        public float HitRadius { get; private set; }

        /// <summary>이 시간이 지나도 못 맞히면 회수된다.</summary>
        public float MaxFlightSeconds { get; private set; } = DefaultMaxFlightSeconds;

        /// <summary>붙은 자리에 띄울 이펙트의 Addressables 주소와 수명.</summary>
        public string HitEffectAsset { get; private set; }
        public float HitEffectLifetime { get; private set; }

        /// <summary>지속 피해 — 횟수, 간격(초), 한 번의 피해.</summary>
        public int Ticks { get; private set; }
        public float TickInterval { get; private set; }
        public int TickDamage { get; private set; }

        public override void Clear()
        {
            base.Clear();

            // Id 를 0 으로 되돌리지 않으면 다음 투사체가 이전 대상을 쫓는다.
            TargetEntityId    = 0;
            AttackerEntityId  = 0;
            Speed             = 0f;
            HitRadius         = 0f;
            MaxFlightSeconds  = DefaultMaxFlightSeconds;
            HitEffectAsset    = null;
            HitEffectLifetime = 0f;
            Ticks             = 0;
            TickInterval      = 0f;
            TickDamage        = 0;
        }
    }
}
