//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Download;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Download")]
    public sealed class DownloadComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;
        private const int OneMegaBytes = 1024 * 1024;

        private IDownloadManager m_DownloadManager = null;
        private EventComponent m_EventComponent = null;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_DownloadAgentHelperTypeName = "UnityGameFramework.Runtime.UnityWebRequestDownloadAgentHelper";

        [SerializeField]
        private DownloadAgentHelperBase m_CustomDownloadAgentHelper = null;

        [SerializeField]
        private int m_DownloadAgentHelperCount = 3;

        [SerializeField]
        private float m_Timeout = 30f;

        [SerializeField]
        private int m_FlushSize = OneMegaBytes;

        public bool Paused
        {
            get
            {
                return m_DownloadManager.Paused;
            }
            set
            {
                m_DownloadManager.Paused = value;
            }
        }

        public int TotalAgentCount
        {
            get
            {
                return m_DownloadManager.TotalAgentCount;
            }
        }

        public int FreeAgentCount
        {
            get
            {
                return m_DownloadManager.FreeAgentCount;
            }
        }

        public int WorkingAgentCount
        {
            get
            {
                return m_DownloadManager.WorkingAgentCount;
            }
        }

        public int WaitingTaskCount
        {
            get
            {
                return m_DownloadManager.WaitingTaskCount;
            }
        }

        public float Timeout
        {
            get
            {
                return m_DownloadManager.Timeout;
            }
            set
            {
                m_DownloadManager.Timeout = m_Timeout = value;
            }
        }

        public int FlushSize
        {
            get
            {
                return m_DownloadManager.FlushSize;
            }
            set
            {
                m_DownloadManager.FlushSize = m_FlushSize = value;
            }
        }

        public float CurrentSpeed
        {
            get
            {
                return m_DownloadManager.CurrentSpeed;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_DownloadManager = GameFrameworkEntry.GetModule<IDownloadManager>();
            if (m_DownloadManager == null)
            {
                Log.Fatal("Download manager is invalid.");
                return;
            }

            m_DownloadManager.DownloadStart += OnDownloadStart;
            m_DownloadManager.DownloadUpdate += OnDownloadUpdate;
            m_DownloadManager.DownloadSuccess += OnDownloadSuccess;
            m_DownloadManager.DownloadFailure += OnDownloadFailure;
            m_DownloadManager.FlushSize = m_FlushSize;
            m_DownloadManager.Timeout = m_Timeout;
        }

        private void Start()
        {
            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (m_InstanceRoot == null)
            {
                m_InstanceRoot = new GameObject("Download Agent Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < m_DownloadAgentHelperCount; i++)
            {
                AddDownloadAgentHelper(i);
            }
        }

        public TaskInfo GetDownloadInfo(int serialId)
        {
            return m_DownloadManager.GetDownloadInfo(serialId);
        }

        public TaskInfo[] GetDownloadInfos(string tag)
        {
            return m_DownloadManager.GetDownloadInfos(tag);
        }

        public void GetDownloadInfos(string tag, List<TaskInfo> results)
        {
            m_DownloadManager.GetDownloadInfos(tag, results);
        }

        public TaskInfo[] GetAllDownloadInfos()
        {
            return m_DownloadManager.GetAllDownloadInfos();
        }

        public void GetAllDownloadInfos(List<TaskInfo> results)
        {
            m_DownloadManager.GetAllDownloadInfos(results);
        }

        public int AddDownload(string downloadPath, string downloadUri)
        {
            return AddDownload(downloadPath, downloadUri, null, DefaultPriority, null);
        }

        public int AddDownload(string downloadPath, string downloadUri, string tag)
        {
            return AddDownload(downloadPath, downloadUri, tag, DefaultPriority, null);
        }

        public int AddDownload(string downloadPath, string downloadUri, int priority)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, null);
        }

        public int AddDownload(string downloadPath, string downloadUri, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, DefaultPriority, userData);
        }

        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority)
        {
            return AddDownload(downloadPath, downloadUri, tag, priority, null);
        }

        public int AddDownload(string downloadPath, string downloadUri, string tag, object userData)
        {
            return AddDownload(downloadPath, downloadUri, tag, DefaultPriority, userData);
        }

        public int AddDownload(string downloadPath, string downloadUri, int priority, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, userData);
        }

        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority, object userData)
        {
            return m_DownloadManager.AddDownload(downloadPath, downloadUri, tag, priority, userData);
        }

        public bool RemoveDownload(int serialId)
        {
            return m_DownloadManager.RemoveDownload(serialId);
        }

        public int RemoveDownloads(string tag)
        {
            return m_DownloadManager.RemoveDownloads(tag);
        }

        public int RemoveAllDownloads()
        {
            return m_DownloadManager.RemoveAllDownloads();
        }

        private void AddDownloadAgentHelper(int index)
        {
            DownloadAgentHelperBase downloadAgentHelper = Helper.CreateHelper(m_DownloadAgentHelperTypeName, m_CustomDownloadAgentHelper, index);
            if (downloadAgentHelper == null)
            {
                Log.Error("Can not create download agent helper.");
                return;
            }

            downloadAgentHelper.name = Utility.Text.Format("Download Agent Helper - {0}", index);
            Transform transform = downloadAgentHelper.transform;
            transform.SetParent(m_InstanceRoot);
            transform.localScale = Vector3.one;

            m_DownloadManager.AddDownloadAgentHelper(downloadAgentHelper);
        }

        private void OnDownloadStart(object sender, GameFramework.Download.DownloadStartEventArgs e)
        {
            m_EventComponent.Fire(this, DownloadStartEventArgs.Create(e));
        }

        private void OnDownloadUpdate(object sender, GameFramework.Download.DownloadUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, DownloadUpdateEventArgs.Create(e));
        }

        private void OnDownloadSuccess(object sender, GameFramework.Download.DownloadSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, DownloadSuccessEventArgs.Create(e));
        }

        private void OnDownloadFailure(object sender, GameFramework.Download.DownloadFailureEventArgs e)
        {
            Log.Warning("Download failure, download serial id '{0}', download path '{1}', download uri '{2}', error message '{3}'.", e.SerialId, e.DownloadPath, e.DownloadUri, e.ErrorMessage);
            m_EventComponent.Fire(this, DownloadFailureEventArgs.Create(e));
        }
    }
}
