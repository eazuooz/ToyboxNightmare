//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Config;
using GameFramework.Resource;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Config")]
    public sealed class ConfigComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private IConfigManager m_ConfigManager = null;
        private EventComponent m_EventComponent = null;

        [SerializeField]
        private bool m_EnableLoadConfigUpdateEvent = false;

        [SerializeField]
        private bool m_EnableLoadConfigDependencyAssetEvent = false;

        [SerializeField]
        private string m_ConfigHelperTypeName = "UnityGameFramework.Runtime.DefaultConfigHelper";

        [SerializeField]
        private ConfigHelperBase m_CustomConfigHelper = null;

        [SerializeField]
        private int m_CachedBytesSize = 0;

        public int Count
        {
            get
            {
                return m_ConfigManager.Count;
            }
        }

        public int CachedBytesSize
        {
            get
            {
                return m_ConfigManager.CachedBytesSize;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_ConfigManager = GameFrameworkEntry.GetModule<IConfigManager>();
            if (m_ConfigManager == null)
            {
                Log.Fatal("Config manager is invalid.");
                return;
            }

            m_ConfigManager.ReadDataSuccess += OnReadDataSuccess;
            m_ConfigManager.ReadDataFailure += OnReadDataFailure;

            if (m_EnableLoadConfigUpdateEvent)
            {
                m_ConfigManager.ReadDataUpdate += OnReadDataUpdate;
            }

            if (m_EnableLoadConfigDependencyAssetEvent)
            {
                m_ConfigManager.ReadDataDependencyAsset += OnReadDataDependencyAsset;
            }
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (baseComponent.EditorResourceMode)
            {
                m_ConfigManager.SetResourceManager(baseComponent.EditorResourceHelper);
            }
            else
            {
                m_ConfigManager.SetResourceManager(GameFrameworkEntry.GetModule<IResourceManager>());
            }

            ConfigHelperBase configHelper = Helper.CreateHelper(m_ConfigHelperTypeName, m_CustomConfigHelper);
            if (configHelper == null)
            {
                Log.Error("Can not create config helper.");
                return;
            }

            configHelper.name = "Config Helper";
            Transform transform = configHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            m_ConfigManager.SetDataProviderHelper(configHelper);
            m_ConfigManager.SetConfigHelper(configHelper);
            if (m_CachedBytesSize > 0)
            {
                EnsureCachedBytesSize(m_CachedBytesSize);
            }
        }

        public void EnsureCachedBytesSize(int ensureSize)
        {
            m_ConfigManager.EnsureCachedBytesSize(ensureSize);
        }

        public void FreeCachedBytes()
        {
            m_ConfigManager.FreeCachedBytes();
        }

        public void ReadData(string configAssetName)
        {
            m_ConfigManager.ReadData(configAssetName);
        }

        public void ReadData(string configAssetName, int priority)
        {
            m_ConfigManager.ReadData(configAssetName, priority);
        }

        public void ReadData(string configAssetName, object userData)
        {
            m_ConfigManager.ReadData(configAssetName, userData);
        }

        public void ReadData(string configAssetName, int priority, object userData)
        {
            m_ConfigManager.ReadData(configAssetName, priority, userData);
        }

        public bool ParseData(string configString)
        {
            return m_ConfigManager.ParseData(configString);
        }

        public bool ParseData(string configString, object userData)
        {
            return m_ConfigManager.ParseData(configString, userData);
        }

        public bool ParseData(byte[] configBytes)
        {
            return m_ConfigManager.ParseData(configBytes);
        }

        public bool ParseData(byte[] configBytes, object userData)
        {
            return m_ConfigManager.ParseData(configBytes, userData);
        }

        public bool ParseData(byte[] configBytes, int startIndex, int length)
        {
            return m_ConfigManager.ParseData(configBytes, startIndex, length);
        }

        public bool ParseData(byte[] configBytes, int startIndex, int length, object userData)
        {
            return m_ConfigManager.ParseData(configBytes, startIndex, length, userData);
        }

        public bool HasConfig(string configName)
        {
            return m_ConfigManager.HasConfig(configName);
        }

        public bool GetBool(string configName)
        {
            return m_ConfigManager.GetBool(configName);
        }

        public bool GetBool(string configName, bool defaultValue)
        {
            return m_ConfigManager.GetBool(configName, defaultValue);
        }

        public int GetInt(string configName)
        {
            return m_ConfigManager.GetInt(configName);
        }

        public int GetInt(string configName, int defaultValue)
        {
            return m_ConfigManager.GetInt(configName, defaultValue);
        }

        public float GetFloat(string configName)
        {
            return m_ConfigManager.GetFloat(configName);
        }

        public float GetFloat(string configName, float defaultValue)
        {
            return m_ConfigManager.GetFloat(configName, defaultValue);
        }

        public string GetString(string configName)
        {
            return m_ConfigManager.GetString(configName);
        }

        public string GetString(string configName, string defaultValue)
        {
            return m_ConfigManager.GetString(configName, defaultValue);
        }

        public bool AddConfig(string configName, bool boolValue, int intValue, float floatValue, string stringValue)
        {
            return m_ConfigManager.AddConfig(configName, boolValue, intValue, floatValue, stringValue);
        }

        public bool RemoveConfig(string configName)
        {
            return m_ConfigManager.RemoveConfig(configName);
        }

        public void RemoveAllConfigs()
        {
            m_ConfigManager.RemoveAllConfigs();
        }

        private void OnReadDataSuccess(object sender, ReadDataSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, LoadConfigSuccessEventArgs.Create(e));
        }

        private void OnReadDataFailure(object sender, ReadDataFailureEventArgs e)
        {
            Log.Warning("Load config failure, asset name '{0}', error message '{1}'.", e.DataAssetName, e.ErrorMessage);
            m_EventComponent.Fire(this, LoadConfigFailureEventArgs.Create(e));
        }

        private void OnReadDataUpdate(object sender, ReadDataUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, LoadConfigUpdateEventArgs.Create(e));
        }

        private void OnReadDataDependencyAsset(object sender, ReadDataDependencyAssetEventArgs e)
        {
            m_EventComponent.Fire(this, LoadConfigDependencyAssetEventArgs.Create(e));
        }
    }
}
