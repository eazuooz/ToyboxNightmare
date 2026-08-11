using GameFramework;
using GameFramework.Event;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 적이 죽었을 때 발행된다. 점수 집계와 드롭 스폰이 이 이벤트를 구독한다.
    /// </summary>
    public class EnemyDiedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(EnemyDiedEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>죽은 적의 Addressables 주소. 종별 카운트에 쓴다.</summary>
        public string EnemyAssetName { get; private set; }

        /// <summary>이 적을 잡았을 때 얻는 점수.</summary>
        public int ScoreValue { get; private set; }

        /// <summary>죽은 위치. 드롭 스폰 지점으로 쓴다.</summary>
        public Vector3 Position { get; private set; }

        public static EnemyDiedEventArgs Create(string enemyAssetName, int scoreValue, Vector3 position)
        {
            var args = ReferencePool.Acquire<EnemyDiedEventArgs>();
            args.EnemyAssetName = enemyAssetName;
            args.ScoreValue = scoreValue;
            args.Position = position;
            return args;
        }

        public override void Clear()
        {
            EnemyAssetName = null;
            ScoreValue = 0;
            Position = Vector3.zero;
        }
    }
}
