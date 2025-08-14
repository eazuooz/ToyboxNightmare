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
#if UNITY_EDITOR

        public static bool UpdatableVersionListSerializeCallback_V0(Stream stream, UpdatableVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(s_CachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(s_CachedHashBytes);
                binaryWriter.WriteEncryptedString(versionList.ApplicableGameVersion, s_CachedHashBytes);
                binaryWriter.Write(versionList.InternalResourceVersion);
                UpdatableVersionList.Asset[] assets = versionList.GetAssets();
                binaryWriter.Write(assets.Length);
                UpdatableVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write(resources.Length);
                foreach (UpdatableVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, s_CachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                    binaryWriter.Write(resource.CompressedLength);
                    binaryWriter.Write(resource.CompressedHashCode);
                    int[] assetIndexes = resource.GetAssetIndexes();
                    binaryWriter.Write(assetIndexes.Length);
                    byte[] hashBytes = new byte[CachedHashBytesLength];
                    foreach (int assetIndex in assetIndexes)
                    {
                        Utility.Converter.GetBytes(resource.HashCode, hashBytes);
                        UpdatableVersionList.Asset asset = assets[assetIndex];
                        binaryWriter.WriteEncryptedString(asset.Name, hashBytes);
                        int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                        binaryWriter.Write(dependencyAssetIndexes.Length);
                        foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                        {
                            binaryWriter.WriteEncryptedString(assets[dependencyAssetIndex].Name, hashBytes);
                        }
                    }
                }

                UpdatableVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                binaryWriter.Write(resourceGroups.Length);
                foreach (UpdatableVersionList.ResourceGroup resourceGroup in resourceGroups)
                {
                    binaryWriter.WriteEncryptedString(resourceGroup.Name, s_CachedHashBytes);
                    int[] resourceIndexes = resourceGroup.GetResourceIndexes();
                    binaryWriter.Write(resourceIndexes.Length);
                    foreach (ushort resourceIndex in resourceIndexes)
                    {
                        binaryWriter.Write(resourceIndex);
                    }
                }
            }

            Array.Clear(s_CachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }

        public static bool UpdatableVersionListSerializeCallback_V1(Stream stream, UpdatableVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(s_CachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(s_CachedHashBytes);
                binaryWriter.WriteEncryptedString(versionList.ApplicableGameVersion, s_CachedHashBytes);
                binaryWriter.Write7BitEncodedInt32(versionList.InternalResourceVersion);
                UpdatableVersionList.Asset[] assets = versionList.GetAssets();
                binaryWriter.Write7BitEncodedInt32(assets.Length);
                foreach (UpdatableVersionList.Asset asset in assets)
                {
                    binaryWriter.WriteEncryptedString(asset.Name, s_CachedHashBytes);
                    int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(dependencyAssetIndexes.Length);
                    foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(dependencyAssetIndex);
                    }
                }

                UpdatableVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write7BitEncodedInt32(resources.Length);
                foreach (UpdatableVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Extension != DefaultExtension ? resource.Extension : null, s_CachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write7BitEncodedInt32(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                    binaryWriter.Write7BitEncodedInt32(resource.CompressedLength);
                    binaryWriter.Write(resource.CompressedHashCode);
                    int[] assetIndexes = resource.GetAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(assetIndexes.Length);
                    foreach (int assetIndex in assetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(assetIndex);
                    }
                }

                UpdatableVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                binaryWriter.Write7BitEncodedInt32(resourceGroups.Length);
                foreach (UpdatableVersionList.ResourceGroup resourceGroup in resourceGroups)
                {
                    binaryWriter.WriteEncryptedString(resourceGroup.Name, s_CachedHashBytes);
                    int[] resourceIndexes = resourceGroup.GetResourceIndexes();
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

        public static bool UpdatableVersionListSerializeCallback_V2(Stream stream, UpdatableVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(s_CachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(s_CachedHashBytes);
                binaryWriter.WriteEncryptedString(versionList.ApplicableGameVersion, s_CachedHashBytes);
                binaryWriter.Write7BitEncodedInt32(versionList.InternalResourceVersion);
                UpdatableVersionList.Asset[] assets = versionList.GetAssets();
                binaryWriter.Write7BitEncodedInt32(assets.Length);
                foreach (UpdatableVersionList.Asset asset in assets)
                {
                    binaryWriter.WriteEncryptedString(asset.Name, s_CachedHashBytes);
                    int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(dependencyAssetIndexes.Length);
                    foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(dependencyAssetIndex);
                    }
                }

                UpdatableVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write7BitEncodedInt32(resources.Length);
                foreach (UpdatableVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Extension != DefaultExtension ? resource.Extension : null, s_CachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write7BitEncodedInt32(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                    binaryWriter.Write7BitEncodedInt32(resource.CompressedLength);
                    binaryWriter.Write(resource.CompressedHashCode);
                    int[] assetIndexes = resource.GetAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(assetIndexes.Length);
                    foreach (int assetIndex in assetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(assetIndex);
                    }
                }

                UpdatableVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();
                binaryWriter.Write7BitEncodedInt32(fileSystems.Length);
                foreach (UpdatableVersionList.FileSystem fileSystem in fileSystems)
                {
                    binaryWriter.WriteEncryptedString(fileSystem.Name, s_CachedHashBytes);
                    int[] resourceIndexes = fileSystem.GetResourceIndexes();
                    binaryWriter.Write7BitEncodedInt32(resourceIndexes.Length);
                    foreach (int resourceIndex in resourceIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(resourceIndex);
                    }
                }

                UpdatableVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                binaryWriter.Write7BitEncodedInt32(resourceGroups.Length);
                foreach (UpdatableVersionList.ResourceGroup resourceGroup in resourceGroups)
                {
                    binaryWriter.WriteEncryptedString(resourceGroup.Name, s_CachedHashBytes);
                    int[] resourceIndexes = resourceGroup.GetResourceIndexes();
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

#endif
    }
}
