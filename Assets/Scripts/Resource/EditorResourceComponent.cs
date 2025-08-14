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
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    public sealed class EditorResourceComponent : MonoBehaviour, IResourceManager
    {
        private const int DefaultPriority = 0;
        private static readonly int AssetsStringLength = "Assets".Length;

        [SerializeField]
        private bool mEnableCachedAssets = true;

        [SerializeField]
        private int mLoadAssetCountPerFrame = 1;

        [SerializeField]
        private float mMinLoadAssetRandomDelaySeconds = 0f;

        [SerializeField]
        private float mMaxLoadAssetRandomDelaySeconds = 0f;

        private string mReadOnlyPath = null;
        private string mReadWritePath = null;
        private Dictionary<string, UnityEngine.Object> mCachedAssets = null;
        private GameFrameworkLinkedList<LoadAssetInfo> mLoadAssetInfos = null;
        private GameFrameworkLinkedList<LoadSceneInfo> mLoadSceneInfos = null;
        private GameFrameworkLinkedList<UnloadSceneInfo> mUnloadSceneInfos = null;

        public string ReadOnlyPath
        {
            get
            {
                return mReadOnlyPath;
            }
        }

        public string ReadWritePath
        {
            get
            {
                return mReadWritePath;
            }
        }

        public ResourceMode ResourceMode
        {
            get
            {
                return ResourceMode.Unspecified;
            }
        }

        public string CurrentVariant
        {
            get
            {
                return null;
            }
        }

        public PackageVersionListSerializer PackageVersionListSerializer
        {
            get
            {
                throw new NotSupportedException("ReadWriteVersionListSerializer");
            }
        }

        public UpdatableVersionListSerializer UpdatableVersionListSerializer
        {
            get
            {
                throw new NotSupportedException("ReadWriteVersionListSerializer");
            }
        }

        public ReadOnlyVersionListSerializer ReadOnlyVersionListSerializer
        {
            get
            {
                throw new NotSupportedException("ReadWriteVersionListSerializer");
            }
        }

        public ReadWriteVersionListSerializer ReadWriteVersionListSerializer
        {
            get
            {
                throw new NotSupportedException("ReadWriteVersionListSerializer");
            }
        }

        public ResourcePackVersionListSerializer ResourcePackVersionListSerializer
        {
            get
            {
                throw new NotSupportedException("ResourcePackVersionListSerializer");
            }
        }

        public string ApplicableGameVersion
        {
            get
            {
                throw new NotSupportedException("ApplicableGameVersion");
            }
        }

        public int InternalResourceVersion
        {
            get
            {
                throw new NotSupportedException("InternalResourceVersion");
            }
        }

        public int AssetCount
        {
            get
            {
                throw new NotSupportedException("AssetCount");
            }
        }

        public int ResourceCount
        {
            get
            {
                throw new NotSupportedException("ResourceCount");
            }
        }

        public int ResourceGroupCount
        {
            get
            {
                throw new NotSupportedException("ResourceGroupCount");
            }
        }

        public string UpdatePrefixUri
        {
            get
            {
                throw new NotSupportedException("UpdatePrefixUri");
            }
            set
            {
                throw new NotSupportedException("UpdatePrefixUri");
            }
        }

        public int GenerateReadWriteVersionListLength
        {
            get
            {
                throw new NotSupportedException("GenerateReadWriteVersionListLength");
            }
            set
            {
                throw new NotSupportedException("GenerateReadWriteVersionListLength");
            }
        }

        public string ApplyingResourcePackPath
        {
            get
            {
                throw new NotSupportedException("ApplyingResourcePackPath");
            }
        }

        public int ApplyWaitingCount
        {
            get
            {
                throw new NotSupportedException("ApplyWaitingCount");
            }
        }

        public int UpdateRetryCount
        {
            get
            {
                throw new NotSupportedException("UpdateRetryCount");
            }
            set
            {
                throw new NotSupportedException("UpdateRetryCount");
            }
        }

        public IResourceGroup UpdatingResourceGroup
        {
            get
            {
                throw new NotSupportedException("UpdatingResourceGroup");
            }
        }

        public int UpdateWaitingCount
        {
            get
            {
                throw new NotSupportedException("UpdateWaitingCount");
            }
        }

        public int UpdateWaitingWhilePlayingCount
        {
            get
            {
                throw new NotSupportedException("UpdateWaitingWhilePlayingCount");
            }
        }

        public int UpdateCandidateCount
        {
            get
            {
                throw new NotSupportedException("UpdateCandidateCount");
            }
        }

        public int LoadTotalAgentCount
        {
            get
            {
                throw new NotSupportedException("LoadTotalAgentCount");
            }
        }

        public int LoadFreeAgentCount
        {
            get
            {
                throw new NotSupportedException("LoadFreeAgentCount");
            }
        }

        public int LoadWorkingAgentCount
        {
            get
            {
                throw new NotSupportedException("LoadWorkingAgentCount");
            }
        }

        public int LoadWaitingTaskCount
        {
            get
            {
                throw new NotSupportedException("LoadWaitingTaskCount");
            }
        }

        public float AssetAutoReleaseInterval
        {
            get
            {
                throw new NotSupportedException("AssetAutoReleaseInterval");
            }
            set
            {
                throw new NotSupportedException("AssetAutoReleaseInterval");
            }
        }

        public int AssetCapacity
        {
            get
            {
                throw new NotSupportedException("AssetCapacity");
            }
            set
            {
                throw new NotSupportedException("AssetCapacity");
            }
        }

        public float AssetExpireTime
        {
            get
            {
                throw new NotSupportedException("AssetExpireTime");
            }
            set
            {
                throw new NotSupportedException("AssetExpireTime");
            }
        }

        public int AssetPriority
        {
            get
            {
                throw new NotSupportedException("AssetPriority");
            }
            set
            {
                throw new NotSupportedException("AssetPriority");
            }
        }

        public float ResourceAutoReleaseInterval
        {
            get
            {
                throw new NotSupportedException("ResourceAutoReleaseInterval");
            }
            set
            {
                throw new NotSupportedException("ResourceAutoReleaseInterval");
            }
        }

        public int ResourceCapacity
        {
            get
            {
                throw new NotSupportedException("ResourceCapacity");
            }
            set
            {
                throw new NotSupportedException("ResourceCapacity");
            }
        }

        public float ResourceExpireTime
        {
            get
            {
                throw new NotSupportedException("ResourceExpireTime");
            }
            set
            {
                throw new NotSupportedException("ResourceExpireTime");
            }
        }

        public int ResourcePriority
        {
            get
            {
                throw new NotSupportedException("ResourcePriority");
            }
            set
            {
                throw new NotSupportedException("ResourcePriority");
            }
        }

        public int LoadWaitingAssetCount
        {
            get
            {
                return mLoadAssetInfos.Count;
            }
        }

#pragma warning disable 0067, 0414

        public event EventHandler<GameFramework.Resource.ResourceVerifyStartEventArgs> ResourceVerifyStart = null;

        public event EventHandler<GameFramework.Resource.ResourceVerifySuccessEventArgs> ResourceVerifySuccess = null;

        public event EventHandler<GameFramework.Resource.ResourceVerifyFailureEventArgs> ResourceVerifyFailure = null;

        public event EventHandler<GameFramework.Resource.ResourceApplyStartEventArgs> ResourceApplyStart = null;

        public event EventHandler<GameFramework.Resource.ResourceApplySuccessEventArgs> ResourceApplySuccess = null;

        public event EventHandler<GameFramework.Resource.ResourceApplyFailureEventArgs> ResourceApplyFailure = null;

        public event EventHandler<GameFramework.Resource.ResourceUpdateStartEventArgs> ResourceUpdateStart = null;

        public event EventHandler<GameFramework.Resource.ResourceUpdateChangedEventArgs> ResourceUpdateChanged = null;

        public event EventHandler<GameFramework.Resource.ResourceUpdateSuccessEventArgs> ResourceUpdateSuccess = null;

        public event EventHandler<GameFramework.Resource.ResourceUpdateFailureEventArgs> ResourceUpdateFailure = null;

        public event EventHandler<GameFramework.Resource.ResourceUpdateAllCompleteEventArgs> ResourceUpdateAllComplete = null;

#pragma warning restore 0067, 0414

        private void Awake()
        {
            mReadOnlyPath = null;
            mReadWritePath = null;
            mCachedAssets = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
            mLoadAssetInfos = new GameFrameworkLinkedList<LoadAssetInfo>();
            mLoadSceneInfos = new GameFrameworkLinkedList<LoadSceneInfo>();
            mUnloadSceneInfos = new GameFrameworkLinkedList<UnloadSceneInfo>();

            BaseComponent baseComponent = GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Error("Can not find base component.");
                return;
            }

            if (baseComponent.EditorResourceMode)
            {
                baseComponent.EditorResourceHelper = this;
                enabled = true;
            }
            else
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (mLoadAssetInfos.Count > 0)
            {
                int count = 0;
                LinkedListNode<LoadAssetInfo> current = mLoadAssetInfos.First;
                while (current != null && count < mLoadAssetCountPerFrame)
                {
                    LoadAssetInfo loadAssetInfo = current.Value;
                    float elapseSeconds = (float)(DateTime.UtcNow - loadAssetInfo.StartTime).TotalSeconds;
                    if (elapseSeconds >= loadAssetInfo.DelaySeconds)
                    {
                        UnityEngine.Object asset = GetCachedAsset(loadAssetInfo.AssetName);
                        if (asset == null)
                        {
#if UNITY_EDITOR
                            if (loadAssetInfo.AssetType != null)
                            {
                                asset = UnityEditor.AssetDatabase.LoadAssetAtPath(loadAssetInfo.AssetName, loadAssetInfo.AssetType);
                            }
                            else
                            {
                                asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(loadAssetInfo.AssetName);
                            }

                            if (mEnableCachedAssets && asset != null)
                            {
                                mCachedAssets.Add(loadAssetInfo.AssetName, asset);
                            }
#endif
                        }

                        if (asset != null)
                        {
                            if (loadAssetInfo.LoadAssetCallbacks.LoadAssetSuccessCallback != null)
                            {
                                loadAssetInfo.LoadAssetCallbacks.LoadAssetSuccessCallback(loadAssetInfo.AssetName, asset, elapseSeconds, loadAssetInfo.UserData);
                            }
                        }
                        else
                        {
                            if (loadAssetInfo.LoadAssetCallbacks.LoadAssetFailureCallback != null)
                            {
                                loadAssetInfo.LoadAssetCallbacks.LoadAssetFailureCallback(loadAssetInfo.AssetName, LoadResourceStatus.AssetError, "Can not load this asset from asset database.", loadAssetInfo.UserData);
                            }
                        }

                        LinkedListNode<LoadAssetInfo> next = current.Next;
                        mLoadAssetInfos.Remove(loadAssetInfo);
                        current = next;
                        count++;
                    }
                    else
                    {
                        if (loadAssetInfo.LoadAssetCallbacks.LoadAssetUpdateCallback != null)
                        {
                            loadAssetInfo.LoadAssetCallbacks.LoadAssetUpdateCallback(loadAssetInfo.AssetName, elapseSeconds / loadAssetInfo.DelaySeconds, loadAssetInfo.UserData);
                        }

                        current = current.Next;
                    }
                }
            }

            if (mLoadSceneInfos.Count > 0)
            {
                LinkedListNode<LoadSceneInfo> current = mLoadSceneInfos.First;
                while (current != null)
                {
                    LoadSceneInfo loadSceneInfo = current.Value;
                    if (loadSceneInfo.AsyncOperation.isDone)
                    {
                        if (loadSceneInfo.AsyncOperation.allowSceneActivation)
                        {
                            if (loadSceneInfo.LoadSceneCallbacks.LoadSceneSuccessCallback != null)
                            {
                                loadSceneInfo.LoadSceneCallbacks.LoadSceneSuccessCallback(loadSceneInfo.SceneAssetName, (float)(DateTime.UtcNow - loadSceneInfo.StartTime).TotalSeconds, loadSceneInfo.UserData);
                            }
                        }
                        else
                        {
                            if (loadSceneInfo.LoadSceneCallbacks.LoadSceneFailureCallback != null)
                            {
                                loadSceneInfo.LoadSceneCallbacks.LoadSceneFailureCallback(loadSceneInfo.SceneAssetName, LoadResourceStatus.AssetError, "Can not load this scene from asset database.", loadSceneInfo.UserData);
                            }
                        }

                        LinkedListNode<LoadSceneInfo> next = current.Next;
                        mLoadSceneInfos.Remove(loadSceneInfo);
                        current = next;
                    }
                    else
                    {
                        if (loadSceneInfo.LoadSceneCallbacks.LoadSceneUpdateCallback != null)
                        {
                            loadSceneInfo.LoadSceneCallbacks.LoadSceneUpdateCallback(loadSceneInfo.SceneAssetName, loadSceneInfo.AsyncOperation.progress, loadSceneInfo.UserData);
                        }

                        current = current.Next;
                    }
                }
            }

            if (mUnloadSceneInfos.Count > 0)
            {
                LinkedListNode<UnloadSceneInfo> current = mUnloadSceneInfos.First;
                while (current != null)
                {
                    UnloadSceneInfo unloadSceneInfo = current.Value;
                    if (unloadSceneInfo.AsyncOperation.isDone)
                    {
                        if (unloadSceneInfo.AsyncOperation.allowSceneActivation)
                        {
                            if (unloadSceneInfo.UnloadSceneCallbacks.UnloadSceneSuccessCallback != null)
                            {
                                unloadSceneInfo.UnloadSceneCallbacks.UnloadSceneSuccessCallback(unloadSceneInfo.SceneAssetName, unloadSceneInfo.UserData);
                            }
                        }
                        else
                        {
                            if (unloadSceneInfo.UnloadSceneCallbacks.UnloadSceneFailureCallback != null)
                            {
                                unloadSceneInfo.UnloadSceneCallbacks.UnloadSceneFailureCallback(unloadSceneInfo.SceneAssetName, unloadSceneInfo.UserData);
                            }
                        }

                        LinkedListNode<UnloadSceneInfo> next = current.Next;
                        mUnloadSceneInfos.Remove(unloadSceneInfo);
                        current = next;
                    }
                    else
                    {
                        current = current.Next;
                    }
                }
            }
        }

        public void SetReadOnlyPath(string readOnlyPath)
        {
            if (string.IsNullOrEmpty(readOnlyPath))
            {
                Log.Error("Read-only path is invalid.");
                return;
            }

            mReadOnlyPath = readOnlyPath;
        }

        public void SetReadWritePath(string readWritePath)
        {
            if (string.IsNullOrEmpty(readWritePath))
            {
                Log.Error("Read-write path is invalid.");
                return;
            }

            mReadWritePath = readWritePath;
        }

        public void SetResourceMode(ResourceMode resourceMode)
        {
            throw new NotSupportedException("SetResourceMode");
        }

        public void SetCurrentVariant(string currentVariant)
        {
            throw new NotSupportedException("SetCurrentVariant");
        }

        public void SetObjectPoolManager(IObjectPoolManager objectPoolManager)
        {
            throw new NotSupportedException("SetObjectPoolManager");
        }

        public void SetFileSystemManager(IFileSystemManager fileSystemManager)
        {
            throw new NotSupportedException("SetFileSystemManager");
        }

        public void SetDownloadManager(IDownloadManager downloadManager)
        {
            throw new NotSupportedException("SetDownloadManager");
        }

        public void SetDecryptResourceCallback(DecryptResourceCallback decryptResourceCallback)
        {
            throw new NotSupportedException("SetDecryptResourceCallback");
        }

        public void SetResourceHelper(IResourceHelper resourceHelper)
        {
            throw new NotSupportedException("SetResourceHelper");
        }

        public void AddLoadResourceAgentHelper(ILoadResourceAgentHelper loadResourceAgentHelper)
        {
            throw new NotSupportedException("AddLoadResourceAgentHelper");
        }

        public void InitResources(InitResourcesCompleteCallback initResourcesCompleteCallback)
        {
            throw new NotSupportedException("InitResources");
        }

        public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
        {
            throw new NotSupportedException("CheckVersionList");
        }

        public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode, UpdateVersionListCallbacks updateVersionListCallbacks)
        {
            throw new NotSupportedException("UpdateVersionList");
        }

        public void VerifyResources(int verifyResourceLengthPerFrame, VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            throw new NotSupportedException("VerifyResources");
        }

        public void CheckResources(bool ignoreOtherVariant, CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            throw new NotSupportedException("CheckResources");
        }

        public void ApplyResources(string resourcePackPath, ApplyResourcesCompleteCallback applyResourcesCompleteCallback)
        {
            throw new NotSupportedException("ApplyResources");
        }

        public void UpdateResources(UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            throw new NotSupportedException("UpdateResources");
        }

        public void UpdateResources(string resourceGroupName, UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            throw new NotSupportedException("UpdateResources");
        }

        public void StopUpdateResources()
        {
            throw new NotSupportedException("StopUpdateResources");
        }

        public bool VerifyResourcePack(string resourcePackPath)
        {
            throw new NotSupportedException("VerifyResourcePack");
        }

        public TaskInfo[] GetAllLoadAssetInfos()
        {
            throw new NotSupportedException("GetAllLoadAssetInfos");
        }

        public void GetAllLoadAssetInfos(List<TaskInfo> results)
        {
            throw new NotSupportedException("GetAllLoadAssetInfos");
        }

        public HasAssetResult HasAsset(string assetName)
        {
#if UNITY_EDITOR
            UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetName);
            if (obj == null)
            {
                return HasAssetResult.NotExist;
            }

            HasAssetResult result = obj.GetType() == typeof(UnityEditor.DefaultAsset) ? HasAssetResult.BinaryOnDisk : HasAssetResult.AssetOnDisk;
            obj = null;
            UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
            return result;
