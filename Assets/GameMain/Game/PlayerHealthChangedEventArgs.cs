using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어 체력 비율이 바뀌었을 때 발행된다. HUD 가 구독해 체력 바와 피격 플래시를 갱신한다.
    ///
    /// <b>비율이 줄어들 때만 오는 것이 아니다.</b> 스폰 직후에도 한 번 발행되므로
    /// (그때는 <see cref="FromRatio"/> 와 <see cref="ToRatio"/> 가 같다) HUD 가
    /// 새 판의 만피 상태를 알 수 있다. 피격 연출은 두 값을 비교해서 결정할 것.
    /// </summary>
    public class PlayerHealthChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PlayerHealthChangedEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>변경 전 체력 비율(0~1).</summary>
        public float FromRatio { get; private set; }

        /// <summary>변경 후 체력 비율(0~1).</summary>
        public float ToRatio { get; private set; }

        /// <summary>이번 변화가 피해인가. 스폰 시 통보와 구분하는 데 쓴다.</summary>
        public bool IsDamage => ToRatio < FromRatio;

        public static PlayerHealthChangedEventArgs Create(float fromRatio, float toRatio)
        {
            var args = ReferencePool.Acquire<PlayerHealthChangedEventArgs>();
            args.FromRatio = fromRatio;
            args.ToRatio   = toRatio;
            return args;
        }

        public override void Clear()
        {
            FromRatio = 0f;
            ToRatio   = 0f;
        }
    }
}
