using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 일시정지 상태의 단일 소유자. 원본 <c>PauseMenu.IsPaused</c> 에 해당한다.
    ///
    /// <b>timeScale 만으로는 부족하다.</b> GameFramework 는 정지 중에도 매 프레임
    /// <c>OnUpdate</c> 를 부르고 elapseSeconds 만 0 이 된다 — 즉 타이머는 멈추지만
    /// 입력 처리는 그대로 돈다. 그래서 입력을 읽는 쪽이 이 플래그를 직접 봐야 한다
    /// (원본도 <c>PlayerInputPC.CanUpdate</c> 에서 같은 검사를 한다).
    /// </summary>
    public static class GamePause
    {
        /// <summary>지금 일시정지 중인가.</summary>
        public static bool IsPaused { get; private set; }

        /// <summary>정지 진입 직전의 timeScale. 원본도 0 으로 덮기 전 값을 보관한다.</summary>
        private static float sTimeScaleBeforePause = 1f;

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;

            IsPaused = paused;

            if (paused)
            {
                sTimeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = sTimeScaleBeforePause;
        }

        /// <summary>
        /// 판을 벗어날 때 반드시 부른다. 정지 상태로 프로시저가 바뀌면
        /// timeScale 이 0 인 채로 남아 게임이 통째로 멈춘다.
        /// </summary>
        public static void Clear()
        {
            SetPaused(false);
            sTimeScaleBeforePause = 1f;
        }
    }
}
