using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 엔티티 스폰 파라미터의 공통 베이스.
    ///
    /// <c>EntityComponent.ShowEntity</c> 의 userData 로 넘어가서, 인스턴스가 만들어진 뒤
    /// <c>EntityLogic.OnShow(object userData)</c> 에서 다시 꺼내진다. 즉 <b>스폰 요청 시점의
    /// 값을 로드 완료 시점까지 실어 나르는 그릇</b>이다. 로드는 Addressables 비동기라
    /// 두 시점 사이에 여러 프레임이 끼어들 수 있으므로, 여기 담긴 값은 그동안 유효해야 한다.
    ///
    /// <b>ReferencePool 로 재사용한다.</b> 적 스폰과 투사체 발사는 초당 수십 번 도는 경로라
    /// 매번 <c>new</c> 하면 그만큼 힙 할당이 쌓인다. 이 프로젝트의 EventArgs 4종과 같은
    /// Acquire/Clear/Release 계약을 쓴다.
    ///
    /// <b>수명 규약</b>
    ///   - 취득: 파생 클래스의 정적 <c>Create</c> 팩토리만. 호출부에서 <c>new</c> 하지 말 것.
    ///   - 반납: 소유 엔티티 로직(<see cref="EntityLogicBase"/>)의 OnHide 가 정확히 1회.
    ///     스폰이 실패해 OnShow 자체가 불리지 않으면 <c>OnShowEntityFailure</c> 가 대신 반납한다.
    ///   - 이중 Release 는 코어에서 즉시 예외다.
    ///
    /// <b><see cref="Clear"/> 에 필드를 빠뜨리지 말 것.</b> Release 시점에 호출되므로,
    /// 되돌리지 않은 필드는 다음에 풀에서 나온 객체가 이전 스폰의 값을 그대로 물고 나온다.
    /// 필드 초기화식은 <c>new</c> 되는 첫 1회에만 도므로 기본값 복원도 여기서 해야 한다.
    ///
    /// 인스펙터에 노출되는 경로가 없어 <c>[Serializable]</c> / <c>[SerializeField]</c> 는
    /// 효과가 없다. 그래서 붙이지 않는다.
    /// </summary>
    public abstract class EntityData : IReference
    {
        private int mId = 0;
        private int mTypeId = 0;
        private Vector3 mPosition = Vector3.zero;
        private Quaternion mRotation = Quaternion.identity;

        /// <summary>
        /// <c>ReferencePool.Acquire&lt;T&gt;()</c> 가 <c>new()</c> 제약을 걸기 때문에
        /// 파라미터 없는 생성자가 반드시 있어야 한다. 값 주입은 파생 <c>Create</c> 가
        /// <see cref="Fill"/> 로 한다.
        /// </summary>
        protected EntityData()
        {
        }

        /// <summary>
        /// 파생 <c>Create</c> 팩토리가 취득 직후 부르는 공통 초기화.
        /// </summary>
        /// <param name="entityId">
        /// <see cref="EntitySerialId.Next"/> 로 발급받은 ID. 직접 만든 숫자를 넣지 말 것.
        /// </param>
        /// <param name="typeId">엔티티 타입 ID. 타입 테이블이 아직 없어 전 프로젝트가 1 로 고정이다.</param>
        protected void Fill(int entityId, int typeId)
        {
            // EntitySerialId 는 1 부터 발급한다. 0 이나 음수가 들어왔다면 발급을 거치지 않고
            // 호출부가 숫자를 직접 만든 것이다 — 계약 위반이라 assert 로 잡는다.
            //
            // 여기서 대신 ID 를 발급해 주지 않는 이유: ShowEntity 에 넘긴 entityId 와
            // EntityData.Id 가 반드시 같아야 하는데, 이 메서드가 몰래 다른 번호를 쥐여 주면
            // 둘이 갈라져 나중에 훨씬 찾기 어려운 버그가 된다. 값은 받은 그대로 둔다.
            GameAssert.IsTrue(entityId > 0,
                "EntityData 의 entityId 가 0 이하다. EntitySerialId.Next() 로 발급받은 값을 넘겨야 한다.");

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

        /// <summary>
        /// 풀 반납 시 코어가 부른다. 파생은 <c>base.Clear()</c> 를 먼저 부르고
        /// 자기 필드를 이어서 되돌린다.
        /// </summary>
        public virtual void Clear()
        {
            mId = 0;
            mTypeId = 0;
            mPosition = Vector3.zero;
            mRotation = Quaternion.identity;
        }
    }
}
