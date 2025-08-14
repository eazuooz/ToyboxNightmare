//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.FileSystem;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/File System")]
    public sealed class FileSystemComponent : GameFrameworkComponent
    {
        private IFileSystemManager m_FileSystemManager = null;

        [SerializeField]
        private string m_FileSystemHelperTypeName = "UnityGameFramework.Runtime.DefaultFileSystemHelper";

        [SerializeField]
        private FileSystemHelperBase m_CustomFileSystemHelper = null;

        public int Count
        {
            get
            {
                return m_FileSystemManager.Count;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_FileSystemManager = GameFrameworkEntry.GetModule<IFileSystemManager>();
            if (m_FileSystemManager == null)
            {
                Log.Fatal("File system manager is invalid.");
                return;
            }

            FileSystemHelperBase fileSystemHelper = Helper.CreateHelper(m_FileSystemHelperTypeName, m_CustomFileSystemHelper);
            if (fileSystemHelper == null)
            {
                Log.Error("Can not create fileSystem helper.");
                return;
            }

            fileSystemHelper.name = "FileSystem Helper";
            Transform transform = fileSystemHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            m_FileSystemManager.SetFileSystemHelper(fileSystemHelper);
        }

        private void Start()
        {
        }

        public bool HasFileSystem(string fullPath)
        {
            return m_FileSystemManager.HasFileSystem(fullPath);
        }

        public IFileSystem GetFileSystem(string fullPath)
        {
            return m_FileSystemManager.GetFileSystem(fullPath);
        }

        public IFileSystem CreateFileSystem(string fullPath, FileSystemAccess access, int maxFileCount, int maxBlockCount)
        {
            return m_FileSystemManager.CreateFileSystem(fullPath, access, maxFileCount, maxBlockCount);
        }

        public IFileSystem LoadFileSystem(string fullPath, FileSystemAccess access)
        {
            return m_FileSystemManager.LoadFileSystem(fullPath, access);
        }

        public void DestroyFileSystem(IFileSystem fileSystem, bool deletePhysicalFile)
        {
            m_FileSystemManager.DestroyFileSystem(fileSystem, deletePhysicalFile);
        }

        public IFileSystem[] GetAllFileSystems()
        {
            return m_FileSystemManager.GetAllFileSystems();
        }

        public void GetAllFileSystems(List<IFileSystem> results)
        {
            m_FileSystemManager.GetAllFileSystems(results);
        }
    }
}
