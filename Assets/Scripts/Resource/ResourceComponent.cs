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

        private IResourceManager mResourceManager = null;
        private EventComponent mEventComponent = null;
        private bool mEditorResourceMode = false;
        private bool mForceUnloadUnusedAssets = false;
        private bool mPreorderUnloadUnusedAssets = false;
        private bool mPerformGCCollect = false;
        private AsyncOperation mAsyncOperation = null;
        private float mLastUnloadUnusedAssetsOperationElapseSeconds = 0f;
        private ResourceHelperBase mResourceHelper = null;

        [SerializeField]
        private ResourceMode mResourceMode = ResourceMode.Package;

        [SerializeField]
        private ReadWritePathType mReadWritePathType = ReadWritePathType.Unspecified;

        [SerializeField]
        private float mMinUnloadUnusedAssetsInterval = 60f;

        [SerializeField]
        private float mMaxUnloadUnusedAssetsInterval = 300f;

        [SerializeField]
        private float mAssetAutoReleaseInterval = 60f;

        [SerializeField]
        private int mAssetCapacity = 64;

        [SerializeField]
        private float mAssetExpireTime = 60f;

        [SerializeField]
        private int mAssetPriority = 0;

        [SerializeField]
        private float mResourceAutoReleaseInterval = 60f;

        [SerializeField]
        private int mResourceCapacity = 16;

        [SerializeField]
        private float mResourceExpireTime = 60f;

        [SerializeField]
        private int mResourcePriority = 0;

        [SerializeField]
        private string mUpdatePrefixUri = null;

        [SerializeField]
        private int mGenerateReadWriteVersionListLength = OneMegaBytes;

        [SerializeField]
        private int mUpdateRetryCount = 3;

        [SerializeField]
        private Transform mInstanceRoot = null;

        [SerializeField]
        private string mResourceHelperTypeName = "UnityGameFramework.Runtime.DefaultResourceHelper";

        [SerializeField]
        private ResourceHelperBase mCustomResourceHelper = null;

        [SerializeField]
        private string mLoadResourceAgentHelperTypeName = "UnityGameFramework.Runtime.DefaultLoadResourceAgentHelper";

        [SerializeField]
        private LoadResourceAgentHelperBase mCustomLoadResourceAgentHelper = null;

        [SerializeField]
        private int mLoadResourceAgentHelperCount = 3;

        public string ReadOnlyPath
        {
            get
            {
                return mResourceManager.ReadOnlyPath;
            }
        }

        public string ReadWritePath
        {
            get
            {
                return mResourceManager.ReadWritePath;
            }
        }

        public ResourceMode ResourceMode
        {
            get
            {
                return mResourceManager.ResourceMode;
            }
        }

        public ReadWritePathType ReadWritePathType
        {
            get
            {
                return mReadWritePathType;
            }
        }

        public string CurrentVariant
        {
            get
            {
                return mResourceManager.CurrentVariant;
            }
        }

        public PackageVersionListSerializer PackageVersionListSerializer
        {
            get
            {
                return mResourceManager.PackageVersionListSerializer;
            }
        }

        public UpdatableVersionListSerializer UpdatableVersionListSerializer
        {
            get
            {
                return mResourceManager.UpdatableVersionListSerializer;
            }
        }

        public ReadOnlyVersionListSerializer ReadOnlyVersionListSerializer
        {
            get
            {
                return mResourceManager.ReadOnlyVersionListSerializer;
            }
        }

        public ReadWriteVersionListSerializer ReadWriteVersionListSerializer
        {
            get
            {
                return mResourceManager.ReadWriteVersionListSerializer;
            }
        }

        public ResourcePackVersionListSerializer ResourcePackVersionListSerializer
        {
            get
            {
                return mResourceManager.ResourcePackVersionListSerializer;
            }
        }

        public float LastUnloadUnusedAssetsOperationElapseSeconds
        {
            get
            {
                return mLastUnloadUnusedAssetsOperationElapseSeconds;
            }
        }

        public float MinUnloadUnusedAssetsInterval
        {
            get
            {
                return mMinUnloadUnusedAssetsInterval;
            }
            set
            {
                mMinUnloadUnusedAssetsInterval = value;
            }
        }

        public float MaxUnloadUnusedAssetsInterval
        {
            get
            {
                return mMaxUnloadUnusedAssetsInterval;
            }
            set
            {
                mMaxUnloadUnusedAssetsInterval = value;
            }
        }

        public string ApplicableGameVersion
        {
            get
            {
                return mResourceManager.ApplicableGameVersion;
            }
        }

        public int InternalResourceVersion
        {
            get
            {
                return mResourceManager.InternalResourceVersion;
            }
        }

        public int AssetCount
        {
            get
            {
                return mResourceManager.AssetCount;
            }
        }

        public int ResourceCount
        {
            get
            {
                return mResourceManager.ResourceCount;
            }
        }

        public int ResourceGroupCount
        {
            get
            {
                return mResourceManager.ResourceGroupCount;
            }
        }

        public string UpdatePrefixUri
        {
            get
            {
                return mResourceManager.UpdatePrefixUri;
            }
            set
            {
                mResourceManager.UpdatePrefixUri = mUpdatePrefixUri = value;
            }
        }

        public int GenerateReadWriteVersionListLength
        {
            get
            {
                return mResourceManager.GenerateReadWriteVersionListLength;
            }
            set
            {
                mResourceManager.GenerateReadWriteVersionListLength = mGenerateReadWriteVersionListLength = value;
            }
        }

        public string ApplyingResourcePackPath
        {
            get
            {
                return mResourceManager.ApplyingResourcePackPath;
            }
        }

        public int ApplyWaitingCount
        {
            get
            {
                return mResourceManager.ApplyWaitingCount;
            }
        }

        public int UpdateRetryCount
        {
            get
            {
                return mResourceManager.UpdateRetryCount;
            }
            set
            {
                mResourceManager.UpdateRetryCount = mUpdateRetryCount = value;
            }
        }

        public IResourceGroup UpdatingResourceGroup
        {
            get
            {
                return mResourceManager.UpdatingResourceGroup;
            }
        }

        public int UpdateWaitingCount
        {
            get
            {
                return mResourceManager.UpdateWaitingCount;
            }
        }

        public int UpdateWaitingWhilePlayingCount
        {
            get
            {
                return mResourceManager.UpdateWaitingWhilePlayingCount;
            }
        }

        public int UpdateCandidateCount
        {
            get
            {
                return mResourceManager.UpdateCandidateCount;
            }
        }

        public int LoadTotalAgentCount
        {
            get
            {
                return mResourceManager.LoadTotalAgentCount;
            }
        }

        public int LoadFreeAgentCount
        {
            get
            {
                return mResourceManager.LoadFreeAgentCount;
            }
        }

        public int LoadWorkingAgentCount
        {
            get
            {
                return mResourceManager.LoadWorkingAgentCount;
            }
        }

        public int LoadWaitingTaskCount
        {
            get
            {
                return mResourceManager.LoadWaitingTaskCount;
            }
        }

        public float AssetAutoReleaseInterval
        {
            get
            {
                return mResourceManager.AssetAutoReleaseInterval;
            }
            set
            {
                mResourceManager.AssetAutoReleaseInterval = mAssetAutoReleaseInterval = value;
            }
        }

        public int AssetCapacity
        {
            get
            {
                return mResourceManager.AssetCapacity;
            }
            set
            {
                mResourceManager.AssetCapacity = mAssetCapacity = value;
            }
        }

        public float AssetExpireTime
        {
            get
            {
                return mResourceManager.AssetExpireTime;
            }
            set
            {
                mResourceManager.AssetExpireTime = mAssetExpireTime = value;
            }
        }

        public int AssetPriority
        {
            get
            {
                return mResourceManager.AssetPriority;
            }
            set
            {
                mResourceManager.AssetPriority = mAssetPriority = value;
            }
        }

        public float ResourceAutoReleaseInterval
        {
            get
            {
                return mResourceManager.ResourceAutoReleaseInterval;
            }
            set
            {
                mResourceManager.ResourceAutoReleaseInterval = mResourceAutoReleaseInterval = value;
            }
        }

        public int ResourceCapacity
        {
            get
            {
                return mResourceManager.ResourceCapacity;
            }
            set
            {
                mResourceManager.ResourceCapacity = mResourceCapacity = value;
            }
        }

        public float ResourceExpireTime
        {
            get
            {
                return mResourceManager.ResourceExpireTime;
            }
            set
            {
                mResourceManager.ResourceExpireTime = mResourceExpireTime = value;
            }
        }

        public int ResourcePriority
        {
            get
            {
                return mResourceManager.ResourcePriority;
            }
            set
            {
                mResourceManager.ResourcePriority = mResourcePriority = value;
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

            mEventComponent = GameEntry.GetComponent<EventComponent>();
            if (mEventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            mEditorResourceMode = baseComponent.EditorResourceMode;
            mResourceManager = mEditorResourceMode ? baseComponent.EditorResourceHelper : GameFrameworkEntry.GetModule<IResourceManager>();
            if (mResourceManager == null)
            {
                Log.Fatal("Resource manager is invalid.");
                return;
            }

            mResourceManager.ResourceVerifyStart += OnResourceVerifyStart;
            mResourceManager.ResourceVerifySuccess += OnResourceVerifySuccess;
            mResourceManager.ResourceVerifyFailure += OnResourceVerifyFailure;
            mResourceManager.ResourceApplyStart += OnResourceApplyStart;
            mResourceManager.ResourceApplySuccess += OnResourceApplySuccess;
            mResourceManager.ResourceApplyFailure += OnResourceApplyFailure;
            mResourceManager.ResourceUpdateStart += OnResourceUpdateStart;
            mResourceManager.ResourceUpdateChanged += OnResourceUpdateChanged;
            mResourceManager.ResourceUpdateSuccess += OnResourceUpdateSuccess;
            mResourceManager.ResourceUpdateFailure += OnResourceUpdateFailure;
            mResourceManager.ResourceUpdateAllComplete += OnResourceUpdateAllComplete;

            mResourceManager.SetReadOnlyPath(Application.streamingAssetsPath);
            if (mReadWritePathType == ReadWritePathType.TemporaryCache)
            {
                mResourceManager.SetReadWritePath(Application.temporaryCachePath);
            }
            else
            {
                if (mReadWritePathType == ReadWritePathType.Unspecified)
                {
                    mReadWritePathType = ReadWritePathType.PersistentData;
                }

                mResourceManager.SetReadWritePath(Application.persistentDataPath);
            }

            if (mEditorResourceMode)
            {
                return;
            }

            SetResourceMode(mResourceMode);
            mResourceManager.SetObjectPoolManager(GameFrameworkEntry.GetModule<IObjectPoolManager>());
            mResourceManager.SetFileSystemManager(GameFrameworkEntry.GetModule<IFileSystemManager>());
            mResourceManager.SetDownloadManager(GameFrameworkEntry.GetModule<IDownloadManager>());
            mResourceManager.AssetAutoReleaseInterval = mAssetAutoReleaseInterval;
            mResourceManager.AssetCapacity = mAssetCapacity;
            mResourceManager.AssetExpireTime = mAssetExpireTime;
            mResourceManager.AssetPriority = mAssetPriority;
            mResourceManager.ResourceAutoReleaseInterval = mResourceAutoReleaseInterval;
            mResourceManager.ResourceCapacity = mResourceCapacity;
            mResourceManager.ResourceExpireTime = mResourceExpireTime;
            mResourceManager.ResourcePriority = mResourcePriority;
            if (mResourceMode == ResourceMode.Updatable || mResourceMode == ResourceMode.UpdatableWhilePlaying)
            {
                mResourceManager.UpdatePrefixUri = mUpdatePrefixUri;
                mResourceManager.GenerateReadWriteVersionListLength = mGenerateReadWriteVersionListLength;
                mResourceManager.UpdateRetryCount = mUpdateRetryCount;
            }

            mResourceHelper = Helper.CreateHelper(mResourceHelperTypeName, mCustomResourceHelper);
            if (mResourceHelper == null)
            {
                Log.Error("Can not create resource helper.");
                return;
            }

            mResourceHelper.name = "Resource Helper";
            Transform transform = mResourceHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            mResourceManager.SetResourceHelper(mResourceHelper);

            if (mInstanceRoot == null)
            {
                mInstanceRoot = new GameObject("Load Resource Agent Instances").transform;
                mInstanceRoot.SetParent(gameObject.transform);
                mInstanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < mLoadResourceAgentHelperCount; i++)
            {
                AddLoadResourceAgentHelper(i);
            }
        }

        private void Update()
        {
            mLastUnloadUnusedAssetsOperationElapseSeconds += Time.unscaledDeltaTime;
            if (mAsyncOperation == null && (mForceUnloadUnusedAssets || mLastUnloadUnusedAssetsOperationElapseSeconds >= mMaxUnloadUnusedAssetsInterval || mPreorderUnloadUnusedAssets && mLastUnloadUnusedAssetsOperationElapseSeconds >= mMinUnloadUnusedAssetsInterval))
            {
                Log.Info("Unload unused assets...");
                mForceUnloadUnusedAssets = false;
                mPreorderUnloadUnusedAssets = false;
                mLastUnloadUnusedAssetsOperationElapseSeconds = 0f;
                mAsyncOperation = Resources.UnloadUnusedAssets();
            }

            if (mAsyncOperation != null && mAsyncOperation.isDone)
            {
                mAsyncOperation = null;
                if (mPerformGCCollect)
                {
                    Log.Info("GC.Collect...");
                    mPerformGCCollect = false;
                    GC.Collect();
                }
            }
        }

        public void SetResourceMode(ResourceMode resourceMode)
        {
            mResourceManager.SetResourceMode(resourceMode);
            switch (resourceMode)
            {
                case ResourceMode.Package:
                    mResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V0);
                    mResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V1);
                    mResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V2);
                    break;

                case ResourceMode.Updatable:
                case ResourceMode.UpdatableWhilePlaying:
                    mResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V0);
                    mResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V1);
                    mResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V2);

                    mResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(0, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V0);
                    mResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(1, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V1_V2);
                    mResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(2, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V1_V2);

                    mResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V0);
                    mResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V1);
                    mResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V2);

                    mResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(0, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V0);
                    mResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(1, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V1);
                    mResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(2, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V2);

                    mResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V0);
                    mResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V1);
                    mResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V2);

                    mResourceManager.ResourcePackVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.ResourcePackVersionListDeserializeCallback_V0);
                    break;
            }
        }

        public void SetCurrentVariant(string currentVariant)
        {
            mResourceManager.SetCurrentVariant(!string.IsNullOrEmpty(currentVariant) ? currentVariant : null);
        }

        public void SetDecryptResourceCallback(DecryptResourceCallback decryptResourceCallback)
        {
            mResourceManager.SetDecryptResourceCallback(decryptResourceCallback);
        }

        public void UnloadUnusedAssets(bool performGCCollect)
        {
            mPreorderUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                mPerformGCCollect = performGCCollect;
            }
        }

        public void ForceUnloadUnusedAssets(bool performGCCollect)
        {
            mForceUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                mPerformGCCollect = performGCCollect;
            }
        }

        public void InitResources(InitResourcesCompleteCallback initResourcesCompleteCallback)
        {
            mResourceManager.InitResources(initResourcesCompleteCallback);
        }

        public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
        {
            return mResourceManager.CheckVersionList(latestInternalResourceVersion);
        }

        public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode, UpdateVersionListCallbacks updateVersionListCallbacks)
        {
            mResourceManager.UpdateVersionList(versionListLength, versionListHashCode, versionListCompressedLength, versionListCompressedHashCode, updateVersionListCallbacks);
        }

        public void VerifyResources(VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            mResourceManager.VerifyResources(0, verifyResourcesCompleteCallback);
        }

        public void VerifyResources(int verifyResourceLengthPerFrame, VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            mResourceManager.VerifyResources(verifyResourceLengthPerFrame, verifyResourcesCompleteCallback);
        }

        public void CheckResources(CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            mResourceManager.CheckResources(false, checkResourcesCompleteCallback);
        }

        public void CheckResources(bool ignoreOtherVariant, CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            mResourceManager.CheckResources(ignoreOtherVariant, checkResourcesCompleteCallback);
        }

        public void ApplyResources(string resourcePackPath, ApplyResourcesCompleteCallback applyResourcesCompleteCallback)
        {
            mResourceManager.ApplyResources(resourcePackPath, applyResourcesCompleteCallback);
        }

        public void UpdateResources(UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            mResourceManager.UpdateResources(updateResourcesCompleteCallback);
        }

        public void UpdateResources(string resourceGroupName, UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            mResourceManager.UpdateResources(resourceGroupName, updateResourcesCompleteCallback);
        }

        public void StopUpdateResources()
        {
            mResourceManager.StopUpdateResources();
        }

        public bool VerifyResourcePack(string resourcePackPath)
        {
            return mResourceManager.VerifyResourcePack(resourcePackPath);
        }

        public TaskInfo[] GetAllLoadAssetInfos()
        {
            return mResourceManager.GetAllLoadAssetInfos();
        }

        public void GetAllLoadAssetInfos(List<TaskInfo> results)
        {
            mResourceManager.GetAllLoadAssetInfos(results);
        }

        public HasAssetResult HasAsset(string assetName)
        {
            return mResourceManager.HasAsset(assetName);
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

            mResourceManager.LoadAsset(assetName, assetType, priority, loadAssetCallbacks, userData);
        }

        public void UnloadAsset(object asset)
        {
            mResourceManager.UnloadAsset(asset);
        }

        public string GetBinaryPath(string binaryAssetName)
        {
            return mResourceManager.GetBinaryPath(binaryAssetName);
        }

        public bool GetBinaryPath(string binaryAssetName, out bool storageInReadOnly, out bool storageInFileSystem, out string relativePath, out string fileName)
        {
            return mResourceManager.GetBinaryPath(binaryAssetName, out storageInReadOnly, out storageInFileSystem, out relativePath, out fileName);
        }

        public int GetBinaryLength(string binaryAssetName)
        {
            return mResourceManager.GetBinaryLength(binaryAssetName);
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

            mResourceManager.LoadBinary(binaryAssetName, loadBinaryCallbacks, userData);
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

            return mResourceManager.LoadBinaryFromFileSystem(binaryAssetName);
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

            return mResourceManager.LoadBinaryFromFileSystem(binaryAssetName, buffer, startIndex, length);
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

            return mResourceManager.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, length);
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

            return mResourceManager.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, startIndex, length);
        }

        public bool HasResourceGroup(string resourceGroupName)
        {
            return mResourceManager.HasResourceGroup(resourceGroupName);
        }

        public IResourceGroup GetResourceGroup()
        {
            return mResourceManager.GetResourceGroup();
        }

        public IResourceGroup GetResourceGroup(string resourceGroupName)
        {
            return mResourceManager.GetResourceGroup(resourceGroupName);
        }

        public IResourceGroup[] GetAllResourceGroups()
        {
            return mResourceManager.GetAllResourceGroups();
        }

        public void GetAllResourceGroups(List<IResourceGroup> results)
        {
            mResourceManager.GetAllResourceGroups(results);
        }

        public IResourceGroupCollection GetResourceGroupCollection(params string[] resourceGroupNames)
        {
            return mResourceManager.GetResourceGroupCollection(resourceGroupNames);
        }

        public IResourceGroupCollection GetResourceGroupCollection(List<string> resourceGroupNames)
        {
            return mResourceManager.GetResourceGroupCollection(resourceGroupNames);
        }

        private void AddLoadResourceAgentHelper(int index)
        {
            LoadResourceAgentHelperBase loadResourceAgentHelper = Helper.CreateHelper(mLoadResourceAgentHelperTypeName, mCustomLoadResourceAgentHelper, index);
            if (loadResourceAgentHelper == null)
            {
                Log.Error("Can not create load resource agent helper.");
                return;
            }

            loadResourceAgentHelper.name = Utility.Text.Format("Load Resource Agent Helper - {0}", index);
            Transform transform = loadResourceAgentHelper.transform;
            transform.SetParent(mInstanceRoot);
            transform.localScale = Vector3.one;

            mResourceManager.AddLoadResourceAgentHelper(loadResourceAgentHelper);
        }

        private void OnResourceVerifyStart(object sender, GameFramework.Resource.ResourceVerifyStartEventArgs e)
        {
            mEventComponent.Fire(this, ResourceVerifyStartEventArgs.Create(e));
        }

        private void OnResourceVerifySuccess(object sender, GameFramework.Resource.ResourceVerifySuccessEventArgs e)
        {
            mEventComponent.Fire(this, ResourceVerifySuccessEventArgs.Create(e));
        }

        private void OnResourceVerifyFailure(object sender, GameFramework.Resource.ResourceVerifyFailureEventArgs e)
        {
            mEventComponent.Fire(this, ResourceVerifyFailureEventArgs.Create(e));
        }

        private void OnResourceApplyStart(object sender, GameFramework.Resource.ResourceApplyStartEventArgs e)
        {
            mEventComponent.Fire(this, ResourceApplyStartEventArgs.Create(e));
        }

        private void OnResourceApplySuccess(object sender, GameFramework.Resource.ResourceApplySuccessEventArgs e)
        {
            mEventComponent.Fire(this, ResourceApplySuccessEventArgs.Create(e));
        }

        private void OnResourceApplyFailure(object sender, GameFramework.Resource.ResourceApplyFailureEventArgs e)
        {
            mEventComponent.Fire(this, ResourceApplyFailureEventArgs.Create(e));
        }

        private void OnResourceUpdateStart(object sender, GameFramework.Resource.ResourceUpdateStartEventArgs e)
        {
            mEventComponent.Fire(this, ResourceUpdateStartEventArgs.Create(e));
        }

        private void OnResourceUpdateChanged(object sender, GameFramework.Resource.ResourceUpdateChangedEventArgs e)
        {
            mEventComponent.Fire(this, ResourceUpdateChangedEventArgs.Create(e));
        }

        private void OnResourceUpdateSuccess(object sender, GameFramework.Resource.ResourceUpdateSuccessEventArgs e)
        {
            mEventComponent.Fire(this, ResourceUpdateSuccessEventArgs.Create(e));
        }

        private void OnResourceUpdateFailure(object sender, GameFramework.Resource.ResourceUpdateFailureEventArgs e)
        {
            mEventComponent.Fire(this, ResourceUpdateFailureEventArgs.Create(e));
        }

        private void OnResourceUpdateAllComplete(object sender, GameFramework.Resource.ResourceUpdateAllCompleteEventArgs e)
        {
            mEventComponent.Fire(this, ResourceUpdateAllCompleteEventArgs.Create(e));
        }
    }
}
