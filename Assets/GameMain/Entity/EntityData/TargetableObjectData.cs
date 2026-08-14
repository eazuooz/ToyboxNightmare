using System;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 체력을 가지는 엔티티의 데이터 베이스.
    /// <see cref="TargetableObject"/> 가 OnShow 에서 이걸 받아 들고 있으며,
    /// 피격/사망 판정은 전부 여기 담긴 <see cref="HitPoints"/> 로 이뤄진다.
    /// </summary>
    [Serializable]
    public abstract class TargetableObjectData : EntityData
    {
        // 진영(CampType) 시스템은 아직 복원되지 않았다. 타입 자체가 존재하지 않으므로
        // 아래 주석을 그냥 풀면 컴파일이 깨진다 — 복원한다면 CampType 부터 만들 것.
        //[SerializeField]
        //private CampType mCamp = CampType.Unknown;

        [SerializeField]
        private int mHitPoints = 0;

        protected TargetableObjectData(int entityId, int typeId/*, CampType camp*/)
            : base(entityId, typeId)
        {
            //mCamp = camp;

            // 여기서는 0 으로만 둔다. 파생 클래스가 자신의 MaxHitPoints 로 반드시 채워야 한다 —
            // 빠뜨리면 스폰 직후 IsDead 가 true 가 된다(EnemyData 생성자 주석 참조).
            mHitPoints = 0;
        }

        //public CampType Camp
        //{
        //    get
        //    {
        //        return mCamp;
        //    }
        //}

        /// <summary>
        /// 현재 체력. 0 이하면 죽은 것으로 본다.
        /// 과다 피해를 입으면 음수가 될 수 있다 — 잘라내지 않는 것이 원본 동작이다.
        /// </summary>
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

        /// <summary>파생 클래스가 자신의 스탯에서 돌려주는 최대 체력.</summary>
        public abstract int MaxHitPoints
        {
            get;
        }

        /// <summary>
        /// 체력 비율(0~1). HUD 와 피격 판정이 쓴다.
        ///
        /// <see cref="MaxHitPoints"/> 가 0 인 데이터가 들어올 수 있어(예: EnemyTable 조회 실패로
        /// default 구조체가 넘어온 경우) 0 나눗셈을 막는다. 그때는 "체력 없음"인 0 을 돌려준다.
        /// </summary>
        public float HitPointRatio
        {
            get
            {
                return MaxHitPoints > 0 ? (float)HitPoints / MaxHitPoints : 0.0f;
            }
        }
    }
}
