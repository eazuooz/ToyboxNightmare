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
    public sealed class ResourceVerifyFailureEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceVerifyFailureEventArgs).GetHashCode();

        public ResourceVerifyFailureEventArgs()
        {
            Name = null;
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

        public static ResourceVerifyFailureEventArgs Create(GameFramework.Resource.ResourceVerifyFailureEventArgs e)
        {
            ResourceVerifyFailureEventArgs resourceVerifyFailureEventArgs = ReferencePool.Acquire<ResourceVerifyFailureEventArgs>();
            resourceVerifyFailureEventArgs.Name = e.Name;
            return resourceVerifyFailureEventArgs;
        }

        public override void Clear()
        {
            Name = null;
        }
    }
}
