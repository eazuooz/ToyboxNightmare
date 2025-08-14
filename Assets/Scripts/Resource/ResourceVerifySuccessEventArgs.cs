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
    public sealed class ResourceVerifySuccessEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceVerifySuccessEventArgs).GetHashCode();

        public ResourceVerifySuccessEventArgs()
        {
            Name = null;
            Length = 0;
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

        public int Length
        {
            get;
            private set;
        }

        public static ResourceVerifySuccessEventArgs Create(GameFramework.Resource.ResourceVerifySuccessEventArgs e)
        {
            ResourceVerifySuccessEventArgs resourceVerifySuccessEventArgs = ReferencePool.Acquire<ResourceVerifySuccessEventArgs>();
            resourceVerifySuccessEventArgs.Name = e.Name;
            resourceVerifySuccessEventArgs.Length = e.Length;
            return resourceVerifySuccessEventArgs;
        }

        public override void Clear()
        {
            Name = null;
            Length = 0;
        }
    }
}
