using System.Runtime.InteropServices;
using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Infrastructure.Input;

public sealed class Win32TriggerListener : ITriggerListener, IDisposable
{
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_ESCAPE = 0x1B;

    private IntPtr _mouseHook = IntPtr.Zero;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private HookProc? _mouseProc;
    private HookProc? _keyboardProc;
    private Action<Trigger>? _onCaptured;
    private Action? _onCancelled;

    public bool IsTriggered(Trigger trigger)
    {
        var virtualKeyCode = trigger switch
        {
            KeyTrigger key => key.VirtualKeyCode,
            MouseTrigger mouse => MouseVirtualKeyCodes.From(mouse.Button),
            _ => 0
        };
        return virtualKeyCode != 0 && (GetAsyncKeyState(virtualKeyCode) & 0x0001) != 0;
    }

    public void BeginCapture(Action<Trigger> onCaptured, Action onCancelled)
    {
        _onCaptured = onCaptured;
        _onCancelled = onCancelled;

        var moduleHandle = GetModuleHandle(null);
        _mouseProc = MouseHookCallback;
        _keyboardProc = KeyboardHookCallback;
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
    }

    public void EndCapture()
    {
        if (_mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
        if (_keyboardHook != IntPtr.Zero) { UnhookWindowsHookEx(_keyboardHook); _keyboardHook = IntPtr.Zero; }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            TriggerButton? button = (int)wParam switch
            {
                WM_LBUTTONDOWN => TriggerButton.Left,
                WM_RBUTTONDOWN => TriggerButton.Right,
                WM_MBUTTONDOWN => TriggerButton.Middle,
                _ => null
            };
            if (button is { } capturedButton)
            {
                var captured = _onCaptured;
                EndCapture();
                captured?.Invoke(new MouseTrigger(capturedButton));
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == WM_KEYDOWN)
        {
            int virtualKeyCode = Marshal.ReadInt32(lParam);
            if (virtualKeyCode == VK_ESCAPE)
            {
                var cancelled = _onCancelled;
                EndCapture();
                cancelled?.Invoke();
                return (IntPtr)1;
            }

            var captured = _onCaptured;
            EndCapture();
            captured?.Invoke(new KeyTrigger(virtualKeyCode));
            return (IntPtr)1;
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    public void Dispose() => EndCapture();
}
