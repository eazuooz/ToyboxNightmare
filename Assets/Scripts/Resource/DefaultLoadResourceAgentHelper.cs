//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.FileSystem;
using GameFramework.Resource;
using System;
using UnityEngine;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#endif
using UnityEngine.SceneManagement;
using Utility = GameFramework.Utility;

namespace UnityGameFramework.Runtime
{
    public class DefaultLoadResourceAgentHelper : LoadResourceAgentHelperBase, IDisposable
    {
        private string mFileFullPath = null;
        private string mFileName = null;
        private string mBytesFullPath = null;
        private string mAssetName = null;
        private float mLastProgress = 0f;
        private bool mDisposed = false;
#if UNITY_5_4_OR_NEWER
        private UnityWebRequest mUnityWebRequest = null;
#else
        private WWW mWWW = null;
#endif
        private AssetBundleCreateRequest mFileAssetBundleCreateRequest = null;
        private AssetBundleCreateRequest mBytesAssetBundleCreateRequest = null;
        private AssetBundleRequest mAssetBundleRequest = null;
        private AsyncOperation mAsyncOperation = null;

        private EventHandler<LoadResourceAgentHelperUpdateEventArgs> mLoadResourceAgentHelperUpdateEventHandler = null;
        private EventHandler<LoadResourceAgentHelperReadFileCompleteEventArgs> mLoadResourceAgentHelperReadFileCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperReadBytesCompleteEventArgs> mLoadResourceAgentHelperReadBytesCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperParseBytesCompleteEventArgs> mLoadResourceAgentHelperParseBytesCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperLoadCompleteEventArgs> mLoadResourceAgentHelperLoadCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperErrorEventArgs> mLoadResourceAgentHelperErrorEventHandler = null;

        public override event EventHandler<LoadResourceAgentHelperUpdateEventArgs> LoadResourceAgentHelperUpdate
        {
            add
            {
                mLoadResourceAgentHelperUpdateEventHandler += value;
            }
            remove
            {
                mLoadResourceAgentHelperUpdateEventHandler -= value;
            }
        }

        public override event EventHandler<LoadResourceAgentHelperReadFileCompleteEventArgs> LoadResourceAgentHelperReadFileComplete
        {
            add
            {
                mLoadResourceAgentHelperReadFileCompleteEventHandler += value;
            }
            remove
            {
                mLoadResourceAgentHelperReadFileCompleteEventHandler -= value;
            }
        }

        public override event EventHandler<LoadResourceAgentHelperReadBytesCompleteEventArgs> LoadResourceAgentHelperReadBytesComplete
        {
            add
            {
                mLoadResourceAgentHelperReadBytesCompleteEventHandler += value;
            }
            remove
            {
                mLoadResourceAgentHelperReadBytesCompleteEventHandler -= value;
            }
        }

        public override event EventHandler<LoadResourceAgentHelperParseBytesCompleteEventArgs> LoadResourceAgentHelperParseBytesComplete
        {
            add
            {
                mLoadResourceAgentHelperParseBytesCompleteEventHandler += value;
            }
            remove
            {
                mLoadResourceAgentHelperParseBytesCompleteEventHandler -= value;
            }
        }

        public override event EventHandler<LoadResourceAgentHelperLoadCompleteEventArgs> LoadResourceAgentHelperLoadComplete
        {
            add
            {
                mLoadResourceAgentHelperLoadCompleteEventHandler += value;
            }
            remove
            {
                mLoadResourceAgentHelperLoadCompleteEventHandler -= value;
            }
        }

        public override event EventHandler<LoadResourceAgentHelperErrorEventArgs> LoadResourceAgentHelperError
        {
            add
            {
                mLoadResourceAgentHelperErrorEventHandler += value;
            }
            remove
            {
                mLoadResourceAgentHelperErrorEventHandler -= value;
            }
        }

