namespace ToyBoxNightmare
{
    /// <summary>
    /// UI 폼의 Addressables 주소와 UI 그룹 이름을 모아 둔다.
    /// <see cref="WeaponTable"/>·<see cref="EnemyTable"/> 와 같은 관례다 —
    /// 문자열을 코드 곳곳에 흩뿌리면 오타가 나도 <c>OpenUIForm</c> 이 조용히 실패한다.
    /// </summary>
    public static class UITable
    {
        /// <summary>
        /// 기본 UI 그룹. <c>GameFramework.prefab</c> 의 UIComponent.mUIGroups 에
        /// <b>같은 이름으로 등록돼 있어야 한다.</b> 없으면 OpenUIForm 이 실패한다.
        /// </summary>
        public const string DefaultGroup = "Default";

        /// <summary>
        /// HUD 폼의 Addressables 주소. Groups 창에 이 문자열로 등록돼 있어야 한다
        /// (CLAUDE.md 의 Addressables 규약 — 주소와 코드 문자열이 정확히 같아야 한다).
        /// </summary>
        public const string HudForm = "HUDForm";

        /// <summary>
        /// 일시정지 폼의 Addressables 주소.
        ///
        /// HUD 와 <b>같은 그룹</b>에 넣는다. 겹치는 순서는 UI 그룹 depth 가 아니라
        /// 각 폼 Canvas 의 sortingOrder 로 정한다 — 두 폼 모두 자기 Canvas(Screen Space Overlay)를
        /// 들고 있어서 그쪽이 실제로 렌더 순서를 결정하기 때문이다.
        /// (게다가 <c>DefaultUIGroupHelper.SetDepth</c> 는 빈 구현이라 그룹 depth 가 아무 일도 하지 않는다.)
        /// </summary>
        public const string PauseMenuForm = "PauseMenuForm";
    }
}
