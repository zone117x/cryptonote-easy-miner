using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CryptoNoteMiner
{
    class ArchitectureCheck
    {
        public static bool Is64Bit()
        {
            var is64BitProcess = (IntPtr.Size == 0x8);
            var is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();
            return is64BitOperatingSystem;
        }



        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5&& Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    if (!IsWow64Process(p.Handle, out bool retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
