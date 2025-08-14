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
    public sealed class ResourceVerifyStartEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceVerifyStartEventArgs).GetHashCode();

        public ResourceVerifyStartEventArgs()
        {
            Count = 0;
            TotalLength = 0L;
        }

        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        public int Count
        {
            get;
            private set;
        }

        public long TotalLength
        {
            get;
            private set;
        }

        public static ResourceVerifyStartEventArgs Create(GameFramework.Resource.ResourceVerifyStartEventArgs e)
        {
            ResourceVerifyStartEventArgs resourceVerifyStartEventArgs = ReferencePool.Acquire<ResourceVerifyStartEventArgs>();
            resourceVerifyStartEventArgs.Count = e.Count;
            resourceVerifyStartEventArgs.TotalLength = e.TotalLength;
            return resourceVerifyStartEventArgs;
        }

        public override void Clear()
        {
            Count = 0;
            TotalLength = 0L;
        }
    }
}
