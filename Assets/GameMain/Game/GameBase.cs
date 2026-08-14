//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------

using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 한 판(라운드)의 규칙을 담는 게임 모드의 공통 베이스.
    /// 프로시저(<see cref="ProcedureMain"/>)가 생성 → Initialize → 매 프레임 Update → Shutdown 순으로 굴린다.
    ///
    /// MonoBehaviour 가 아니라 순수 C# 객체다. 재시작할 때마다 새 인스턴스가 만들어지므로
    /// 파생 클래스는 상태 초기화를 <see cref="Initialize"/> 에서 명시적으로 해야 한다.
    /// </summary>
    public abstract class GameBase
    {
        /// <summary>이 모드의 식별자.</summary>
        public abstract GameMode GameMode
        {
            get;
        }

        /// <summary>
        /// 게임오버 확정 여부. 프로시저가 이 값을 보고 ProcedureGameOver 로 전이한다.
        /// 한 번 true 가 되면 같은 판에서 다시 false 로 돌아가지 않는다.
        /// </summary>
        public bool GameOver
        {
            get;
            protected set;
        }

        /// <summary>
        /// 판 시작. 파생 클래스는 반드시 base 를 먼저 호출해야 <see cref="GameOver"/> 가 내려간다.
        /// </summary>
        public virtual void Initialize()
        {
            GameOver = false;
        }

        /// <summary>판 종료. 구독 해제와 참조 정리를 여기서 한다.</summary>
        public virtual void Shutdown()
        {
        }

        public virtual void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        protected virtual void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
        }

        protected virtual void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            // 이벤트 ID 로 등록했으니 캐스팅은 항상 성공해야 한다. 실패하면 구독 등록이 잘못된 것이다.
            ShowEntityFailureEventArgs ne = e as ShowEntityFailureEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("ShowEntityFailure 핸들러에 다른 타입의 이벤트 인자가 들어왔다.");
                return;
            }

            Log.Warning("Show entity failure with error message '{0}'.", ne.ErrorMessage);
        }
    }
}
