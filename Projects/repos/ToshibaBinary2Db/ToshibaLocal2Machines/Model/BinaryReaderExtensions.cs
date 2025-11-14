using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ToshibaLocal2Machines.Model
{
    public static class BinaryReaderExtensions
    {
        public static T ReadClass<T>(this BinaryReader reader) where T : class
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] bytes = reader.ReadBytes(size);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr, size);
            T structure = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return structure;
        }

        public static byte[] WriteByteArray<T>( T obj) 
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);

            byte[] bytes =  new byte[size];

            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            return bytes;           
        }
    }
}
