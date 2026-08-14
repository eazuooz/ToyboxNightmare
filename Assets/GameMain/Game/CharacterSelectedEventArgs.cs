using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 선택 화면에서 캐릭터를 클릭했을 때 <see cref="PlayerSelectLogic"/> 이 발행한다.
    /// SurvivalGame 이 이걸 받아 스포너를 열고 플레이어 엔티티를 스폰한다.
    /// </summary>
    public class CharacterSelectedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(CharacterSelectedEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>선택된 캐릭터의 키. Addressables 주소와 같은 문자열이다("Girl" / "Boy").</summary>
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
