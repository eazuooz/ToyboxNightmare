//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


#if !UNITY_2018_3_OR_NEWER

using GameFramework;
using GameFramework.Download;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public class WWWDownloadAgentHelper : DownloadAgentHelperBase, IDisposable
    {
        private WWW m_WWW = null;
        private int m_LastDownloadedSize = 0;
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

            m_WWW = new WWW(downloadUri);
        }

        public override void Download(string downloadUri, long fromPosition, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null || m_DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            Dictionary<string, string> header = new Dictionary<string, string>
            {
                { "Range", Utility.Text.Format("bytes={0}-", fromPosition) }
            };

            m_WWW = new WWW(downloadUri, null, header);
        }

        public override void Download(string downloadUri, long fromPosition, long toPosition, object userData)
        {
            if (m_DownloadAgentHelperUpdateBytesEventHandler == null || m_DownloadAgentHelperUpdateLengthEventHandler == null || m_DownloadAgentHelperCompleteEventHandler == null || m_DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            Dictionary<string, string> header = new Dictionary<string, string>
            {
                { "Range", Utility.Text.Format("bytes={0}-{1}", fromPosition, toPosition) }
            };

            m_WWW = new WWW(downloadUri, null, header);
        }

        public override void Reset()
        {
            if (m_WWW != null)
            {
                m_WWW.Dispose();
                m_WWW = null;
            }

            m_LastDownloadedSize = 0;
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
            if (m_WWW == null)
            {
                return;
            }

            int deltaLength = m_WWW.bytesDownloaded - m_LastDownloadedSize;
            if (deltaLength > 0)
            {
                m_LastDownloadedSize = m_WWW.bytesDownloaded;
                DownloadAgentHelperUpdateLengthEventArgs downloadAgentHelperUpdateLengthEventArgs = DownloadAgentHelperUpdateLengthEventArgs.Create(deltaLength);
                m_DownloadAgentHelperUpdateLengthEventHandler(this, downloadAgentHelperUpdateLengthEventArgs);
                ReferencePool.Release(downloadAgentHelperUpdateLengthEventArgs);
            }

            if (m_WWW == null)
            {
                return;
            }

            if (!m_WWW.isDone)
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_WWW.error))
            {
                DownloadAgentHelperErrorEventArgs dodwnloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(m_WWW.error.StartsWith(RangeNotSatisfiableErrorCode.ToString(), StringComparison.Ordinal), m_WWW.error);
                m_DownloadAgentHelperErrorEventHandler(this, dodwnloadAgentHelperErrorEventArgs);
                ReferencePool.Release(dodwnloadAgentHelperErrorEventArgs);
            }
            else
            {
                byte[] bytes = m_WWW.bytes;
                DownloadAgentHelperUpdateBytesEventArgs downloadAgentHelperUpdateBytesEventArgs = DownloadAgentHelperUpdateBytesEventArgs.Create(bytes, 0, bytes.Length);
                m_DownloadAgentHelperUpdateBytesEventHandler(this, downloadAgentHelperUpdateBytesEventArgs);
                ReferencePool.Release(downloadAgentHelperUpdateBytesEventArgs);

                DownloadAgentHelperCompleteEventArgs downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create(bytes.LongLength);
                m_DownloadAgentHelperCompleteEventHandler(this, downloadAgentHelperCompleteEventArgs);
                ReferencePool.Release(downloadAgentHelperCompleteEventArgs);
            }
        }
    }
}

#endif
