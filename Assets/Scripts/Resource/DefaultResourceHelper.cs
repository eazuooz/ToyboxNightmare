//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework.Resource;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#endif
using UnityEngine.SceneManagement;

namespace UnityGameFramework.Runtime
{
    public class DefaultResourceHelper : ResourceHelperBase
    {
        public override void LoadBytes(string fileUri, LoadBytesCallbacks loadBytesCallbacks, object userData)
        {
            StartCoroutine(LoadBytesCo(fileUri, loadBytesCallbacks, userData));
        }

        public override void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData)
        {
#if UNITY_5_5_OR_NEWER
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(UnloadSceneCo(sceneAssetName, unloadSceneCallbacks, userData));
            }
            else
            {
                SceneManager.UnloadSceneAsync(SceneComponent.GetSceneName(sceneAssetName));
            }
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

        public override void Release(object objectToRelease)
        {
            AssetBundle assetBundle = objectToRelease as AssetBundle;
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
                return;
            }

            /* Unity 当前 Resources.UnloadAsset 在 iOS 设备上会导致一些诡异问题，先不用这部分
            SceneAsset sceneAsset = objectToRelease as SceneAsset;
            if (sceneAsset != null)
            {
                return;
            }

            Object unityObject = objectToRelease as Object;
            if (unityObject == null)
            {
                Log.Warning("Asset is invalid.");
                return;
            }

            if (unityObject is GameObject || unityObject is MonoBehaviour)
            {
                // UnloadAsset may only be used on individual assets and can not be used on GameObject's / Components or AssetBundles.
                return;
            }

            Resources.UnloadAsset(unityObject);
            */
        }

        private void Start()
        {
        }

        private IEnumerator LoadBytesCo(string fileUri, LoadBytesCallbacks loadBytesCallbacks, object userData)
        {
            bool isError = false;
            byte[] bytes = null;
            string errorMessage = null;
            DateTime startTime = DateTime.UtcNow;

#if UNITY_5_4_OR_NEWER
            UnityWebRequest unityWebRequest = UnityWebRequest.Get(fileUri);
#if UNITY_2017_2_OR_NEWER
            yield return unityWebRequest.SendWebRequest();
#else
            yield return unityWebRequest.Send();
#endif

#if UNITY_2020_2_OR_NEWER
            isError = unityWebRequest.result != UnityWebRequest.Result.Success;
#elif UNITY_2017_1_OR_NEWER
            isError = unityWebRequest.isNetworkError || unityWebRequest.isHttpError;
#else
            isError = unityWebRequest.isError;
#endif
            bytes = unityWebRequest.downloadHandler.data;
            errorMessage = isError ? unityWebRequest.error : null;
            unityWebRequest.Dispose();
#else
            WWW www = new WWW(fileUri);
            yield return www;

            isError = !string.IsNullOrEmpty(www.error);
            bytes = www.bytes;
            errorMessage = www.error;
            www.Dispose();
#endif

            if (!isError)
            {
                float elapseSeconds = (float)(DateTime.UtcNow - startTime).TotalSeconds;
                loadBytesCallbacks.LoadBytesSuccessCallback(fileUri, bytes, elapseSeconds, userData);
            }
            else if (loadBytesCallbacks.LoadBytesFailureCallback != null)
            {
                loadBytesCallbacks.LoadBytesFailureCallback(fileUri, errorMessage, userData);
            }
        }

#if UNITY_5_5_OR_NEWER
        private IEnumerator UnloadSceneCo(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData)
        {
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(SceneComponent.GetSceneName(sceneAssetName));
            if (asyncOperation == null)
            {
                yield break;
            }

            yield return asyncOperation;

            if (asyncOperation.allowSceneActivation)
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
        }
#endif
    }
}
