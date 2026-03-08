using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 베이스. Player GameObject에 AddComponent로 붙는다.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        protected Player Owner { get; private set; }

        [SerializeField] private float attackInterval = 1f;
        private float mAttackTimer = 0f;

        public float AttackInterval
        {
            get => attackInterval;
            set => attackInterval = Mathf.Max(0.1f, value);
        }

        public void Initialize(Player owner)
        {
            Owner = owner;
            mAttackTimer = attackInterval; // 초기화 즉시 공격 가능
        }

        private void Update()
        {
            if (Owner == null || Owner.IsDead) return;

            mAttackTimer += Time.deltaTime;
            if (mAttackTimer >= attackInterval)
            {
                mAttackTimer = 0f;
                Attack();
            }
        }

        protected abstract void Attack();
    }
}
