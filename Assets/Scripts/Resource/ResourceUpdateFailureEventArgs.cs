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
    public sealed class ResourceUpdateFailureEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceUpdateFailureEventArgs).GetHashCode();

        public ResourceUpdateFailureEventArgs()
        {
            Name = null;
            DownloadUri = null;
            RetryCount = 0;
            TotalRetryCount = 0;
            ErrorMessage = null;
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

        public string DownloadUri
        {
            get;
            private set;
        }

        public int RetryCount
        {
            get;
            private set;
        }

        public int TotalRetryCount
        {
            get;
            private set;
        }

        public string ErrorMessage
        {
            get;
            private set;
        }

        public static ResourceUpdateFailureEventArgs Create(GameFramework.Resource.ResourceUpdateFailureEventArgs e)
        {
            ResourceUpdateFailureEventArgs resourceUpdateFailureEventArgs = ReferencePool.Acquire<ResourceUpdateFailureEventArgs>();
            resourceUpdateFailureEventArgs.Name = e.Name;
            resourceUpdateFailureEventArgs.DownloadUri = e.DownloadUri;
            resourceUpdateFailureEventArgs.RetryCount = e.RetryCount;
            resourceUpdateFailureEventArgs.TotalRetryCount = e.TotalRetryCount;
            resourceUpdateFailureEventArgs.ErrorMessage = e.ErrorMessage;
            return resourceUpdateFailureEventArgs;
        }

        public override void Clear()
        {
            Name = null;
            DownloadUri = null;
            RetryCount = 0;
            TotalRetryCount = 0;
            ErrorMessage = null;
        }
    }
}
