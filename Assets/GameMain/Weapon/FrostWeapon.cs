using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 빙결. 발사 버튼을 누르는 동안 콘을 켜고 그 안의 적을 얼린다. <b>데미지는 0이다</b> — 원본도 그렇다.
    /// Lightning 같은 딜링 무기와 병행 장착하는 것이 전제다.
    ///
    /// <b>쿨다운이 없다</b>(원본과 동일). 콘 방향은 캐릭터 전방 고정이고, 캐릭터가 마우스를
    /// 바라보므로 결과적으로 마우스 방향이 된다 — 원본도 콘을 적 쪽으로 돌리지 않는다.
    ///
    /// 원본은 Arc 메시를 쓰는 convex 트리거 콜라이더로 Enter/Exit 을 받았는데,
    /// 그 구조는 "빙결 해제 대기 중 재진입 시 재부착 실패" 버그를 안고 있었다.
    /// 여기서는 매 프레임 "지금 콘 안에 있는 집합"을 다시 계산하므로 Enter/Exit 개념이
    /// 사라지고 그 버그가 구조적으로 없어진다.
    /// </summary>
    public class FrostWeapon : WeaponBase
    {
        private const string FrostAttackPath = "Antenna/FrostAttack";
        private const string FrostConePath   = "Antenna/FrostAttack/FrostCone";
        private const string FrostArcPath    = "Antenna/FrostAttack/FrostArc";

        /// <summary>콘 꼭짓점의 FrostAttack 로컬 좌표. 원본 Arc 메시 정점에서 역산했다.</summary>
        private static readonly Vector3 ApexLocal = new Vector3(-0.015296f, 0.044137f, 0.342790f);

        /// <summary>방향 벡터로 쓰기엔 너무 짧다고 볼 제곱 길이. 이보다 짧으면 정규화가 무의미하다.</summary>
        private const float MinDirectionSqrMagnitude = 0.0001f;

        /// <summary>
        /// 콘이 지금 어디에 어떻게 놓여 있는지 — 꼭짓점과 바라보는 축.
        /// 이 둘은 항상 같이 구해서 같이 쓰이므로 한 덩어리로 묶었다.
        /// </summary>
        private struct ConePose
        {
            public Vector3 Apex;
            public Vector3 Axis;
        }

        private Transform  mFrostAttack = null;
        private GameObject mFrostCone   = null;

        /// <summary>발사 버튼을 누르고 있는가. 콘을 켜 둘지와 재판정을 돌릴지를 이걸로 정한다.</summary>
        private bool mFiring = false;

        /// <summary>이번 프레임에 얼릴 적들. 매 프레임 재사용해 할당을 없앤다.</summary>
        private readonly List<Enemy> mConeTargets = new List<Enemy>(16);

        /// <summary>반경 안 후보 스크래치. 위와 같은 이유로 필드에 둔다.</summary>
        private readonly List<Enemy> mCandidates = new List<Enemy>(16);

        /// <summary>총구 스파클. 무기를 바꾸면 베이스가 꺼 준다.</summary>
        protected override string VfxRootPath => FrostAttackPath;

        /// <summary>원본에 쿨다운이 없다. 발사해도 로드아웃이 아무것도 걸지 않는다.</summary>
        public override float Cooldown => 0f;

        /// <summary>
        /// Root 가 null 인 경우는 없다 — <see cref="WeaponBase.Initialize"/> 가 owner 없이는
        /// 여기까지 오지 않는다.
        /// </summary>
        protected override void OnInitialize()
        {
            mFiring = false;

            ResolveConeTransforms();
            SetConeActive(false);
        }

        private void ResolveConeTransforms()
        {
            if (mFrostAttack == null)
            {
                mFrostAttack = Root.Find(FrostAttackPath);
                if (mFrostAttack == null)
                {
                    Log.Warning("FrostWeapon: '{0}' 을 찾지 못했다.", FrostAttackPath);
                }
                else
                {
                    // 활성/비활성은 베이스가 VfxRootPath 로 관리한다. 여기서 켜면
                    // 다른 무기로 바꿔도 총구 파티클이 남는다.
                    DisableLegacyArcCollider();
                }
            }

            if (mFrostCone == null && mFrostAttack != null)
            {
                Transform cone = Root.Find(FrostConePath);
                mFrostCone = cone != null ? cone.gameObject : null;
            }
        }

        /// <summary>코드 판정으로 갈았으므로 원본 판정용 콜라이더는 꺼 둔다(이중 판정 방지).</summary>
        private void DisableLegacyArcCollider()
        {
            Transform arc = Root.Find(FrostArcPath);
            if (arc == null) return;

            MeshCollider meshCollider = arc.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.enabled = false;
            }
        }

        /// <summary>
        /// 누르는 동안 콘을 켠다. <b>false 를 돌려준다</b> — 쿨다운을 소모하지 않는 발사라
        /// 로드아웃이 전역 쿨다운을 걸면 안 된다.
        /// </summary>
        protected override bool OnFireHeld()
        {
            mFiring = true;
            SetConeActive(true);

            return false;
        }

        /// <summary>
        /// 버튼을 뗀 프레임. 로드아웃이 <b>쿨다운과 무관하게</b> 불러 주므로 콘이 남지 않는다.
        /// (원본은 이 경로에도 ReadyToAttack 검사가 걸려 있어, 직전에 Stink(5초)를 쏘고
        ///  Frost 로 바꾸면 버튼을 떼도 콘이 안 꺼졌다. 명백한 결함이라 이식하지 않았다.)
        /// </summary>
        protected override void OnStopFiring()
        {
            mFiring = false;
            SetConeActive(false);
        }

        /// <summary>
        /// 콘이 켜져 있는 동안의 재판정. Frost 는 레티클도 쿨다운도 없어
        /// <paramref name="isReady"/> 를 보지 않는다 — 활성 무기에 대해 매 프레임 불리는
        /// 훅이 이것뿐이라 여기에 얹었다.
        /// </summary>
        protected override void OnUpdateAim(bool isReady)
        {
            if (!mFiring) return;

            // 원본은 y 슬래브(±0.25)도 봤지만, 그건 적의 캡슐 콜라이더가 그 높이를
            // 관통했기 때문에 성립했다. 우리는 중심점 거리로 판정하므로 그대로 옮기면
            // 거의 아무도 안 맞는다. 의도적으로 생략한다.
            int frozenCount = CollectCone(mConeTargets);

            for (int i = 0; i < frozenCount; i++)
            {
                mConeTargets[i].RefreshFrostContact();
            }
        }

        /// <summary>지금 실제로 얼릴 적들 — 반경과 부채꼴 각도를 둘 다 통과한 집합이다.</summary>
        private int CollectCone(List<Enemy> results)
        {
            results.Clear();

            ConePose cone;
            if (!TryGetCone(out cone)) return 0;

            int candidateCount = WeaponUtil.FindEnemiesInSphere(
                cone.Apex, WeaponTable.FrostConeRadius, mCandidates);

            for (int i = 0; i < candidateCount; i++)
            {
                if (IsInCone(mCandidates[i], cone))
                {
                    results.Add(mCandidates[i]);
                }
            }

            return results.Count;
        }

        /// <summary>
        /// 콘의 현재 자세. 축은 <b>캐릭터 전방</b>이다 — 콘을 더 이상 돌리지 않으므로
        /// FrostAttack 의 전방이 곧 캐릭터 전방이고, 판정을 눈에 보이는 콘과 같은
        /// 트랜스폼에서 뽑아야 둘이 어긋나지 않는다.
        /// </summary>
        private bool TryGetCone(out ConePose cone)
        {
            cone = new ConePose { Apex = Vector3.zero, Axis = Vector3.forward };

            if (mFrostAttack == null || Owner == null) return false;

            cone.Apex = mFrostAttack.TransformPoint(ApexLocal);

            Vector3 axis = mFrostAttack.forward;
            axis.y = 0f;

            // 안테나가 정확히 수직을 보고 있으면 수평 성분이 사라진다. 그때만 캐릭터 정면으로 대신한다.
            if (axis.sqrMagnitude < MinDirectionSqrMagnitude)
            {
                axis   = Owner.CachedTransform.forward;
                axis.y = 0f;
            }

            // 축은 반드시 수평이어야 한다. IsInCone 은 적까지의 상대 벡터만 눕히므로,
            // 축에 y 가 남으면 두 벡터 사이 각이 부풀려져 부채꼴이 실제보다 좁아진다.
            // (폴백 경로에서 이 평탄화가 빠져 있던 것이 W7 의 버그다.)
            if (axis.sqrMagnitude < MinDirectionSqrMagnitude)
            {
                // 캐릭터까지 수직을 보고 있다. 수평 방향을 만들 수 없으니 이번 판정은 포기한다.
                return false;
            }

            cone.Axis = axis;
            return true;
        }

        private static bool IsInCone(Enemy enemy, ConePose cone)
        {
            Vector3 toEnemy = enemy.CachedTransform.position - cone.Apex;
            toEnemy.y = 0f;

            return Vector3.Angle(cone.Axis, toEnemy) <= WeaponTable.FrostConeHalfAngle;
        }

        /// <summary>다른 무기로 전환되면 콘과 루프 사운드를 반드시 끈다.</summary>
        protected override void OnWeaponDisabled()
        {
            mFiring = false;
            SetConeActive(false);
        }

        /// <summary>
        /// 캐릭터가 바뀌면 이 둘은 남의 트랜스폼이 된다. 버려서 다음 Initialize 가 다시 찾게 한다 —
        /// <see cref="ResolveConeTransforms"/> 가 <c>if (mX == null)</c> 로만 걸러서,
        /// 여기서 안 버리면 옛 캐릭터의 콘을 계속 켜게 된다.
        /// </summary>
        protected override void OnDispose()
        {
            mFrostAttack = null;
            mFrostCone   = null;
            mFiring      = false;
        }

        private void SetConeActive(bool active)
        {
            if (mFrostCone != null && mFrostCone.activeSelf != active)
            {
                mFrostCone.SetActive(active);
            }
        }
    }
}
