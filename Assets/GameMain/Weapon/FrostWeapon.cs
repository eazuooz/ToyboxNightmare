using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 빙결. 전방 원뿔 안의 적을 얼린다. <b>데미지는 0이다</b> — 원본도 그렇다.
    /// Lightning 같은 딜링 무기와 병행 장착하는 것이 전제다.
    ///
    /// 원본은 Arc 메시를 쓰는 convex 트리거 콜라이더로 Enter/Exit 을 받았는데,
    /// 그 구조는 "빙결 해제 대기 중 재진입 시 재부착 실패" 버그를 안고 있었다.
    /// 여기서는 매 틱 "지금 콘 안에 있는 집합"을 다시 계산하므로 Enter/Exit 개념이
    /// 사라지고 그 버그가 구조적으로 없어진다.
    /// </summary>
    public class FrostWeapon : WeaponBase
    {
        private const string FrostAttackPath = "Antenna/FrostAttack";
        private const string FrostConePath   = "Antenna/FrostAttack/FrostCone";
        private const string FrostArcPath    = "Antenna/FrostAttack/FrostArc";

        /// <summary>총구 스파클. 무기를 바꾸면 베이스가 꺼 준다.</summary>
        protected override string VfxRootPath => FrostAttackPath;

        /// <summary>Frost 는 자기 링이 없어 Stink 쪽 링을 템플릿으로 빌려 쓴다.</summary>
        protected override string TargetRingPath => "Antenna/StinkAttack/TargetRing";

        /// <summary><b>범위 무기다.</b> 반경 안 전원에게 마커를 띄운다.</summary>
        protected override bool MarkAllTargets => true;

        protected override float AttackRadius => WeaponTable.FrostConeRadius;

        /// <summary>
        /// 반경 <b>과</b> 각도를 둘 다 봐야 한다. 반경 안이지만 부채꼴 밖이면 노랑 —
        /// "콘을 돌리면 닿는다" 는 뜻이다.
        /// </summary>
        protected override TargetState EvaluateTarget(Enemy enemy)
        {
            Vector3 apex, axis;
            if (!TryGetCone(out apex, out axis))
            {
                return TargetState.OutOfRange;
            }

            float distance = WeaponUtil.PlanarDistance(enemy.CachedTransform.position, apex);
            if (distance > WeaponTable.FrostConeRadius)
            {
                return distance <= WeaponTable.FrostConeRadius * WeaponUtil.NearRangeScale
                    ? TargetState.Near
                    : TargetState.OutOfRange;
            }

            return IsInCone(enemy, apex, axis) ? TargetState.InRange : TargetState.Near;
        }

        /// <summary>지금 실제로 얼릴 적들. 마커 후보(반경)와 달리 각도까지 통과한 집합이다.</summary>
        private int CollectCone(System.Collections.Generic.List<Enemy> results)
        {
            results.Clear();

            Vector3 apex, axis;
            if (!TryGetCone(out apex, out axis))
            {
                return 0;
            }

            int count = WeaponUtil.FindEnemiesInSphere(apex, WeaponTable.FrostConeRadius, Candidates);
            for (int i = 0; i < count; i++)
            {
                if (IsInCone(Candidates[i], apex, axis))
                {
                    results.Add(Candidates[i]);
                }
            }

            return results.Count;
        }

        private bool TryGetCone(out Vector3 apex, out Vector3 axis)
        {
            apex = Vector3.zero;
            axis = Vector3.forward;

            if (mFrostAttack == null || Owner == null)
            {
                return false;
            }

            apex = mFrostAttack.TransformPoint(ApexLocal);

            axis = mFrostAttack.forward;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.0001f)
            {
                axis = Owner.CachedTransform.forward;
            }

            return true;
        }

        private static bool IsInCone(Enemy enemy, Vector3 apex, Vector3 axis)
        {
            Vector3 to = enemy.CachedTransform.position - apex;
            to.y = 0f;

            return Vector3.Angle(axis, to) <= WeaponTable.FrostConeHalfAngle;
        }

        /// <summary>콘 꼭짓점의 FrostAttack 로컬 좌표. 원본 Arc 메시 정점에서 역산했다.</summary>
        private static readonly Vector3 ApexLocal = new Vector3(-0.015296f, 0.044137f, 0.342790f);

        private const float TurnSpeed = 360f; // deg/s

        private Transform  mFrostAttack = null;
        private GameObject mFrostCone   = null;

        protected override void OnInitialize()
        {
            // 발사가 아니라 콘 재판정 주기다.
            AttackInterval = WeaponTable.FrostRetickInterval;

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

                    // 코드 판정으로 갈았으므로 원본 판정용 콜라이더는 꺼 둔다(이중 판정 방지).
                    Transform arc = Root.Find(FrostArcPath);
                    if (arc != null)
                    {
                        var meshCollider = arc.GetComponent<MeshCollider>();
                        if (meshCollider != null)
                        {
                            meshCollider.enabled = false;
                        }
                    }
                }
            }

            if (mFrostCone == null && mFrostAttack != null)
            {
                Transform cone = Root.Find(FrostConePath);
                mFrostCone = cone != null ? cone.gameObject : null;
            }

            SetConeActive(false);
        }

        protected override void Attack()
        {
            // 판정은 EvaluateTarget 의 InRange 조건과 완전히 같다 — 초록 링이 뜬 적이 곧 얼 적이다.
            //
            // 원본은 y 슬래브(±0.25)도 봤지만, 그건 적의 캡슐 콜라이더가 그 높이를
            // 관통했기 때문에 성립했다. 우리는 중심점 거리로 판정하므로 그대로 옮기면
            // 거의 아무도 안 맞는다. 의도적으로 생략한다.
            int count = CollectCone(mConeTargets);

            for (int i = 0; i < count; i++)
            {
                mConeTargets[i].RefreshFrostContact();
            }

            // 미스트와 루프 사운드는 대상이 있을 때만. 상시 켜두면 루프음이 게임 내내 울린다.
            SetConeActive(count > 0);
        }

        private readonly System.Collections.Generic.List<Enemy> mConeTargets =
            new System.Collections.Generic.List<Enemy>(16);

        /// <summary>
        /// 콘 조준. 예전에는 <c>LateUpdate</c> 였지만, 이제 플레이어의 입력 처리가 끝난 뒤
        /// <see cref="WeaponBase.OnUpdate"/> 가 불러 준다. 플레이어 회전은 FixedUpdate 에서
        /// 일어나고 그 포즈가 Update 시점에 이미 반영돼 있으므로 결과는 같다.
        /// </summary>
        protected override void OnAim(float elapseSeconds)
        {
            if (mFrostAttack == null)
            {
                return;
            }

            // 가장 가까운 적 쪽으로 콘을 부드럽게 돌린다. 캐릭터 본체는 돌리지 않는다
            // (플레이어 이동/조준 회전과 싸운다). yaw 만 건드린다.
            Enemy target = FindNearestEnemy(WeaponTable.FrostConeRadius);
            if (target == null)
            {
                return;
            }

            Vector3 to = target.CachedTransform.position - mFrostAttack.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion want = Quaternion.LookRotation(to);
            mFrostAttack.rotation = Quaternion.RotateTowards(
                mFrostAttack.rotation, want, TurnSpeed * elapseSeconds);
        }

        /// <summary>다른 무기로 전환되면 콘과 루프 사운드를 반드시 끈다.</summary>
        protected override void OnWeaponDisabled()
        {
            SetConeActive(false);
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
