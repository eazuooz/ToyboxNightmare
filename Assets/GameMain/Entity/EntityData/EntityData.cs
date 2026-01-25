using System;
using UnityEngine;

namespace ToyBoxNightmare
{
    [Serializable]
    public abstract class EntityData
    {
        [SerializeField]
        private int mId = 0;

        [SerializeField]
        private int mTypeId = 0;

        [SerializeField]
        private Vector3 mPosition = Vector3.zero;

        [SerializeField]
        private Quaternion mRotation = Quaternion.identity;

        protected EntityData(int entityId, int typeId)
        {
            mId = entityId;
            mTypeId = typeId;
        }

        /// <summary>
        /// 엔티티 고유 ID (런타임에서 유니크).
        /// </summary>
        public int Id
        {
            get
            {
                return mId;
            }
        }

        /// <summary>
        /// 엔티티 타입 ID (DataTable RowId 등).
        /// </summary>
        public int TypeId
        {
            get
            {
                return mTypeId;
            }
        }

        /// <summary>
        /// 스폰 위치.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return mPosition;
            }
            set
            {
                mPosition = value;
            }
        }

        /// <summary>
        /// 스폰 회전.
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return mRotation;
            }
            set
            {
                mRotation = value;
            }
        }
    }
}
