using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    public class CharacterSelectedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(CharacterSelectedEventArgs).GetHashCode();
        public override int Id => EventId;

        public string CharacterKey { get; private set; }

        public static CharacterSelectedEventArgs Create(string characterKey)
        {
            var args = ReferencePool.Acquire<CharacterSelectedEventArgs>();
            args.CharacterKey = characterKey;
            return args;
        }

        public override void Clear()
        {
            CharacterKey = null;
        }
    }
}
