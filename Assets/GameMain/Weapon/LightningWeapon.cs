using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 즉발 히트스캔 무기. 쿨다운마다 가장 가까운 적을 자동으로 노려 빔을 쏜다.
    ///
    /// 투사체 엔티티도 디버프도 필요 없어서 첫 무기로 골랐다 — 신규 EntityLogic 이 0개다.
    /// 튜닝값은 원본 Girl.prefab 의 LightningAttack 실효값.
    /// </summary>
    public class LightningWeapon : WeaponBase
    {
        // 튜닝값(사거리/데미지/쿨다운)은 WeaponTable 로 옮겼다. 무기마다 튜닝 창구가 다르면
        // 밸런스를 만질 때 어느 파일을 열어야 하는지가 매번 달라진다.
        private const string BoltPath = "Antenna/LightningAttack/LightningBolt";

        /// <summary>
        /// 원점과 목표가 사실상 겹쳤다고 볼 거리. 이보다 짧으면 방향 벡터를 정규화할 때
        /// 0 으로 나누게 된다.
        /// </summary>
        private const float MinAimDistance = 0.001f;

        private LightningBoltVfx mBolt = null;

        /// <summary>총구 스파클. 무기를 바꾸면 베이스가 꺼 준다.</summary>
        protected override string VfxRootPath => "Antenna/LightningAttack";

        /// <summary>Lightning 은 자기 링이 없어 Stink 쪽 링을 템플릿으로 빌려 쓴다.</summary>
        protected override string TargetRingPath => "Antenna/StinkAttack/TargetRing";

        protected override float AttackRadius => WeaponTable.LightningRange;

        /// <summary>
        /// 거리로 먼저 거른 뒤, 사거리 안이라도 <b>벽에 가려 있으면 초록을 주지 않는다</b> —
        /// 못 때리는데 초록이면 마커가 거짓말을 하게 된다.
        /// </summary>
        protected override TargetState EvaluateTarget(Enemy enemy)
        {
            float distance = WeaponUtil.PlanarDistance(
                enemy.CachedTransform.position, Owner.CachedTransform.position);

            TargetState state = WeaponUtil.ClassifyByRange(distance, WeaponTable.LightningRange);
            if (state != TargetState.InRange) return state;

            // 거리는 됐다. 시야가 막혔으면 초록 대신 노랑 — "돌아가면 닿는다" 는 뜻이다.
            return HasLineOfSight(enemy) ? TargetState.InRange : TargetState.Near;
        }

        private bool HasLineOfSight(Enemy target)
        {
            Vector3 origin    = MuzzleOrigin;
            Vector3 direction = target.AimPoint - origin;

            float distance = direction.magnitude;
            if (distance <= MinAimDistance)
            {
                // 코앞에 겹쳐 있다. 사이에 낄 것이 없다.
                return true;
            }

            direction /= distance;

            RaycastHit hit;
            if (!Physics.Raycast(origin, direction, out hit, distance, HitscanMask))
            {
                return true; // 사이에 아무것도 없다
            }

            // 레이가 먼저 맞은 것이 그 적 본인이어야 시야가 뚫린 것이다.
            Entity blocker = hit.collider.GetComponentInParent<Entity>();
            return blocker != null && ReferenceEquals(blocker.Logic, target);
        }

        /// <summary>
        /// Root 가 null 인 경우는 없다 — <see cref="WeaponBase.Initialize"/> 가 owner 없이는
        /// 여기까지 오지 않는다. 총구 해석도 베이스가 <see cref="WeaponBase.MuzzleOrigin"/> 으로 맡는다.
        /// </summary>
        protected override void OnInitialize()
        {
            AttackInterval = WeaponTable.LightningCooldown;

            ResolveBoltVfx();
        }

        private void ResolveBoltVfx()
        {
            if (mBolt != null) return;

            Transform boltTransform = Root.Find(BoltPath);
            if (boltTransform == null)
            {
                Log.Warning("LightningWeapon: '{0}' 을 찾지 못했다. 빔 연출 없이 동작한다.", BoltPath);
                return;
            }

            // 프리팹에서 비활성으로 저장돼 있다. 연출 컴포넌트가 돌아야 하므로 켜 두고,
            // 실제 표시 여부는 LineRenderer/Light 를 껐다 켜서 제어한다.
            boltTransform.gameObject.SetActive(true);

            mBolt = boltTransform.GetComponent<LightningBoltVfx>();
            if (mBolt == null)
            {
                mBolt = boltTransform.gameObject.AddComponent<LightningBoltVfx>();
            }
        }

        /// <summary>
        /// 빔은 스스로 꺼지지 못한다 — 무기를 끄면 빔 GameObject 가 VFX 루트와 함께 비활성이
        /// 되어 Update 가 멈추기 때문이다. 여기서 확실히 끊는다.
        /// </summary>
        protected override void OnWeaponDisabled()
        {
            if (mBolt != null)
            {
                mBolt.StopImmediate();
            }
        }

        /// <summary>
        /// 캐릭터가 바뀌면 이 참조는 남의 것이 된다. 버려서 다음 Initialize 가 다시 찾게 한다 —
        /// <see cref="ResolveBoltVfx"/> 가 <c>if (mBolt != null) return;</c> 으로만 걸러서,
        /// 여기서 안 버리면 옛 캐릭터의 빔을 계속 물고 있는다.
        /// </summary>
        protected override void OnDispose()
        {
            mBolt = null;
        }

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(WeaponTable.LightningRange);
            if (target == null) return;

            // 발사 원점은 안테나. LightningBolt GO 도 안테나 아래에 있어 같은 위치이므로,
            // VFX 는 시작점을 따로 받지 않고 자기 트랜스폼을 매 프레임 읽는다.
            Vector3 origin = MuzzleOrigin;

            // 적 콜라이더 중심을 노린다. 발밑을 노리면 바닥에 막힌다.
            Vector3 targetPoint = target.AimPoint;
            Vector3 direction   = targetPoint - origin;

            float distance = direction.magnitude;
            if (distance <= MinAimDistance) return;

            direction /= distance;

            // 아무것도 안 맞으면(사이에 콜라이더가 없다) 목표 지점까지 빔만 그린다.
            // 벽(Blocking) 이 앞을 막으면 그쪽에서 멈춘다.
            Vector3 beamEndPoint = targetPoint;

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, distance, HitscanMask))
            {
                beamEndPoint = hit.point;
                ApplyHitscanHit(hit);
            }

            PlayBolt(beamEndPoint);
        }

        /// <summary>레이가 맞은 것을 처리한다 — 적이면 피해를 주고, 무엇에 맞았든 착탄 이펙트를 띄운다.</summary>
        private void ApplyHitscanHit(RaycastHit hit)
        {
            Entity entity   = hit.collider.GetComponentInParent<Entity>();
            Enemy  hitEnemy = entity != null ? entity.Logic as Enemy : null;
            if (hitEnemy != null && !hitEnemy.IsDead)
            {
                hitEnemy.ApplyDamage(Owner.Entity, WeaponTable.LightningDamage);
            }

            // 착탄 이펙트. 원본은 위치만 옮기고 회전은 건드리지 않는다.
            // strikeableMask 에 걸렸을 때만 나오므로(바닥 레이어 8 은 마스크 밖) 여기가 맞다.
            WeaponUtil.SpawnEffect(typeof(HitEffect), WeaponTable.LightningHitAsset,
                hit.point, Quaternion.identity, WeaponTable.LightningHitLifetime);
        }

        /// <summary>빔 연출은 없어도 게임플레이는 돌아간다. 프리팹에서 못 찾았으면 조용히 넘어간다.</summary>
        private void PlayBolt(Vector3 endPoint)
        {
            if (mBolt != null)
            {
                mBolt.Play(endPoint);
            }
        }
    }
}
