using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 점수가 바뀌었을 때 발행된다. HUD 가 구독한다(M6).
    /// </summary>
    public class ScoreChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ScoreChangedEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>변경 후 총점.</summary>
        public int Score { get; private set; }

        /// <summary>이번에 더해진 값.</summary>
        public int Delta { get; private set; }

        public static ScoreChangedEventArgs Create(int score, int delta)
        {
            var args = ReferencePool.Acquire<ScoreChangedEventArgs>();
            args.Score = score;
            args.Delta = delta;
            return args;
        }

        public override void Clear()
        {
            Score = 0;
            Delta = 0;
        }
    }
}
