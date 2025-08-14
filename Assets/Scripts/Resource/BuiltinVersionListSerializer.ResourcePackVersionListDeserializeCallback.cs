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
        public static ResourcePackVersionList ResourcePackVersionListDeserializeCallback_V0(Stream stream)
        {
            using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8))
            {
                byte[] encryptBytes = binaryReader.ReadBytes(CachedHashBytesLength);
                int dataOffset = binaryReader.ReadInt32();
                long dataLength = binaryReader.ReadInt64();
                int dataHashCode = binaryReader.ReadInt32();
                int resourceCount = binaryReader.Read7BitEncodedInt32();
                ResourcePackVersionList.Resource[] resources = resourceCount > 0 ? new ResourcePackVersionList.Resource[resourceCount] : null;
                for (int i = 0; i < resourceCount; i++)
                {
                    string name = binaryReader.ReadEncryptedString(encryptBytes);
                    string variant = binaryReader.ReadEncryptedString(encryptBytes);
                    string extension = binaryReader.ReadEncryptedString(encryptBytes) ?? DefaultExtension;
                    byte loadType = binaryReader.ReadByte();
                    long offset = binaryReader.Read7BitEncodedInt64();
                    int length = binaryReader.Read7BitEncodedInt32();
                    int hashCode = binaryReader.ReadInt32();
                    int compressedLength = binaryReader.Read7BitEncodedInt32();
                    int compressedHashCode = binaryReader.ReadInt32();
                    resources[i] = new ResourcePackVersionList.Resource(name, variant, extension, loadType, offset, length, hashCode, compressedLength, compressedHashCode);
                }

                return new ResourcePackVersionList(dataOffset, dataLength, dataHashCode, resources);
            }
        }
    }
}
