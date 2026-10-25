using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어 엔티티 데이터. SurvivalGame 이 스폰할 때 만들어 <see cref="PlayerLogic"/> 의 OnShow 로 넘긴다.
    ///
    /// 플레이어의 <b>능력치</b>를 담는다(최대·현재 체력, 이동 속도, 사망 대기 시간). 캐릭터마다 다르게 하려면
    /// 스폰하는 쪽이 여기 값을 채운다. 실행 중에만 의미 있는 것(입력, 조준, 타이머, 컴포넌트)은
    /// <see cref="PlayerLogic"/> 에 둔다 — 이 객체는 참조 풀에서 나와 회수 때 반납되는 "주문서"다.
    /// </summary>
    public class PlayerData : TargetableObjectData
    {
        /// <summary>
        /// 이동 속도 하한. 0 이하가 되면 플레이어가 아예 움직이지 못하므로 세터에서 잘라낸다.
        /// </summary>
        private const float MinMoveSpeed = 1f;

        // 풀에서 나온 객체는 필드 초기화식이 다시 돌지 않는다. Clear 가 이 상수로 되돌린다.
        private const int   DefaultMaxHP      = 100;
        private const float DefaultMoveSpeed  = 5f;

        /// <summary>원본 GameManager 의 delayOnPlayerDeath.</summary>
        private const float DefaultDeathDelay = 1f;

        private int   mMaxHP      = DefaultMaxHP;
        private float mMoveSpeed  = DefaultMoveSpeed;
        private float mDeathDelay = DefaultDeathDelay;

        public static PlayerData Create(int entityId, int typeId)
        {
            PlayerData data = ReferencePool.Acquire<PlayerData>();
            data.Fill(entityId, typeId);

            // TargetableObjectData 는 HitPoints 를 0 으로 두므로 여기서 반드시 채운다.
            // 빠뜨리면 스폰 직후 IsDead 가 true 다.
            data.HitPoints = data.mMaxHP;

            return data;
        }

        public override int MaxHitPoints => mMaxHP;

        /// <summary>초당 이동 거리. 세터는 <see cref="MinMoveSpeed"/> 아래로 내려가지 않게 막는다.</summary>
        public float MoveSpeed
        {
            get => mMoveSpeed;
            set => mMoveSpeed = Mathf.Max(MinMoveSpeed, value);
        }

        /// <summary>쓰러진 뒤 회수될 때까지 기다리는 시간(초). 사망 애니메이션이 보일 시간이다.</summary>
        public float DeathDelay => mDeathDelay;

        public override void Clear()
        {
            base.Clear();

            mMaxHP      = DefaultMaxHP;
            mMoveSpeed  = DefaultMoveSpeed;
            mDeathDelay = DefaultDeathDelay;
        }
    }
}
