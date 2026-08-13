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
        private const float Range    = 20f;
        private const int   Damage   = 50;
        private const float Cooldown = 1f;

        /// <summary>발사점으로 쓸 자식 트랜스폼 이름. A6 에서 보존해 둔 것.</summary>
        private const string MuzzlePath = "Antenna";
        private const string BoltPath   = "Antenna/LightningAttack/LightningBolt";

        /// <summary>총구 스파클. 무기를 바꾸면 베이스가 꺼 준다.</summary>
        protected override string VfxRootPath => "Antenna/LightningAttack";

        private Transform        mMuzzle = null;
        private LightningBoltVfx mBolt   = null;

        protected override void OnInitialize()
        {
            AttackInterval = Cooldown;

            if (mMuzzle == null)
            {
                mMuzzle = transform.Find(MuzzlePath);
                if (mMuzzle == null)
                {
                    Log.Warning("LightningWeapon: '{0}' 을 찾지 못했다. 캐릭터 원점에서 발사한다.", MuzzlePath);
                }
            }

            if (mBolt == null)
            {
                Transform boltTransform = transform.Find(BoltPath);
                if (boltTransform == null)
                {
                    Log.Warning("LightningWeapon: '{0}' 을 찾지 못했다. 빔 연출 없이 동작한다.", BoltPath);
                }
                else
                {
                    // 프리팹에서 비활성으로 저장돼 있다. 연출 컴포넌트가 돌아야 하므로 켜 두고,
                    // 실제 표시 여부는 LineRenderer/Light 를 껐다 켜서 제어한다.
                    boltTransform.gameObject.SetActive(true);

                    mBolt = boltTransform.GetComponent<LightningBoltVfx>();
                    if (mBolt == null)
                    {
                        mBolt = boltTransform.gameObject.AddComponent<LightningBoltVfx>();
                    }
                }
            }
        }

        protected override void Attack()
        {
            Enemy target = FindNearestEnemy(Range);
            if (target == null)
            {
                return;
            }

            // 발사 원점은 안테나. LightningBolt GO 도 안테나 아래에 있어 같은 위치이므로,
            // VFX 는 시작점을 따로 받지 않고 자기 트랜스폼을 매 프레임 읽는다.
            Vector3 origin = mMuzzle != null
                ? mMuzzle.position
                : Owner.CachedTransform.position + Vector3.up;

            // 적 콜라이더 중심을 노린다. 발밑을 노리면 바닥에 막힌다.
            Vector3 targetPoint = GetAimPoint(target);
            Vector3 direction   = targetPoint - origin;

            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return;
            }

            direction /= distance;

            // 벽(Blocking) 이 앞을 막으면 그쪽에서 멈춘다.
            Vector3 endPoint = origin + direction * distance;
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, distance, HitscanMask))
            {
                endPoint = hit.point;

                Entity entity = hit.collider.GetComponentInParent<Entity>();
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
            else
            {
                // 아무것도 안 맞았다(사이에 콜라이더가 없다). 목표 지점까지 빔만 그린다.
                endPoint = targetPoint;
            }

            if (mBolt != null)
            {
                mBolt.Play(endPoint);
            }
        }

        /// <summary>적의 몸통 높이를 노린다.</summary>
        private static Vector3 GetAimPoint(Enemy enemy)
        {
            Collider collider = enemy.GetComponent<Collider>();
            return collider != null ? collider.bounds.center : enemy.CachedTransform.position + Vector3.up * 0.5f;
        }
    }
}
