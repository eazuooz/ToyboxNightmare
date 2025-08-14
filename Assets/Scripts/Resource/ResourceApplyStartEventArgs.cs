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
    public sealed class ResourceApplyStartEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceApplyStartEventArgs).GetHashCode();

        public ResourceApplyStartEventArgs()
        {
            ResourcePackPath = null;
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

        public string ResourcePackPath
        {
            get;
            private set;
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

        public static ResourceApplyStartEventArgs Create(GameFramework.Resource.ResourceApplyStartEventArgs e)
        {
            ResourceApplyStartEventArgs resourceApplyStartEventArgs = ReferencePool.Acquire<ResourceApplyStartEventArgs>();
            resourceApplyStartEventArgs.ResourcePackPath = e.ResourcePackPath;
            resourceApplyStartEventArgs.Count = e.Count;
            resourceApplyStartEventArgs.TotalLength = e.TotalLength;
            return resourceApplyStartEventArgs;
        }

        public override void Clear()
        {
            ResourcePackPath = null;
            Count = 0;
            TotalLength = 0L;
        }
    }
}
