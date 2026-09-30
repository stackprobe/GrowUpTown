using System.Runtime.InteropServices;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal static class WindowPlacement
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(Point point, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);

    // Capture before creating the window, while the cursor still identifies the launch monitor.
    internal static nint CaptureMonitor() => OperatingSystem.IsWindows() && GetCursorPos(out var point)
        ? MonitorFromPoint(point, 2 /* MONITOR_DEFAULTTONEAREST */) : 0;

    internal static unsafe void Center(nint monitor)
    {
        if (monitor == 0) return;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;
        nint window = (nint)GetWindowHandle();
        const uint flags = 0x0001 | 0x0004 | 0x0010; // Keep size, z-order and activation.
        // Move to the selected monitor first so DPI changes settle before measuring the frame.
        if (!SetWindowPos(window, 0, info.Monitor.Left, info.Monitor.Top, 0, 0, flags)) return;
        if (!GetWindowRect(window, out var rect) || !GetMonitorInfo(monitor, ref info)) return;
        int x = info.Monitor.Left + (info.Monitor.Right - info.Monitor.Left - (rect.Right - rect.Left)) / 2;
        int y = info.Monitor.Top + (info.Monitor.Bottom - info.Monitor.Top - (rect.Bottom - rect.Top)) / 2;
        SetWindowPos(window, 0, x, y, 0, 0, flags);
    }
}
