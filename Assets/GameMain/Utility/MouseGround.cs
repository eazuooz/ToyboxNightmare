using UnityEngine;
using UnityEngine.InputSystem;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 마우스 커서 아래의 지면 좌표. 원본 좀비토이의 <c>MouseLocation</c> 에 해당한다.
    ///
    /// 원본은 씬에 싱글턴 MonoBehaviour 를 두고 매 프레임 갱신했지만, 여기서는
    /// <b>필요한 쪽이 필요할 때 직접 묻는</b> 순수 함수로 둔다 — 갱신 순서에 의존하지 않고,
    /// 씬에 오브젝트를 하나 더 두지 않아도 된다.
    ///
    /// 원본은 whatIsGround 레이어마스크로 실제 바닥 콜라이더를 때렸지만 우리는 y=0 평면을
    /// 쓴다. 이 게임의 바닥은 평면 하나뿐이라 결과가 같고, 콜라이더 유무에 흔들리지 않는다.
    /// </summary>
    public static class MouseGround
    {
        /// <summary>
        /// 커서 아래 지면 좌표를 구한다. 못 구하면 false — 호출부가 대체값을 정한다.
        /// 카메라가 없거나(씬 로드 중) 마우스가 없거나(패드 전용) 시선이 지면과
        /// 평행하면 실패한다. 전부 정상적으로 일어날 수 있어 로그를 남기지 않는다.
        /// </summary>
        public static bool TryGetGroundPoint(out Vector3 point)
        {
            point = Vector3.zero;

            Camera mainCamera = Camera.main;
            if (mainCamera == null || Mouse.current == null) return false;

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Ray     ray            = mainCamera.ScreenPointToRay(screenPosition);
            Plane   groundPlane    = new Plane(Vector3.up, Vector3.zero);

            if (!groundPlane.Raycast(ray, out float distance)) return false;

            point = ray.GetPoint(distance);
            return true;
        }
    }
}
