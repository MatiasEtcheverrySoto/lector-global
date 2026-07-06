using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LectorGlobalApp
{
    public class RawKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public bool CtrlDown { get; private set; }
        public bool WinDown { get; private set; }
        public bool AltDown { get; private set; }
        public bool ShiftDown { get; private set; }

        public event Func<int, bool> OnPhysicalKeyPressed; // Returns true if the key should be swallowed

        public RawKeyboardHook()
        {
            _proc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kbStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                bool isInjected = (kbStruct.flags & 0x10) != 0;

                if (!isInjected)
                {
                    int msg = wParam.ToInt32();
                    bool isDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                    bool isUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);

                    if (isDown || isUp)
                    {
                        if (kbStruct.vkCode == 0xA2 || kbStruct.vkCode == 0xA3 || kbStruct.vkCode == 0x11) // LCtrl, RCtrl, Ctrl
                            CtrlDown = isDown;
                        else if (kbStruct.vkCode == 0x5B || kbStruct.vkCode == 0x5C) // LWin, RWin
                            WinDown = isDown;
                        else if (kbStruct.vkCode == 0xA4 || kbStruct.vkCode == 0xA5 || kbStruct.vkCode == 0x12) // LAlt, RAlt, Alt
                            AltDown = isDown;
                        else if (kbStruct.vkCode == 0xA0 || kbStruct.vkCode == 0xA1 || kbStruct.vkCode == 0x10) // LShift, RShift, Shift
                            ShiftDown = isDown;
                    }

                    if (isDown && OnPhysicalKeyPressed != null)
                    {
                        bool swallow = OnPhysicalKeyPressed(kbStruct.vkCode);
                        if (swallow)
                        {
                            return (IntPtr)1; // Swallow the key
                        }
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }
    }
}
