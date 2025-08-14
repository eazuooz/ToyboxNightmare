//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Resource;
using System;
using System.IO;
using System.Text;

namespace UnityGameFramework.Runtime
{
    public static partial class BuiltinVersionListSerializer
    {
        public static bool LocalVersionListSerializeCallback_V0(Stream stream, LocalVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(s_CachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(s_CachedHashBytes);
                LocalVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write(resources.Length);
                foreach (LocalVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, s_CachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                }
            }

            Array.Clear(s_CachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }

        public static bool LocalVersionListSerializeCallback_V1(Stream stream, LocalVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(s_CachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(s_CachedHashBytes);
                LocalVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write7BitEncodedInt32(resources.Length);
                foreach (LocalVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Extension != DefaultExtension ? resource.Extension : null, s_CachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write7BitEncodedInt32(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                }
            }

            Array.Clear(s_CachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }

        public static bool LocalVersionListSerializeCallback_V2(Stream stream, LocalVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(s_CachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(s_CachedHashBytes);
                LocalVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write7BitEncodedInt32(resources.Length);
                foreach (LocalVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Extension != DefaultExtension ? resource.Extension : null, s_CachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write7BitEncodedInt32(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                }

                LocalVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();
                binaryWriter.Write7BitEncodedInt32(fileSystems.Length);
                foreach (LocalVersionList.FileSystem fileSystem in fileSystems)
                {
                    binaryWriter.WriteEncryptedString(fileSystem.Name, s_CachedHashBytes);
                    int[] resourceIndexes = fileSystem.GetResourceIndexes();
                    binaryWriter.Write7BitEncodedInt32(resourceIndexes.Length);
                    foreach (int resourceIndex in resourceIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(resourceIndex);
                    }
                }
            }

            Array.Clear(s_CachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }
    }
}
