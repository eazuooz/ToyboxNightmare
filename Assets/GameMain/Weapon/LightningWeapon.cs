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
    public class LightningWeapon : WeaponBase
    {
        // 튜닝값(사거리/데미지/쿨다운)은 WeaponTable 로 옮겼다. 무기마다 튜닝 창구가 다르면
        // 밸런스를 만질 때 어느 파일을 열어야 하는지가 매번 달라진다.
        private const string BoltPath = "Antenna/LightningAttack/LightningBolt";

        private LightningBoltVfx mBolt = null;

        /// <summary>총구 스파클. 무기를 바꾸면 베이스가 꺼 준다.</summary>
        protected override string VfxRootPath => "Antenna/LightningAttack";

        /// <summary>발사에 성공하면 로드아웃이 이 값으로 전역 쿨다운을 건다. 원본과 같은 1초.</summary>
        public override float Cooldown => WeaponTable.LightningCooldown;

        /// <summary>
        /// Root 가 null 인 경우는 없다 — <see cref="WeaponBase.Initialize"/> 가 owner 없이는
        /// 여기까지 오지 않는다. 총구 해석도 베이스가 <see cref="WeaponBase.MuzzleOrigin"/> 으로 맡는다.
        /// </summary>
        protected override void OnInitialize()
        {
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
        /// 발사 버튼을 누르고 있는 동안. 로드아웃이 쿨다운을 통과시킨 프레임에만 불린다.
        ///
        /// 원본은 안테나 트랜스폼의 전방으로 그냥 쏜다. 자동조준(최근접 적 탐색)도
        /// 시야 검사도 없다 — 벽에 막히면 벽에 맞는 것이 정상 동작이다.
        /// </summary>
        protected override bool OnFireHeld()
        {
            // 발사 원점은 안테나. LightningBolt GO 도 안테나 아래에 있어 같은 위치이므로,
            // VFX 는 시작점을 따로 받지 않고 자기 트랜스폼을 매 프레임 읽는다.
            Vector3 origin = MuzzleOrigin;

            // Transform.forward 는 이미 단위 벡터라 정규화가 필요 없다.
            Vector3 direction = Owner.CachedTransform.forward;

            // 아무것도 안 맞으면(사이에 콜라이더가 없다) 최대 사거리까지 빔만 그린다.
            Vector3 beamEndPoint = origin + direction * WeaponTable.LightningRange;

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit,
                                WeaponTable.LightningRange, WeaponUtil.HitscanMask))
            {
                beamEndPoint = hit.point;
                ApplyHitscanHit(hit);
            }

            PlayBolt(beamEndPoint);

            // 빗나가도 쿨다운은 소모한다(원본과 동일 — 원본은 Fire() 가 void 다).
            return true;
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
    }
}
