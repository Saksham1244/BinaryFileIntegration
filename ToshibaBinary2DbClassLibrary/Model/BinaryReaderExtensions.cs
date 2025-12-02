using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ToshibaBinary2DbClassLibrary.Model
{
    public static class BinaryReaderExtensions
    {
        public static T ReadStruct<T>(this BinaryReader reader) where T : struct
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            byte[] bytes = reader.ReadBytes(size);
            IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ptr, size);
            T structure = (T)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr, typeof(T));
            System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            return structure;
        }

        //https://www.codeproject.com/Articles/19472/Convert-a-byte-array-to-a-struct-which-contains-ma#:~:text=Introduction,another%20instance%20of%20the%20class:
        public static T ReadClass<T>(this BinaryReader reader) where T : class
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            byte[] bytes = reader.ReadBytes(size);
            IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ptr, size);
            T structure = (T)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr, typeof(T));
            System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            return structure;
        }

        public static byte[] WriteByteArray<T>(T obj)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);

            byte[] bytes = new byte[size];

            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }

        /// <summary>
        /// Reads a single-precision floating point number in Big-Endian format
        /// Use this for machines that send data in Big-Endian byte order
        /// </summary>
        public static float ReadSingleBigEndian(this BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Reads a 32-bit integer in Big-Endian format
        /// </summary>
        public static int ReadInt32BigEndian(this BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
