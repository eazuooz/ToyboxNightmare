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
    public sealed class ResourceUpdateChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceUpdateChangedEventArgs).GetHashCode();

        public ResourceUpdateChangedEventArgs()
        {
            Name = null;
            DownloadPath = null;
            DownloadUri = null;
            CurrentLength = 0;
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

        public static ResourceUpdateChangedEventArgs Create(GameFramework.Resource.ResourceUpdateChangedEventArgs e)
        {
            ResourceUpdateChangedEventArgs resourceUpdateChangedEventArgs = ReferencePool.Acquire<ResourceUpdateChangedEventArgs>();
            resourceUpdateChangedEventArgs.Name = e.Name;
            resourceUpdateChangedEventArgs.DownloadPath = e.DownloadPath;
            resourceUpdateChangedEventArgs.DownloadUri = e.DownloadUri;
            resourceUpdateChangedEventArgs.CurrentLength = e.CurrentLength;
            resourceUpdateChangedEventArgs.CompressedLength = e.CompressedLength;
            return resourceUpdateChangedEventArgs;
        }

        public override void Clear()
        {
            Name = null;
            DownloadPath = null;
            DownloadUri = null;
            CurrentLength = 0;
            CompressedLength = 0;
        }
    }
}
