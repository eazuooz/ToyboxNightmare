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
    public sealed class ResourceUpdateStartEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceUpdateStartEventArgs).GetHashCode();

        public ResourceUpdateStartEventArgs()
        {
            Name = null;
            DownloadPath = null;
            DownloadUri = null;
            CurrentLength = 0;
            CompressedLength = 0;
            RetryCount = 0;
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

        public int CurrentLength
        {
            get;
            private set;
        }

        public int CompressedLength
        {
            get;
            private set;
        }

        public int RetryCount
        {
            get;
            private set;
        }

        public static ResourceUpdateStartEventArgs Create(GameFramework.Resource.ResourceUpdateStartEventArgs e)
        {
            ResourceUpdateStartEventArgs resourceUpdateStartEventArgs = ReferencePool.Acquire<ResourceUpdateStartEventArgs>();
            resourceUpdateStartEventArgs.Name = e.Name;
            resourceUpdateStartEventArgs.DownloadPath = e.DownloadPath;
            resourceUpdateStartEventArgs.DownloadUri = e.DownloadUri;
            resourceUpdateStartEventArgs.CurrentLength = e.CurrentLength;
            resourceUpdateStartEventArgs.CompressedLength = e.CompressedLength;
            resourceUpdateStartEventArgs.RetryCount = e.RetryCount;
            return resourceUpdateStartEventArgs;
        }

        public override void Clear()
        {
            Name = null;
            DownloadPath = null;
            DownloadUri = null;
            CurrentLength = 0;
            CompressedLength = 0;
            RetryCount = 0;
        }
    }
}
