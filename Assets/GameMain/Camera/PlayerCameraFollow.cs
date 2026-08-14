using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어를 따라다니는 카메라. MainCamera 에 붙인다.
    ///
    /// 원본 ZombieToys 의 CameraFollow 를 대체한다(그쪽은 GameManager.Instance 를
    /// 참조해 이 프로젝트에서는 NRE 가 난다).
    ///
    /// 플레이어가 아직 없는 캐릭터 선택 단계에서는 아무것도 하지 않으므로,
    /// 카메라는 씬에 배치된 선택 앵글에 그대로 머문다. 플레이어가 스폰되면
    /// 위치와 회전을 함께 보간해 게임 앵글로 넘어간다.
    /// </summary>
    public class PlayerCameraFollow : MonoBehaviour
    {
        // offset 이 이보다 짧으면 방향을 구할 수 없다. 길이 0 벡터를 LookRotation 에
        // 넘기면 Unity 가 에러를 뱉는다.
        private const float MinOffsetSqrMagnitude = 0.0001f;

        [Tooltip("플레이어 기준 카메라 위치. 원본 게임 앵글은 (0, 15, -22).")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -22f);

        [Tooltip("추종 보간 속도. 클수록 빠르게 따라붙는다. 원본은 5.")]
        [SerializeField] private float smoothing = 5f;

        // offset 의 반대 방향을 바라보면 항상 플레이어를 향한다.
        // 회전을 따로 하드코딩하지 않으므로 offset 만 바꾸면 각도가 함께 맞는다.
        private Quaternion mTargetRotation = Quaternion.identity;

        // 씬에 배치된 원본 위치/회전 = 캐릭터 선택 앵글. 플레이어가 사라지면 여기로 돌아간다.
        private Vector3    mSelectPosition = Vector3.zero;
        private Quaternion mSelectRotation = Quaternion.identity;

        private void Start()
        {
            mSelectPosition = transform.position;
            mSelectRotation = transform.rotation;
            RecalculateRotation();

            WarnIfMisconfigured();
        }

        private void OnValidate()
        {
            RecalculateRotation();
        }

        private void LateUpdate()
        {
            // 프레임 독립 보간 계수. Lerp/Slerp 가 내부에서 [0, 1] 로 클램프한다.
            float smoothStep = smoothing * Time.deltaTime;

            Transform followTarget = FindFollowTarget();
            if (followTarget == null)
            {
                MoveTowardSelectAngle(smoothStep);
                return;
            }

            MoveTowardPlayerAngle(followTarget, smoothStep);
        }

        /// <summary>
        /// 따라갈 플레이어의 Transform. 아직 스폰 전이거나 이미 회수됐으면 null.
        /// 엔티티는 풀에서 재사용되므로 참조가 살아 있어도 Available 을 확인해야 한다.
        /// </summary>
        private static Transform FindFollowTarget()
        {
            Player player = Player.Instance;
            if (player == null || !player.Available) return null;

            return player.CachedTransform;
        }

        /// <summary>
        /// 플레이어가 없다 = 캐릭터 선택 단계이거나 게임오버 직후다.
        /// 여기서 아무것도 하지 않으면 카메라가 플레이어가 죽은 자리에 얼어붙고,
        /// 재시작 시 (∓2, 0, 0) 에 스폰되는 선택 캐릭터가 화면 밖이라
        /// 클릭할 수 없어 영구 소프트락이 된다. 반드시 선택 앵글로 되돌린다.
        /// </summary>
        private void MoveTowardSelectAngle(float smoothStep)
        {
            transform.position = Vector3.Lerp(transform.position, mSelectPosition, smoothStep);
            transform.rotation = Quaternion.Slerp(transform.rotation, mSelectRotation, smoothStep);
        }

        private void MoveTowardPlayerAngle(Transform followTarget, float smoothStep)
        {
            transform.position = Vector3.Lerp(transform.position, followTarget.position + offset, smoothStep);
            transform.rotation = Quaternion.Slerp(transform.rotation, mTargetRotation, smoothStep);
        }

        private void RecalculateRotation()
        {
            if (offset.sqrMagnitude <= MinOffsetSqrMagnitude)
            {
                // 방향을 못 구하는 상황이다. 마지막으로 성립했던 회전을 그대로 둔다.
                return;
            }

            mTargetRotation = Quaternion.LookRotation(-offset);
        }

        /// <summary>
        /// 인스펙터 설정 실수 방어. 둘 다 증상이 "카메라가 안 따라온다" 하나로만 보여서
        /// 원인을 짚기 어렵기 때문에 기동 시점에 한 번 알린다.
        /// </summary>
        private void WarnIfMisconfigured()
        {
            if (smoothing <= 0f)
            {
                Log.Warning("PlayerCameraFollow: smoothing 이 {0} 이라 카메라가 제자리에 멈춘다.", smoothing);
            }

            if (offset.sqrMagnitude <= MinOffsetSqrMagnitude)
            {
                Log.Warning("PlayerCameraFollow: offset 이 사실상 0 이라 카메라가 플레이어와 겹치고 회전도 갱신되지 않는다.");
            }
        }
    }
}
