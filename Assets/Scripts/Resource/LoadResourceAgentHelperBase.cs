//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework.FileSystem;
using GameFramework.Resource;
using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public abstract class LoadResourceAgentHelperBase : MonoBehaviour, ILoadResourceAgentHelper
    {
        public abstract event EventHandler<LoadResourceAgentHelperUpdateEventArgs> LoadResourceAgentHelperUpdate;

        public abstract event EventHandler<LoadResourceAgentHelperReadFileCompleteEventArgs> LoadResourceAgentHelperReadFileComplete;

        public abstract event EventHandler<LoadResourceAgentHelperReadBytesCompleteEventArgs> LoadResourceAgentHelperReadBytesComplete;

        public abstract event EventHandler<LoadResourceAgentHelperParseBytesCompleteEventArgs> LoadResourceAgentHelperParseBytesComplete;

        public abstract event EventHandler<LoadResourceAgentHelperLoadCompleteEventArgs> LoadResourceAgentHelperLoadComplete;

        public abstract event EventHandler<LoadResourceAgentHelperErrorEventArgs> LoadResourceAgentHelperError;

        public abstract void ReadFile(string fullPath);

        public abstract void ReadFile(IFileSystem fileSystem, string name);

        public abstract void ReadBytes(string fullPath);

        public abstract void ReadBytes(IFileSystem fileSystem, string name);

        public abstract void ParseBytes(byte[] bytes);

        public abstract void LoadAsset(object resource, string assetName, Type assetType, bool isScene);

        public abstract void Reset();
    }
}
