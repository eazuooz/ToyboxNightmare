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

        public static bool ResourcePackVersionListSerializeCallback_V0(Stream stream, ResourcePackVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(s_CachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(s_CachedHashBytes);
                binaryWriter.Write(versionList.Offset);
                binaryWriter.Write(versionList.Length);
                binaryWriter.Write(versionList.HashCode);
                ResourcePackVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write7BitEncodedInt32(resources.Length);
                foreach (ResourcePackVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, s_CachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Extension != DefaultExtension ? resource.Extension : null, s_CachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write7BitEncodedInt64(resource.Offset);
                    binaryWriter.Write7BitEncodedInt32(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                    binaryWriter.Write7BitEncodedInt32(resource.CompressedLength);
                    binaryWriter.Write(resource.CompressedHashCode);
                }
            }

            Array.Clear(s_CachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }

#endif
    }
}
