using System;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어 엔티티 데이터. SurvivalGame 이 스폰할 때 만들어 <see cref="Player"/> 의 OnShow 로 넘긴다.
    /// </summary>
    [Serializable]
    public class PlayerData : TargetableObjectData
    {
        /// <summary>
        /// 이동 속도 하한. 0 이하가 되면 플레이어가 아예 움직이지 못하므로 세터에서 잘라낸다.
        /// </summary>
        private const float MinMoveSpeed = 1f;

        [SerializeField] private int   mMaxHP     = 100;
        [SerializeField] private float mMoveSpeed = 5f;

        public PlayerData(int entityId, int typeId) : base(entityId, typeId)
        {
            // TargetableObjectData 생성자가 HitPoints 를 0 으로 두므로 여기서 반드시 채운다.
            // 빠뜨리면 스폰 직후 IsDead 가 true 다.
            HitPoints = mMaxHP;
        }

        public override int MaxHitPoints => mMaxHP;

        /// <summary>초당 이동 거리. 세터는 <see cref="MinMoveSpeed"/> 아래로 내려가지 않게 막는다.</summary>
        public float MoveSpeed
        {
            get => mMoveSpeed;
            set => mMoveSpeed = Mathf.Max(MinMoveSpeed, value);
        }
    }
}
