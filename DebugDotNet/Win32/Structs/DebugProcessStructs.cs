using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDotNet.Win32.Structs
{
    /// <summary>
    /// StartupInfo is used by the NativeRoutine <see cref="NativeMethods.CreateProcessW(string, string, IntPtr, IntPtr, bool, uint, IntPtr, string, ref StartupInfo, out ProcessInformation)"/> and the code that leads there. This includes <see cref="DebugProcess"/> also
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

    public struct StartupInfo : IEquatable<StartupInfo>
    {
        /// <summary>
        /// When Filling this out, set this to <see cref="Marshal.SizeOf(object)"/> where the object is <see cref="StartupInfo"/>
        /// </summary>
        public int cb { get; set; }
        /// <summary>
        /// Reserved, Always keep at null
        /// </summary>
        public string lpReserved { get; set; }
        /// <summary>
        /// FROM MSDN, The name of the desktop, or the name of both the desktop and window station for this process. A backslash in the string indicates that the string includes both the desktop and window station names.
        /// </summary>
        public string lpDesktop { get; set; }
        /// <summary>
        /// For Console Processes, this specifies the Window Title. This defaults to the Program's location if NULL.  For GUI leave this NULL.
        /// </summary>
        public string lpTitle { get; set; }
        /// <summary>
        /// Specifies Starting X Position, requires <see cref="StartInfoFlags.StartInfoUsePositon"/> set
        /// </summary>
        public int dwX { get; set; }
        /// <summary>
        /// Specifies Starting Y Position, requires <see cref="StartInfoFlags.StartInfoUsePositon"/> set
        /// </summary>
        public int dwY { get; set; }
        /// <summary>
        /// Specifies the Window's width, requires <see cref="StartInfoFlags.StartInfoUseSize"/> set
        /// </summary>
        public int dwXSize { get; set; }
        /// <summary>
        /// Specifies the Window's Height, requires <see cref="StartInfoFlags.StartInfoUseSize"/> set
        /// </summary>
        public int dwYSize { get; set; }
        /// <summary>
        /// Specifies the Console's screen buffer's width, requires <see cref="StartInfoFlags.StartInfoUseCountChars"/> set
        /// </summary>
        public int dwXCountChars { get; set; }
        /// <summary>
        /// Specifies the Console's screen buffer's height, requires <see cref="StartInfoFlags.StartInfoUseCountChars"/> set
        /// </summary>
        public int dwYCountChars { get; set; }
        /// <summary>
        /// Specifies the console windows starting text and background color, requires <see cref="StartInfoFlags.StartInfoUseFillAttribute"/> set
        /// </summary>
        public int dwFillAttribute { get; set; }
        /// <summary>
        /// Controls a good portion of if the values are OK to use. <see cref="StartInfoFlags"/>
        /// </summary>
        public StartInfoFlags dwFlags { get; set; }
        /// <summary>
        /// Controls how the window will be shown if <see cref="StartInfoFlags.StartInfoUseShowWindow"/> is set
        /// </summary>

        public ShowWindowSettings wShowWindow { get; set; }
        /// <summary>
        /// Used by the C Runtime, leave at 0
        /// </summary>
        public short cbReserved2 { get; set; }
        /// <summary>
        /// Used by the C Runtime, leave at null / <see cref="IntPtr.Zero"/>
        /// </summary>
        public IntPtr lpReserved2 { get; set; }
        /// <summary>
        /// <list type="table">
        ///     <listheader>Depends on <see cref="dwFlags"/></listheader>
        ///     <item>
        ///     <term>If <see cref="StartInfoFlags.StartInfoUseStdHandles"/> is set,</term>
        ///     <description>This specifies a Standard Input Win32 Raw Handle.</description>
        ///     </item>
        ///     <item>
        ///     
        ///     <term> If <see cref="StartInfoFlags.StatInfoUseHotKey"/> is set </term>
        ///     <description>This specifies a HotKey value that will be sent to the main window of the new process using WM_HOTKEY</description>
        ///     
        ///     </item>
        ///     <item>
        ///     
        ///     <term> If neither are specified </term>
        ///     <description>This value is ignored</description>
        ///     </item>
        /// </list>
        /// </summary>
        public IntPtr hStdInput { get; set; }
        /// <summary>
        /// <list type="table">
        ///     <listheader>Depends on <see cref="dwFlags"/></listheader>
        ///     <item>
        ///     <term>If <see cref="StartInfoFlags.StartInfoUseStdHandles"/> is set,</term>
        ///     <description>This specifies a Standard Output Win32 Raw Handle.</description>
        ///     </item>
        ///     <item>
        ///     
        ///     <term> In Windows 7+, and process is launched from taskbar </term>
        ///     <description>This is a handle to the monitor that launched the process</description>
        ///     </item>
        ///     <item>
        ///     
        ///     <term> If neither conditions are met</term>
        ///     <description>This value is ignored</description>
        ///     </item>
        /// </list>
        /// </summary>
        public IntPtr hStdOutput { get; set; }
        /// <summary>
        /// <list type="table">
        ///     <listheader>Depends on <see cref="dwFlags"/></listheader>
        ///     <item>
        ///     <term>If <see cref="StartInfoFlags.StartInfoUseStdHandles"/> is set,</term>
        ///     <description>This specifies a Standard Error Win32 Raw Handle.</description>
        ///     </item>
        ///     <item>
        ///     <term>if not specified</term>
        ///     <description>this value is ignored</description>
        ///     </item>
        /// </list>
        /// </summary>
        public IntPtr hStdError { get; set; }

        /// <summary>
        /// Compare an obj and this to see if they are equal
        /// </summary>
        /// <param name="obj">compare against</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj is StartupInfo)
                {
                    return Equals((StartupInfo)obj);
                }
                return false;
            }
        }

        /// <summary>
        /// Get a hash code of the members of this structure
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int ret = 0;
            ret += cb.GetHashCode();
            ret += cbReserved2.GetHashCode();
            ret += dwFillAttribute.GetHashCode();
            ret += dwFlags.GetHashCode();
            ret += dwX.GetHashCode();
            ret += dwXCountChars.GetHashCode();
            ret += dwXSize.GetHashCode();
            ret += dwY.GetHashCode();
            ret += dwYCountChars.GetHashCode();
            ret += dwYSize.GetHashCode();
            ret += hStdError.GetHashCode();
            ret += hStdInput.GetHashCode();
            ret += hStdOutput.GetHashCode();
            ret += lpDesktop.GetHashCode();
            ret += lpReserved.GetHashCode();
            ret += lpReserved2.GetHashCode();
            ret += lpTitle.GetHashCode();
            ret += wShowWindow.GetHashCode();
            return ret;
        }

        /// <summary>
        /// is left equal to right?
        /// </summary>
        /// <param name="left">left side to compare</param>
        /// <param name="right">right side to compare</param>
        /// <returns>true ifs equal</returns>
        public static bool operator ==(StartupInfo left, StartupInfo right)
        {
            return left.Equals(right);
        }


        /// <summary>
        /// is left equal different from  right?
        /// </summary>
        /// <param name="left">left side to compare</param>
        /// <param name="right">right side to compare</param>
        /// <returns>true if different</returns>
        public static bool operator !=(StartupInfo left, StartupInfo right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compare any  <see cref="StartupInfo"/> struct with this one
        /// </summary>
        /// <param name="other">the one to compare against</param>
        /// <returns>true if equal</returns>
        public bool Equals(StartupInfo other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (other.cb != cb)
                {
                    return false;
                }

                if (other.cbReserved2 != cbReserved2)
                {
                    return false;
                }

                if (other.dwFillAttribute != dwFillAttribute)
                {
                    return false;
                }
                if (other.dwFlags != dwFlags)
                {
                    return false;
                }

                if (other.dwX != dwX)
                {
                    return false;
                }

                if (other.dwXCountChars != dwXCountChars)
                {
                    return false;
                }

                if (other.dwXSize != dwXSize)
                {
                    return false;
                }

                if (other.dwY != dwY)
                {
                    return false;
                }
                if (other.dwYCountChars != dwYCountChars)
                {
                    return false;
                }

                if (other.dwYSize != dwYSize)
                {
                    return false;
                }

                if (other.hStdError != hStdError)
                {
                    return false;
                }
                if (other.hStdInput != hStdInput)
                {
                    return false;
                }

                if (other.hStdOutput != hStdOutput)
                {
                    return false;
                }

                if (other.lpDesktop != lpDesktop)
                {
                    return false;
                }

                if (other.lpReserved != lpReserved)
                {
                    return false;
                }

                if (other.lpReserved2 != lpReserved2)
                {
                    return false;
                }

                if (other.lpTitle != lpTitle)
                {
                    return false;
                }

                if (other.wShowWindow != wShowWindow)
                {
                    return false;
                }

                return true;
            }
        }
    }

}
