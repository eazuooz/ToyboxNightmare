//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.WebRequest;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Web Request")]
    public sealed class WebRequestComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private IWebRequestManager m_WebRequestManager = null;
        private EventComponent m_EventComponent = null;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_WebRequestAgentHelperTypeName = "UnityGameFramework.Runtime.UnityWebRequestAgentHelper";

        [SerializeField]
        private WebRequestAgentHelperBase m_CustomWebRequestAgentHelper = null;

        [SerializeField]
        private int m_WebRequestAgentHelperCount = 1;

        [SerializeField]
        private float m_Timeout = 30f;

        public int TotalAgentCount
        {
            get
            {
                return m_WebRequestManager.TotalAgentCount;
            }
        }

        public int FreeAgentCount
        {
            get
            {
                return m_WebRequestManager.FreeAgentCount;
            }
        }

        public int WorkingAgentCount
        {
            get
            {
                return m_WebRequestManager.WorkingAgentCount;
            }
        }

        public int WaitingTaskCount
        {
            get
            {
                return m_WebRequestManager.WaitingTaskCount;
            }
        }

        public float Timeout
        {
            get
            {
                return m_WebRequestManager.Timeout;
            }
            set
            {
                m_WebRequestManager.Timeout = m_Timeout = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_WebRequestManager = GameFrameworkEntry.GetModule<IWebRequestManager>();
            if (m_WebRequestManager == null)
            {
                Log.Fatal("Web request manager is invalid.");
                return;
            }

            m_WebRequestManager.Timeout = m_Timeout;
            m_WebRequestManager.WebRequestStart += OnWebRequestStart;
            m_WebRequestManager.WebRequestSuccess += OnWebRequestSuccess;
            m_WebRequestManager.WebRequestFailure += OnWebRequestFailure;
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
                m_InstanceRoot = new GameObject("Web Request Agent Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < m_WebRequestAgentHelperCount; i++)
            {
                AddWebRequestAgentHelper(i);
            }
        }

        public TaskInfo GetWebRequestInfo(int serialId)
        {
            return m_WebRequestManager.GetWebRequestInfo(serialId);
        }

        public TaskInfo[] GetWebRequestInfos(string tag)
        {
            return m_WebRequestManager.GetWebRequestInfos(tag);
        }

        public void GetAllWebRequestInfos(string tag, List<TaskInfo> results)
        {
            m_WebRequestManager.GetAllWebRequestInfos(tag, results);
        }

        public TaskInfo[] GetAllWebRequestInfos()
        {
            return m_WebRequestManager.GetAllWebRequestInfos();
        }

        public void GetAllWebRequestInfos(List<TaskInfo> results)
        {
            m_WebRequestManager.GetAllWebRequestInfos(results);
        }

        public int AddWebRequest(string webRequestUri)
        {
            return AddWebRequest(webRequestUri, null, null, null, DefaultPriority, null);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData)
        {
            return AddWebRequest(webRequestUri, postData, null, null, DefaultPriority, null);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, null, DefaultPriority, null);
        }

        public int AddWebRequest(string webRequestUri, string tag)
        {
            return AddWebRequest(webRequestUri, null, null, tag, DefaultPriority, null);
        }

        public int AddWebRequest(string webRequestUri, int priority)
        {
            return AddWebRequest(webRequestUri, null, null, null, priority, null);
        }

        public int AddWebRequest(string webRequestUri, object userData)
        {
            return AddWebRequest(webRequestUri, null, null, null, DefaultPriority, userData);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData, string tag)
        {
            return AddWebRequest(webRequestUri, postData, null, tag, DefaultPriority, null);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm, string tag)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, tag, DefaultPriority, null);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData, int priority)
        {
            return AddWebRequest(webRequestUri, postData, null, null, priority, null);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm, int priority)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, null, priority, null);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData, object userData)
        {
            return AddWebRequest(webRequestUri, postData, null, null, DefaultPriority, userData);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm, object userData)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, null, DefaultPriority, userData);
        }

        public int AddWebRequest(string webRequestUri, string tag, int priority)
        {
            return AddWebRequest(webRequestUri, null, null, tag, priority, null);
        }

        public int AddWebRequest(string webRequestUri, string tag, object userData)
        {
            return AddWebRequest(webRequestUri, null, null, tag, DefaultPriority, userData);
        }

        public int AddWebRequest(string webRequestUri, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, null, null, null, priority, userData);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData, string tag, int priority)
        {
            return AddWebRequest(webRequestUri, postData, null, tag, priority, null);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm, string tag, int priority)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, tag, priority, null);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData, string tag, object userData)
        {
            return AddWebRequest(webRequestUri, postData, null, tag, DefaultPriority, userData);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm, string tag, object userData)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, tag, DefaultPriority, userData);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, postData, null, null, priority, userData);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, null, priority, userData);
        }

        public int AddWebRequest(string webRequestUri, string tag, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, null, null, tag, priority, userData);
        }

        public int AddWebRequest(string webRequestUri, byte[] postData, string tag, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, postData, null, tag, priority, userData);
        }

        public int AddWebRequest(string webRequestUri, WWWForm wwwForm, string tag, int priority, object userData)
        {
            return AddWebRequest(webRequestUri, null, wwwForm, tag, priority, userData);
        }

        public bool RemoveWebRequest(int serialId)
        {
            return m_WebRequestManager.RemoveWebRequest(serialId);
        }

        public int RemoveWebRequests(string tag)
        {
            return m_WebRequestManager.RemoveWebRequests(tag);
        }

        public int RemoveAllWebRequests()
        {
            return m_WebRequestManager.RemoveAllWebRequests();
        }

        private void AddWebRequestAgentHelper(int index)
        {
            WebRequestAgentHelperBase webRequestAgentHelper = Helper.CreateHelper(m_WebRequestAgentHelperTypeName, m_CustomWebRequestAgentHelper, index);
            if (webRequestAgentHelper == null)
            {
                Log.Error("Can not create web request agent helper.");
                return;
            }

            webRequestAgentHelper.name = Utility.Text.Format("Web Request Agent Helper - {0}", index);
            Transform transform = webRequestAgentHelper.transform;
            transform.SetParent(m_InstanceRoot);
            transform.localScale = Vector3.one;

            m_WebRequestManager.AddWebRequestAgentHelper(webRequestAgentHelper);
        }

        private int AddWebRequest(string webRequestUri, byte[] postData, WWWForm wwwForm, string tag, int priority, object userData)
        {
            return m_WebRequestManager.AddWebRequest(webRequestUri, postData, tag, priority, WWWFormInfo.Create(wwwForm, userData));
        }

        private void OnWebRequestStart(object sender, GameFramework.WebRequest.WebRequestStartEventArgs e)
        {
            m_EventComponent.Fire(this, WebRequestStartEventArgs.Create(e));
        }

        private void OnWebRequestSuccess(object sender, GameFramework.WebRequest.WebRequestSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, WebRequestSuccessEventArgs.Create(e));
        }

        private void OnWebRequestFailure(object sender, GameFramework.WebRequest.WebRequestFailureEventArgs e)
        {
            Log.Warning("Web request failure, web request serial id '{0}', web request uri '{1}', error message '{2}'.", e.SerialId, e.WebRequestUri, e.ErrorMessage);
            m_EventComponent.Fire(this, WebRequestFailureEventArgs.Create(e));
        }
    }
}
