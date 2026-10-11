//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------

using UnityEditor;

namespace UnityGameFramework.Editor
{
    public abstract class GameFrameworkInspector : UnityEditor.Editor
    {
        private bool mIsCompiling = false;

        public override void OnInspectorGUI()
        {
            if (mIsCompiling && !EditorApplication.isCompiling)
            {
                mIsCompiling = false;
                OnCompileComplete();
            }
            else if (!mIsCompiling && EditorApplication.isCompiling)
            {
                mIsCompiling = true;
                OnCompileStart();
            }
        }

        protected virtual void OnCompileStart()
        {
        }

        protected virtual void OnCompileComplete()
        {
        }

        protected bool IsPrefabInHierarchy(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.Regular;
#else
            return PrefabUtility.GetPrefabType(obj) != PrefabType.Prefab;
#endif
        }
    }
}
