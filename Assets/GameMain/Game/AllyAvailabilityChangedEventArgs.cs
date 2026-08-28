using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 아군을 소환할 수 있는지가 바뀌었을 때 발행된다. HUD 의 아군 아이콘이 구독한다.
    ///
    /// 매 점수 획득마다 쏘지 않고 <b>가부가 실제로 뒤집힐 때만</b> 발행한다 —
    /// 적 하나 잡을 때마다 같은 값을 반복 통보하면 HUD 가 매번 헛일을 한다.
    /// </summary>
    public class AllyAvailabilityChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(AllyAvailabilityChangedEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>지금 소환할 수 있는가.</summary>
        public bool CanSummon { get; private set; }

        public static AllyAvailabilityChangedEventArgs Create(bool canSummon)
        {
            var args = ReferencePool.Acquire<AllyAvailabilityChangedEventArgs>();
            args.CanSummon = canSummon;
            return args;
        }

        public override void Clear()
        {
            CanSummon = false;
        }
    }
}
