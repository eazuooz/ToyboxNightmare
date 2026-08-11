using UnityEngine;

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
        }

        private void OnValidate()
        {
            RecalculateRotation();
        }

        private void RecalculateRotation()
        {
            if (offset.sqrMagnitude > 0.0001f)
            {
                mTargetRotation = Quaternion.LookRotation(-offset);
            }
        }

        private void LateUpdate()
        {
            float t = smoothing * Time.deltaTime;

            Player player = Player.Instance;
            Transform target = (player != null && player.Available) ? player.CachedTransform : null;

            if (target == null)
            {
                // 플레이어가 없다 = 캐릭터 선택 단계이거나 게임오버 직후다.
                // 여기서 그냥 return 하면 카메라가 플레이어가 죽은 자리에 얼어붙고,
                // 재시작 시 (∓2, 0, 0) 에 스폰되는 선택 캐릭터가 화면 밖이라
                // 클릭할 수 없어 영구 소프트락이 된다. 반드시 선택 앵글로 되돌린다.
                transform.position = Vector3.Lerp(transform.position, mSelectPosition, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, mSelectRotation, t);
                return;
            }

            transform.position = Vector3.Lerp(transform.position, target.position + offset, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, mTargetRotation, t);
        }
    }
}
