//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework.UI;
using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public sealed class UIForm : MonoBehaviour, IUIForm
    {
        private int m_SerialId;
        private string m_UIFormAssetName;
        private IUIGroup m_UIGroup;
        private int m_DepthInUIGroup;
        private bool m_PauseCoveredUIForm;
        private UIFormLogic m_UIFormLogic;

        public int SerialId
        {
            get
            {
                return m_SerialId;
            }
        }

        public string UIFormAssetName
        {
            get
            {
                return m_UIFormAssetName;
            }
        }

        public object Handle
        {
            get
            {
                return gameObject;
            }
        }

        public IUIGroup UIGroup
        {
            get
            {
                return m_UIGroup;
            }
        }

        public int DepthInUIGroup
        {
            get
            {
                return m_DepthInUIGroup;
            }
        }

        public bool PauseCoveredUIForm
        {
            get
            {
                return m_PauseCoveredUIForm;
            }
        }

        public UIFormLogic Logic
        {
            get
            {
                return m_UIFormLogic;
            }
        }

        public void OnInit(int serialId, string uiFormAssetName, IUIGroup uiGroup, bool pauseCoveredUIForm, bool isNewInstance, object userData)
        {
            m_SerialId = serialId;
            m_UIFormAssetName = uiFormAssetName;
            m_UIGroup = uiGroup;
            m_DepthInUIGroup = 0;
            m_PauseCoveredUIForm = pauseCoveredUIForm;

            if (!isNewInstance)
            {
                return;
            }

            m_UIFormLogic = GetComponent<UIFormLogic>();
            if (m_UIFormLogic == null)
            {
                Log.Error("UI form '{0}' can not get UI form logic.", uiFormAssetName);
                return;
            }

            try
            {
                m_UIFormLogic.OnInit(userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnInit with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnRecycle()
        {
            try
            {
                m_UIFormLogic.OnRecycle();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnRecycle with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }

            m_SerialId = 0;
            m_DepthInUIGroup = 0;
            m_PauseCoveredUIForm = true;
        }

        public void OnOpen(object userData)
        {
            try
            {
                m_UIFormLogic.OnOpen(userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnOpen with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnClose(bool isShutdown, object userData)
        {
            try
            {
                m_UIFormLogic.OnClose(isShutdown, userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnClose with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnPause()
        {
            try
            {
                m_UIFormLogic.OnPause();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnPause with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnResume()
        {
            try
            {
                m_UIFormLogic.OnResume();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnResume with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnCover()
        {
            try
            {
                m_UIFormLogic.OnCover();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnCover with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnReveal()
        {
            try
            {
                m_UIFormLogic.OnReveal();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnReveal with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnRefocus(object userData)
        {
            try
            {
                m_UIFormLogic.OnRefocus(userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnRefocus with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            try
            {
                m_UIFormLogic.OnUpdate(elapseSeconds, realElapseSeconds);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnUpdate with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }

        public void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            m_DepthInUIGroup = depthInUIGroup;
            try
            {
                m_UIFormLogic.OnDepthChanged(uiGroupDepth, depthInUIGroup);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnDepthChanged with exception '{2}'.", m_SerialId, m_UIFormAssetName, exception);
            }
        }
    }
}
