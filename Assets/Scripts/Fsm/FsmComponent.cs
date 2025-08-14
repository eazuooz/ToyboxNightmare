//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Fsm;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/FSM")]
    public sealed class FsmComponent : GameFrameworkComponent
    {
        private IFsmManager m_FsmManager = null;

        public int Count
        {
            get
            {
                return m_FsmManager.Count;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_FsmManager = GameFrameworkEntry.GetModule<IFsmManager>();
            if (m_FsmManager == null)
            {
                Log.Fatal("FSM manager is invalid.");
                return;
            }
        }

        private void Start()
        {
        }

        public bool HasFsm<T>() where T : class
        {
            return m_FsmManager.HasFsm<T>();
        }

        public bool HasFsm(Type ownerType)
        {
            return m_FsmManager.HasFsm(ownerType);
        }

        public bool HasFsm<T>(string name) where T : class
        {
            return m_FsmManager.HasFsm<T>(name);
        }

        public bool HasFsm(Type ownerType, string name)
        {
            return m_FsmManager.HasFsm(ownerType, name);
        }

        public IFsm<T> GetFsm<T>() where T : class
        {
            return m_FsmManager.GetFsm<T>();
        }

        public FsmBase GetFsm(Type ownerType)
        {
            return m_FsmManager.GetFsm(ownerType);
        }

        public IFsm<T> GetFsm<T>(string name) where T : class
        {
            return m_FsmManager.GetFsm<T>(name);
        }

        public FsmBase GetFsm(Type ownerType, string name)
        {
            return m_FsmManager.GetFsm(ownerType, name);
        }

        public FsmBase[] GetAllFsms()
        {
            return m_FsmManager.GetAllFsms();
        }

        public void GetAllFsms(List<FsmBase> results)
        {
            m_FsmManager.GetAllFsms(results);
        }

        public IFsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states) where T : class
        {
            return m_FsmManager.CreateFsm(owner, states);
        }

        public IFsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class
        {
            return m_FsmManager.CreateFsm(name, owner, states);
        }

        public IFsm<T> CreateFsm<T>(T owner, List<FsmState<T>> states) where T : class
        {
            return m_FsmManager.CreateFsm(owner, states);
        }

        public IFsm<T> CreateFsm<T>(string name, T owner, List<FsmState<T>> states) where T : class
        {
            return m_FsmManager.CreateFsm(name, owner, states);
        }

        public bool DestroyFsm<T>() where T : class
        {
            return m_FsmManager.DestroyFsm<T>();
        }

        public bool DestroyFsm(Type ownerType)
        {
            return m_FsmManager.DestroyFsm(ownerType);
        }

        public bool DestroyFsm<T>(string name) where T : class
        {
            return m_FsmManager.DestroyFsm<T>(name);
        }

        public bool DestroyFsm(Type ownerType, string name)
        {
            return m_FsmManager.DestroyFsm(ownerType, name);
        }

        public bool DestroyFsm<T>(IFsm<T> fsm) where T : class
        {
            return m_FsmManager.DestroyFsm(fsm);
        }

        public bool DestroyFsm(FsmBase fsm)
        {
            return m_FsmManager.DestroyFsm(fsm);
        }
    }
}
