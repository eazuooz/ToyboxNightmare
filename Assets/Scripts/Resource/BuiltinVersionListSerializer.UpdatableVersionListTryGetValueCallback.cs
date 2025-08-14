//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using System.IO;
using System.Text;

namespace UnityGameFramework.Runtime
{
    public static partial class BuiltinVersionListSerializer
    {
        public static bool UpdatableVersionListTryGetValueCallback_V0(Stream stream, string key, out object value)
        {
            value = null;
            if (key != "InternalResourceVersion")
            {
                return false;
            }

            using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8))
            {
                binaryReader.BaseStream.Position += CachedHashBytesLength;
                byte stringLength = binaryReader.ReadByte();
                binaryReader.BaseStream.Position += stringLength;
                value = binaryReader.ReadInt32();
            }

            return true;
        }

        public static bool UpdatableVersionListTryGetValueCallback_V1_V2(Stream stream, string key, out object value)
        {
            value = null;
            if (key != "InternalResourceVersion")
            {
                return false;
            }

            using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8))
            {
                binaryReader.BaseStream.Position += CachedHashBytesLength;
                byte stringLength = binaryReader.ReadByte();
                binaryReader.BaseStream.Position += stringLength;
                value = binaryReader.Read7BitEncodedInt32();
            }

            return true;
        }
    }
}
