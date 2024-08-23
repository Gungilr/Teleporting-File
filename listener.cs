using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Collections;
using System.Windows.Forms;
using Shell32;
using System.Threading;
using SHDocVw;

namespace DesktopFileSelector
{
    class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetShellFolderView(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SHGetPathFromIDList(IntPtr pidl, [Out] StringBuilder pszPath);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const uint WM_USER = 0x0400;
        const uint WM_USER_GET_SEL_COUNT = WM_USER + 50;
        const uint WM_USER_GET_SEL_ITEMS = WM_USER + 51;

        [StructLayout(LayoutKind.Sequential)]
        public struct IconLayoutItem
        {
            public int Index;
            public string Size;
            public int Column;
            public int Row;
            public string FileAttributes;
            public string EntryName;
        }

        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETITEMPOSITION = LVM_FIRST + 15;

        [StructLayout(LayoutKind.Sequential)]
        public struct LVITEMPOSITION
        {
            public int x;
            public int y;
        }

        private struct LVITEM
        {
            public int mask;
            public int iItem;
            public int iSubItem;
            public int state;
            public int stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public int cColumns;
            public IntPtr puColumns;
            public IntPtr piColFmt;
            public int iGroup;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const int KEYEVENTF_KEYUP = 0x0002;
        const byte VK_CONTROL = 0x11;
        const byte VK_ESC = 0x1B;
        const byte VK_SPACE = 0x20;

        private static string fileName = "VIRUS";

        static void Main(string[] args)
        {
            Thread thread = new Thread(new ThreadStart(RunFunction));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();  // Keep the main thread alive
        }

        private static int fileIndex()
        {
            // get the handle of the desktop listview
            IntPtr vHandle = WinApiWrapper.FindWindow("Progman", "Program Manager");
            vHandle = WinApiWrapper.FindWindowEx(vHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            vHandle = WinApiWrapper.FindWindowEx(vHandle, IntPtr.Zero, "SysListView32", "FolderView");

            //IntPtr vHandle = WinApiWrapper.GetForegroundWindow();

            //Get total count of the icons on the desktop
            int vItemCount = WinApiWrapper.SendMessage(vHandle, WinApiWrapper.LVM_GETITEMCOUNT, 0, 0);
            //MessageBox.Show(vItemCount.ToString());
            uint vProcessId;
            WinApiWrapper.GetWindowThreadProcessId(vHandle, out vProcessId);
            IntPtr vProcess = WinApiWrapper.OpenProcess(WinApiWrapper.PROCESS_VM_OPERATION | WinApiWrapper.PROCESS_VM_READ |
            WinApiWrapper.PROCESS_VM_WRITE, false, vProcessId);
            IntPtr vPointer = WinApiWrapper.VirtualAllocEx(vProcess, IntPtr.Zero, 4096,
            WinApiWrapper.MEM_RESERVE | WinApiWrapper.MEM_COMMIT, WinApiWrapper.PAGE_READWRITE);
            try
            {
                for (int j = 0; j < vItemCount; j++)
                {
                    byte[] vBuffer = new byte[256];
                    WinApiWrapper.LVITEM[] vItem = new WinApiWrapper.LVITEM[1];
                    vItem[0].mask = WinApiWrapper.LVIF_TEXT;
                    vItem[0].iItem = j;
                    vItem[0].iSubItem = 0;
                    vItem[0].cchTextMax = vBuffer.Length;
                    vItem[0].pszText = (IntPtr)((int)vPointer + Marshal.SizeOf(typeof(WinApiWrapper.LVITEM)));
                    uint vNumberOfBytesRead = 0;
                    WinApiWrapper.WriteProcessMemory(vProcess, vPointer,
                    Marshal.UnsafeAddrOfPinnedArrayElement(vItem, 0),
                    Marshal.SizeOf(typeof(WinApiWrapper.LVITEM)), ref vNumberOfBytesRead);
                    WinApiWrapper.SendMessage(vHandle, WinApiWrapper.LVM_GETITEMW, j, vPointer.ToInt32());
                    WinApiWrapper.ReadProcessMemory(vProcess,
                    (IntPtr)((int)vPointer + Marshal.SizeOf(typeof(WinApiWrapper.LVITEM))),
                    Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0),
                    vBuffer.Length, ref vNumberOfBytesRead);
                    string vText = Encoding.Unicode.GetString(vBuffer, 0,
                    (int)vNumberOfBytesRead);
                    string IconName = vText;

                    //Check if item is selected
                    var result = WinApiWrapper.SendMessage(vHandle, WinApiWrapper.LVM_GETITEMSTATE, j, (int)WinApiWrapper.LVIS_SELECTED);
                    if (result == WinApiWrapper.LVIS_SELECTED)
                    {
                        return j;
                    }
                }
            }
            finally
            {
                WinApiWrapper.VirtualFreeEx(vProcess, vPointer, 0, WinApiWrapper.MEM_RELEASE);
                WinApiWrapper.CloseHandle(vProcess);
            }
            return -1;
        }

        static void RunFunction()
        {
            while (true)
            {
                try
                {
                    MyFunction();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred in RunFunction: " + ex.Message);
                }
                Thread.Sleep(50);  // Sleep for 500 milliseconds
            }
        }

        public static void ParseIconLayout(int FunDex)
        {
            try
            {
                IntPtr vHandle = IntPtr.Zero;
                WinApiWrapper.EnumWindows((hWnd, lParam) =>
                {
                    IntPtr shellViewHandle = WinApiWrapper.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellViewHandle != IntPtr.Zero)
                    {
                        vHandle = WinApiWrapper.FindWindowEx(shellViewHandle, IntPtr.Zero, "SysListView32", "FolderView");
                        return false; // Stop enumeration
                    }
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                if (vHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to find SysListView32 window.");
                }

                Random rnd = new Random();
                int lParam = (rnd.Next(100, 600) << 16) | (rnd.Next(100, 1200) & 0xFFFF);
                WinApiWrapper.SendMessage(vHandle, LVM_SETITEMPOSITION, FunDex, lParam);
                MessageBox.Show("NO U!");
                Console.WriteLine("Moving stuff");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in ParseIconLayout: " + ex.Message);
            }
        }

        static void MyFunction()
        {
            Console.WriteLine("Function executed at: " + DateTime.Now);
            try
            {
                selectedFiles();
                GetListOfSelectedFilesAndFolderOfWindowsExplorer();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in MyFunction: " + ex.Message);
            }
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        public static void Move()
        {
            short SWP_NOMOVE = 0X2;
            short SWP_NOSIZE = 1;
            short SWP_NOZORDER = 0X4;
            int SWP_SHOWWINDOW = 0x0040;

            Process[] processes = Process.GetProcesses(".");
            foreach (var process in processes)
            {
                var handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    SetWindowPos(handle, 0, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);
                }
            }
        }
        private static void unselect()
        {
            try
            {
                keybd_event(VK_ESC, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100); // Adjust delay as necessary

                // Simulate pressing Space key down
                keybd_event(VK_ESC, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Simulate pressing Ctrl key down
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100); // Adjust delay as necessary

                // Simulate pressing Space key down
                keybd_event(VK_SPACE, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100); // Adjust delay as necessary

                // Simulate releasing Space key
                keybd_event(VK_SPACE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                Thread.Sleep(100); // Adjust delay as necessary

                // Simulate releasing Ctrl key
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in unselect: " + ex.Message);
            }
        }

        public static void CreateFileOnDesktop(string fileName, string content)
        {
            try
            {
                // Get the path to the desktop
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Combine the desktop path with the file name to get the full file path
                string filePath = Path.Combine(desktopPath, fileName);

                // Write the content to the file
                File.WriteAllText(filePath, content);

                Console.WriteLine($"File '{fileName}' created on desktop with content: {content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in CreateFileOnDesktop: " + ex.Message);
            }
        }

        private static void selectedFiles()
        {
            try
            {
                // Find the desktop listview handle using EnumWindows
                IntPtr vHandle = IntPtr.Zero;
                WinApiWrapper.EnumWindows((hWnd, lParam) =>
                {
                    IntPtr shellViewHandle = WinApiWrapper.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellViewHandle != IntPtr.Zero)
                    {
                        vHandle = WinApiWrapper.FindWindowEx(shellViewHandle, IntPtr.Zero, "SysListView32", "FolderView");
                        return false; // Stop enumeration
                    }
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                if (vHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to find SysListView32 window.");
                }

                // Get total count of the icons on the desktop
                int vItemCount = WinApiWrapper.SendMessage(vHandle, WinApiWrapper.LVM_GETITEMCOUNT, 0, 0);
                if (vItemCount == 0)
                {
                    Console.WriteLine("No items found on the desktop.");
                }

                uint vProcessId;
                WinApiWrapper.GetWindowThreadProcessId(vHandle, out vProcessId);
                IntPtr vProcess = WinApiWrapper.OpenProcess(WinApiWrapper.PROCESS_VM_OPERATION | WinApiWrapper.PROCESS_VM_READ |
                WinApiWrapper.PROCESS_VM_WRITE, false, vProcessId);
                if (vProcess == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to open process.");
                }

                IntPtr vPointer = WinApiWrapper.VirtualAllocEx(vProcess, IntPtr.Zero, 4096,
WinApiWrapper.MEM_RESERVE | WinApiWrapper.MEM_COMMIT, WinApiWrapper.PAGE_READWRITE);
                if (vPointer == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to allocate memory.");
                }
                try
                {
                    for (int j = 0; j < vItemCount; j++)
                    {
                        byte[] vBuffer = new byte[256];
                        WinApiWrapper.LVITEM[] vItem = new WinApiWrapper.LVITEM[1];
                        vItem[0].mask = WinApiWrapper.LVIF_TEXT;
                        vItem[0].iItem = j;
                        vItem[0].iSubItem = 0;
                        vItem[0].cchTextMax = vBuffer.Length;
                        vItem[0].pszText = (IntPtr)((int)vPointer + Marshal.SizeOf(typeof(WinApiWrapper.LVITEM)));
                        uint vNumberOfBytesRead = 0;
                        WinApiWrapper.WriteProcessMemory(vProcess, vPointer,
                        Marshal.UnsafeAddrOfPinnedArrayElement(vItem, 0),
                        Marshal.SizeOf(typeof(WinApiWrapper.LVITEM)), ref vNumberOfBytesRead);
                        WinApiWrapper.SendMessage(vHandle, WinApiWrapper.LVM_GETITEMW, j, vPointer.ToInt32());
                        WinApiWrapper.ReadProcessMemory(vProcess,
                        (IntPtr)((int)vPointer + Marshal.SizeOf(typeof(WinApiWrapper.LVITEM))),
                        Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0),
                        vBuffer.Length, ref vNumberOfBytesRead);
                        string vText = Encoding.Unicode.GetString(vBuffer, 0,
                        (int)vNumberOfBytesRead);
                        string IconName = vText;

                        var result = WinApiWrapper.SendMessage(vHandle, WinApiWrapper.LVM_GETITEMSTATE, j, (int)WinApiWrapper.LVIS_SELECTED);
                        if (result == WinApiWrapper.LVIS_SELECTED && !(vText.IndexOf(fileName) == -1))
                        {
                            int id = fileIndex();
                            unselect();
                            ParseIconLayout(id);
                        }
                    }
                }
                finally
                {
                    WinApiWrapper.VirtualFreeEx(vProcess, vPointer, 0, WinApiWrapper.MEM_RELEASE);
                    WinApiWrapper.CloseHandle(vProcess);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in selectedFiles: " + ex.Message);
            }
        }

        public static void GetListOfSelectedFilesAndFolderOfWindowsExplorer()
        {
            try
            {
                string filename;
                ArrayList selected = new ArrayList();
                var shell = new Shell32.Shell();
                //For each explorer
                foreach (SHDocVw.InternetExplorer window in new SHDocVw.ShellWindows())
                {
                    filename = Path.GetFileNameWithoutExtension(window.FullName).ToLower();
                    if (filename.ToLowerInvariant() == "explorer")
                    {
                        Shell32.FolderItems items = ((Shell32.IShellFolderViewDual2)window.Document).SelectedItems();
                        foreach (Shell32.FolderItem item in items)
                        {
                            selected.Add(item.Path);
                            if (!(item.Path.IndexOf(fileName) == -1))
                            {
                                unselect();
                                Move();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in GetListOfSelectedFilesAndFolderOfWindowsExplorer: " + ex.Message);
            }
        }
    }
}
