using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 즉발 히트스캔 무기. 발사 버튼을 누르고 있는 동안 쿨다운마다 <b>캐릭터 전방</b>으로 빔을 쏜다.
    ///
    /// 조준은 무기가 아니라 캐릭터가 한다 — 플레이어가 마우스 지면 지점을 바라보므로
    /// 전방이 곧 마우스 방향이다. 원본 LightningAttack 이 <c>new Ray(transform.position,
    /// transform.forward)</c> 하나로 끝나는 이유가 이것이고, 그래서 레티클도 없다.
    /// </summary>
    public class LightningWeaponLogic : WeaponLogicBase
    {
        // 능력치(사거리/피해/쿨다운/착탄 이펙트)는 LightningWeaponData 에 있다. 값은 WeaponTable 에서 채워진다.
        /// <summary>빔 연출 오브젝트. 무기 프리팹(LightningAttack) 루트 기준 경로다.</summary>
        private const string BoltPath = "LightningBolt";

        private LightningBoltVfx mBolt = null;

        /// <summary>이 무기의 능력치. 회수된 뒤에는 null.</summary>
        private LightningWeaponData Stats => DataAs<LightningWeaponData>();

        /// <summary>빔 연출은 무기 프리팹의 자식이라 인스턴스당 한 번만 찾으면 된다.</summary>
        protected override void OnWeaponInit()
        {
            ResolveBoltVfx();
        }

        /// <summary>풀에서 다시 나올 때 이전 판의 빔이 남아 있지 않게 한다.</summary>
        protected override void OnWeaponShow()
        {
            if (mBolt != null)
            {
                mBolt.StopImmediate();
            }
        }

        private void ResolveBoltVfx()
        {
            Transform boltTransform = CachedTransform.Find(BoltPath);
            if (boltTransform == null)
            {
                Log.Warning("LightningWeaponLogic: '{0}' 을 찾지 못했다. 빔 연출 없이 동작한다.", BoltPath);
                return;
            }

            // 프리팹에서 비활성으로 저장돼 있다. 연출 컴포넌트가 돌아야 하므로 켜 두고,
            // 실제 표시 여부는 LineRenderer/Light 를 껐다 켜서 제어한다.
            boltTransform.gameObject.SetActive(true);

            mBolt = new LightningBoltVfx(boltTransform);
        }

        /// <summary>
        /// 빔 연출은 스스로 돌지 않는다. 이 무기가 떠 있는 동안 여기서 굴린다 —
        /// 손에 들려 있지 않아도 엔티티의 OnUpdate 는 불리므로, 내려놓은 뒤의 잔여 시간도 여기서 소진된다.
        /// </summary>
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (mBolt != null)
            {
                mBolt.OnUpdate(elapseSeconds);
            }
        }

        /// <summary>
        /// 발사 버튼을 누르고 있는 동안. 로드아웃이 쿨다운을 통과시킨 프레임에만 불린다.
        ///
        /// 원본은 안테나 트랜스폼의 전방으로 그냥 쏜다. 자동조준(최근접 적 탐색)도
        /// 시야 검사도 없다 — 벽에 막히면 벽에 맞는 것이 정상 동작이다.
        /// </summary>
        protected override bool OnFireHeld()
        {
            LightningWeaponData stats = Stats;
            if (stats == null) return false;

            // 발사 원점은 무기 자신(소유자의 안테나에 붙어 있다). LightningBolt GO 도 그 아래에
            // 있어 같은 위치이므로, VFX 는 시작점을 따로 받지 않고 자기 트랜스폼을 매 프레임 읽는다.
            Vector3 origin = MuzzleOrigin;

            // Transform.forward 는 이미 단위 벡터라 정규화가 필요 없다.
            Vector3 direction = Owner.CachedTransform.forward;

            // 아무것도 안 맞으면(사이에 콜라이더가 없다) 최대 사거리까지 빔만 그린다.
            Vector3 beamEndPoint = origin + direction * stats.Range;

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit,
                                stats.Range, WeaponUtil.HitscanMask))
            {
                beamEndPoint = hit.point;
                ApplyHitscanHit(hit, stats);
            }

            PlayBolt(beamEndPoint);

            // 빗나가도 쿨다운은 소모한다(원본과 동일 — 원본은 Fire() 가 void 다).
            return true;
        }

        /// <summary>레이가 맞은 것을 처리한다 — 적이면 피해를 주고, 무엇에 맞았든 착탄 이펙트를 띄운다.</summary>
        private void ApplyHitscanHit(RaycastHit hit, LightningWeaponData stats)
        {
            Entity entity   = hit.collider.GetComponentInParent<Entity>();
            EnemyLogic  hitEnemy = entity != null ? entity.Logic as EnemyLogic : null;
            if (hitEnemy != null && !hitEnemy.IsDead)
            {
                hitEnemy.ApplyDamage(Owner.Entity, stats.Damage);
            }

            // 착탄 이펙트. 원본은 위치만 옮기고 회전은 건드리지 않는다.
            // strikeableMask 에 걸렸을 때만 나오므로(바닥 레이어 8 은 마스크 밖) 여기가 맞다.
            WeaponUtil.SpawnEffect(typeof(HitEffectLogic), stats.HitEffectAsset,
                hit.point, Quaternion.identity, stats.HitEffectLifetime);
            GameSound.PlaySfx(SoundTable.LightningHit, hit.point);
        }

        /// <summary>빔 연출은 없어도 게임플레이는 돌아간다. 프리팹에서 못 찾았으면 조용히 넘어간다.</summary>
        private void PlayBolt(Vector3 endPoint)
        {
            GameSound.PlaySfx(SoundTable.LightningFire, MuzzleOrigin);

            if (mBolt != null)
            {
                mBolt.Play(endPoint);
            }
        }

        /// <summary>
        /// 무기를 내려놓으면 빔 GameObject 가 무기와 함께 보이지 않게 된다. 남은 시간이 흐르기를
        /// 기다리지 않고 여기서 바로 끊는다.
        /// </summary>
        protected override void OnWeaponDisabled()
        {
            if (mBolt != null)
            {
                mBolt.StopImmediate();
            }
        }
    }
}
