using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YourTech.IO;

namespace YourTech {
    static partial class Yrextension {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBuffer(this Stream stream, byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(this Stream stream, Int16 value) {
            stream.WriteBuffer(BitConverter.GetBytes(value));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(this Stream stream, Int32 value) {
            stream.WriteBuffer(BitConverter.GetBytes(value));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(this Stream stream, Int64 value) {
            stream.WriteBuffer(BitConverter.GetBytes(value));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingle(this Stream stream, Single value) {
            stream.WriteBuffer(BitConverter.GetBytes(value));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this Stream stream, Double value) {
            stream.WriteBuffer(BitConverter.GetBytes(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBuffer(this Stream stream, int length) {
            byte[] retVal = new byte[length];
            int len = 0;
            if ((len = stream.Read(retVal, 0, length)) != length) throw new StonException($"Expected {length} but Received {len}");
            return retVal;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16 ReadInt16(this Stream stream) {
            return BitConverter.ToInt16(stream.ReadBuffer(2), 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32 ReadInt32(this Stream stream) {
            return BitConverter.ToInt32(stream.ReadBuffer(4), 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64 ReadInt64(this Stream stream) {
            return BitConverter.ToInt64(stream.ReadBuffer(8), 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single ReadSingle(this Stream stream) {
            return BitConverter.ToSingle(stream.ReadBuffer(4), 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double ReadDouble(this Stream stream) {
            return BitConverter.ToDouble(stream.ReadBuffer(8), 0);
        }
    }
}
