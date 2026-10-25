using GameFramework;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취(<see cref="StinkWeaponLogic"/>)의 능력치. 값은 <see cref="WeaponTable"/> 에서 온다.
    ///
    /// 무기가 던지는 투사체와 터지는 가스 구름의 능력치도 여기 있다. 무기가 쏠 때
    /// <see cref="StinkProjectileData"/> 에 실어 보내고, 투사체가 터질 때 <see cref="StinkHitData"/> 로 넘긴다.
    /// </summary>
    public class StinkWeaponData : WeaponData
    {
        public static StinkWeaponData Create(int entityId, int typeId, int ownerId, int slot)
        {
            StinkWeaponData data = ReferencePool.Acquire<StinkWeaponData>();
            data.FillWeapon(entityId, typeId, ownerId, slot, WeaponTable.StinkCooldown);

            data.ThrowRange        = WeaponTable.StinkThrowRange;
            data.ProjectileAsset   = WeaponTable.StinkProjectileAsset;
            data.ProjectileSpeed   = WeaponTable.StinkSpeed;
            data.HitEffectAsset    = WeaponTable.StinkHitAsset;
            data.HitEffectLifetime = WeaponTable.StinkHitLifetime;
            data.BlastRadius       = WeaponTable.StinkBlastRadius;
            data.FleeDuration      = WeaponTable.StinkFleeDuration;

            return data;
        }

        /// <summary>던질 수 있는 최대 거리(캐릭터 기준 평면 거리).</summary>
        public float ThrowRange { get; private set; }

        /// <summary>투사체의 Addressables 주소와 속도.</summary>
        public string ProjectileAsset { get; private set; }
        public float ProjectileSpeed { get; private set; }

        /// <summary>터진 자리에 띄울 가스 구름의 Addressables 주소와 수명.</summary>
        public string HitEffectAsset { get; private set; }
        public float HitEffectLifetime { get; private set; }

        /// <summary>가스가 닿는 반경.</summary>
        public float BlastRadius { get; private set; }

        /// <summary>가스를 맞은 적이 달아나는 시간(초).</summary>
        public float FleeDuration { get; private set; }

        public override void Clear()
        {
            base.Clear();

            ThrowRange        = 0f;
            ProjectileAsset   = null;
            ProjectileSpeed   = 0f;
            HitEffectAsset    = null;
            HitEffectLifetime = 0f;
            BlastRadius       = 0f;
            FleeDuration      = 0f;
        }
    }
}
