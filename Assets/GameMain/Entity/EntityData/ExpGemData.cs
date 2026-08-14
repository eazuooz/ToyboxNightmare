namespace ToyBoxNightmare
{
    /// <summary>
    /// 경험치 보석(<see cref="ExpGem"/>) 데이터.
    /// 현재 이 데이터를 만들어 스폰하는 곳이 없다 — 레벨 시스템 복원 시 연결한다.
    /// </summary>
    public class ExpGemData : EntityData
    {
        /// <summary>보석 하나가 주는 경험치.</summary>
        private const int DefaultExpAmount = 5;

        /// <summary>플레이어에게 끌려갈 때의 초당 이동 거리.</summary>
        private const float DefaultMoveSpeed = 4f;

        public int ExpAmount  { get; set; } = DefaultExpAmount;
        public float MoveSpeed { get; set; } = DefaultMoveSpeed;

        public ExpGemData(int entityId, int typeId) : base(entityId, typeId) { }
    }
}
