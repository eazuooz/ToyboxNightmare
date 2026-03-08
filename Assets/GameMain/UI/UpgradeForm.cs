using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 레벨업 시 표시되는 업그레이드 선택 UI.
    /// 3개의 UpgradeItemUI 자식을 순서대로 활성화한다.
    /// </summary>
    public class UpgradeForm : UIFormLogic
    {
        [SerializeField] private UpgradeItemUI[] mItems = null; // Inspector에서 3개 연결

        private List<UpgradeDefinition> mOptions = null;

        protected internal override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            Time.timeScale = 0f; // 게임 일시 정지

            mOptions = (userData as UpgradeFormData)?.Options;
            if (mOptions == null) return;

            for (int i = 0; i < mItems.Length; i++)
            {
                if (i < mOptions.Count)
                {
                    mItems[i].gameObject.SetActive(true);
                    int captured = i; // 클로저 캡처용
                    mItems[i].Setup(
                        mOptions[i].Name,
                        mOptions[i].Description,
                        () => OnSelectUpgrade(captured));
                }
                else
                {
                    mItems[i].gameObject.SetActive(false);
                }
            }
        }

        protected internal override void OnClose(bool isShutdown, object userData)
        {
            Time.timeScale = 1f; // 게임 재개
            base.OnClose(isShutdown, userData);
        }

        private void OnSelectUpgrade(int index)
        {
            mOptions[index].Apply();
            GameEntry.GetComponent<UIComponent>().CloseUIForm(UIForm);
        }
    }
}
