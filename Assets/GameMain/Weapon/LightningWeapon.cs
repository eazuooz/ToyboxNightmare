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
        // 원본 실효값은 20 이었지만 자동 조준으로 바뀌면서 화면 밖의 적까지 때려
        // 교전 거리가 사라졌다. 절반으로 줄인다.
        private const float Range    = 10f;
        private const int   Damage   = 50;
        private const float Cooldown = 1f;

        /// <summary>발사점으로 쓸 자식 트랜스폼 이름. A6 에서 보존해 둔 것.</summary>
        private const string MuzzlePath = "Antenna";
        private const string BoltPath   = "Antenna/LightningAttack/LightningBolt";

        /// <summary>
        /// 원점과 목표가 사실상 겹쳤다고 볼 거리. 이보다 짧으면 방향 벡터를 정규화할 때
        /// 0 으로 나누게 된다.
        /// </summary>
        private const float MinAimDistance = 0.001f;

        /// <summary>적에게 콜라이더가 없을 때 쓸 조준 높이(발밑 기준). 대략 몸통 중심이다.</summary>
        private const float FallbackAimHeight = 0.5f;

        private Transform        mMuzzle = null;
        private LightningBoltVfx mBolt   = null;

        /// <summary>총구 스파클. 무기를 바꾸면 베이스가 꺼 준다.</summary>
        protected override string VfxRootPath => "Antenna/LightningAttack";

        /// <summary>Lightning 은 자기 링이 없어 Stink 쪽 링을 템플릿으로 빌려 쓴다.</summary>
        protected override string TargetRingPath => "Antenna/StinkAttack/TargetRing";

        protected override float AttackRadius => Range;

        /// <summary>
        /// 거리로 먼저 거른 뒤, 사거리 안이라도 <b>벽에 가려 있으면 초록을 주지 않는다</b> —
        /// 못 때리는데 초록이면 마커가 거짓말을 하게 된다.
        /// </summary>
        protected override TargetState EvaluateTarget(Enemy enemy)
        {
            float distance = WeaponUtil.PlanarDistance(
                enemy.CachedTransform.position, Owner.CachedTransform.position);

            if (distance > Range * WeaponUtil.NearRangeScale) return TargetState.OutOfRange;

            if (distance > Range) return TargetState.Near;

            return HasLineOfSight(enemy) ? TargetState.InRange : TargetState.Near;
        }

        private bool HasLineOfSight(Enemy target)
        {
            Vector3 origin    = GetMuzzleOrigin();
            Vector3 direction = GetAimPoint(target) - origin;

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

        protected override void OnInitialize()
        {
            AttackInterval = Cooldown;

            // Root 는 Initialize 가 넘겨준 Player 의 트랜스폼이다. 없으면 아래 Find 가 전부 NRE 다.
            GameAssert.IsTrue(Root != null,
                "LightningWeapon: Root 가 없다. Initialize 에 유효한 Player 를 넘겨야 한다.");
            if (Root == null) return;

            ResolveMuzzle();
            ResolveBoltVfx();
        }

        private void ResolveMuzzle()
        {
            if (mMuzzle != null) return;

            mMuzzle = Root.Find(MuzzlePath);
            if (mMuzzle == null)
            {
                Log.Warning("LightningWeapon: '{0}' 을 찾지 못했다. 캐릭터 원점에서 발사한다.", MuzzlePath);
            }
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

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(Range);
            if (target == null) return;

            // 발사 원점은 안테나. LightningBolt GO 도 안테나 아래에 있어 같은 위치이므로,
            // VFX 는 시작점을 따로 받지 않고 자기 트랜스폼을 매 프레임 읽는다.
            Vector3 origin = GetMuzzleOrigin();

            // 적 콜라이더 중심을 노린다. 발밑을 노리면 바닥에 막힌다.
            Vector3 targetPoint = GetAimPoint(target);
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
                hitEnemy.ApplyDamage(Owner.Entity, Damage);
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

        /// <summary>
        /// 발사 원점. 안테나를 못 찾았으면 캐릭터 원점에서 한 칸 띄운다 —
        /// 발밑에서 쏘면 레이가 바로 바닥에 막힌다.
        /// </summary>
        private Vector3 GetMuzzleOrigin()
        {
            return mMuzzle != null
                ? mMuzzle.position
                : Owner.CachedTransform.position + Vector3.up;
        }

        /// <summary>적의 몸통 높이를 노린다.</summary>
        private static Vector3 GetAimPoint(Enemy enemy)
        {
            Collider collider = enemy.GetComponent<Collider>();
            return collider != null
                ? collider.bounds.center
                : enemy.CachedTransform.position + Vector3.up * FallbackAimHeight;
        }
    }
}
