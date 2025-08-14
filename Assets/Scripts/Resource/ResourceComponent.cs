//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Download;
using GameFramework.FileSystem;
using GameFramework.ObjectPool;
using GameFramework.Resource;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Resource")]
    public sealed class ResourceComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;
        private const int OneMegaBytes = 1024 * 1024;

        private IResourceManager m_ResourceManager = null;
        private EventComponent m_EventComponent = null;
        private bool m_EditorResourceMode = false;
        private bool m_ForceUnloadUnusedAssets = false;
        private bool m_PreorderUnloadUnusedAssets = false;
        private bool m_PerformGCCollect = false;
        private AsyncOperation m_AsyncOperation = null;
        private float m_LastUnloadUnusedAssetsOperationElapseSeconds = 0f;
        private ResourceHelperBase m_ResourceHelper = null;

        [SerializeField]
        private ResourceMode m_ResourceMode = ResourceMode.Package;

        [SerializeField]
        private ReadWritePathType m_ReadWritePathType = ReadWritePathType.Unspecified;

        [SerializeField]
        private float m_MinUnloadUnusedAssetsInterval = 60f;

        [SerializeField]
        private float m_MaxUnloadUnusedAssetsInterval = 300f;

        [SerializeField]
        private float m_AssetAutoReleaseInterval = 60f;

        [SerializeField]
        private int m_AssetCapacity = 64;

        [SerializeField]
        private float m_AssetExpireTime = 60f;

        [SerializeField]
        private int m_AssetPriority = 0;

        [SerializeField]
        private float m_ResourceAutoReleaseInterval = 60f;

        [SerializeField]
        private int m_ResourceCapacity = 16;

        [SerializeField]
        private float m_ResourceExpireTime = 60f;

        [SerializeField]
        private int m_ResourcePriority = 0;

        [SerializeField]
        private string m_UpdatePrefixUri = null;

        [SerializeField]
        private int m_GenerateReadWriteVersionListLength = OneMegaBytes;

        [SerializeField]
        private int m_UpdateRetryCount = 3;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_ResourceHelperTypeName = "UnityGameFramework.Runtime.DefaultResourceHelper";

        [SerializeField]
        private ResourceHelperBase m_CustomResourceHelper = null;

        [SerializeField]
        private string m_LoadResourceAgentHelperTypeName = "UnityGameFramework.Runtime.DefaultLoadResourceAgentHelper";

        [SerializeField]
        private LoadResourceAgentHelperBase m_CustomLoadResourceAgentHelper = null;

        [SerializeField]
        private int m_LoadResourceAgentHelperCount = 3;

        public string ReadOnlyPath
        {
            get
            {
                return m_ResourceManager.ReadOnlyPath;
            }
        }

        public string ReadWritePath
        {
            get
            {
                return m_ResourceManager.ReadWritePath;
            }
        }

        public ResourceMode ResourceMode
        {
            get
            {
                return m_ResourceManager.ResourceMode;
            }
        }

        public ReadWritePathType ReadWritePathType
        {
            get
            {
                return m_ReadWritePathType;
            }
        }

        public string CurrentVariant
        {
            get
            {
                return m_ResourceManager.CurrentVariant;
            }
        }

        public PackageVersionListSerializer PackageVersionListSerializer
        {
            get
            {
                return m_ResourceManager.PackageVersionListSerializer;
            }
        }

        public UpdatableVersionListSerializer UpdatableVersionListSerializer
        {
            get
            {
                return m_ResourceManager.UpdatableVersionListSerializer;
            }
        }

        public ReadOnlyVersionListSerializer ReadOnlyVersionListSerializer
        {
            get
            {
                return m_ResourceManager.ReadOnlyVersionListSerializer;
            }
        }

        public ReadWriteVersionListSerializer ReadWriteVersionListSerializer
        {
            get
            {
                return m_ResourceManager.ReadWriteVersionListSerializer;
            }
        }

        public ResourcePackVersionListSerializer ResourcePackVersionListSerializer
        {
            get
            {
                return m_ResourceManager.ResourcePackVersionListSerializer;
            }
        }

        public float LastUnloadUnusedAssetsOperationElapseSeconds
        {
            get
            {
                return m_LastUnloadUnusedAssetsOperationElapseSeconds;
            }
        }

        public float MinUnloadUnusedAssetsInterval
        {
            get
            {
                return m_MinUnloadUnusedAssetsInterval;
            }
            set
            {
                m_MinUnloadUnusedAssetsInterval = value;
            }
        }

        public float MaxUnloadUnusedAssetsInterval
        {
            get
            {
                return m_MaxUnloadUnusedAssetsInterval;
            }
            set
            {
                m_MaxUnloadUnusedAssetsInterval = value;
            }
        }

        public string ApplicableGameVersion
        {
            get
            {
                return m_ResourceManager.ApplicableGameVersion;
            }
        }

        public int InternalResourceVersion
        {
            get
            {
                return m_ResourceManager.InternalResourceVersion;
            }
        }

        public int AssetCount
        {
            get
            {
                return m_ResourceManager.AssetCount;
            }
        }

        public int ResourceCount
        {
            get
            {
                return m_ResourceManager.ResourceCount;
            }
        }

        public int ResourceGroupCount
        {
            get
            {
                return m_ResourceManager.ResourceGroupCount;
            }
        }

        public string UpdatePrefixUri
        {
            get
            {
                return m_ResourceManager.UpdatePrefixUri;
            }
            set
            {
                m_ResourceManager.UpdatePrefixUri = m_UpdatePrefixUri = value;
            }
        }

        public int GenerateReadWriteVersionListLength
        {
            get
            {
                return m_ResourceManager.GenerateReadWriteVersionListLength;
            }
            set
            {
                m_ResourceManager.GenerateReadWriteVersionListLength = m_GenerateReadWriteVersionListLength = value;
            }
        }

        public string ApplyingResourcePackPath
        {
            get
            {
                return m_ResourceManager.ApplyingResourcePackPath;
            }
        }

        public int ApplyWaitingCount
        {
            get
            {
                return m_ResourceManager.ApplyWaitingCount;
            }
        }

        public int UpdateRetryCount
        {
            get
            {
                return m_ResourceManager.UpdateRetryCount;
            }
            set
            {
                m_ResourceManager.UpdateRetryCount = m_UpdateRetryCount = value;
            }
        }

        public IResourceGroup UpdatingResourceGroup
        {
            get
            {
                return m_ResourceManager.UpdatingResourceGroup;
            }
        }

        public int UpdateWaitingCount
        {
            get
            {
                return m_ResourceManager.UpdateWaitingCount;
            }
        }

        public int UpdateWaitingWhilePlayingCount
        {
            get
            {
                return m_ResourceManager.UpdateWaitingWhilePlayingCount;
            }
        }

        public int UpdateCandidateCount
        {
            get
            {
                return m_ResourceManager.UpdateCandidateCount;
            }
        }

        public int LoadTotalAgentCount
        {
            get
            {
                return m_ResourceManager.LoadTotalAgentCount;
            }
        }

        public int LoadFreeAgentCount
        {
            get
            {
                return m_ResourceManager.LoadFreeAgentCount;
            }
        }

        public int LoadWorkingAgentCount
        {
            get
            {
                return m_ResourceManager.LoadWorkingAgentCount;
            }
        }

        public int LoadWaitingTaskCount
        {
            get
            {
                return m_ResourceManager.LoadWaitingTaskCount;
            }
        }

        public float AssetAutoReleaseInterval
        {
            get
            {
                return m_ResourceManager.AssetAutoReleaseInterval;
            }
            set
            {
                m_ResourceManager.AssetAutoReleaseInterval = m_AssetAutoReleaseInterval = value;
            }
        }

        public int AssetCapacity
        {
            get
            {
                return m_ResourceManager.AssetCapacity;
            }
            set
            {
                m_ResourceManager.AssetCapacity = m_AssetCapacity = value;
            }
        }

        public float AssetExpireTime
        {
            get
            {
                return m_ResourceManager.AssetExpireTime;
            }
            set
            {
                m_ResourceManager.AssetExpireTime = m_AssetExpireTime = value;
            }
        }

        public int AssetPriority
        {
            get
            {
                return m_ResourceManager.AssetPriority;
            }
            set
            {
                m_ResourceManager.AssetPriority = m_AssetPriority = value;
            }
        }

        public float ResourceAutoReleaseInterval
        {
            get
            {
                return m_ResourceManager.ResourceAutoReleaseInterval;
            }
            set
            {
                m_ResourceManager.ResourceAutoReleaseInterval = m_ResourceAutoReleaseInterval = value;
            }
        }

        public int ResourceCapacity
        {
            get
            {
                return m_ResourceManager.ResourceCapacity;
            }
            set
            {
                m_ResourceManager.ResourceCapacity = m_ResourceCapacity = value;
            }
        }

        public float ResourceExpireTime
        {
            get
            {
                return m_ResourceManager.ResourceExpireTime;
            }
            set
            {
                m_ResourceManager.ResourceExpireTime = m_ResourceExpireTime = value;
            }
        }

        public int ResourcePriority
        {
            get
            {
                return m_ResourceManager.ResourcePriority;
            }
            set
            {
                m_ResourceManager.ResourcePriority = m_ResourcePriority = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
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

            m_EditorResourceMode = baseComponent.EditorResourceMode;
            m_ResourceManager = m_EditorResourceMode ? baseComponent.EditorResourceHelper : GameFrameworkEntry.GetModule<IResourceManager>();
            if (m_ResourceManager == null)
            {
                Log.Fatal("Resource manager is invalid.");
                return;
            }

            m_ResourceManager.ResourceVerifyStart += OnResourceVerifyStart;
            m_ResourceManager.ResourceVerifySuccess += OnResourceVerifySuccess;
            m_ResourceManager.ResourceVerifyFailure += OnResourceVerifyFailure;
            m_ResourceManager.ResourceApplyStart += OnResourceApplyStart;
            m_ResourceManager.ResourceApplySuccess += OnResourceApplySuccess;
            m_ResourceManager.ResourceApplyFailure += OnResourceApplyFailure;
            m_ResourceManager.ResourceUpdateStart += OnResourceUpdateStart;
            m_ResourceManager.ResourceUpdateChanged += OnResourceUpdateChanged;
            m_ResourceManager.ResourceUpdateSuccess += OnResourceUpdateSuccess;
            m_ResourceManager.ResourceUpdateFailure += OnResourceUpdateFailure;
            m_ResourceManager.ResourceUpdateAllComplete += OnResourceUpdateAllComplete;

            m_ResourceManager.SetReadOnlyPath(Application.streamingAssetsPath);
            if (m_ReadWritePathType == ReadWritePathType.TemporaryCache)
            {
                m_ResourceManager.SetReadWritePath(Application.temporaryCachePath);
            }
            else
            {
                if (m_ReadWritePathType == ReadWritePathType.Unspecified)
                {
                    m_ReadWritePathType = ReadWritePathType.PersistentData;
                }

                m_ResourceManager.SetReadWritePath(Application.persistentDataPath);
            }

            if (m_EditorResourceMode)
            {
                return;
            }

            SetResourceMode(m_ResourceMode);
            m_ResourceManager.SetObjectPoolManager(GameFrameworkEntry.GetModule<IObjectPoolManager>());
            m_ResourceManager.SetFileSystemManager(GameFrameworkEntry.GetModule<IFileSystemManager>());
            m_ResourceManager.SetDownloadManager(GameFrameworkEntry.GetModule<IDownloadManager>());
            m_ResourceManager.AssetAutoReleaseInterval = m_AssetAutoReleaseInterval;
            m_ResourceManager.AssetCapacity = m_AssetCapacity;
            m_ResourceManager.AssetExpireTime = m_AssetExpireTime;
            m_ResourceManager.AssetPriority = m_AssetPriority;
            m_ResourceManager.ResourceAutoReleaseInterval = m_ResourceAutoReleaseInterval;
            m_ResourceManager.ResourceCapacity = m_ResourceCapacity;
            m_ResourceManager.ResourceExpireTime = m_ResourceExpireTime;
            m_ResourceManager.ResourcePriority = m_ResourcePriority;
            if (m_ResourceMode == ResourceMode.Updatable || m_ResourceMode == ResourceMode.UpdatableWhilePlaying)
            {
                m_ResourceManager.UpdatePrefixUri = m_UpdatePrefixUri;
                m_ResourceManager.GenerateReadWriteVersionListLength = m_GenerateReadWriteVersionListLength;
                m_ResourceManager.UpdateRetryCount = m_UpdateRetryCount;
            }

            m_ResourceHelper = Helper.CreateHelper(m_ResourceHelperTypeName, m_CustomResourceHelper);
            if (m_ResourceHelper == null)
            {
                Log.Error("Can not create resource helper.");
                return;
            }

            m_ResourceHelper.name = "Resource Helper";
            Transform transform = m_ResourceHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            m_ResourceManager.SetResourceHelper(m_ResourceHelper);

            if (m_InstanceRoot == null)
            {
                m_InstanceRoot = new GameObject("Load Resource Agent Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < m_LoadResourceAgentHelperCount; i++)
            {
                AddLoadResourceAgentHelper(i);
            }
        }

        private void Update()
        {
            m_LastUnloadUnusedAssetsOperationElapseSeconds += Time.unscaledDeltaTime;
            if (m_AsyncOperation == null && (m_ForceUnloadUnusedAssets || m_LastUnloadUnusedAssetsOperationElapseSeconds >= m_MaxUnloadUnusedAssetsInterval || m_PreorderUnloadUnusedAssets && m_LastUnloadUnusedAssetsOperationElapseSeconds >= m_MinUnloadUnusedAssetsInterval))
            {
                Log.Info("Unload unused assets...");
                m_ForceUnloadUnusedAssets = false;
                m_PreorderUnloadUnusedAssets = false;
                m_LastUnloadUnusedAssetsOperationElapseSeconds = 0f;
                m_AsyncOperation = Resources.UnloadUnusedAssets();
            }

            if (m_AsyncOperation != null && m_AsyncOperation.isDone)
            {
                m_AsyncOperation = null;
                if (m_PerformGCCollect)
                {
                    Log.Info("GC.Collect...");
                    m_PerformGCCollect = false;
                    GC.Collect();
                }
            }
        }

        public void SetResourceMode(ResourceMode resourceMode)
        {
            m_ResourceManager.SetResourceMode(resourceMode);
            switch (resourceMode)
            {
                case ResourceMode.Package:
                    m_ResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V0);
                    m_ResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V1);
                    m_ResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V2);
                    break;

                case ResourceMode.Updatable:
                case ResourceMode.UpdatableWhilePlaying:
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V0);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V1);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V2);

                    m_ResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(0, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V0);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(1, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V1_V2);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(2, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V1_V2);

                    m_ResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V0);
                    m_ResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V1);
                    m_ResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V2);

                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(0, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V0);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(1, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V1);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(2, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V2);

                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V0);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V1);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V2);

                    m_ResourceManager.ResourcePackVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.ResourcePackVersionListDeserializeCallback_V0);
                    break;
            }
        }

        public void SetCurrentVariant(string currentVariant)
        {
            m_ResourceManager.SetCurrentVariant(!string.IsNullOrEmpty(currentVariant) ? currentVariant : null);
        }

        public void SetDecryptResourceCallback(DecryptResourceCallback decryptResourceCallback)
        {
            m_ResourceManager.SetDecryptResourceCallback(decryptResourceCallback);
        }

        public void UnloadUnusedAssets(bool performGCCollect)
        {
            m_PreorderUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                m_PerformGCCollect = performGCCollect;
            }
        }

        public void ForceUnloadUnusedAssets(bool performGCCollect)
        {
            m_ForceUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                m_PerformGCCollect = performGCCollect;
            }
        }

        public void InitResources(InitResourcesCompleteCallback initResourcesCompleteCallback)
        {
            m_ResourceManager.InitResources(initResourcesCompleteCallback);
        }

        public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
        {
            return m_ResourceManager.CheckVersionList(latestInternalResourceVersion);
        }

        public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode, UpdateVersionListCallbacks updateVersionListCallbacks)
        {
            m_ResourceManager.UpdateVersionList(versionListLength, versionListHashCode, versionListCompressedLength, versionListCompressedHashCode, updateVersionListCallbacks);
        }

        public void VerifyResources(VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            m_ResourceManager.VerifyResources(0, verifyResourcesCompleteCallback);
        }

        public void VerifyResources(int verifyResourceLengthPerFrame, VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            m_ResourceManager.VerifyResources(verifyResourceLengthPerFrame, verifyResourcesCompleteCallback);
        }

        public void CheckResources(CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            m_ResourceManager.CheckResources(false, checkResourcesCompleteCallback);
        }

        public void CheckResources(bool ignoreOtherVariant, CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            m_ResourceManager.CheckResources(ignoreOtherVariant, checkResourcesCompleteCallback);
        }

        public void ApplyResources(string resourcePackPath, ApplyResourcesCompleteCallback applyResourcesCompleteCallback)
        {
            m_ResourceManager.ApplyResources(resourcePackPath, applyResourcesCompleteCallback);
        }

        public void UpdateResources(UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            m_ResourceManager.UpdateResources(updateResourcesCompleteCallback);
        }

        public void UpdateResources(string resourceGroupName, UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            m_ResourceManager.UpdateResources(resourceGroupName, updateResourcesCompleteCallback);
        }

        public void StopUpdateResources()
        {
            m_ResourceManager.StopUpdateResources();
        }

        public bool VerifyResourcePack(string resourcePackPath)
        {
            return m_ResourceManager.VerifyResourcePack(resourcePackPath);
        }

        public TaskInfo[] GetAllLoadAssetInfos()
        {
            return m_ResourceManager.GetAllLoadAssetInfos();
        }

        public void GetAllLoadAssetInfos(List<TaskInfo> results)
        {
            m_ResourceManager.GetAllLoadAssetInfos(results);
        }

        public HasAssetResult HasAsset(string assetName)
        {
            return m_ResourceManager.HasAsset(assetName);
        }

        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, null, DefaultPriority, loadAssetCallbacks, null);
        }

        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, assetType, DefaultPriority, loadAssetCallbacks, null);
        }

        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, null, priority, loadAssetCallbacks, null);
        }

        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            LoadAsset(assetName, null, DefaultPriority, loadAssetCallbacks, userData);
        }

        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, assetType, priority, loadAssetCallbacks, null);
        }

        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            LoadAsset(assetName, assetType, DefaultPriority, loadAssetCallbacks, userData);
        }

        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            LoadAsset(assetName, null, priority, loadAssetCallbacks, userData);
        }

        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error("Asset name is invalid.");
                return;
            }

            if (!assetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Asset name '{0}' is invalid.", assetName);
                return;
            }

            m_ResourceManager.LoadAsset(assetName, assetType, priority, loadAssetCallbacks, userData);
        }

        public void UnloadAsset(object asset)
        {
            m_ResourceManager.UnloadAsset(asset);
        }

        public string GetBinaryPath(string binaryAssetName)
        {
            return m_ResourceManager.GetBinaryPath(binaryAssetName);
        }

        public bool GetBinaryPath(string binaryAssetName, out bool storageInReadOnly, out bool storageInFileSystem, out string relativePath, out string fileName)
        {
            return m_ResourceManager.GetBinaryPath(binaryAssetName, out storageInReadOnly, out storageInFileSystem, out relativePath, out fileName);
        }

        public int GetBinaryLength(string binaryAssetName)
        {
            return m_ResourceManager.GetBinaryLength(binaryAssetName);
        }

        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks)
        {
            LoadBinary(binaryAssetName, loadBinaryCallbacks, null);
        }

        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return;
            }

            if (!binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return;
            }

            m_ResourceManager.LoadBinary(binaryAssetName, loadBinaryCallbacks, userData);
        }

        public byte[] LoadBinaryFromFileSystem(string binaryAssetName)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return null;
            }

            if (!binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return null;
            }

            return m_ResourceManager.LoadBinaryFromFileSystem(binaryAssetName);
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinaryFromFileSystem(binaryAssetName, buffer, 0, buffer.Length);
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinaryFromFileSystem(binaryAssetName, buffer, startIndex, buffer.Length - startIndex);
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return 0;
            }

            if (!binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return 0;
            }

            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return m_ResourceManager.LoadBinaryFromFileSystem(binaryAssetName, buffer, startIndex, length);
        }

        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, length);
        }

        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return null;
            }

            if (!binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return null;
            }

            return m_ResourceManager.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, length);
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, 0, buffer.Length);
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, 0, length);
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, startIndex, length);
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, 0, buffer.Length);
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, 0, length);
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return 0;
            }

            if (!binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return 0;
            }

            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return m_ResourceManager.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, startIndex, length);
        }

        public bool HasResourceGroup(string resourceGroupName)
        {
            return m_ResourceManager.HasResourceGroup(resourceGroupName);
        }

        public IResourceGroup GetResourceGroup()
        {
            return m_ResourceManager.GetResourceGroup();
        }

        public IResourceGroup GetResourceGroup(string resourceGroupName)
        {
            return m_ResourceManager.GetResourceGroup(resourceGroupName);
        }

        public IResourceGroup[] GetAllResourceGroups()
        {
            return m_ResourceManager.GetAllResourceGroups();
        }

        public void GetAllResourceGroups(List<IResourceGroup> results)
        {
            m_ResourceManager.GetAllResourceGroups(results);
        }

        public IResourceGroupCollection GetResourceGroupCollection(params string[] resourceGroupNames)
        {
            return m_ResourceManager.GetResourceGroupCollection(resourceGroupNames);
        }

        public IResourceGroupCollection GetResourceGroupCollection(List<string> resourceGroupNames)
        {
            return m_ResourceManager.GetResourceGroupCollection(resourceGroupNames);
        }

        private void AddLoadResourceAgentHelper(int index)
        {
            LoadResourceAgentHelperBase loadResourceAgentHelper = Helper.CreateHelper(m_LoadResourceAgentHelperTypeName, m_CustomLoadResourceAgentHelper, index);
            if (loadResourceAgentHelper == null)
            {
                Log.Error("Can not create load resource agent helper.");
                return;
            }

            loadResourceAgentHelper.name = Utility.Text.Format("Load Resource Agent Helper - {0}", index);
            Transform transform = loadResourceAgentHelper.transform;
            transform.SetParent(m_InstanceRoot);
            transform.localScale = Vector3.one;

            m_ResourceManager.AddLoadResourceAgentHelper(loadResourceAgentHelper);
        }

        private void OnResourceVerifyStart(object sender, GameFramework.Resource.ResourceVerifyStartEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceVerifyStartEventArgs.Create(e));
        }

        private void OnResourceVerifySuccess(object sender, GameFramework.Resource.ResourceVerifySuccessEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceVerifySuccessEventArgs.Create(e));
        }

        private void OnResourceVerifyFailure(object sender, GameFramework.Resource.ResourceVerifyFailureEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceVerifyFailureEventArgs.Create(e));
        }

        private void OnResourceApplyStart(object sender, GameFramework.Resource.ResourceApplyStartEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceApplyStartEventArgs.Create(e));
        }

        private void OnResourceApplySuccess(object sender, GameFramework.Resource.ResourceApplySuccessEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceApplySuccessEventArgs.Create(e));
        }

        private void OnResourceApplyFailure(object sender, GameFramework.Resource.ResourceApplyFailureEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceApplyFailureEventArgs.Create(e));
        }

        private void OnResourceUpdateStart(object sender, GameFramework.Resource.ResourceUpdateStartEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateStartEventArgs.Create(e));
        }

        private void OnResourceUpdateChanged(object sender, GameFramework.Resource.ResourceUpdateChangedEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateChangedEventArgs.Create(e));
        }

        private void OnResourceUpdateSuccess(object sender, GameFramework.Resource.ResourceUpdateSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateSuccessEventArgs.Create(e));
        }

        private void OnResourceUpdateFailure(object sender, GameFramework.Resource.ResourceUpdateFailureEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateFailureEventArgs.Create(e));
        }

        private void OnResourceUpdateAllComplete(object sender, GameFramework.Resource.ResourceUpdateAllCompleteEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateAllCompleteEventArgs.Create(e));
        }
    }
}
