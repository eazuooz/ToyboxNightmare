//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Event;

namespace UnityGameFramework.Runtime
{
    public sealed class ResourceUpdateSuccessEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceUpdateSuccessEventArgs).GetHashCode();

        public ResourceUpdateSuccessEventArgs()
        {
            Name = null;
            DownloadPath = null;
            DownloadUri = null;
            Length = 0;
            CompressedLength = 0;
        }

        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        public string Name
        {
            get;
            private set;
        }

        public string DownloadPath
        {
            get;
            private set;
        }

        public string DownloadUri
        {
            get;
            private set;
        }

        public int Length
        {
            get;
            private set;
        }

        public int CompressedLength
        {
            get;
            private set;
        }

        public static ResourceUpdateSuccessEventArgs Create(GameFramework.Resource.ResourceUpdateSuccessEventArgs e)
        {
            ResourceUpdateSuccessEventArgs resourceUpdateSuccessEventArgs = ReferencePool.Acquire<ResourceUpdateSuccessEventArgs>();
            resourceUpdateSuccessEventArgs.Name = e.Name;
            resourceUpdateSuccessEventArgs.DownloadPath = e.DownloadPath;
            resourceUpdateSuccessEventArgs.DownloadUri = e.DownloadUri;
            resourceUpdateSuccessEventArgs.Length = e.Length;
            resourceUpdateSuccessEventArgs.CompressedLength = e.CompressedLength;
            return resourceUpdateSuccessEventArgs;
        }

        public override void Clear()
        {
            Name = null;
            DownloadPath = null;
            DownloadUri = null;
            Length = 0;
            CompressedLength = 0;
        }
    }
}
