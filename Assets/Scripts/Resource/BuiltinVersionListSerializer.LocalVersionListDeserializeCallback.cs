//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework.Resource;
using System.IO;
using System.Text;

namespace UnityGameFramework.Runtime
{
    public static partial class BuiltinVersionListSerializer
    {
        public static LocalVersionList LocalVersionListDeserializeCallback_V0(Stream stream)
        {
            using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8))
            {
                byte[] encryptBytes = binaryReader.ReadBytes(CachedHashBytesLength);
                int resourceCount = binaryReader.ReadInt32();
                LocalVersionList.Resource[] resources = resourceCount > 0 ? new LocalVersionList.Resource[resourceCount] : null;
                for (int i = 0; i < resourceCount; i++)
                {
                    string name = binaryReader.ReadEncryptedString(encryptBytes);
                    string variant = binaryReader.ReadEncryptedString(encryptBytes);
                    byte loadType = binaryReader.ReadByte();
                    int length = binaryReader.ReadInt32();
                    int hashCode = binaryReader.ReadInt32();
                    resources[i] = new LocalVersionList.Resource(name, variant, null, loadType, length, hashCode);
                }

                return new LocalVersionList(resources, null);
            }
        }

        public static LocalVersionList LocalVersionListDeserializeCallback_V1(Stream stream)
        {
            using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8))
            {
                byte[] encryptBytes = binaryReader.ReadBytes(CachedHashBytesLength);
                int resourceCount = binaryReader.Read7BitEncodedInt32();
                LocalVersionList.Resource[] resources = resourceCount > 0 ? new LocalVersionList.Resource[resourceCount] : null;
                for (int i = 0; i < resourceCount; i++)
                {
                    string name = binaryReader.ReadEncryptedString(encryptBytes);
                    string variant = binaryReader.ReadEncryptedString(encryptBytes);
                    string extension = binaryReader.ReadEncryptedString(encryptBytes) ?? DefaultExtension;
                    byte loadType = binaryReader.ReadByte();
                    int length = binaryReader.Read7BitEncodedInt32();
                    int hashCode = binaryReader.ReadInt32();
                    resources[i] = new LocalVersionList.Resource(name, variant, extension, loadType, length, hashCode);
                }

                return new LocalVersionList(resources, null);
            }
        }

        public static LocalVersionList LocalVersionListDeserializeCallback_V2(Stream stream)
        {
            using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8))
            {
                byte[] encryptBytes = binaryReader.ReadBytes(CachedHashBytesLength);
                int resourceCount = binaryReader.Read7BitEncodedInt32();
                LocalVersionList.Resource[] resources = resourceCount > 0 ? new LocalVersionList.Resource[resourceCount] : null;
                for (int i = 0; i < resourceCount; i++)
                {
                    string name = binaryReader.ReadEncryptedString(encryptBytes);
                    string variant = binaryReader.ReadEncryptedString(encryptBytes);
                    string extension = binaryReader.ReadEncryptedString(encryptBytes) ?? DefaultExtension;
                    byte loadType = binaryReader.ReadByte();
                    int length = binaryReader.Read7BitEncodedInt32();
                    int hashCode = binaryReader.ReadInt32();
                    resources[i] = new LocalVersionList.Resource(name, variant, extension, loadType, length, hashCode);
                }

                int fileSystemCount = binaryReader.Read7BitEncodedInt32();
                LocalVersionList.FileSystem[] fileSystems = fileSystemCount > 0 ? new LocalVersionList.FileSystem[fileSystemCount] : null;
                for (int i = 0; i < fileSystemCount; i++)
                {
                    string name = binaryReader.ReadEncryptedString(encryptBytes);
                    int resourceIndexCount = binaryReader.Read7BitEncodedInt32();
                    int[] resourceIndexes = resourceIndexCount > 0 ? new int[resourceIndexCount] : null;
                    for (int j = 0; j < resourceIndexCount; j++)
                    {
                        resourceIndexes[j] = binaryReader.Read7BitEncodedInt32();
                    }

                    fileSystems[i] = new LocalVersionList.FileSystem(name, resourceIndexes);
                }

                return new LocalVersionList(resources, fileSystems);
            }
        }
    }
}
