//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Network;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Network")]
    public sealed class NetworkComponent : GameFrameworkComponent
    {
        private INetworkManager m_NetworkManager = null;
        private EventComponent m_EventComponent = null;

        public int NetworkChannelCount
        {
            get
            {
                return m_NetworkManager.NetworkChannelCount;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_NetworkManager = GameFrameworkEntry.GetModule<INetworkManager>();
            if (m_NetworkManager == null)
            {
                Log.Fatal("Network manager is invalid.");
                return;
            }

            m_NetworkManager.NetworkConnected += OnNetworkConnected;
            m_NetworkManager.NetworkClosed += OnNetworkClosed;
            m_NetworkManager.NetworkMissHeartBeat += OnNetworkMissHeartBeat;
            m_NetworkManager.NetworkError += OnNetworkError;
            m_NetworkManager.NetworkCustomError += OnNetworkCustomError;
        }

        private void Start()
        {
            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }
        }

        public bool HasNetworkChannel(string name)
        {
            return m_NetworkManager.HasNetworkChannel(name);
        }

        public INetworkChannel GetNetworkChannel(string name)
        {
            return m_NetworkManager.GetNetworkChannel(name);
        }

        public INetworkChannel[] GetAllNetworkChannels()
        {
            return m_NetworkManager.GetAllNetworkChannels();
        }

        public void GetAllNetworkChannels(List<INetworkChannel> results)
        {
            m_NetworkManager.GetAllNetworkChannels(results);
        }

        public INetworkChannel CreateNetworkChannel(string name, ServiceType serviceType, INetworkChannelHelper networkChannelHelper)
        {
            return m_NetworkManager.CreateNetworkChannel(name, serviceType, networkChannelHelper);
        }

        public bool DestroyNetworkChannel(string name)
        {
            return m_NetworkManager.DestroyNetworkChannel(name);
        }

        private void OnNetworkConnected(object sender, GameFramework.Network.NetworkConnectedEventArgs e)
        {
            m_EventComponent.Fire(this, NetworkConnectedEventArgs.Create(e));
        }

        private void OnNetworkClosed(object sender, GameFramework.Network.NetworkClosedEventArgs e)
        {
            m_EventComponent.Fire(this, NetworkClosedEventArgs.Create(e));
        }

        private void OnNetworkMissHeartBeat(object sender, GameFramework.Network.NetworkMissHeartBeatEventArgs e)
        {
            m_EventComponent.Fire(this, NetworkMissHeartBeatEventArgs.Create(e));
        }

        private void OnNetworkError(object sender, GameFramework.Network.NetworkErrorEventArgs e)
        {
            m_EventComponent.Fire(this, NetworkErrorEventArgs.Create(e));
        }

        private void OnNetworkCustomError(object sender, GameFramework.Network.NetworkCustomErrorEventArgs e)
        {
            m_EventComponent.Fire(this, NetworkCustomErrorEventArgs.Create(e));
        }
    }
}
