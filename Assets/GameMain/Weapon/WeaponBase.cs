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

        /// <summary>
        /// 이 무기가 소유한 총구 VFX 루트의 경로(캐릭터 기준). null 이면 없는 것으로 본다.
        ///
        /// 무기를 바꾸면 이 GameObject 가 함께 꺼진다. 안 그러면 장착했던 무기의
        /// 총구 파티클이 전부 겹쳐서 남는다.
        /// </summary>
        protected virtual string VfxRootPath => null;

        private Transform mVfxRoot     = null;
        private bool      mVfxResolved = false;

        /// <summary>
        /// 지금 노리는 적의 발밑에 띄울 마커의 경로. null 이면 마커를 쓰지 않는다.
        ///
        /// 원본에서는 마우스 지면 조준점을 가리키고 사거리/쿨다운을 3색으로 알리던
        /// 물건이지만, 자동 조준으로 바뀌면서 그 역할이 사라졌다. "지금 누구를
        /// 노리고 있는지" 를 보여주는 용도로 바꿔 쓴다.
        /// </summary>
        protected virtual string TargetRingPath => null;

        /// <summary>마커가 적을 찾는 반경. 보통 그 무기의 탐지 반경과 같게 둔다.</summary>
        protected virtual float TargetRingRadius => 0f;

        /// <summary>지면과 겹쳐 z-fighting 나지 않게 살짝 띄운다.</summary>
        private const float TargetRingHeight = 0.06f;

        private Transform mTargetRing     = null;
        private bool      mTargetRingResolved = false;

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

        // Unity 메시지는 virtual 이 아니라서, 파생 클래스가 OnEnable/OnDisable 을 직접
        // 선언하면 base 쪽이 호출되지 않는다. 베이스가 잡고 훅으로 넘긴다.
        private void OnEnable()
        {
            SetVfxRootActive(true);
            OnWeaponEnabled();
        }

        private void OnDisable()
        {
            SetVfxRootActive(false);
            OnWeaponDisabled();
        }

        protected virtual void OnWeaponEnabled() { }
        protected virtual void OnWeaponDisabled() { }

        /// <summary>지금 노리는 적의 발밑으로 마커를 옮긴다. 대상이 없으면 숨긴다.</summary>
        private void UpdateTargetRing()
        {
            if (!mTargetRingResolved)
            {
                mTargetRingResolved = true;

                string path = TargetRingPath;
                if (!string.IsNullOrEmpty(path))
                {
                    mTargetRing = transform.Find(path);
                    if (mTargetRing == null)
                    {
                        Log.Warning("{0}: 타겟 마커 '{1}' 을 찾지 못했다.", GetType().Name, path);
                    }
                }
            }

            if (mTargetRing == null || TargetRingRadius <= 0f)
            {
                return;
            }

            Enemy target = FindNearestEnemy(TargetRingRadius);
            if (target == null)
            {
                SetTargetRingVisible(false);
                return;
            }

            SetTargetRingVisible(true);

            Vector3 position = target.CachedTransform.position;
            position.y += TargetRingHeight;
            mTargetRing.position = position;

            // 부모(안테나)가 회전하므로 월드 회전을 직접 고정해 항상 지면에 눕힌다.
            mTargetRing.rotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        private void SetTargetRingVisible(bool visible)
        {
            if (mTargetRing != null && mTargetRing.gameObject.activeSelf != visible)
            {
                mTargetRing.gameObject.SetActive(visible);
            }
        }

        private void SetVfxRootActive(bool active)
        {
            if (!mVfxResolved)
            {
                mVfxResolved = true;

                string path = VfxRootPath;
                if (!string.IsNullOrEmpty(path))
                {
                    mVfxRoot = transform.Find(path);
                    if (mVfxRoot == null)
                    {
                        Log.Warning("{0}: VFX 루트 '{1}' 을 찾지 못했다.", GetType().Name, path);
                    }
                }
            }

            if (mVfxRoot != null && mVfxRoot.gameObject.activeSelf != active)
            {
                mVfxRoot.gameObject.SetActive(active);
            }
        }

        private void Update()
        {
            if (Owner == null || !Owner.Available || Owner.IsDead)
            {
                SetTargetRingVisible(false);
                return;
            }

            UpdateTargetRing();

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
