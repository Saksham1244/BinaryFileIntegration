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
    }
}
