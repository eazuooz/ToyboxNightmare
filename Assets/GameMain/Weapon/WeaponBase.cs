using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 베이스. Player GameObject 에 AddComponent 로 붙는다.
    ///
    /// 뱀서라이크 모델이다 — 조준·발사 입력이 없다. 각 무기가 자기 쿨다운을 돌리며
    /// 자동으로 <see cref="Attack"/> 을 호출하고, 여러 무기를 동시에 장착한다.
    ///
    /// Player 엔티티는 풀에서 재사용되므로 무기 컴포넌트도 인스턴스에 남는다.
    /// 재스폰 때마다 <see cref="Initialize"/> 를 다시 불러 상태를 리셋할 것.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        /// <summary>Shootable(9). 적 탐색용.</summary>
        protected const int ShootableMask = 1 << 9;

        /// <summary>Shootable(9) | Blocking(10) | Environment(14) = 17920. 원본 Lightning 마스크.</summary>
        protected const int HitscanMask = (1 << 9) | (1 << 10) | (1 << 14);

        protected Player Owner { get; private set; }

        [SerializeField] private float attackInterval = 1f;
        private float mAttackTimer = 0f;

        public float AttackInterval
        {
            get => attackInterval;
            set => attackInterval = Mathf.Max(0.05f, value);
        }

        /// <summary>스폰(재스폰)마다 호출된다. 소유자 연결과 쿨다운 리셋을 겸한다.</summary>
        public void Initialize(Player owner)
        {
            Owner = owner;
            mAttackTimer = attackInterval; // 장착 직후 첫 발은 즉시
            OnInitialize();
        }

        /// <summary>파생 무기가 자기 상태를 리셋할 훅.</summary>
        protected virtual void OnInitialize() { }

        private void Update()
        {
            if (Owner == null || !Owner.Available || Owner.IsDead)
            {
                return;
            }

            mAttackTimer += Time.deltaTime;
            if (mAttackTimer < attackInterval)
            {
                return;
            }

            // 위상을 유지한다. 프레임이 튀어도 발사 주기가 밀리지 않는다.
            mAttackTimer -= attackInterval;
            Attack();
        }

        /// <summary>쿨다운마다 호출된다. 파생 무기가 여기서 발사한다.</summary>
        protected abstract void Attack();

        // ─── 공통 유틸 ───

        /// <summary>
        /// Owner 주변 radius 안에서 가장 가까운 살아있는 적을 반환한다.
        /// Shootable 레이어만 훑으므로 바닥/환경 콜라이더를 건드리지 않는다.
        /// </summary>
        protected Enemy FindNearestEnemy(float radius)
        {
            if (Owner == null)
            {
                return null;
            }

            Vector3 origin = Owner.CachedTransform.position;
            Collider[] hits = Physics.OverlapSphere(origin, radius, ShootableMask);

            Enemy nearest = null;
            float minSqr = float.MaxValue;

            foreach (Collider col in hits)
            {
                Entity entity = col.GetComponentInParent<Entity>();
                if (entity == null)
                {
                    continue;
                }

                Enemy enemy = entity.Logic as Enemy;
                if (enemy == null || enemy.IsDead || !enemy.Available)
                {
                    continue;
                }

                float sqr = (enemy.CachedTransform.position - origin).sqrMagnitude;
                if (sqr < minSqr)
                {
                    minSqr = sqr;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}
