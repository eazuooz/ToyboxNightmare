using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 캐릭터 선택 화면의 스포트라이트. 커서를 따라다니다가 캐릭터가 정해지면 꺼진다.
    ///
    /// 원본은 <c>CharacterSpotlight</c> 와 <c>LookAtMouse</c> 두 스크립트로 나뉘어 있었고
    /// 각각 <c>GameManager.Instance</c> / <c>MouseLocation.Instance</c> 싱글턴에 의존했다.
    /// 우리에겐 그 둘이 없어 그대로 쓰면 즉시 NRE 다. 하나로 합쳐 다시 썼다.
    ///
    /// <b>자기 GameObject 를 끄지 않는다.</b> 원본은 선택이 끝나면 <c>SetActive(false)</c> 로
    /// 스스로를 껐는데, 원본은 재시작이 씬 리로드라 그래도 됐다. 우리는 프로시저 전이로
    /// 재시작하므로 그렇게 하면 <b>두 번째 판부터 스포트라이트가 영영 안 켜진다.</b>
    /// 그래서 조명 자식만 켜고 끈다.
    /// </summary>
    public class CharacterSelectSpotlight : MonoBehaviour
    {
        [Tooltip("커서를 따라다닐 조명. 프리팹의 'Select spot PC' 자식이다.")]
        [SerializeField] private GameObject spotlight = null;

        private void Start()
        {
            if (spotlight == null)
            {
                Log.Warning("CharacterSelectSpotlight: 조명 자식이 지정되지 않았다. 선택 화면 연출이 나오지 않는다.");
            }
        }

        private void Update()
        {
            bool isSelecting = IsSelecting();

            SetSpotlightActive(isSelecting);

            if (!isSelecting || spotlight == null) return;

            AimAtCursor();
        }

        /// <summary>아직 캐릭터를 고르는 중인가. 플레이어가 살아 있으면 선택은 끝난 것이다.</summary>
        private static bool IsSelecting()
        {
            Player player = Player.Instance;

            return player == null || !player.Available;
        }

        private void SetSpotlightActive(bool active)
        {
            if (spotlight == null) return;
            if (spotlight.activeSelf == active) return;

            spotlight.SetActive(active);
        }

        /// <summary>커서가 가리키는 지면을 비춘다. 지면을 못 잡은 프레임은 직전 각도를 유지한다.</summary>
        private void AimAtCursor()
        {
            if (!MouseGround.TryGetGroundPoint(out Vector3 groundPoint)) return;

            spotlight.transform.LookAt(groundPoint);
        }
    }
}
