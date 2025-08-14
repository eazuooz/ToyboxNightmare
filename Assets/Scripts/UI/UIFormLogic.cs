//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public abstract class UIFormLogic : MonoBehaviour
    {
        private bool m_Available = false;
        private bool m_Visible = false;
        private UIForm m_UIForm = null;
        private Transform m_CachedTransform = null;
        private int m_OriginalLayer = 0;

        public UIForm UIForm
        {
            get
            {
                return m_UIForm;
            }
        }

        public string Name
        {
            get
            {
                return gameObject.name;
            }
            set
            {
                gameObject.name = value;
            }
        }

        public bool Available
        {
            get
            {
                return m_Available;
            }
        }

        public bool Visible
        {
            get
            {
                return m_Available && m_Visible;
            }
            set
            {
                if (!m_Available)
                {
                    Log.Warning("UI form '{0}' is not available.", Name);
                    return;
                }

                if (m_Visible == value)
                {
                    return;
                }

                m_Visible = value;
                InternalSetVisible(value);
            }
        }

        public Transform CachedTransform
        {
            get
            {
                return m_CachedTransform;
            }
        }

        protected internal virtual void OnInit(object userData)
        {
            if (m_CachedTransform == null)
            {
                m_CachedTransform = transform;
            }

            m_UIForm = GetComponent<UIForm>();
            m_OriginalLayer = gameObject.layer;
        }

        protected internal virtual void OnRecycle()
        {
        }

        protected internal virtual void OnOpen(object userData)
        {
            m_Available = true;
            Visible = true;
        }

        protected internal virtual void OnClose(bool isShutdown, object userData)
        {
            gameObject.SetLayerRecursively(m_OriginalLayer);
            Visible = false;
            m_Available = false;
        }

        protected internal virtual void OnPause()
        {
            Visible = false;
        }

        protected internal virtual void OnResume()
        {
            Visible = true;
        }

        protected internal virtual void OnCover()
        {
        }

        protected internal virtual void OnReveal()
        {
        }

        protected internal virtual void OnRefocus(object userData)
        {
        }

        protected internal virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        protected internal virtual void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
        }

        protected virtual void InternalSetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
