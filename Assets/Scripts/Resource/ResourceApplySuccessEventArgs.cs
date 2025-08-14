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
    public sealed class ResourceApplySuccessEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResourceApplySuccessEventArgs).GetHashCode();

        public ResourceApplySuccessEventArgs()
        {
            Name = null;
            ApplyPath = null;
            ResourcePackPath = null;
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

        public string ApplyPath
        {
            get;
            private set;
        }

        public string ResourcePackPath
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

        public static ResourceApplySuccessEventArgs Create(GameFramework.Resource.ResourceApplySuccessEventArgs e)
        {
            ResourceApplySuccessEventArgs resourceApplySuccessEventArgs = ReferencePool.Acquire<ResourceApplySuccessEventArgs>();
            resourceApplySuccessEventArgs.Name = e.Name;
            resourceApplySuccessEventArgs.ApplyPath = e.ApplyPath;
            resourceApplySuccessEventArgs.ResourcePackPath = e.ResourcePackPath;
            resourceApplySuccessEventArgs.Length = e.Length;
            resourceApplySuccessEventArgs.CompressedLength = e.CompressedLength;
            return resourceApplySuccessEventArgs;
        }

        public override void Clear()
        {
            Name = null;
            ApplyPath = null;
            ResourcePackPath = null;
            Length = 0;
            CompressedLength = 0;
        }
    }
}
