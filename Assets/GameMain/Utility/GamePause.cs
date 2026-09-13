using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 일시정지 상태의 단일 소유자. 원본 <c>PauseMenu.IsPaused</c> 에 해당한다.
    ///
    /// <b>timeScale 은 직접 건드리지 않는다.</b> 프레임워크의
    /// <see cref="BaseComponent.PauseGame"/> / <see cref="BaseComponent.ResumeGame"/> 가
    /// 그 일을 하고, 정지 직전 속도를 기억했다가 되돌려 주는 것까지 맡는다.
    /// 여기서 <c>Time.timeScale</c> 을 직접 0 으로 쓰면 <c>BaseComponent.GameSpeed</c> 캐시가
    /// 어긋나 <c>IsGamePaused</c> 가 <b>정지 중인데도 false 를 돌려준다.</b>
    ///
    /// <b>그런데 timeScale 만으로는 부족하다.</b> GameFramework 는 정지 중에도 매 프레임
    /// <c>OnUpdate</c> 를 부르고 elapseSeconds 만 0 이 된다 — 즉 타이머는 멈추지만
    /// 입력 처리는 그대로 돈다. 그래서 입력을 읽는 쪽이 이 플래그를 직접 봐야 한다
    /// (원본도 <c>PlayerInputPC.CanUpdate</c> 에서 같은 검사를 한다).
    /// </summary>
    public static class GamePause
    {
        /// <summary>
        /// 지금 일시정지 중인가.
        ///
        /// 프레임워크에 묻지 않고 자체 플래그를 쓴다. 이 값은 매 프레임 여러 곳에서 읽는데
        /// <c>GameEntry.GetComponent</c> 는 컴포넌트 목록을 선형 탐색하기 때문이다.
        /// 쓰기 시점에만 프레임워크로 넘긴다.
        /// </summary>
        public static bool IsPaused { get; private set; }

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;

            // 입력 게이트는 프레임워크 상태와 무관하게 반드시 서야 한다.
            // BaseComponent 가 없어도(종료 중 등) 최소한 입력은 막힌 채로 남는다.
            IsPaused = paused;

            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Warning("GamePause: BaseComponent 가 없다. 입력만 막히고 시간은 계속 흐른다.");
                return;
            }

            if (paused)
            {
                baseComponent.PauseGame();
                return;
            }

            baseComponent.ResumeGame();
        }

        /// <summary>
        /// 판을 벗어날 때 반드시 부른다. 정지 상태로 프로시저가 바뀌면
        /// 게임 속도가 0 인 채로 남아 다음 판이 통째로 멈춘다.
        /// </summary>
        public static void Clear()
        {
            SetPaused(false);
        }
    }
}
