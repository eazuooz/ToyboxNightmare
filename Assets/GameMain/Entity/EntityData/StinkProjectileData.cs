using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취 투사체(<see cref="StinkProjectileLogic"/>)의 데이터. 착탄점을 발사 시점에 확정한다.
    ///
    /// 터질 때 띄울 가스 구름의 능력치도 실려 온다. 무기(<see cref="StinkWeaponData"/>)가 정한 값을
    /// 투사체가 받아 그대로 넘긴다 — 투사체는 표를 보지 않는다.
    /// </summary>
    public class StinkProjectileData : EntityData
    {
        public static StinkProjectileData Create(int entityId, int typeId,
                                                 Vector3 position, Vector3 impactPoint, StinkWeaponData weapon)
        {
            StinkProjectileData data = ReferencePool.Acquire<StinkProjectileData>();
            data.Fill(entityId, typeId);

            data.Position          = position;
            data.ImpactPoint       = impactPoint;
            data.Speed             = weapon.ProjectileSpeed;
            data.HitEffectAsset    = weapon.HitEffectAsset;
            data.HitEffectLifetime = weapon.HitEffectLifetime;
            data.BlastRadius       = weapon.BlastRadius;
            data.FleeDuration      = weapon.FleeDuration;

            return data;
        }

        /// <summary>지면 착탄점. 발사 순간에 고정된다 — 매 프레임 재조준하면 아치가 깨진다.</summary>
        public Vector3 ImpactPoint { get; private set; }

        /// <summary>
        /// 수평 진행 속도(초당 거리).
        /// 0 이하가 들어와도 StinkProjectileLogic 이 스스로 하한을 잡으므로 여기서는 막지 않는다.
        /// </summary>
        public float Speed { get; private set; }

        /// <summary>터진 자리에 띄울 가스 구름의 Addressables 주소와 수명.</summary>
        public string HitEffectAsset { get; private set; }
        public float HitEffectLifetime { get; private set; }

        /// <summary>가스가 닿는 반경과, 맞은 적이 달아나는 시간(초).</summary>
        public float BlastRadius { get; private set; }
        public float FleeDuration { get; private set; }

        public override void Clear()
        {
            base.Clear();

            ImpactPoint       = Vector3.zero;
            Speed             = 0f;
            HitEffectAsset    = null;
            HitEffectLifetime = 0f;
            BlastRadius       = 0f;
            FleeDuration      = 0f;
        }
    }
}
