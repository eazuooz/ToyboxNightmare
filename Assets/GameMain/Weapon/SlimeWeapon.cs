using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 점액. 마우스로 찍은 적에게 유도 투사체를 쏴 도트 데미지와 공격 봉인을 건다.
    ///
    /// 원본과 같이 <b>버튼을 뗄 때</b> 발사하고, 대상이 없으면 발사 자체가 실패한다
    /// (쿨다운도 걸리지 않는다). 대상 선정도 원본 그대로 — 마우스 지면 지점에서 위로
    /// 짧은 레이를 쏴 걸린 적을 찍는다.
    /// </summary>
    public class SlimeWeapon : WeaponBase
    {
        /// <summary>
        /// 방향 벡터로 쓰기엔 너무 짧다고 볼 제곱 길이.
        /// 이보다 짧은 벡터를 <c>LookRotation</c> 에 넘기면 Unity 가 에러를 뱉는다.
        /// </summary>
        private const float MinDirectionSqrMagnitude = 0.0001f;

        /// <summary>
        /// 찍어 둔 대상. 원본과 같이 <b>끈적하다</b> — 레이가 빗나간 프레임에는 지우지 않고,
        /// 발사에 성공했을 때만 놓는다.
        /// </summary>
        private Enemy mTarget = null;

        protected override string VfxRootPath => "Antenna/SlimeAttack";

        /// <summary>대상(없으면 마우스 지점)을 가리키는 원본 선택 링.</summary>
        protected override string ReticlePath => "Antenna/SlimeAttack/SlimeSelectRing";

        /// <summary>발사에 성공하면 로드아웃이 이 값으로 전역 쿨다운을 건다. 원본과 같은 3.5초.</summary>
        public override float Cooldown => WeaponTable.SlimeCooldown;

        /// <summary>
        /// 대상을 다시 찍고 레티클을 갱신한다. 원본 UpdateReticule 과 같은 순서다 —
        /// 대상 유무를 먼저 보고, 대상이 있을 때만 쿨다운을 본다.
        /// </summary>
        protected override void OnUpdateAim(bool isReady)
        {
            mTarget = FindTargetUnderCursor();

            // 대상이 있으면 그 발밑에, 없으면 마우스 지점에 놓는다.
            Vector3 reticlePosition = mTarget != null
                ? mTarget.CachedTransform.position
                : Owner.AimPoint;

            TargetState state;
            if (mTarget == null)
            {
                state = TargetState.Invalid;   // 찍은 적이 없다 — 빨강
            }
            else if (!isReady)
            {
                state = TargetState.NotReady;  // 쿨다운 중 — 노랑
            }
            else
            {
                state = TargetState.Ready;     // 지금 쏠 수 있다 — 초록
            }

            ShowReticle(reticlePosition, state);
        }

        /// <summary>
        /// 마우스 지면 지점에서 위로 레이를 쏴 적을 찍는다.
        /// 빗나간 프레임에는 <b>이전 대상을 유지한다</b> — 원본이 <c>target</c> 을 그때 지우지 않는다.
        /// </summary>
        private Enemy FindTargetUnderCursor()
        {
            Enemy current = mTarget;

            // 회수됐거나 죽은 적을 계속 물고 있으면 안 된다. 원본은 Transform 을 들고 있어
            // 죽은 적에게도 그대로 쐈지만, 엔티티 풀에서는 그 자리에 다른 적이 재사용되므로
            // 엉뚱한 적에게 유도탄이 날아간다.
            if (current != null && (!current.Available || current.IsDead)) current = null;

            // 지면 조준이 실패한 프레임은 쏠 지점 자체가 대체값이라 새로 찍지 않는다.
            if (!Owner.HasAimPoint) return current;

            RaycastHit hit;
            if (!Physics.Raycast(Owner.AimPoint, Vector3.up, out hit,
                                 WeaponTable.SlimeTargetRayLength, WeaponUtil.ShootableMask))
            {
                return current;
            }

            Entity entity   = hit.collider.GetComponentInParent<Entity>();
            Enemy  newTarget = entity != null ? entity.Logic as Enemy : null;
            if (newTarget == null || !newTarget.Available || newTarget.IsDead) return current;

            return newTarget;
        }

        /// <summary>
        /// 버튼을 뗀 프레임에 쏜다. 대상이 없으면 <b>false</b> — 원본의 "헛방이면 쿨다운을
        /// 소모하지 않는다" 규약이다(원본 SlimeAttack.Fire 만 bool 을 돌려주는 이유).
        /// </summary>
        protected override bool OnFireReleased()
        {
            if (mTarget == null) return false;

            // 유도탄은 대상을 Id 로 들고 간다. Entity 가 없으면 넘길 Id 자체가 없다 —
            // 살아 있는 적이라면 반드시 붙어 있어야 하므로 계약 위반이다.
            GameAssert.IsTrue(mTarget.Entity != null, "SlimeWeapon: 대상 Enemy 에 Entity 가 없다.");
            if (mTarget.Entity == null)
            {
                mTarget = null;
                return false;
            }

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("SlimeWeapon: EntityComponent 를 찾지 못했다. 투사체를 띄울 수 없다.");
                return false;
            }

            Vector3 origin = MuzzleOrigin;

            int id = EntitySerialId.Next();
            entityComponent.ShowEntity(
                id,
                typeof(SlimeProjectile),
                WeaponTable.SlimeProjectileAsset,
                WeaponTable.ProjectileGroup,
                HomingProjectileData.Create(
                    id, 1,
                    origin,
                    GetLaunchRotation(origin, mTarget),
                    mTarget.Entity.Id,
                    Owner.Entity != null ? Owner.Entity.Id : 0,
                    WeaponTable.SlimeSpeed,
                    WeaponTable.SlimeHitRadius));

            // 원본과 같이 쏘고 나면 대상 선택을 놓는다. 다음 대상은 다시 찍어야 한다.
            mTarget = null;
            return true;
        }

        /// <summary>
        /// 발사 자세. 총구와 적이 정확히 겹치면 방향 벡터가 0 이 되어 <c>LookRotation</c> 이
        /// 에러를 뱉으므로, 그때만 캐릭터가 보는 쪽으로 대신한다.
        /// </summary>
        private Quaternion GetLaunchRotation(Vector3 origin, Enemy target)
        {
            Vector3 toTarget = target.CachedTransform.position - origin;

            return toTarget.sqrMagnitude >= MinDirectionSqrMagnitude
                ? Quaternion.LookRotation(toTarget)
                : Owner.CachedTransform.rotation;
        }

        /// <summary>다른 무기로 전환되면 찍어 둔 대상을 버린다. 돌아왔을 때 그대로 남아 있으면 안 된다.</summary>
        protected override void OnWeaponDisabled()
        {
            mTarget = null;
        }

        /// <summary>캐릭터가 바뀌면 이 참조는 남의 적이 된다.</summary>
        protected override void OnDispose()
        {
            mTarget = null;
        }
    }
}
