using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 적 엔티티 데이터. 종별 수치는 <see cref="EnemyTable"/> 이 들고 있고,
    /// 여기서는 그중 한 종의 스냅샷을 붙들어 <see cref="Enemy"/> 에 넘긴다.
    /// </summary>
    public class EnemyData : TargetableObjectData
    {
        // 풀에서 재사용되며 스폰마다 다시 채워지므로 readonly 일 수 없다.
        private EnemyStats mStats;

        public static EnemyData Create(int entityId, int typeId, EnemyStats stats, Vector3 position)
        {
            // EnemyTable.TryGetStats 가 실패하면 default(EnemyStats) 가 넘어온다.
            // 그러면 MaxHitPoints 가 0 이라 아래 대입 뒤에도 HitPoints 가 0 이고,
            // 적이 스폰되자마자 사망 연출로 들어간다. 조회 결과를 확인하지 않은
            // 호출부의 계약 위반이므로 assert 로 잡는다(스폰 1회당 1번만 검사).
            GameAssert.IsTrue(stats.MaxHitPoints > 0,
                "EnemyData 에 최대 체력 0 인 스탯이 들어왔다. EnemyTable.TryGetStats 반환값을 확인할 것.");

            EnemyData data = ReferencePool.Acquire<EnemyData>();
            data.Fill(entityId, typeId);

            data.mStats   = stats;
            data.Position = position;

            // TargetableObjectData 는 HitPoints 를 0 으로 두므로 여기서 반드시 채운다.
            // 빠뜨리면 스폰 즉시 IsDead 가 true 다.
            data.HitPoints = stats.MaxHitPoints;

            return data;
        }

        public EnemyStats Stats => mStats;

        public override int MaxHitPoints => mStats.MaxHitPoints;

        // 자주 쓰는 스탯의 단축 접근자. 호출부가 data.Stats.X 대신 data.X 로 읽게 한다.
        public string AssetName          => mStats.AssetName;
        public float  MoveSpeed          => mStats.MoveSpeed;
        public int    AttackDamage       => mStats.AttackDamage;
        public float  TimeBetweenAttacks => mStats.TimeBetweenAttacks;
        public int    ScoreValue         => mStats.ScoreValue;

        public override void Clear()
        {
            base.Clear();

            // 이전 종의 스탯을 물고 나가면 체력·속도·점수가 통째로 뒤바뀐다.
            mStats = default(EnemyStats);
        }
    }
}
