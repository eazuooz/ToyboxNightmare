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
                mFrostAttack = transform.Find(FrostAttackPath);
                if (mFrostAttack == null)
                {
                    Log.Warning("FrostWeapon: '{0}' 을 찾지 못했다.", FrostAttackPath);
                }
                else
                {
                    // 프리팹에 비활성으로 저장돼 있다. 이 GO 자신의 파티클이 총구 스파클이다.
                    mFrostAttack.gameObject.SetActive(true);

                    // 코드 판정으로 갈았으므로 원본 판정용 콜라이더는 꺼 둔다(이중 판정 방지).
                    Transform arc = transform.Find(FrostArcPath);
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
                Transform cone = transform.Find(FrostConePath);
                mFrostCone = cone != null ? cone.gameObject : null;
            }

            SetConeActive(false);
        }

        protected override void Attack()
        {
            if (mFrostAttack == null)
            {
                return;
            }

            Vector3 apex = mFrostAttack.TransformPoint(ApexLocal);

            Vector3 axis = mFrostAttack.forward;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.0001f)
            {
                axis = Owner.CachedTransform.forward;
            }

            int count = WeaponUtil.FindEnemiesInSphere(apex, WeaponTable.FrostConeRadius, Candidates);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 to = Candidates[i].CachedTransform.position - apex;
                to.y = 0f;

                // 원본은 y 슬래브(±0.25)도 봤지만, 그건 적의 캡슐 콜라이더가 그 높이를
                // 관통했기 때문에 성립했다. 우리는 중심점 거리로 판정하므로 그대로 옮기면
                // 거의 아무도 안 맞는다. 의도적으로 생략한다.
                if (Vector3.Angle(axis, to) > WeaponTable.FrostConeHalfAngle)
                {
                    continue;
                }

                Candidates[i].RefreshFrostContact();
                hitCount++;
            }

            // 미스트와 루프 사운드는 대상이 있을 때만. 상시 켜두면 루프음이 게임 내내 울린다.
            SetConeActive(hitCount > 0);
        }

        private void LateUpdate()
        {
            if (Owner == null || !Owner.Available || Owner.IsDead)
            {
                // WeaponBase.Update 가 여기서 return 하므로 Attack() 이 안 돈다.
                // 정리를 안 하면 콘이 켜진 채로 남는다.
                SetConeActive(false);
                return;
            }

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
                mFrostAttack.rotation, want, TurnSpeed * Time.deltaTime);
        }

        /// <summary>다른 무기로 전환되면 콘과 루프 사운드를 반드시 끈다.</summary>
        private void OnDisable()
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
