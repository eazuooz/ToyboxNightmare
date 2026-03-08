using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 업그레이드 선택 카드 하나. UpgradeForm이 동적으로 설정한다.
    /// </summary>
    public class UpgradeItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mNameText        = null;
        [SerializeField] private TextMeshProUGUI mDescriptionText = null;
        [SerializeField] private Button          mButton          = null;

        private Action mOnClick = null;

        private void Awake()
        {
            mButton.onClick.AddListener(() => mOnClick?.Invoke());
        }

        public void Setup(string name, string description, Action onClick)
        {
            mNameText.text        = name;
            mDescriptionText.text = description;
            mOnClick              = onClick;
        }
    }
}
