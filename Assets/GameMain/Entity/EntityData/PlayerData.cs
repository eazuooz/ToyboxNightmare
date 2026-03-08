using System;
using UnityEngine;

namespace ToyBoxNightmare
{
    [Serializable]
    public class PlayerData : TargetableObjectData
    {
        [SerializeField] private int   mMaxHP     = 100;
        [SerializeField] private float mMoveSpeed = 5f;

        public PlayerData(int entityId, int typeId) : base(entityId, typeId)
        {
            HitPoints = mMaxHP;
        }

        public override int MaxHitPoints => mMaxHP;

        public float MoveSpeed
        {
            get => mMoveSpeed;
            set => mMoveSpeed = Mathf.Max(1f, value);
        }
    }
}
