using System.Collections.Generic;
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
        protected const int ShootableMask = WeaponUtil.ShootableMask;

        /// <summary>Shootable(9) | Blocking(10) | Environment(14) = 17920. 원본 Lightning 마스크.</summary>
        protected const int HitscanMask = WeaponUtil.HitscanMask;

        /// <summary>파생 무기가 재사용하는 스크래치 버퍼. 프레임 할당을 없앤다.</summary>
        protected readonly List<Enemy> Candidates = new List<Enemy>(32);

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

        /// <summary>
        /// 이번 발사가 헛방이었을 때 파생 무기가 호출한다. seconds 뒤에 다시 시도한다.
        /// 대상이 없는데 매 프레임 OverlapSphere 를 도는 것을 막는다.
        /// </summary>
        protected void RetryAfter(float seconds)
        {
            mAttackTimer = attackInterval - Mathf.Max(0f, seconds);
        }

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

            return WeaponUtil.FindNearestEnemy(Owner.CachedTransform.position, radius, Candidates);
        }
    }
}
