using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 이 게임의 UI 폼 공통 베이스. <see cref="EntityLogicBase"/> 의 UI 쪽 짝이다.
    ///
    /// <b>폼이 겹치는 순서를 프레임워크의 UI 그룹이 정하게 한다.</b> UIComponent 는 폼을 열고
    /// 닫고 앞으로 가져올 때마다 <see cref="OnDepthChanged"/> 로 "몇 번째 깊이의 그룹에서
    /// 몇 번째 폼인가" 를 알려 준다. 여기서 그 값을 폼 Canvas 의 sortingOrder 로 옮긴다.
    /// 그래서 프리팹 Canvas 에 적힌 sortingOrder 는 쓰이지 않는다 — 순서를 바꾸려면
    /// GameFramework.prefab 의 UI 그룹 깊이나, 폼을 여는 그룹(<see cref="UITable"/>)을 바꾼다.
    ///
    /// 프레임워크 기본 <c>DefaultUIGroupHelper.SetDepth</c> 는 아무것도 하지 않는다.
    /// 그룹 깊이를 화면 순서로 옮기는 일은 원래 게임 쪽이 폼에서 하게 되어 있다.
    /// </summary>
    public abstract class UIFormBase : UIFormLogic
    {
        /// <summary>UI 그룹 깊이 1 이 차지하는 sortingOrder 폭. 그룹 하나에 폼이 100개를 넘지 않는 한 그룹끼리 섞이지 않는다.</summary>
        private const int GroupDepthFactor = 10000;

        /// <summary>그룹 안에서 폼 하나가 차지하는 sortingOrder 폭. 폼 안에 하위 Canvas 를 둘 여유를 남긴다.</summary>
        private const int FormDepthFactor = 100;

        private Canvas mCanvas = null;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mCanvas = GetComponent<Canvas>();
            if (mCanvas == null)
            {
                Log.Warning("{0}: 루트에 Canvas 가 없다. UI 그룹 깊이가 화면 순서에 반영되지 않는다.", Name);
            }
        }

        /// <summary>
        /// 그룹 안의 순서가 바뀔 때마다 불린다. 폼이 열린 직후에도 한 번 온다
        /// (코어 <c>UIManager.InternalOpenUIForm</c> 이 OnOpen 다음에 그룹을 Refresh 한다).
        /// </summary>
        protected internal override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            base.OnDepthChanged(uiGroupDepth, depthInUIGroup);

            if (mCanvas == null) return;

            mCanvas.sortingOrder = GroupDepthFactor * uiGroupDepth + FormDepthFactor * depthInUIGroup;
        }
    }
}
