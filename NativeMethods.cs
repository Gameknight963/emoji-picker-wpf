using System.Runtime.InteropServices;

namespace emoji_picker_wpf
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")] internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("dwmapi.dll")] internal static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);
        [DllImport("user32.dll")] internal static extern bool GetCaretPos(out POINT lpPoint);
        [DllImport("user32.dll")] internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        internal const int DWMWA_CAPTION_COLOR = 35;
        internal const int WM_ERASEBKGND = 0x0014;

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight; }

        internal enum AccentState { ACCENT_DISABLED = 0, ACCENT_ENABLE_BLURBEHIND = 3, ACCENT_ENABLE_ACRYLICBLURBEHIND = 4 }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy { public AccentState AccentState; public int AccentFlags, GradientColor, AnimationId; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData { public int Attribute; public IntPtr Data; public int SizeOfData; }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }
    }
}