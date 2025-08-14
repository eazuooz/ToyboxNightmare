//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Download;
using System;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif
using Utility = GameFramework.Utility;

namespace UnityGameFramework.Runtime
{
    public partial class UnityWebRequestDownloadAgentHelper : DownloadAgentHelperBase, IDisposable
    {
        private const int CachedBytesLength = 0x1000;
        private readonly byte[] m_CachedBytes = new byte[CachedBytesLength];

        private UnityWebRequest m_UnityWebRequest = null;
        private bool m_Disposed = false;

        private EventHandler<DownloadAgentHelperUpdateBytesEventArgs> m_DownloadAgentHelperUpdateBytesEventHandler = null;
        private EventHandler<DownloadAgentHelperUpdateLengthEventArgs> m_DownloadAgentHelperUpdateLengthEventHandler = null;
        private EventHandler<DownloadAgentHelperCompleteEventArgs> m_DownloadAgentHelperCompleteEventHandler = null;
        private EventHandler<DownloadAgentHelperErrorEventArgs> m_DownloadAgentHelperErrorEventHandler = null;

        public override event EventHandler<DownloadAgentHelperUpdateBytesEventArgs> DownloadAgentHelperUpdateBytes
        {
            add
            {
                m_DownloadAgentHelperUpdateBytesEventHandler += value;
            }
            remove
            {
                m_DownloadAgentHelperUpdateBytesEventHandler -= value;
            }
        }

        public override event EventHandler<DownloadAgentHelperUpdateLengthEventArgs> DownloadAgentHelperUpdateLength
        {
            add
            {
                m_DownloadAgentHelperUpdateLengthEventHandler += value;
            }
            remove
            {
                m_DownloadAgentHelperUpdateLengthEventHandler -= value;
            }
        }

        public override event EventHandler<DownloadAgentHelperCompleteEventArgs> DownloadAgentHelperComplete
        {
            add
            {
                m_DownloadAgentHelperCompleteEventHandler += value;
            }
            remove
            {
                m_DownloadAgentHelperCompleteEventHandler -= value;
            }
        }

        public override event EventHandler<DownloadAgentHelperErrorEventArgs> DownloadAgentHelperError
        {
            add
            {
                m_DownloadAgentHelperErrorEventHandler += value;
            }
            remove
            {
                m_DownloadAgentHelperErrorEventHandler -= value;
            }
        }

        public override void Download(string downloadUri, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null || m_DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            m_UnityWebRequest = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.downloadHandler = new DownloadHandler(this);
#if UNITY_2017_2_OR_NEWER
            m_UnityWebRequest.SendWebRequest();
#else
            m_UnityWebRequest.Send();
#endif
        }

        public override void Download(string downloadUri, long fromPosition, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null || m_DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            m_UnityWebRequest = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-", fromPosition));
            m_UnityWebRequest.downloadHandler = new DownloadHandler(this);
#if UNITY_2017_2_OR_NEWER
            m_UnityWebRequest.SendWebRequest();
#else
            m_UnityWebRequest.Send();
#endif
        }

        public override void Download(string downloadUri, long fromPosition, long toPosition, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null || m_DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            m_UnityWebRequest = new UnityWebRequest(downloadUri);
            m_UnityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-{1}", fromPosition, toPosition));
            m_UnityWebRequest.downloadHandler = new DownloadHandler(this);
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
                m_UnityWebRequest.Abort();
                m_UnityWebRequest.Dispose();
                m_UnityWebRequest = null;
            }

            Array.Clear(m_CachedBytes, 0, CachedBytesLength);
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
            if (m_UnityWebRequest == null)
            {
                return;
            }

            if (!m_UnityWebRequest.isDone)
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
                DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(m_UnityWebRequest.responseCode == RangeNotSatisfiableErrorCode, m_UnityWebRequest.error);
                m_DownloadAgentHelperErrorEventHandler(this, downloadAgentHelperErrorEventArgs);
                ReferencePool.Release(downloadAgentHelperErrorEventArgs);
            }
            else
            {
                DownloadAgentHelperCompleteEventArgs downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create((long)m_UnityWebRequest.downloadedBytes);
                m_DownloadAgentHelperCompleteEventHandler(this, downloadAgentHelperCompleteEventArgs);
                ReferencePool.Release(downloadAgentHelperCompleteEventArgs);
            }
        }
    }
}
