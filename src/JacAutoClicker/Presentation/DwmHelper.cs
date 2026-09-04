using System.Runtime.InteropServices;

namespace JacaAutoClicker.Presentation;

internal static class DwmHelper
{
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static void Dark(IntPtr hwnd)
    {
        int value = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }
}
