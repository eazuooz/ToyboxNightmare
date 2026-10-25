using GameFramework;

namespace ToyBoxNightmare
{
    /// <summary>번개(<see cref="LightningWeaponLogic"/>)의 능력치. 값은 <see cref="WeaponTable"/> 에서 온다.</summary>
    public class LightningWeaponData : WeaponData
    {
        public static LightningWeaponData Create(int entityId, int typeId, int ownerId, int slot)
        {
            LightningWeaponData data = ReferencePool.Acquire<LightningWeaponData>();
            data.FillWeapon(entityId, typeId, ownerId, slot, WeaponTable.LightningCooldown);

            data.Range             = WeaponTable.LightningRange;
            data.Damage            = WeaponTable.LightningDamage;
            data.HitEffectAsset    = WeaponTable.LightningHitAsset;
            data.HitEffectLifetime = WeaponTable.LightningHitLifetime;

            return data;
        }

        /// <summary>광선이 닿는 최대 거리.</summary>
        public float Range { get; private set; }

        /// <summary>한 발의 피해.</summary>
        public int Damage { get; private set; }

        /// <summary>맞은 자리에 띄울 이펙트의 Addressables 주소와 수명.</summary>
        public string HitEffectAsset { get; private set; }
        public float HitEffectLifetime { get; private set; }

        public override void Clear()
        {
            base.Clear();

            Range             = 0f;
            Damage            = 0;
            HitEffectAsset    = null;
            HitEffectLifetime = 0f;
        }
    }
}
