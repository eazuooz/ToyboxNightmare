//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.DataNode;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Data Node")]
    public sealed class DataNodeComponent : GameFrameworkComponent
    {
        private IDataNodeManager m_DataNodeManager = null;

        public IDataNode Root
        {
            get
            {
                return m_DataNodeManager.Root;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_DataNodeManager = GameFrameworkEntry.GetModule<IDataNodeManager>();
            if (m_DataNodeManager == null)
            {
                Log.Fatal("Data node manager is invalid.");
                return;
            }
        }

        private void Start()
        {
        }

        public T GetData<T>(string path) where T : Variable
        {
            return m_DataNodeManager.GetData<T>(path);
        }

        public Variable GetData(string path)
        {
            return m_DataNodeManager.GetData(path);
        }

        public T GetData<T>(string path, IDataNode node) where T : Variable
        {
            return m_DataNodeManager.GetData<T>(path, node);
        }

        public Variable GetData(string path, IDataNode node)
        {
            return m_DataNodeManager.GetData(path, node);
        }

        public void SetData<T>(string path, T data) where T : Variable
        {
            m_DataNodeManager.SetData(path, data);
        }

        public void SetData(string path, Variable data)
        {
            m_DataNodeManager.SetData(path, data);
        }

        public void SetData<T>(string path, T data, IDataNode node) where T : Variable
        {
            m_DataNodeManager.SetData(path, data, node);
        }

        public void SetData(string path, Variable data, IDataNode node)
        {
            m_DataNodeManager.SetData(path, data, node);
        }

        public IDataNode GetNode(string path)
        {
            return m_DataNodeManager.GetNode(path);
        }

        public IDataNode GetNode(string path, IDataNode node)
        {
            return m_DataNodeManager.GetNode(path, node);
        }

        public IDataNode GetOrAddNode(string path)
        {
            return m_DataNodeManager.GetOrAddNode(path);
        }

        public IDataNode GetOrAddNode(string path, IDataNode node)
        {
            return m_DataNodeManager.GetOrAddNode(path, node);
        }

        public void RemoveNode(string path)
        {
            m_DataNodeManager.RemoveNode(path);
        }

        public void RemoveNode(string path, IDataNode node)
        {
            m_DataNodeManager.RemoveNode(path, node);
        }

        public void Clear()
        {
            m_DataNodeManager.Clear();
        }
    }
}