        public override void ReadFile(string fullPath)
        {
            if (mLoadResourceAgentHelperReadFileCompleteEventHandler == null || mLoadResourceAgentHelperUpdateEventHandler == null || mLoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            mFileFullPath = fullPath;
            mFileAssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(fullPath);
        }

        public override void ReadFile(IFileSystem fileSystem, string name)
        {
#if UNITY_5_3_5 || UNITY_5_3_6 || UNITY_5_3_7 || UNITY_5_3_8 || UNITY_5_4_OR_NEWER
            if (mLoadResourceAgentHelperReadFileCompleteEventHandler == null || mLoadResourceAgentHelperUpdateEventHandler == null || mLoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            FileInfo fileInfo = fileSystem.GetFileInfo(name);
            mFileFullPath = fileSystem.FullPath;
            mFileName = name;
            mFileAssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(fileSystem.FullPath, 0u, (ulong)fileInfo.Offset);
#else
            Log.Fatal("Load from file async with offset is not supported, use Unity 5.3.5f1 or above.");
#endif
        }

        public override void ReadBytes(string fullPath)
        {
            if (mLoadResourceAgentHelperReadBytesCompleteEventHandler == null || mLoadResourceAgentHelperUpdateEventHandler == null || mLoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            mBytesFullPath = fullPath;
#if UNITY_5_4_OR_NEWER
            mUnityWebRequest = UnityWebRequest.Get(Utility.Path.GetRemotePath(fullPath));
#if UNITY_2017_2_OR_NEWER
            mUnityWebRequest.SendWebRequest();
#else
            mUnityWebRequest.Send();
#endif
#else
            mWWW = new WWW(Utility.Path.GetRemotePath(fullPath));
#endif
        }

        public override void ReadBytes(IFileSystem fileSystem, string name)
        {
            if (mLoadResourceAgentHelperReadBytesCompleteEventHandler == null || mLoadResourceAgentHelperUpdateEventHandler == null || mLoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            byte[] bytes = fileSystem.ReadFile(name);
            LoadResourceAgentHelperReadBytesCompleteEventArgs loadResourceAgentHelperReadBytesCompleteEventArgs = LoadResourceAgentHelperReadBytesCompleteEventArgs.Create(bytes);
            mLoadResourceAgentHelperReadBytesCompleteEventHandler(this, loadResourceAgentHelperReadBytesCompleteEventArgs);
            ReferencePool.Release(loadResourceAgentHelperReadBytesCompleteEventArgs);
        }

        public override void ParseBytes(byte[] bytes)
        {
            if (mLoadResourceAgentHelperParseBytesCompleteEventHandler == null || mLoadResourceAgentHelperUpdateEventHandler == null || mLoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            mBytesAssetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(bytes);
        }

        public override void LoadAsset(object resource, string assetName, Type assetType, bool isScene)
        {
            if (mLoadResourceAgentHelperLoadCompleteEventHandler == null || mLoadResourceAgentHelperUpdateEventHandler == null || mLoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            AssetBundle assetBundle = resource as AssetBundle;
            if (assetBundle == null)
            {
                LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.TypeError, "Can not load asset bundle from loaded resource which is not an asset bundle.");
                mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                return;
            }

            if (string.IsNullOrEmpty(assetName))
            {
                LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, "Can not load asset from asset bundle which child name is invalid.");
                mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                return;
            }

            mAssetName = assetName;
            if (isScene)
            {
                int sceneNamePositionStart = assetName.LastIndexOf('/');
                int sceneNamePositionEnd = assetName.LastIndexOf('.');
                if (sceneNamePositionStart <= 0 || sceneNamePositionEnd <= 0 || sceneNamePositionStart > sceneNamePositionEnd)
                {
                    LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, Utility.Text.Format("Scene name '{0}' is invalid.", assetName));
                    mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    return;
                }

                string sceneName = assetName.Substring(sceneNamePositionStart + 1, sceneNamePositionEnd - sceneNamePositionStart - 1);
                mAsyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            else
            {
                if (assetType != null)
                {
                    mAssetBundleRequest = assetBundle.LoadAssetAsync(assetName, assetType);
                }
                else
                {
                    mAssetBundleRequest = assetBundle.LoadAssetAsync(assetName);
                }
            }
        }

        public override void Reset()
        {
            mFileFullPath = null;
            mFileName = null;
            mBytesFullPath = null;
            mAssetName = null;
            mLastProgress = 0f;

#if UNITY_5_4_OR_NEWER
            if (mUnityWebRequest != null)
            {
                mUnityWebRequest.Dispose();
                mUnityWebRequest = null;
            }
#else
            if (mWWW != null)
            {
                mWWW.Dispose();
                mWWW = null;
            }
#endif

            mFileAssetBundleCreateRequest = null;
            mBytesAssetBundleCreateRequest = null;
            mAssetBundleRequest = null;
            mAsyncOperation = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (mDisposed)
            {
                return;
            }

            if (disposing)
            {
#if UNITY_5_4_OR_NEWER
                if (mUnityWebRequest != null)
                {
                    mUnityWebRequest.Dispose();
                    mUnityWebRequest = null;
                }
#else
                if (mWWW != null)
                {
                    mWWW.Dispose();
                    mWWW = null;
                }
#endif
            }

            mDisposed = true;
        }

        private void Update()
        {
#if UNITY_5_4_OR_NEWER
            UpdateUnityWebRequest();
#else
            UpdateWWW();
#endif
            UpdateFileAssetBundleCreateRequest();
            UpdateBytesAssetBundleCreateRequest();
            UpdateAssetBundleRequest();
            UpdateAsyncOperation();
        }

#if UNITY_5_4_OR_NEWER
        private void UpdateUnityWebRequest()
        {
            if (mUnityWebRequest != null)
            {
                if (mUnityWebRequest.isDone)
                {
                    if (string.IsNullOrEmpty(mUnityWebRequest.error))
                    {
                        LoadResourceAgentHelperReadBytesCompleteEventArgs loadResourceAgentHelperReadBytesCompleteEventArgs = LoadResourceAgentHelperReadBytesCompleteEventArgs.Create(mUnityWebRequest.downloadHandler.data);
                        mLoadResourceAgentHelperReadBytesCompleteEventHandler(this, loadResourceAgentHelperReadBytesCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperReadBytesCompleteEventArgs);
                        mUnityWebRequest.Dispose();
                        mUnityWebRequest = null;
                        mBytesFullPath = null;
                        mLastProgress = 0f;
                    }
                    else
                    {
                        bool isError = false;
#if UNITY_2020_2_OR_NEWER
                        isError = mUnityWebRequest.result != UnityWebRequest.Result.Success;
#elif UNITY_2017_1_OR_NEWER
                        isError = mUnityWebRequest.isNetworkError || mUnityWebRequest.isHttpError;
#else
                        isError = mUnityWebRequest.isError;
#endif
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, Utility.Text.Format("Can not load asset bundle '{0}' with error message '{1}'.", mBytesFullPath, isError ? mUnityWebRequest.error : null));
                        mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (mUnityWebRequest.downloadProgress != mLastProgress)
                {
                    mLastProgress = mUnityWebRequest.downloadProgress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.ReadResource, mUnityWebRequest.downloadProgress);
                    mLoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }
#else
        private void UpdateWWW()
        {
            if (mWWW != null)
            {
                if (mWWW.isDone)
                {
                    if (string.IsNullOrEmpty(mWWW.error))
                    {
                        LoadResourceAgentHelperReadBytesCompleteEventArgs loadResourceAgentHelperReadBytesCompleteEventArgs = LoadResourceAgentHelperReadBytesCompleteEventArgs.Create(mWWW.bytes);
                        mLoadResourceAgentHelperReadBytesCompleteEventHandler(this, loadResourceAgentHelperReadBytesCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperReadBytesCompleteEventArgs);
                        mWWW.Dispose();
                        mWWW = null;
                        mBytesFullPath = null;
                        mLastProgress = 0f;
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, Utility.Text.Format("Can not load asset bundle '{0}' with error message '{1}'.", mBytesFullPath, mWWW.error));
                        mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (mWWW.progress != mLastProgress)
                {
                    mLastProgress = mWWW.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.ReadResource, mWWW.progress);
                    mLoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }
#endif

        private void UpdateFileAssetBundleCreateRequest()
        {
            if (mFileAssetBundleCreateRequest != null)
            {
                if (mFileAssetBundleCreateRequest.isDone)
                {
                    AssetBundle assetBundle = mFileAssetBundleCreateRequest.assetBundle;
                    if (assetBundle != null)
                    {
                        AssetBundleCreateRequest oldFileAssetBundleCreateRequest = mFileAssetBundleCreateRequest;
                        LoadResourceAgentHelperReadFileCompleteEventArgs loadResourceAgentHelperReadFileCompleteEventArgs = LoadResourceAgentHelperReadFileCompleteEventArgs.Create(assetBundle);
                        mLoadResourceAgentHelperReadFileCompleteEventHandler(this, loadResourceAgentHelperReadFileCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperReadFileCompleteEventArgs);
                        if (mFileAssetBundleCreateRequest == oldFileAssetBundleCreateRequest)
                        {
                            mFileAssetBundleCreateRequest = null;
                            mLastProgress = 0f;
                        }
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, Utility.Text.Format("Can not load asset bundle from file '{0}' which is not a valid asset bundle.", mFileName == null ? mFileFullPath : Utility.Text.Format("{0} | {1}", mFileFullPath, mFileName)));
                        mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (mFileAssetBundleCreateRequest.progress != mLastProgress)
                {
                    mLastProgress = mFileAssetBundleCreateRequest.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadResource, mFileAssetBundleCreateRequest.progress);
                    mLoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }

        private void UpdateBytesAssetBundleCreateRequest()
        {
            if (mBytesAssetBundleCreateRequest != null)
            {
                if (mBytesAssetBundleCreateRequest.isDone)
                {
                    AssetBundle assetBundle = mBytesAssetBundleCreateRequest.assetBundle;
                    if (assetBundle != null)
                    {
                        AssetBundleCreateRequest oldBytesAssetBundleCreateRequest = mBytesAssetBundleCreateRequest;
                        LoadResourceAgentHelperParseBytesCompleteEventArgs loadResourceAgentHelperParseBytesCompleteEventArgs = LoadResourceAgentHelperParseBytesCompleteEventArgs.Create(assetBundle);
                        mLoadResourceAgentHelperParseBytesCompleteEventHandler(this, loadResourceAgentHelperParseBytesCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperParseBytesCompleteEventArgs);
                        if (mBytesAssetBundleCreateRequest == oldBytesAssetBundleCreateRequest)
                        {
                            mBytesAssetBundleCreateRequest = null;
                            mLastProgress = 0f;
                        }
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, "Can not load asset bundle from memory which is not a valid asset bundle.");
                        mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (mBytesAssetBundleCreateRequest.progress != mLastProgress)
                {
                    mLastProgress = mBytesAssetBundleCreateRequest.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadResource, mBytesAssetBundleCreateRequest.progress);
                    mLoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }

        private void UpdateAssetBundleRequest()
        {
            if (mAssetBundleRequest != null)
            {
                if (mAssetBundleRequest.isDone)
                {
                    if (mAssetBundleRequest.asset != null)
                    {
                        LoadResourceAgentHelperLoadCompleteEventArgs loadResourceAgentHelperLoadCompleteEventArgs = LoadResourceAgentHelperLoadCompleteEventArgs.Create(mAssetBundleRequest.asset);
                        mLoadResourceAgentHelperLoadCompleteEventHandler(this, loadResourceAgentHelperLoadCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperLoadCompleteEventArgs);
                        mAssetName = null;
                        mLastProgress = 0f;
                        mAssetBundleRequest = null;
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, Utility.Text.Format("Can not load asset '{0}' from asset bundle which is not exist.", mAssetName));
                        mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (mAssetBundleRequest.progress != mLastProgress)
                {
                    mLastProgress = mAssetBundleRequest.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadAsset, mAssetBundleRequest.progress);
                    mLoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }

        private void UpdateAsyncOperation()
        {
            if (mAsyncOperation != null)
            {
                if (mAsyncOperation.isDone)
                {
                    if (mAsyncOperation.allowSceneActivation)
                    {
                        SceneAsset sceneAsset = new SceneAsset();
                        LoadResourceAgentHelperLoadCompleteEventArgs loadResourceAgentHelperLoadCompleteEventArgs = LoadResourceAgentHelperLoadCompleteEventArgs.Create(sceneAsset);
                        mLoadResourceAgentHelperLoadCompleteEventHandler(this, loadResourceAgentHelperLoadCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperLoadCompleteEventArgs);
                        mAssetName = null;
                        mLastProgress = 0f;
                        mAsyncOperation = null;
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, Utility.Text.Format("Can not load scene asset '{0}' from asset bundle.", mAssetName));
                        mLoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (mAsyncOperation.progress != mLastProgress)
                {
                    mLastProgress = mAsyncOperation.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadScene, mAsyncOperation.progress);
                    mLoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }
    }
}
