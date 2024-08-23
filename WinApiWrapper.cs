using System;
using System.Runtime.InteropServices;

namespace DesktopFileSelector
{
    public static class WinApiWrapper
    {
        public const int LVM_GETITEMCOUNT = 0x1004;
        public const int LVM_GETITEMW = 0x104B;
        public const int LVM_GETITEMSTATE = 0x102C;
        public const int LVIS_SELECTED = 0x0002;
        public const int LVIF_TEXT = 0x0001;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int MEM_RESERVE = 0x2000;
        public const int MEM_COMMIT = 0x1000;
        public const int PAGE_READWRITE = 0x04;
        public const int MEM_RELEASE = 0x8000;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern void GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct LVITEM
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public uint cColumns;
            public IntPtr puColumns;
            public IntPtr piColFmt;
            public int iGroup;
        }
    }
}
