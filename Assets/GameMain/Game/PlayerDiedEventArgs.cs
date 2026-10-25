using GameFramework;
using GameFramework.Event;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어가 죽었을 때 발행된다. SurvivalGame 이 이걸 받아 GameOver 를 세우고,
    /// ProcedureMain 이 다음 Update 에서 ProcedureGameOver 로 전이한다.
    ///
    /// 점수는 싣지 않는다. 점수를 아는 것은 게임(<see cref="SurvivalGame"/>)이지 플레이어가 아니다 —
    /// 필요한 쪽이 게임에서 읽는다.
    /// </summary>
    public class PlayerDiedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PlayerDiedEventArgs).GetHashCode();
        public override int Id => EventId;

        public static PlayerDiedEventArgs Create()
        {
            return ReferencePool.Acquire<PlayerDiedEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
