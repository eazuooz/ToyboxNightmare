using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    [Serializable]
    public abstract class TargetableObjectData : EntityData
    {
        //[SerializeField]
        //private CampType mCamp = CampType.Unknown;

        [SerializeField]
        private int mHitPoints = 0;

        protected TargetableObjectData(int entityId, int typeId/*, CampType camp*/)
            : base(entityId, typeId)
        {
            //mCamp = camp;
            mHitPoints = 0;
        }

        //public CampType Camp
        //{
        //    get
        //    {
        //        return mCamp;
        //    }
        //}

        public int HitPoints
        {
            get
            {
                return mHitPoints;
            }
            set
            {
                mHitPoints = value;
            }
        }

        public abstract int MaxHitPoints
        {
            get;
        }

        public float HitPointRatio
        {
            get
            {
                return MaxHitPoints > 0 ? (float)HitPoints / MaxHitPoints : 0.0f;
            }
        }
    }
}
