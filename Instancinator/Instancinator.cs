using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

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
                Safe(delegate
                {
                    //PluginLog.Information(p.GetType().Assembly.GetTypes().Select(o => o.ToString()).Join());
                    var yaconfig = GetYesAlreadyConfig(GetYesAlreadyPlugin());
                    var enabled = (bool)yaconfig.GetType().GetProperty("Enabled").GetValue(yaconfig);
                    Svc.Chat.Print($"Enabled: {enabled}");
                });
            }
            else if (arguments == "disableall")
            {
                DisableAllAndCreateIfNotExists(GetYesAlreadyPlugin());
            }
            else if (arguments == "1")
            {
                EnableInstance(Strings.Instance1Regex, GetYesAlreadyPlugin());
            }
            else if (arguments == "2")
            {
                EnableInstance(Strings.Instance2Regex, GetYesAlreadyPlugin());
            }
            else if (arguments == "3")
            {
                EnableInstance(Strings.Instance3Regex, GetYesAlreadyPlugin());
            }
        }

        void EnableInstance(string instance, object yaplugin)
        {
            DisableAllAndCreateIfNotExists(yaplugin);
            var yaconfig = GetYesAlreadyConfig(yaplugin);
            var ListRootFolder = yaconfig.GetType().GetProperty("ListRootFolder").GetValue(yaconfig);
            var RootChildren = (System.Collections.IList)ListRootFolder.GetType().GetProperty("Children").GetValue(ListRootFolder);
            foreach (var e in RootChildren)
            {
                PluginLog.Information(e.GetType().Name + "/" + (string)e.GetType().GetProperty("Name").GetValue(e));
                if (e.GetType().Name == "TextFolderNode" && (string)e.GetType().GetProperty("Name").GetValue(e) == "InstancinatorInternal")
                {
                    PluginLog.Information("Found 1");
                    foreach (var i in (System.Collections.IList)e.GetType().GetProperty("Children").GetValue(e))
                    {
                        var txt = (string)i.GetType().GetProperty("Text").GetValue(i);
                        if (txt == instance || txt == Strings.TravelToInstancedArea)
                        {
                            i.GetType().GetProperty("Enabled").SetValue(i, true);
                        }
                    }
                    return;
                }
            }
        }

        void DisableAllAndCreateIfNotExists(object yaplugin)
        {
            if (!DisableAllEntries(yaplugin))
            {
                var yaconfig = GetYesAlreadyConfig(yaplugin);
                var ListRootFolder = yaconfig.GetType().GetProperty("ListRootFolder").GetValue(yaconfig);
                var RootChildren = (System.Collections.IList)ListRootFolder.GetType().GetProperty("Children").GetValue(ListRootFolder);
                var instance = yaplugin.GetType().Assembly.CreateInstance("YesAlready.TextFolderNode");
                instance.GetType().GetProperty("Name").SetValue(instance, "InstancinatorInternal");
                var ObjectList = (System.Collections.IList)instance.GetType().GetProperty("Children").GetValue(instance);
                ObjectList.Add(CreateListEntryNode(Strings.AetheryteTarget, Strings.TravelToInstancedArea));
                ObjectList.Add(CreateListEntryNode(Strings.AetheryteTarget, Strings.Instance1Regex));
                ObjectList.Add(CreateListEntryNode(Strings.AetheryteTarget, Strings.Instance2Regex));
                ObjectList.Add(CreateListEntryNode(Strings.AetheryteTarget, Strings.Instance3Regex));
                RootChildren.Add(instance);
            }
        }

        object CreateListEntryNode(string target, string text)
        {
            var yaplugin = GetYesAlreadyPlugin();
            var instance = yaplugin.GetType().Assembly.CreateInstance("YesAlready.ListEntryNode");
            instance.GetType().GetProperty("Enabled").SetValue(instance, false);
            instance.GetType().GetProperty("TargetRestricted").SetValue(instance, true);
            instance.GetType().GetProperty("Text").SetValue(instance, text);
            instance.GetType().GetProperty("TargetText").SetValue(instance, target);
            return instance;
        }

        bool DisableAllEntries(object yaplugin)
        {
            var yaconfig = GetYesAlreadyConfig(yaplugin);
            var ListRootFolder = yaconfig.GetType().GetProperty("ListRootFolder").GetValue(yaconfig);
            var RootChildren = (System.Collections.IList)ListRootFolder.GetType().GetProperty("Children").GetValue(ListRootFolder);
            foreach (var e in RootChildren)
            {
                PluginLog.Information(e.GetType().Name + "/" + (string)e.GetType().GetProperty("Name").GetValue(e));
                if (e.GetType().Name == "TextFolderNode" && (string)e.GetType().GetProperty("Name").GetValue(e) == "InstancinatorInternal")
                {
                    PluginLog.Information("Found");
                    foreach (var i in (System.Collections.IList)e.GetType().GetProperty("Children").GetValue(e))
                    {
                        PluginLog.Information((string)i.GetType().GetProperty("Text").GetValue(i));
                        i.GetType().GetProperty("Enabled").SetValue(i, false);
                    }
                    return true;
                }
            }
            return false;
        }

        private void Draw()
        {
            
        }

        object GetYesAlreadyConfig(object yaplugin)
        {
            return yaplugin.GetType().Assembly.GetType("YesAlready.Service", true)
                .GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
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
