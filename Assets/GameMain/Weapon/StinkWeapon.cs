using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취. 마우스가 가리키는 지면으로 포물선을 던져 착탄 지점 주변의 적을 도주시킨다.
    /// <b>데미지는 0이다</b>.
    ///
    /// 원본과 같이 <b>버튼을 뗄 때</b> 발사하고, 레티클이 마우스 지점을 따라다니며
    /// 3색으로 상태를 알린다. 사거리 밖이면 던지지 않는다(쿨다운도 걸리지 않는다).
    /// </summary>
    public class StinkWeapon : WeaponBase
    {
        protected override string VfxRootPath => "Antenna/StinkAttack";

        /// <summary>마우스 지면 지점을 가리키는 원본 레티클.</summary>
        protected override string ReticlePath => "Antenna/StinkAttack/TargetRing";

        /// <summary>발사에 성공하면 로드아웃이 이 값으로 전역 쿨다운을 건다. 원본과 같은 5초.</summary>
        public override float Cooldown => WeaponTable.StinkCooldown;

        /// <summary>
        /// 레티클을 마우스 지점에 두고 색을 정한다. 원본 UpdateReticule 과 같은 순서다 —
        /// 사거리를 먼저 보고, 사거리 안일 때만 쿨다운을 본다.
        /// </summary>
        protected override void OnUpdateAim(bool isReady)
        {
            Vector3 aimPoint = Owner.AimPoint;

            TargetState state;
            if (!CanReach(aimPoint))
            {
                state = TargetState.Invalid;   // 사거리 밖 — 빨강
            }
            else if (!isReady)
            {
                state = TargetState.NotReady;  // 쿨다운 중 — 노랑
            }
            else
            {
                state = TargetState.Ready;     // 지금 던질 수 있다 — 초록
            }

            ShowReticle(aimPoint, state);
        }

        /// <summary>
        /// 버튼을 뗀 프레임에 던진다. 사거리 밖이면 <b>false</b> — 아무것도 안 나갔으니
        /// 쿨다운도 걸리지 않는다.
        /// </summary>
        protected override bool OnFireReleased()
        {
            Vector3 aimPoint = Owner.AimPoint;
            if (!CanReach(aimPoint)) return false;

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("StinkWeapon: EntityComponent 를 찾지 못했다. 투사체를 띄울 수 없다.");
                return false;
            }

            Vector3 origin = MuzzleOrigin;

            // 착탄점은 발사 시점에 확정한다. 매 프레임 재조준하면 아치가 깨진다.
            // 조준 지점은 이미 지면 위지만, 지면 평면이 y=0 이 아닌 맵에서도 투사체가
            // 바닥에서 터지도록 여기서 한 번 눌러 둔다.
            Vector3 impact = aimPoint;
            impact.y = 0f;

            int id = EntitySerialId.Next();
            entityComponent.ShowEntity(
                id,
                typeof(StinkProjectile),
                WeaponTable.StinkProjectileAsset,
                WeaponTable.ProjectileGroup,
                ArcProjectileData.Create(id, 1, origin, impact, WeaponTable.StinkSpeed));

            return true;
        }

        /// <summary>
        /// 던질 수 있는 지점인가 — 원본의 <c>inRange</c>.
        ///
        /// 지면 조준이 실패한 프레임은 착탄점 자체가 대체값(캐릭터 전방 한 칸)이라 무효로 본다.
        /// 원본도 마우스 위치가 유효하지 않으면 <c>inRange</c> 를 false 로 뒀다.
        /// </summary>
        private bool CanReach(Vector3 aimPoint)
        {
            if (!Owner.HasAimPoint) return false;

            // 사거리 판정은 캐릭터 원점 기준이다(원본은 안테나 기준이지만 안테나가 캐릭터에
            // 붙어 있어 차이가 사거리 오차 범위 안이다). 높이차에 흔들리지 않도록 평면 거리.
            float distance = WeaponUtil.PlanarDistance(aimPoint, Owner.CachedTransform.position);

            return distance <= WeaponTable.StinkThrowRange;
        }
    }
}
