//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework.UI;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public class DefaultUIFormHelper : UIFormHelperBase
    {
        private ResourceComponent m_ResourceComponent = null;

        public override object InstantiateUIForm(object uiFormAsset)
        {
            return Instantiate((Object)uiFormAsset);
        }

        public override IUIForm CreateUIForm(object uiFormInstance, IUIGroup uiGroup, object userData)
        {
            GameObject gameObject = uiFormInstance as GameObject;
            if (gameObject == null)
            {
                Log.Error("UI form instance is invalid.");
                return null;
            }

            Transform transform = gameObject.transform;
            transform.SetParent(((MonoBehaviour)uiGroup.Helper).transform);
            transform.localScale = Vector3.one;

            return gameObject.GetOrAddComponent<UIForm>();
        }

        public override void ReleaseUIForm(object uiFormAsset, object uiFormInstance)
        {
            m_ResourceComponent.UnloadAsset(uiFormAsset);
            Destroy((Object)uiFormInstance);
        }

        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
            if (m_ResourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return;
            }
        }
    }
}
