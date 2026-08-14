using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취. 포물선으로 던져 착탄 지점 주변의 적을 도주시킨다. <b>데미지는 0이다</b>.
    /// 원본은 마우스로 지면을 조준하고 레티클이 3색으로 바뀌었지만,
    /// 자동발사에서는 최근접 적의 발밑을 착탄점으로 잡는다.
    /// </summary>
    public class StinkWeapon : WeaponBase
    {
        private const string MuzzlePath = "Antenna";

        /// <summary>
        /// 사거리 안에 아무도 없었을 때 다시 시도하기까지의 시간.
        /// 쿨다운을 통째로 소모하지 않고 짧게 되돌려, 적이 들어오는 즉시 던지게 한다.
        /// </summary>
        private const float MissRetryDelay = 0.5f;

        protected override string VfxRootPath => "Antenna/StinkAttack";

        // 마우스 조준 링을 "지금 노리는 적" 마커로 전용한다.
        protected override string TargetRingPath => "Antenna/StinkAttack/TargetRing";

        /// <summary>단일 대상 — 착탄점을 잡을 최근접 적 하나.</summary>
        protected override float AttackRadius => WeaponTable.StinkDetectRadius;

        private Transform mMuzzle = null;

        protected override void OnInitialize()
        {
            AttackInterval = WeaponTable.StinkCooldown;

            // Root 는 Initialize 가 넘겨준 Player 의 트랜스폼이다. 없으면 Find 가 NRE 다.
            GameAssert.IsTrue(Root != null,
                "StinkWeapon: Root 가 없다. Initialize 에 유효한 Player 를 넘겨야 한다.");
            if (Root == null) return;

            if (mMuzzle == null)
            {
                mMuzzle = Root.Find(MuzzlePath);
                if (mMuzzle == null)
                {
                    Log.Warning("StinkWeapon: '{0}' 을 찾지 못했다. 캐릭터 원점에서 던진다.", MuzzlePath);
                }
            }
        }

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(WeaponTable.StinkDetectRadius);
            if (target == null)
            {
                RetryAfter(MissRetryDelay);
                return;
            }

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("StinkWeapon: EntityComponent 를 찾지 못했다. 투사체를 띄울 수 없다.");
                return;
            }

            Vector3 origin = GetMuzzleOrigin();

            // 착탄점은 발사 시점에 확정한다. 매 프레임 재조준하면 아치가 깨진다.
            Vector3 impact = target.CachedTransform.position;
            impact.y = 0f;

            int id = EntitySerialId.Next();
            entityComponent.ShowEntity(
                id,
                typeof(StinkProjectile),
                WeaponTable.StinkProjectileAsset,
                WeaponTable.ProjectileGroup,
                new ArcProjectileData(id, 1)
                {
                    Position    = origin,
                    ImpactPoint = impact,
                    Speed       = WeaponTable.StinkSpeed,
                });
        }

        /// <summary>던지는 시작점. 안테나를 못 찾았으면 캐릭터 원점에서 한 칸 띄운다.</summary>
        private Vector3 GetMuzzleOrigin()
        {
            return mMuzzle != null
                ? mMuzzle.position
                : Owner.CachedTransform.position + Vector3.up;
        }
    }
}
