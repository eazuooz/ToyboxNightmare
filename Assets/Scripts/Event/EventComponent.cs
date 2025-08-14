//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Event;
using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Event")]
    public sealed class EventComponent : GameFrameworkComponent
    {
        private IEventManager m_EventManager = null;

        public int EventHandlerCount
        {
            get
            {
                return m_EventManager.EventHandlerCount;
            }
        }

        public int EventCount
        {
            get
            {
                return m_EventManager.EventCount;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_EventManager = GameFrameworkEntry.GetModule<IEventManager>();
            if (m_EventManager == null)
            {
                Log.Fatal("Event manager is invalid.");
                return;
            }
        }

        private void Start()
        {
        }

        public int Count(int id)
        {
            return m_EventManager.Count(id);
        }

        public bool Check(int id, EventHandler<GameEventArgs> handler)
        {
            return m_EventManager.Check(id, handler);
        }

        public void Subscribe(int id, EventHandler<GameEventArgs> handler)
        {
            m_EventManager.Subscribe(id, handler);
        }

        public void Unsubscribe(int id, EventHandler<GameEventArgs> handler)
        {
            m_EventManager.Unsubscribe(id, handler);
        }

        public void SetDefaultHandler(EventHandler<GameEventArgs> handler)
        {
            m_EventManager.SetDefaultHandler(handler);
        }

        public void Fire(object sender, GameEventArgs e)
        {
            m_EventManager.Fire(sender, e);
        }

        public void FireNow(object sender, GameEventArgs e)
        {
            m_EventManager.FireNow(sender, e);
        }
    }
}
