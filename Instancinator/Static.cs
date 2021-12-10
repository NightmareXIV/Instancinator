global using static Instancinator.Static;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Instancinator
{
    internal static class Static
    {
        internal class Strings
        {
            public const string Instance1Regex = "//";
            public const string Instance2Regex = "//";
            public const string Instance3Regex = "//";
            public const string TravelToInstancedArea = "Travel to Instanced Area.";
            public const string AetheryteTarget = "aetheryte";
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
    }
}