#else
            return HasAssetResult.NotExist;
#endif
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
            if (loadAssetCallbacks == null)
            {
                Log.Error("Load asset callbacks is invalid.");
                return;
            }

            if (string.IsNullOrEmpty(assetName))
            {
                if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback(assetName, LoadResourceStatus.NotExist, "Asset name is invalid.", userData);
                }

                return;
            }

            if (!assetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback(assetName, LoadResourceStatus.NotExist, Utility.Text.Format("Asset name '{0}' is invalid.", assetName), userData);
                }

                return;
            }

            if (!HasFile(assetName))
            {
                if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback(assetName, LoadResourceStatus.NotExist, Utility.Text.Format("Asset '{0}' is not exist.", assetName), userData);
                }

                return;
            }

            mLoadAssetInfos.AddLast(new LoadAssetInfo(assetName, assetType, priority, DateTime.UtcNow, mMinLoadAssetRandomDelaySeconds + (float)Utility.Random.GetRandomDouble() * (mMaxLoadAssetRandomDelaySeconds - mMinLoadAssetRandomDelaySeconds), loadAssetCallbacks, userData));
        }

        public void UnloadAsset(object asset)
        {
            // Do nothing in editor resource mode.
        }

        public void LoadScene(string sceneAssetName, LoadSceneCallbacks loadSceneCallbacks)
        {
            LoadScene(sceneAssetName, DefaultPriority, loadSceneCallbacks, null);
        }

        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks loadSceneCallbacks)
        {
            LoadScene(sceneAssetName, priority, loadSceneCallbacks, null);
        }

        public void LoadScene(string sceneAssetName, LoadSceneCallbacks loadSceneCallbacks, object userData)
        {
            LoadScene(sceneAssetName, DefaultPriority, loadSceneCallbacks, userData);
        }

        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks loadSceneCallbacks, object userData)
        {
            if (loadSceneCallbacks == null)
            {
                Log.Error("Load scene callbacks is invalid.");
                return;
            }

            if (string.IsNullOrEmpty(sceneAssetName))
            {
                if (loadSceneCallbacks.LoadSceneFailureCallback != null)
                {
                    loadSceneCallbacks.LoadSceneFailureCallback(sceneAssetName, LoadResourceStatus.NotExist, "Scene asset name is invalid.", userData);
                }

                return;
            }

            if (!sceneAssetName.StartsWith("Assets/", StringComparison.Ordinal) || !sceneAssetName.EndsWith(".unity", StringComparison.Ordinal))
            {
                if (loadSceneCallbacks.LoadSceneFailureCallback != null)
                {
                    loadSceneCallbacks.LoadSceneFailureCallback(sceneAssetName, LoadResourceStatus.NotExist, Utility.Text.Format("Scene asset name '{0}' is invalid.", sceneAssetName), userData);
                }

                return;
            }

            if (!HasFile(sceneAssetName))
            {
                if (loadSceneCallbacks.LoadSceneFailureCallback != null)
                {
                    loadSceneCallbacks.LoadSceneFailureCallback(sceneAssetName, LoadResourceStatus.NotExist, Utility.Text.Format("Scene '{0}' is not exist.", sceneAssetName), userData);
                }

                return;
            }

#if UNITY_5_5_OR_NEWER
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneAssetName, LoadSceneMode.Additive);
#else
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(SceneComponent.GetSceneName(sceneAssetName), LoadSceneMode.Additive);
#endif
            if (asyncOperation == null)
            {
                return;
            }

            mLoadSceneInfos.AddLast(new LoadSceneInfo(asyncOperation, sceneAssetName, priority, DateTime.UtcNow, loadSceneCallbacks, userData));
        }

        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks)
        {
            UnloadScene(sceneAssetName, unloadSceneCallbacks, null);
        }

        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return;
            }

            if (!sceneAssetName.StartsWith("Assets/", StringComparison.Ordinal) || !sceneAssetName.EndsWith(".unity", StringComparison.Ordinal))
            {
                Log.Error("Scene asset name '{0}' is invalid.", sceneAssetName);
                return;
            }

            if (unloadSceneCallbacks == null)
            {
                Log.Error("Unload scene callbacks is invalid.");
                return;
            }

            if (!HasFile(sceneAssetName))
            {
                Log.Error("Scene '{0}' is not exist.", sceneAssetName);
                return;
            }

