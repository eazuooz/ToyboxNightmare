using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어가 죽었을 때 발행된다. SurvivalGame 이 이걸 받아 GameOver 를 세우고,
    /// ProcedureMain 이 다음 Update 에서 ProcedureGameOver 로 전이한다.
    /// </summary>
    public class PlayerDiedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PlayerDiedEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>사망 시점의 최종 점수.</summary>
        public int FinalScore { get; private set; }

        public static PlayerDiedEventArgs Create(int finalScore)
        {
            var args = ReferencePool.Acquire<PlayerDiedEventArgs>();
            args.FinalScore = finalScore;
            return args;
        }

        public override void Clear()
        {
            FinalScore = 0;
        }
    }
}
