using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 점액. 유도 투사체를 쏴 맞은 적에게 도트 데미지와 공격 봉인을 건다.
    ///
    /// 원본의 "스티키 타겟"(마우스로 한 번 찍으면 유지)은 마우스 조준 UI 의 산물이라
    /// 이식하지 않는다. 자동발사에서는 발사 순간의 최근접 적으로 충분하다.
    /// </summary>
    public class SlimeWeapon : WeaponBase
    {
        /// <summary>
        /// 사거리 안에 아무도 없었을 때 다시 시도하기까지의 시간.
        /// 원본의 "헛방이면 쿨다운을 소모하지 않는다" 규약을 계승한다.
        /// </summary>
        private const float MissRetryDelay = 0.25f;

        /// <summary>
        /// 방향 벡터로 쓰기엔 너무 짧다고 볼 제곱 길이.
        /// 이보다 짧은 벡터를 <c>LookRotation</c> 에 넘기면 Unity 가 에러를 뱉는다.
        /// </summary>
        private const float MinDirectionSqrMagnitude = 0.0001f;

        protected override string VfxRootPath => "Antenna/SlimeAttack";

        // 스티키 타겟 선택 링을 "지금 노리는 적" 마커로 전용한다.
        protected override string TargetRingPath => "Antenna/SlimeAttack/SlimeSelectRing";

        /// <summary>단일 대상 — 유도탄이 쫓아갈 최근접 적 하나.</summary>
        protected override float AttackRadius => WeaponTable.SlimeDetectRadius;

        /// <summary>
        /// 총구("Antenna") 해석과 폴백은 베이스의 <see cref="WeaponBase.MuzzleOrigin"/> 이 맡는다.
        /// Root null 가드도 <see cref="WeaponBase.Initialize"/> 가 처리하므로 여기엔 없다.
        /// </summary>
        protected override void OnInitialize()
        {
            AttackInterval = WeaponTable.SlimeCooldown;
        }

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(WeaponTable.SlimeDetectRadius);
            if (target == null)
            {
                RetryAfter(MissRetryDelay);
                return;
            }

            // 유도탄은 대상을 Id 로 들고 간다. Entity 가 없으면 넘길 Id 자체가 없다 —
            // 살아 있는 적이라면 반드시 붙어 있어야 하므로 계약 위반이다.
            GameAssert.IsTrue(target.Entity != null, "SlimeWeapon: 대상 Enemy 에 Entity 가 없다.");
            if (target.Entity == null)
            {
                RetryAfter(MissRetryDelay);
                return;
            }

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("SlimeWeapon: EntityComponent 를 찾지 못했다. 투사체를 띄울 수 없다.");
                return;
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
                    GetLaunchRotation(origin, target),
                    target.Entity.Id,
                    Owner.Entity != null ? Owner.Entity.Id : 0,
                    WeaponTable.SlimeSpeed,
                    WeaponTable.SlimeHitRadius));
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
    }
}
