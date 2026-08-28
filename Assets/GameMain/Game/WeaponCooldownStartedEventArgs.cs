using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 발사에 성공해 전역 쿨다운이 걸렸을 때 발행된다. HUD 의 쿨다운 게이지가 구독한다.
    ///
    /// 쿨다운은 무기별이 아니라 <see cref="WeaponLoadout"/> 이 하나만 든다(원본 PlayerAttack 과 같다).
    /// 그래서 게이지도 하나뿐이고, 마지막으로 쏜 무기의 쿨다운 길이가 실린다.
    /// Frost 는 쿨다운이 0 이라 아예 발행되지 않는다.
    /// </summary>
    public class WeaponCooldownStartedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(WeaponCooldownStartedEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>이번에 걸린 쿨다운 길이(초).</summary>
        public float Cooldown { get; private set; }

        public static WeaponCooldownStartedEventArgs Create(float cooldown)
        {
            var args = ReferencePool.Acquire<WeaponCooldownStartedEventArgs>();
            args.Cooldown = cooldown;
            return args;
        }

        public override void Clear()
        {
            Cooldown = 0f;
        }
    }
}
