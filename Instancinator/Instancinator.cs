using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Reflection;

namespace Instancinator
{
    public class Instancinator : IDalamudPlugin
    {
        public string Name => "Instancinator";

        public Instancinator(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Svc.Commands.AddHandler("/inst", new CommandInfo(Cmd));
        }

        public void Dispose()
        {

            Svc.Framework.Update -= Tick;
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            Svc.Commands.RemoveHandler("/inst");
        }

        private void Tick(Framework framework)
        {
            
        }

        private void Cmd(string command, string arguments)
        {
            if(arguments == "check")
            {
                var p = GetYesAlreadyPlugin();
                if(p != null)
                {
                    Safe(delegate
                    {

                    });
                }
            }
        }

        private void Draw()
        {
            
        }

        IDalamudPlugin GetYesAlreadyPlugin()
        {
            try
            {
                var pluginManager = Svc.PluginInterface.GetType().Assembly.
                    GetType("Dalamud.Service`1", true).MakeGenericType(Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)).
                    GetMethod("Get").Invoke(null, BindingFlags.Default, null, new object[] { }, null);
                var installedPlugins = (System.Collections.IList)pluginManager.GetType().GetProperty("InstalledPlugins").GetValue(pluginManager);

                foreach (var t in installedPlugins)
                {
                    if ((string)t.GetType().GetProperty("Name").GetValue(t) == "Yes Already")
                    {
                        return (IDalamudPlugin)t.GetType().GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(t);
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                PluginLog.Error("Can't find YesAlready plugin: " + e.Message);
                PluginLog.Error(e.StackTrace);
                return null;
            }
        }
    }
}