#if UNITY_5_5_OR_NEWER
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(sceneAssetName);
            if (asyncOperation == null)
            {
                return;
            }

            mUnloadSceneInfos.AddLast(new UnloadSceneInfo(asyncOperation, sceneAssetName, unloadSceneCallbacks, userData));
#else
            if (SceneManager.UnloadScene(SceneComponent.GetSceneName(sceneAssetName)))
            {
                if (unloadSceneCallbacks.UnloadSceneSuccessCallback != null)
                {
                    unloadSceneCallbacks.UnloadSceneSuccessCallback(sceneAssetName, userData);
                }
            }
            else
            {
                if (unloadSceneCallbacks.UnloadSceneFailureCallback != null)
                {
                    unloadSceneCallbacks.UnloadSceneFailureCallback(sceneAssetName, userData);
                }
            }
#endif
        }

        public string GetBinaryPath(string binaryAssetName)
        {
            if (!HasFile(binaryAssetName))
            {
                return null;
            }

            return Application.dataPath.Substring(0, Application.dataPath.Length - AssetsStringLength) + binaryAssetName;
        }

        public bool GetBinaryPath(string binaryAssetName, out bool storageInReadOnly, out bool storageInFileSystem, out string relativePath, out string fileName)
        {
            throw new NotSupportedException("GetBinaryPath");
        }

        public int GetBinaryLength(string binaryAssetName)
        {
            string binaryPath = GetBinaryPath(binaryAssetName);
            if (string.IsNullOrEmpty(binaryPath))
            {
                return -1;
            }

            return (int)new System.IO.FileInfo(binaryPath).Length;
        }

        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks)
        {
            LoadBinary(binaryAssetName, loadBinaryCallbacks, null);
        }

        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
        {
            if (loadBinaryCallbacks == null)
            {
                Log.Error("Load binary callbacks is invalid.");
                return;
            }

            if (string.IsNullOrEmpty(binaryAssetName))
            {
                if (loadBinaryCallbacks.LoadBinaryFailureCallback != null)
                {
                    loadBinaryCallbacks.LoadBinaryFailureCallback(binaryAssetName, LoadResourceStatus.NotExist, "Binary asset name is invalid.", userData);
                }

                return;
            }

            if (!binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                if (loadBinaryCallbacks.LoadBinaryFailureCallback != null)
                {
                    loadBinaryCallbacks.LoadBinaryFailureCallback(binaryAssetName, LoadResourceStatus.NotExist, Utility.Text.Format("Binary asset name '{0}' is invalid.", binaryAssetName), userData);
                }

                return;
            }

            string binaryPath = GetBinaryPath(binaryAssetName);
            if (binaryPath == null)
            {
                if (loadBinaryCallbacks.LoadBinaryFailureCallback != null)
                {
                    loadBinaryCallbacks.LoadBinaryFailureCallback(binaryAssetName, LoadResourceStatus.NotExist, Utility.Text.Format("Binary asset '{0}' is not exist.", binaryAssetName), userData);
                }

                return;
            }

            try
            {
                byte[] binaryBytes = File.ReadAllBytes(binaryPath);
                loadBinaryCallbacks.LoadBinarySuccessCallback(binaryAssetName, binaryBytes, 0f, userData);
            }
            catch (Exception exception)
            {
                if (loadBinaryCallbacks.LoadBinaryFailureCallback != null)
                {
                    loadBinaryCallbacks.LoadBinaryFailureCallback(binaryAssetName, LoadResourceStatus.AssetError, exception.ToString(), userData);
                }
            }
        }

        public byte[] LoadBinaryFromFileSystem(string binaryAssetName)
        {
            throw new NotSupportedException("LoadBinaryFromFileSystem");
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            throw new NotSupportedException("LoadBinaryFromFileSystem");
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex)
        {
            throw new NotSupportedException("LoadBinaryFromFileSystem");
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            throw new NotSupportedException("LoadBinaryFromFileSystem");
        }

        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int length)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, int length)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int length)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int length)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int startIndex, int length)
        {
            throw new NotSupportedException("LoadBinarySegmentFromFileSystem");
        }

        public bool HasResourceGroup(string resourceGroupName)
        {
            throw new NotSupportedException("HasResourceGroup");
        }

        public IResourceGroup GetResourceGroup()
        {
            throw new NotSupportedException("GetResourceGroup");
        }

        public IResourceGroup GetResourceGroup(string resourceGroupName)
        {
            throw new NotSupportedException("GetResourceGroup");
        }

        public IResourceGroup[] GetAllResourceGroups()
        {
            throw new NotSupportedException("GetAllResourceGroups");
        }

        public void GetAllResourceGroups(List<IResourceGroup> results)
        {
            throw new NotSupportedException("GetAllResourceGroups");
        }

        public IResourceGroupCollection GetResourceGroupCollection(params string[] resourceGroupNames)
        {
            throw new NotSupportedException("GetResourceGroupCollection");
        }

        public IResourceGroupCollection GetResourceGroupCollection(List<string> resourceGroupNames)
        {
            throw new NotSupportedException("GetResourceGroupCollection");
        }

        private bool HasFile(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return false;
            }

            if (HasCachedAsset(assetName))
            {
                return true;
            }

            string assetFullName = Application.dataPath.Substring(0, Application.dataPath.Length - AssetsStringLength) + assetName;
            if (string.IsNullOrEmpty(assetFullName))
            {
                return false;
            }

            string[] splitedAssetFullName = assetFullName.Split('/');
            string currentPath = Path.GetPathRoot(assetFullName);
            for (int i = 1; i < splitedAssetFullName.Length - 1; i++)
            {
                string[] directoryNames = Directory.GetDirectories(currentPath, splitedAssetFullName[i]);
                if (directoryNames.Length != 1)
                {
                    return false;
                }

                currentPath = directoryNames[0];
            }

            string[] fileNames = Directory.GetFiles(currentPath, splitedAssetFullName[splitedAssetFullName.Length - 1]);
            if (fileNames.Length != 1)
            {
                return false;
            }

            string fileFullName = Utility.Path.GetRegularPath(fileNames[0]);
            if (fileFullName == null)
            {
                return false;
            }

            if (assetFullName != fileFullName)
            {
                if (assetFullName.ToLowerInvariant() == fileFullName.ToLowerInvariant())
                {
                    Log.Warning("The real path of the specific asset '{0}' is '{1}'. Check the case of letters in the path.", assetName, "Assets" + fileFullName.Substring(Application.dataPath.Length));
                }

                return false;
            }

            return true;
        }

        private bool HasCachedAsset(string assetName)
        {
            if (!mEnableCachedAssets)
            {
                return false;
            }

            if (string.IsNullOrEmpty(assetName))
            {
                return false;
            }

            return mCachedAssets.ContainsKey(assetName);
        }

        private UnityEngine.Object GetCachedAsset(string assetName)
        {
            if (!mEnableCachedAssets)
            {
                return null;
            }

            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            UnityEngine.Object asset = null;
            if (mCachedAssets.TryGetValue(assetName, out asset))
            {
                return asset;
            }

            return null;
        }

        [StructLayout(LayoutKind.Auto)]
        private struct LoadAssetInfo
        {
            private readonly string mAssetName;
            private readonly Type mAssetType;
            private readonly int mPriority;
            private readonly DateTime mStartTime;
            private readonly float mDelaySeconds;
            private readonly LoadAssetCallbacks mLoadAssetCallbacks;
            private readonly object mUserData;

            public LoadAssetInfo(string assetName, Type assetType, int priority, DateTime startTime, float delaySeconds, LoadAssetCallbacks loadAssetCallbacks, object userData)
            {
                mAssetName = assetName;
                mAssetType = assetType;
                mPriority = priority;
                mStartTime = startTime;
                mDelaySeconds = delaySeconds;
                mLoadAssetCallbacks = loadAssetCallbacks;
                mUserData = userData;
            }

            public string AssetName
            {
                get
                {
                    return mAssetName;
                }
            }

            public Type AssetType
            {
                get
                {
                    return mAssetType;
                }
            }

            public int Priority
            {
                get
                {
                    return mPriority;
                }
            }

            public DateTime StartTime
            {
                get
                {
                    return mStartTime;
                }
            }

            public float DelaySeconds
            {
                get
                {
                    return mDelaySeconds;
                }
            }

            public LoadAssetCallbacks LoadAssetCallbacks
            {
                get
                {
                    return mLoadAssetCallbacks;
                }
            }

            public object UserData
            {
                get
                {
                    return mUserData;
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct LoadSceneInfo
        {
            private readonly AsyncOperation mAsyncOperation;
            private readonly string mSceneAssetName;
            private readonly int mPriority;
            private readonly DateTime mStartTime;
            private readonly LoadSceneCallbacks mLoadSceneCallbacks;
            private readonly object mUserData;

            public LoadSceneInfo(AsyncOperation asyncOperation, string sceneAssetName, int priority, DateTime startTime, LoadSceneCallbacks loadSceneCallbacks, object userData)
            {
                mAsyncOperation = asyncOperation;
                mSceneAssetName = sceneAssetName;
                mPriority = priority;
                mStartTime = startTime;
                mLoadSceneCallbacks = loadSceneCallbacks;
                mUserData = userData;
            }

            public AsyncOperation AsyncOperation
            {
                get
                {
                    return mAsyncOperation;
                }
            }

            public string SceneAssetName
            {
                get
                {
                    return mSceneAssetName;
                }
            }

            public int Priority
            {
                get
                {
                    return mPriority;
                }
            }

            public DateTime StartTime
            {
                get
                {
                    return mStartTime;
                }
            }

            public LoadSceneCallbacks LoadSceneCallbacks
            {
                get
                {
                    return mLoadSceneCallbacks;
                }
            }

            public object UserData
            {
                get
                {
                    return mUserData;
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct UnloadSceneInfo
        {
            private readonly AsyncOperation mAsyncOperation;
            private readonly string mSceneAssetName;
            private readonly UnloadSceneCallbacks mUnloadSceneCallbacks;
            private readonly object mUserData;

            public UnloadSceneInfo(AsyncOperation asyncOperation, string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData)
            {
                mAsyncOperation = asyncOperation;
                mSceneAssetName = sceneAssetName;
                mUnloadSceneCallbacks = unloadSceneCallbacks;
                mUserData = userData;
            }

            public AsyncOperation AsyncOperation
            {
                get
                {
                    return mAsyncOperation;
                }
            }

            public string SceneAssetName
            {
                get
                {
                    return mSceneAssetName;
                }
            }

            public UnloadSceneCallbacks UnloadSceneCallbacks
            {
                get
                {
                    return mUnloadSceneCallbacks;
                }
            }

            public object UserData
            {
                get
                {
                    return mUserData;
                }
            }
        }
    }
}
