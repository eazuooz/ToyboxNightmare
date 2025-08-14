//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.WebRequest;
using System;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif
using Utility = GameFramework.Utility;

namespace UnityGameFramework.Runtime
{
    public class UnityWebRequestAgentHelper : WebRequestAgentHelperBase, IDisposable
    {
        private UnityWebRequest m_UnityWebRequest = null;
        private bool m_Disposed = false;

        private EventHandler<WebRequestAgentHelperCompleteEventArgs> m_WebRequestAgentHelperCompleteEventHandler = null;
        private EventHandler<WebRequestAgentHelperErrorEventArgs> m_WebRequestAgentHelperErrorEventHandler = null;

        public override event EventHandler<WebRequestAgentHelperCompleteEventArgs> WebRequestAgentHelperComplete
        {
            add
            {
                m_WebRequestAgentHelperCompleteEventHandler += value;
            }
            remove
            {
                m_WebRequestAgentHelperCompleteEventHandler -= value;
            }
        }

        public override event EventHandler<WebRequestAgentHelperErrorEventArgs> WebRequestAgentHelperError
        {
            add
            {
                m_WebRequestAgentHelperErrorEventHandler += value;
            }
            remove
            {
                m_WebRequestAgentHelperErrorEventHandler -= value;
            }
        }

        public override void Request(string webRequestUri, object userData)
        {
            if (m_WebRequestAgentHelperCompleteEventHandler == null || m_WebRequestAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Web request agent helper handler is invalid.");
                return;
            }

            WWWFormInfo wwwFormInfo = (WWWFormInfo)userData;
            if (wwwFormInfo.WWWForm == null)
            {
                m_UnityWebRequest = UnityWebRequest.Get(webRequestUri);
            }
            else
            {
                m_UnityWebRequest = UnityWebRequest.Post(webRequestUri, wwwFormInfo.WWWForm);
            }

#if UNITY_2017_2_OR_NEWER
            m_UnityWebRequest.SendWebRequest();
#else
            m_UnityWebRequest.Send();
#endif
        }

        public override void Request(string webRequestUri, byte[] postData, object userData)
        {
            if (m_WebRequestAgentHelperCompleteEventHandler == null || m_WebRequestAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Web request agent helper handler is invalid.");
                return;
            }

            m_UnityWebRequest = UnityWebRequest.PostWwwForm(webRequestUri, Utility.Converter.GetString(postData));
#if UNITY_2017_2_OR_NEWER
            m_UnityWebRequest.SendWebRequest();
#else
            m_UnityWebRequest.Send();
#endif
        }

        public override void Reset()
        {
            if (m_UnityWebRequest != null)
            {
                m_UnityWebRequest.Dispose();
                m_UnityWebRequest = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
            {
                return;
            }

            if (disposing)
            {
                if (m_UnityWebRequest != null)
                {
                    m_UnityWebRequest.Dispose();
                    m_UnityWebRequest = null;
                }
            }

            m_Disposed = true;
        }

        private void Update()
        {
            if (m_UnityWebRequest == null || !m_UnityWebRequest.isDone)
            {
                return;
            }

            bool isError = false;
#if UNITY_2020_2_OR_NEWER
            isError = m_UnityWebRequest.result != UnityWebRequest.Result.Success;
#elif UNITY_2017_1_OR_NEWER
            isError = m_UnityWebRequest.isNetworkError || m_UnityWebRequest.isHttpError;
#else
            isError = m_UnityWebRequest.isError;
#endif
            if (isError)
            {
                WebRequestAgentHelperErrorEventArgs webRequestAgentHelperErrorEventArgs = WebRequestAgentHelperErrorEventArgs.Create(m_UnityWebRequest.error);
                m_WebRequestAgentHelperErrorEventHandler(this, webRequestAgentHelperErrorEventArgs);
                ReferencePool.Release(webRequestAgentHelperErrorEventArgs);
            }
            else if (m_UnityWebRequest.downloadHandler.isDone)
            {
                WebRequestAgentHelperCompleteEventArgs webRequestAgentHelperCompleteEventArgs = WebRequestAgentHelperCompleteEventArgs.Create(m_UnityWebRequest.downloadHandler.data);
                m_WebRequestAgentHelperCompleteEventHandler(this, webRequestAgentHelperCompleteEventArgs);
                ReferencePool.Release(webRequestAgentHelperCompleteEventArgs);
            }
        }
    }
}
