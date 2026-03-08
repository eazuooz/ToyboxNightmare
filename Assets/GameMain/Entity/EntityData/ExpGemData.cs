using UnityEngine;

namespace ToyBoxNightmare
{
    public class ExpGemData : EntityData
    {
        public int ExpAmount  { get; set; } = 5;
        public float MoveSpeed { get; set; } = 4f;

        public ExpGemData(int entityId, int typeId) : base(entityId, typeId) { }
    }
}
