global using static Instancinator.Static;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Instancinator
{
    internal static class Static
    {
        internal class Strings
        {
            public static string[] Instances = { "//", "//", "//"};
            public const string TravelToInstancedArea = "Travel to Instanced Area.";
            public const string AetheryteTarget = "aetheryte";
            public static ushort[] Territories = { 957, 958, 959, 960, 961, 956 };
            public const string Signature = "Your destination is currently congested";
        }

        public static string Safe(Action a)
        {
            try
            {
                a();
                return null;
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n{e.StackTrace ?? ""}";
                PluginLog.Error(error);
                return error;
            }
        }

        public static void ChatPrintIfNotNull(string s)
        {
            if(!string.IsNullOrEmpty(s))
            {
                Svc.Chat.Print(s);
            }
        }

        public static string Join(this IEnumerable<string> s, string separator = "\n")
        {
            return string.Join(separator, s);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        public class Keypress
        {
            public const int LControlKey = 162;
            public const int Space = 32;
            public const int Escape = 0x1B;
            public const int Num0 = 0x60;

            const uint WM_KEYUP = 0x101;
            const uint WM_KEYDOWN = 0x100;


            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            public static void SendKeycode(IntPtr hwnd, int keycode)
            {
                SendMessage(hwnd, WM_KEYDOWN, (IntPtr)keycode, (IntPtr)0);
                SendMessage(hwnd, WM_KEYUP, (IntPtr)keycode, (IntPtr)0);
            }
        }

        public static bool TryFindGameWindow(out IntPtr hwnd)
        {
            hwnd = IntPtr.Zero;
            while (true)
            {
                hwnd = FindWindowEx(IntPtr.Zero, hwnd, "FFXIVGAME", null);
                if (hwnd == IntPtr.Zero) break;
                GetWindowThreadProcessId(hwnd, out var pid);
                if (pid == Process.GetCurrentProcess().Id) break;
            }
            return hwnd != IntPtr.Zero;
        }

        public static bool ImGuiIconButton(FontAwesomeIcon icon)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}Inst");
            ImGui.PopFont();
            return result;
        }
    }
}
