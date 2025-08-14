//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework.Resource;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public abstract class ResourceHelperBase : MonoBehaviour, IResourceHelper
    {
        public abstract void LoadBytes(string fileUri, LoadBytesCallbacks loadBytesCallbacks, object userData);

        public abstract void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData);

        public abstract void Release(object objectToRelease);
    }
}
