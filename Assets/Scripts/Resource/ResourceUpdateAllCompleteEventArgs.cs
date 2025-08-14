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
    public sealed class ResourceUpdateAllCompleteEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceUpdateAllCompleteEventArgs).GetHashCode();

        public ResourceUpdateAllCompleteEventArgs()
        {
        }

        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        public static ResourceUpdateAllCompleteEventArgs Create(GameFramework.Resource.ResourceUpdateAllCompleteEventArgs e)
        {
            return ReferencePool.Acquire<ResourceUpdateAllCompleteEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
