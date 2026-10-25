using GameFramework;

namespace ToyBoxNightmare
{
    /// <summary>냉기(<see cref="FrostWeaponLogic"/>)의 능력치. 값은 <see cref="WeaponTable"/> 에서 온다.</summary>
    public class FrostWeaponData : WeaponData
    {
        public static FrostWeaponData Create(int entityId, int typeId, int ownerId, int slot)
        {
            FrostWeaponData data = ReferencePool.Acquire<FrostWeaponData>();
            data.FillWeapon(entityId, typeId, ownerId, slot, WeaponTable.FrostCooldown);

            data.ConeRadius    = WeaponTable.FrostConeRadius;
            data.ConeHalfAngle = WeaponTable.FrostConeHalfAngle;

            return data;
        }

        /// <summary>부채꼴의 반지름.</summary>
        public float ConeRadius { get; private set; }

        /// <summary>부채꼴 반각(도). 개구각은 이것의 두 배다.</summary>
        public float ConeHalfAngle { get; private set; }

        public override void Clear()
        {
            base.Clear();

            ConeRadius    = 0f;
            ConeHalfAngle = 0f;
        }
    }
}
