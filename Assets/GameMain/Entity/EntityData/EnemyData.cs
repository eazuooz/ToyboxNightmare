using System;

namespace ToyBoxNightmare
{
    [Serializable]
    public class EnemyData : TargetableObjectData
    {
        private readonly EnemyStats mStats;

        public EnemyData(int entityId, int typeId, EnemyStats stats) : base(entityId, typeId)
        {
            mStats = stats;

            // TargetableObjectData 생성자가 HitPoints 를 0 으로 두므로 여기서 반드시 채운다.
            // 빠뜨리면 스폰 즉시 IsDead 가 true 다.
            HitPoints = stats.MaxHitPoints;
        }

        public EnemyStats Stats => mStats;

        public override int MaxHitPoints => mStats.MaxHitPoints;

        public string AssetName          => mStats.AssetName;
        public float  MoveSpeed          => mStats.MoveSpeed;
        public int    AttackDamage       => mStats.AttackDamage;
        public float  TimeBetweenAttacks => mStats.TimeBetweenAttacks;
        public int    ScoreValue         => mStats.ScoreValue;
    }
}
