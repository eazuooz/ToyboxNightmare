using GameFramework;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 슬라임(<see cref="SlimeWeaponLogic"/>)의 능력치. 값은 <see cref="WeaponTable"/> 에서 온다.
    ///
    /// 유도탄과 지속 피해의 능력치도 여기 있다. 무기가 쏠 때 <see cref="SlimeProjectileData"/> 에 실어 보낸다.
    /// </summary>
    public class SlimeWeaponData : WeaponData
    {
        public static SlimeWeaponData Create(int entityId, int typeId, int ownerId, int slot)
        {
            SlimeWeaponData data = ReferencePool.Acquire<SlimeWeaponData>();
            data.FillWeapon(entityId, typeId, ownerId, slot, WeaponTable.SlimeCooldown);

            data.TargetRayLength   = WeaponTable.SlimeTargetRayLength;
            data.ProjectileAsset   = WeaponTable.SlimeProjectileAsset;
            data.ProjectileSpeed   = WeaponTable.SlimeSpeed;
            data.HitRadius         = WeaponTable.SlimeHitRadius;
            data.HitEffectAsset    = WeaponTable.SlimeHitAsset;
            data.HitEffectLifetime = WeaponTable.SlimeHitLifetime;
            data.Ticks             = WeaponTable.SlimeTicks;
            data.TickInterval      = WeaponTable.SlimeTickInterval;
            data.TickDamage        = WeaponTable.SlimeTickDamage;

            return data;
        }

        /// <summary>마우스 지면 지점에서 위로 쏘아 적을 찍는 레이의 길이.</summary>
        public float TargetRayLength { get; private set; }

        /// <summary>유도탄의 Addressables 주소, 속도, 명중 반경.</summary>
        public string ProjectileAsset { get; private set; }
        public float ProjectileSpeed { get; private set; }
        public float HitRadius { get; private set; }

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

            TargetRayLength   = 0f;
            ProjectileAsset   = null;
            ProjectileSpeed   = 0f;
            HitRadius         = 0f;
            HitEffectAsset    = null;
            HitEffectLifetime = 0f;
            Ticks             = 0;
            TickInterval      = 0f;
            TickDamage        = 0;
        }
    }
}
