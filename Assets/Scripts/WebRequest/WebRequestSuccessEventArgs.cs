//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Event;

namespace UnityGameFramework.Runtime
{
    public sealed class WebRequestSuccessEventArgs : GameEventArgs
    {
        private byte[] m_WebResponseBytes = null;

        public static readonly int EventId = typeof(WebRequestSuccessEventArgs).GetHashCode();

        public WebRequestSuccessEventArgs()
        {
            SerialId = 0;
            WebRequestUri = null;
            m_WebResponseBytes = null;
            UserData = null;
        }

        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        public int SerialId
        {
            get;
            private set;
        }

        public string WebRequestUri
        {
            get;
            private set;
        }

        public object UserData
        {
            get;
            private set;
        }

        public byte[] GetWebResponseBytes()
        {
            return m_WebResponseBytes;
        }

        public static WebRequestSuccessEventArgs Create(GameFramework.WebRequest.WebRequestSuccessEventArgs e)
        {
            WWWFormInfo wwwFormInfo = (WWWFormInfo)e.UserData;
            WebRequestSuccessEventArgs webRequestSuccessEventArgs = ReferencePool.Acquire<WebRequestSuccessEventArgs>();
            webRequestSuccessEventArgs.SerialId = e.SerialId;
            webRequestSuccessEventArgs.WebRequestUri = e.WebRequestUri;
            webRequestSuccessEventArgs.m_WebResponseBytes = e.GetWebResponseBytes();
            webRequestSuccessEventArgs.UserData = wwwFormInfo.UserData;
            ReferencePool.Release(wwwFormInfo);
            return webRequestSuccessEventArgs;
        }

        public override void Clear()
        {
            SerialId = 0;
            WebRequestUri = null;
            m_WebResponseBytes = null;
            UserData = null;
        }
    }
}
