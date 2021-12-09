global using static Instancinator.Static;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Instancinator
{
    internal class Static
    {
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
    }
}
