using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 직선 투사체(<see cref="Projectile"/>)용. 방향·속도·수명이 발사 시점에 확정되고
    /// 이후 갱신되지 않는다 — 유도가 필요하면 <see cref="HomingProjectileData"/> 를 쓴다.
    /// </summary>
    public class ProjectileData : EntityData
    {
        // 세터에 사전조건을 붙이기 위해 Direction 만 백킹 필드를 둔다.
        // 기본값은 auto-property 시절과 같은 영벡터로 유지한다.
        private Vector3 mDirection = Vector3.zero;

        /// <summary>명중한 적에게 넣을 피해량.</summary>
        public int Damage      { get; set; }

        /// <summary>초당 이동 거리.</summary>
        public float Speed     { get; set; }

        /// <summary>이 시간이 지나면 아무것도 맞히지 못해도 회수된다.</summary>
        public float Lifetime  { get; set; }

        /// <summary>
        /// 진행 방향. <b>정규화된 벡터여야 한다</b> — Projectile 이 이 값을 그대로
        /// transform.forward 에 대입하고 이동량에도 곱하므로, 영벡터면 Unity 가
        /// "Look rotation viewing vector is zero" 를 뱉고 길이가 1 이 아니면 Speed 가 왜곡된다.
        /// </summary>
        public Vector3 Direction
        {
            get => mDirection;
            set
            {
                // 발사 지점과 대상 위치가 완전히 겹치면 (target - owner).normalized 가
                // 영벡터를 돌려준다. 정상 플레이에서는 나오지 않는 값이라 계약 위반으로 본다.
                GameAssert.IsTrue(value.sqrMagnitude > 0f,
                    "ProjectileData.Direction 에 영벡터가 들어왔다. 발사 지점과 대상이 같은 위치인지 확인할 것.");

                mDirection = value;
            }
        }

        public ProjectileData(int entityId, int typeId) : base(entityId, typeId) { }
    }
}
