//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


#if !UNITY_2018_3_OR_NEWER

using GameFramework;
using GameFramework.WebRequest;
using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public class WWWWebRequestAgentHelper : WebRequestAgentHelperBase, IDisposable
    {
        private WWW m_WWW = null;
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
                m_WWW = new WWW(webRequestUri);
            }
            else
            {
                m_WWW = new WWW(webRequestUri, wwwFormInfo.WWWForm);
            }
        }

        public override void Request(string webRequestUri, byte[] postData, object userData)
        {
            if (m_WebRequestAgentHelperCompleteEventHandler == null || m_WebRequestAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Web request agent helper handler is invalid.");
                return;
            }

            m_WWW = new WWW(webRequestUri, postData);
        }

        public override void Reset()
        {
            if (m_WWW != null)
            {
                m_WWW.Dispose();
                m_WWW = null;
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
                if (m_WWW != null)
                {
                    m_WWW.Dispose();
                    m_WWW = null;
                }
            }

            m_Disposed = true;
        }

        private void Update()
        {
            if (m_WWW == null || !m_WWW.isDone)
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_WWW.error))
            {
                WebRequestAgentHelperErrorEventArgs webRequestAgentHelperErrorEventArgs = WebRequestAgentHelperErrorEventArgs.Create(m_WWW.error);
                m_WebRequestAgentHelperErrorEventHandler(this, webRequestAgentHelperErrorEventArgs);
                ReferencePool.Release(webRequestAgentHelperErrorEventArgs);
            }
            else
            {
                WebRequestAgentHelperCompleteEventArgs webRequestAgentHelperCompleteEventArgs = WebRequestAgentHelperCompleteEventArgs.Create(m_WWW.bytes);
                m_WebRequestAgentHelperCompleteEventHandler(this, webRequestAgentHelperCompleteEventArgs);
                ReferencePool.Release(webRequestAgentHelperCompleteEventArgs);
            }
        }
    }
}

#endif
