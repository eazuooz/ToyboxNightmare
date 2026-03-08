using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    public class LevelUpEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(LevelUpEventArgs).GetHashCode();

        public override int Id => EventId;

        public int Level { get; private set; }

        public static LevelUpEventArgs Create(int level)
        {
            LevelUpEventArgs e = ReferencePool.Acquire<LevelUpEventArgs>();
            e.Level = level;
            return e;
        }

        public override void Clear()
        {
            Level = 0;
        }
    }
}
