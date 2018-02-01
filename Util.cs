using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Util
{
    public class Utility
    {
        [DllImport("kernel32.dll")]
        private static extern int WideCharToMultiByte(uint CodePage, uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string lpWideCharStr, int cchWideChar, [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder lpMultiByteStr, int cbMultiByte, IntPtr lpDefaultChar, IntPtr lpUsedDefaultChar);

        public static string Utf16ToUtf8(string utf16String)
        {
            var iNewDataLen = WideCharToMultiByte(Convert.ToUInt32(Encoding.UTF8.CodePage), 0, utf16String, utf16String.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
            if (iNewDataLen > 1)
            {
                var utf8String = new StringBuilder(iNewDataLen);
                WideCharToMultiByte(Convert.ToUInt32(Encoding.UTF8.CodePage), 0, utf16String, -1, utf8String, utf8String.Capacity, IntPtr.Zero, IntPtr.Zero);

                return utf8String.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
