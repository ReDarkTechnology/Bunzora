using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Bunzora.Gtk;

public class LinuxStylus : Stylus
{
    private IntPtr display;
    private int stylusDeviceId;

    /// Event types (constants) for X11
    public const int MotionNotify = 6;
    public const int ButtonPress = 4;
    public const int ButtonRelease = 5;

    public override void Hook(IntPtr hwnd)
    {
        display = hwnd;
        if (display == IntPtr.Zero)
        {
            Console.WriteLine("Failed to open X display.");
            throw new Exception("Unable to open X display.");
        }
        stylusDeviceId = GetStylusDeviceId();

        MainScene.Instance.OnPreEventPoll += PollEvents;
    }

    /// <summary>
    /// Extracting tablet device id by running xinput list and parsing the output
    /// </summary>
    private static int GetStylusDeviceId()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xinput",
                Arguments = "list",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var devices = output.Split('\n')
            .Where(line => line.Contains("stylus") || line.Contains("tablet"))
            .ToList();

        if (devices.Any())
        {
            var regex = new Regex(@"id=(\d+)");
            foreach (var device in devices)
            {
                var match = regex.Match(device);
                if (match.Success)
                {
                    int deviceId = int.Parse(match.Groups[1].Value);
                    return deviceId;
                }
            }
        }
        else
        {
            Console.WriteLine("No stylus or tablet devices found.");
        }
        return -1;
    }

    /// <summary>
    /// Poll the events using the Raylib's X11 display and read the pen tablet events
    /// </summary>
    public void PollEvents()
    {
        try
        {
            while (XPending(display) > 0)
            {
                Console.WriteLine("Polling event...");
                XPeekEvent(display, out XEvent ev); //TODO: Program always fails here, no errors, nothing.
                // That function just exits the app with exit code 0 as well, Ask X11, GLFW, Raylib team soon or something...

                if (ev.type == MotionNotify && ev.xmotion.deviceid == stylusDeviceId)
                {
                    Vector2 position = new Vector2(ev.xmotion.x, ev.xmotion.y);
                    float pressure = ev.xmotion.pressure / 1024f;
                    OnStylusMove?.Invoke(position, pressure);
                }
                else if (ev.type == ButtonPress && ev.xbutton.deviceid == stylusDeviceId)
                {
                    Vector2 position = new Vector2(ev.xbutton.x, ev.xbutton.y);
                    float pressure = 1.0f; 
                    OnStylusDown?.Invoke(position, pressure);
                }
                else if (ev.type == ButtonRelease && ev.xbutton.deviceid == stylusDeviceId)
                {
                    OnStylusUp?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during event polling: {ex.Message}");
        }
    }

    [DllImport("libX11.so", CallingConvention = CallingConvention.Cdecl)]
    private static extern int XPending(IntPtr display);
    [DllImport("libX11.so", CallingConvention = CallingConvention.Cdecl)]
    private static extern void XPeekEvent(IntPtr display, out XEvent ev);
    [DllImport("libX11.so", CallingConvention = CallingConvention.Cdecl)]
    private static extern void XNextEvent(IntPtr display, out XEvent ev);

    [StructLayout(LayoutKind.Explicit)]
    public struct XEvent
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(4)] public XMotionEvent xmotion;
        [FieldOffset(4)] public XButtonEvent xbutton;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XMotionEvent
    {
        public int type;
        public int x;
        public int y;
        public int deviceid;
        public float pressure;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XButtonEvent
    {
        public int type;
        public int x;
        public int y;
        public int deviceid;
        public int button;
    }
}
