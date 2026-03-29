//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------

using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public abstract class GameBase
    {
        public virtual void Initialize()
        {
            GameOver = false;
        }
        public virtual void Shutdown()
        {
    
        }
        public virtual void Update(float elapseSeconds, float realElapseSeconds)
        {

        }
        protected virtual void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
        }

        protected virtual void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            ShowEntityFailureEventArgs ne = (ShowEntityFailureEventArgs)e;
            Log.Warning("Show entity failure with error message '{0}'.", ne.ErrorMessage);
        }
        public abstract GameMode GameMode
        {
            get;
        }
        public bool GameOver
        {
            get;
            protected set;
        }

    }
}