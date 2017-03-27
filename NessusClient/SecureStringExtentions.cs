using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace NessusClient
{
    public static class SecureStringExtentions
    {
        public static byte[] ToBytes(this SecureString secureString)
        {
            var bytes = new byte[secureString.Length];
            var ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                Marshal.Copy(ptr, bytes, 0, bytes.Length);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
            return bytes;
        }

        public static SecureString ToSecureString(this byte[] bytes)
        {
            
            var chars = Encoding.UTF8.GetChars(bytes);
            try
            {
                var result = new SecureString();
                foreach (var c in chars)
                {
                    result.AppendChar(c);
                }
                return result;
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
                Array.Clear(chars, 0, chars.Length);
            }
            
        }
    }
}
