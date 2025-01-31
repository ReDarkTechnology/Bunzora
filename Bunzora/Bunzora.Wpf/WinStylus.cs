using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Bunzora.Wpf;

public class WindowsStylus : Stylus
{
    private const int WM_POINTERDOWN = 0x0246;
    private const int WM_POINTERUPDATE = 0x0245;
    private const int WM_POINTERUP = 0x0247;

    private static IntPtr originalWndProc = IntPtr.Zero;
    private static WndProcDelegate newWndProcDelegate;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Constants for disabling pen feedback
    private const uint PENVISUALIZATIONOFF = 0x00000000;

    [DllImport("user32.dll")]
    private static extern bool SetWindowFeedbackSetting(IntPtr hwnd, uint feedbackType, uint flags, uint size, ref uint value);

    public void HandleWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_POINTERDOWN || msg == WM_POINTERUPDATE || msg == WM_POINTERUP)
        {
            var pointerId = GET_POINTERID_WPARAM((ulong)wParam);
            if (msg != WM_POINTERUPDATE)
            {
                Console.WriteLine($"Message received: {msg}\nPointer ID: {pointerId}\nParamW: {wParam}\nParamL: {lParam}");
            }
            User32.POINTER_INFO pointerInfo = new User32.POINTER_INFO();
            if (User32.GetPointerInfo(pointerId, ref pointerInfo))
            {
                if (User32.GetPointerPenInfo(pointerId, out User32.POINTER_PEN_INFO penInfo))
                {
                    float pressure = penInfo.pressure / 1024f; // Normalize pressure (0.0 to 1.0)
                    Vector2 pos = new Vector2(penInfo.pointerInfo.ptPixelLocation.X, penInfo.pointerInfo.ptPixelLocation.Y);

                    if (msg == WM_POINTERDOWN)
                    {
                        OnStylusDown?.Invoke(pos, pressure);
                    }
                    else if (msg == WM_POINTERUPDATE)
                    {
                        OnStylusMove?.Invoke(pos, pressure);
                    }
                    else if (msg == WM_POINTERUP)
                    {
                        OnStylusUp?.Invoke();
                    }
                }
                else
                {
                    if (msg == WM_POINTERDOWN)
                    {
                        Console.WriteLine("Failed getting PointerPenInfo");
                        OnStylusDown?.Invoke(Vector2.Zero, 1f);
                    }
                    else if (msg == WM_POINTERUPDATE)
                    {
                        OnStylusMove?.Invoke(Vector2.Zero, 1f);
                    }
                    else if (msg == WM_POINTERUP)
                    {
                        Console.WriteLine("Failed getting PointerPenInfo");
                        OnStylusUp?.Invoke();
                    }
                }
            }
            else
            {
                if (msg == WM_POINTERDOWN)
                {
                    Console.WriteLine("Failed getting PointerInfo");
                    OnStylusDown?.Invoke(Vector2.Zero, 1f);
                }
                else if (msg == WM_POINTERUPDATE)
                {
                    OnStylusMove?.Invoke(Vector2.Zero, 1f);
                }
                else if (msg == WM_POINTERUP)
                {
                    Console.WriteLine("Failed getting PointerInfo");
                    OnStylusUp?.Invoke();
                }
            }
        }
    }

    public static ushort LOWORD(ulong l) { return (ushort)(l & 0xFFFF); }
    public static ushort HIWORD(ulong l) { return (ushort)((l >> 16) & 0xFFFF); }
    public static ushort GET_POINTERID_WPARAM(ulong wParam) { return LOWORD(wParam); }
    public static ushort GET_X_LPARAM(ulong lp) { return LOWORD(lp); }
    public static ushort GET_Y_LPARAM(ulong lp) { return HIWORD(lp); }

    public override void Hook(IntPtr hwnd)
    {
        base.Hook(hwnd);
        newWndProcDelegate = CustomWndProc;
        originalWndProc = User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(newWndProcDelegate));
        ConfigurePenInput(hwnd, true);
    }

    static void ConfigurePenInput(IntPtr hwnd, bool isCanvas)
    {
        uint setting = isCanvas ? PENVISUALIZATIONOFF : 1; // Enable/Disable feedback

        // Disables visual pen feedback (ink ripples, gestures)
        SetWindowFeedbackSetting(hwnd, 0, 0, (uint)Marshal.SizeOf(setting), ref setting);

        if (isCanvas)
        {
            User32.SetProp(hwnd, "MicrosoftTabletPenServiceProperty", new IntPtr(1));
            User32.SetProp(hwnd, "DisableProcessWindowsPenFeedback", new IntPtr(1));
        }
        else
        {
            User32.RemoveProp(hwnd, "MicrosoftTabletPenServiceProperty");
            User32.RemoveProp(hwnd, "DisableProcessWindowsPenFeedback");
        }
    }

    private IntPtr CustomWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        HandleWndProc(hwnd, msg, wParam, lParam);
        return User32.CallWindowProc(originalWndProc, hwnd, msg, wParam, lParam); 
    }
}
