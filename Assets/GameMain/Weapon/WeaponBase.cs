using UnityEngine;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 베이스. Player GameObject 에 AddComponent 로 붙는다.
    ///
    /// 두 가지 발사 모드를 지원한다:
    ///   - 자동 발사 (Attack 오버라이드): 일정 간격마다 자동으로 호출된다. ProjectileWeapon, AreaWeapon 용.
    ///   - 수동 발사 (OnFireStart/OnFireHeld/OnFireStop 오버라이드): Player 의 입력에 따라 호출된다.
    ///     Lightning, Frost, Stink, Slime 무기 용.
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

        // ─── 자동 발사 (오버라이드 선택적) ───
        protected virtual void Attack() { }

        // ─── 수동 발사 입력 (오버라이드 선택적) ───
        /// <summary>Fire1 버튼을 누른 순간 한 번 호출된다.</summary>
        public virtual void OnFireStart() { }

        /// <summary>Fire1 버튼을 누르고 있는 동안 매 프레임 호출된다.</summary>
        public virtual void OnFireHeld() { }

        /// <summary>Fire1 버튼을 뗀 순간 한 번 호출된다.</summary>
        public virtual void OnFireStop() { }

        // ─── 공통 유틸 ───

        /// <summary>Owner 주변 radius 안에서 가장 가까운 살아있는 적을 반환한다.</summary>
        protected Enemy FindNearestEnemy(float radius = 20f)
        {
            Collider[] hits = Physics.OverlapSphere(Owner.CachedTransform.position, radius);
            Enemy nearest = null;
            float minDist = float.MaxValue;

            foreach (Collider col in hits)
            {
                Entity entity = col.GetComponent<Entity>();
                if (entity?.Logic is Enemy enemy && !enemy.IsDead)
                {
                    float dist = Vector3.Distance(Owner.CachedTransform.position, enemy.CachedTransform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }

        /// <summary>마우스 커서가 가리키는 지면(Y=0 평면) 위치를 반환한다.</summary>
        protected Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null) return Owner.CachedTransform.position;

            Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);

            return Owner.CachedTransform.position + Owner.CachedTransform.forward;
        }
    }
}
