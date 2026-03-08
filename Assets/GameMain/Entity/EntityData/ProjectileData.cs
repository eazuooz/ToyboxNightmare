using UnityEngine;

namespace ToyBoxNightmare
{
    public class ProjectileData : EntityData
    {
        public int Damage      { get; set; }
        public float Speed     { get; set; }
        public float Lifetime  { get; set; }
        public Vector3 Direction { get; set; }

        public ProjectileData(int entityId, int typeId) : base(entityId, typeId) { }
    }
}
