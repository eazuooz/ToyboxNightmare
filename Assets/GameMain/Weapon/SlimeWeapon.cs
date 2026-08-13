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
        private const string MuzzlePath = "Antenna";

        protected override string VfxRootPath => "Antenna/SlimeAttack";

        // 스티키 타겟 선택 링을 "지금 노리는 적" 마커로 전용한다.
        protected override string TargetRingPath   => "Antenna/SlimeAttack/SlimeSelectRing";
        protected override float  TargetRingRadius => WeaponTable.SlimeDetectRadius;

        private Transform mMuzzle = null;

        protected override void OnInitialize()
        {
            AttackInterval = WeaponTable.SlimeCooldown;

            if (mMuzzle == null)
            {
                mMuzzle = transform.Find(MuzzlePath);
            }
        }

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(WeaponTable.SlimeDetectRadius);
            if (target == null)
            {
                // 원본의 "헛방이면 쿨다운을 소모하지 않는다" 규약을 계승한다.
                RetryAfter(0.25f);
                return;
            }

            Vector3 origin = mMuzzle != null
                ? mMuzzle.position
                : Owner.CachedTransform.position + Vector3.up;

            int id = EntitySerialId.Next();
            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(SlimeProjectile),
                WeaponTable.SlimeProjectileAsset,
                WeaponTable.ProjectileGroup,
                new HomingProjectileData(id, 1)
                {
                    Position         = origin,
                    Rotation         = Quaternion.LookRotation(
                                          target.CachedTransform.position - origin),
                    TargetEntityId   = target.Entity.Id,
                    AttackerEntityId = Owner.Entity != null ? Owner.Entity.Id : 0,
                    Speed            = WeaponTable.SlimeSpeed,
                    HitRadius        = WeaponTable.SlimeHitRadius,
                });
        }
    }
}
