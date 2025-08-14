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
    public sealed class ResourceApplyFailureEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceApplyFailureEventArgs).GetHashCode();

        public ResourceApplyFailureEventArgs()
        {
            Name = null;
            ResourcePackPath = null;
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

        public string ResourcePackPath
        {
            get;
            private set;
        }

        public string ErrorMessage
        {
            get;
            private set;
        }

        public static ResourceApplyFailureEventArgs Create(GameFramework.Resource.ResourceApplyFailureEventArgs e)
        {
            ResourceApplyFailureEventArgs resourceApplyFailureEventArgs = ReferencePool.Acquire<ResourceApplyFailureEventArgs>();
            resourceApplyFailureEventArgs.Name = e.Name;
            resourceApplyFailureEventArgs.ResourcePackPath = e.ResourcePackPath;
            resourceApplyFailureEventArgs.ErrorMessage = e.ErrorMessage;
            return resourceApplyFailureEventArgs;
        }

        public override void Clear()
        {
            Name = null;
            ResourcePackPath = null;
            ErrorMessage = null;
        }
    }
}
