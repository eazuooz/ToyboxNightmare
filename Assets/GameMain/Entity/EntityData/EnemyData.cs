using System;
using UnityEngine;

namespace ToyBoxNightmare
{
    [Serializable]
    public class EnemyData : TargetableObjectData
    {
        [SerializeField] private int mMaxHP = 30;
        [SerializeField] private float mMoveSpeed = 2f;
        [SerializeField] private int mAttackDamage = 10;
        [SerializeField] private int mExpReward = 5;

        public EnemyData(int entityId, int typeId) : base(entityId, typeId)
        {
            HitPoints = mMaxHP;
        }

        public override int MaxHitPoints => mMaxHP;

        public float MoveSpeed => mMoveSpeed;

        public int AttackDamage => mAttackDamage;

        public int ExpReward => mExpReward;
    }
}
