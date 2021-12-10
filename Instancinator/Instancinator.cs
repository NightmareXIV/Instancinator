using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using System.Numerics;
using ImGuiNET;
using Dalamud.Interface.Colors;
using Dalamud.Interface;

namespace Instancinator
{
    public class Instancinator : IDalamudPlugin
    {
        public string Name => "Instancinator";
        bool draw = false;
        int selectedInst = 0;
        long nextKeypress = 0;

        public Instancinator(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Svc.ClientState.TerritoryChanged += TerrCh;
            Svc.Commands.AddHandler("/inst", new CommandInfo(Cmd));
        }

        public void Dispose()
        {
            Safe(delegate { DisableAllEntries(GetYesAlreadyPlugin()); });
            Svc.Framework.Update -= Tick;
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            Svc.Commands.RemoveHandler("/inst");
            Svc.ClientState.TerritoryChanged -= TerrCh;
        }

        private void TerrCh(object sender, ushort e)
        {
            DisableAllEntries(GetYesAlreadyPlugin());
        }

        private void Tick(Framework framework)
        {
            draw = false;
            if(Svc.ClientState.LocalPlayer != null && !Svc.Condition[ConditionFlag.BoundByDuty])
            {
                foreach(var i in Svc.Objects)
                {
                    if(i.ObjectId == 0xE0000000 && i.Name.ToString() == Strings.AetheryteTarget
                        && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, i.Position) < 10f)
                    {
                        draw = true;
                        if(selectedInst != 0)
                        {
                            if (Svc.Condition[ConditionFlag.BetweenAreas]
                                || Svc.Condition[ConditionFlag.BetweenAreas51])
                            {
                                DisableAllEntries(GetYesAlreadyPlugin());
                            }
                            else
                            {
                                if (!Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
                                    && Environment.TickCount64 > nextKeypress)
                                {
                                    if (Svc.Targets.Target == null)
                                    {
                                        PluginLog.Debug("Setting aetheryte target");
                                        Svc.Targets.SetTarget(i);
                                        nextKeypress = Environment.TickCount64 + 100;
                                    }
                                    else
                                    {
                                        PluginLog.Debug("Clicking");
                                        if (TryFindGameWindow(out var hwnd))
                                        {
                                            Keypress.SendKeycode(hwnd, Keypress.Num0);
                                        }
                                        nextKeypress = Environment.TickCount64 + 1000;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
            if(selectedInst != 0 && !draw)
            {
                DisableAllAndCreateIfNotExists(GetYesAlreadyPlugin());
            }
        }

        private void Draw()
        {
            if (draw)
            {
                if(ImGui.Begin("Instancinator", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
                {
                    ImGui.SetWindowFontScale(1f);
                    ImGui.Text($"Sel: {selectedInst}");
                    ImGui.SetWindowFontScale(2f);
                    if (ImGuiColoredButton(FontAwesomeIcon.DiceOne, selectedInst == 1))
                    {
                        new TickScheduler(delegate { EnableInstance(1, GetYesAlreadyPlugin()); }, Svc.Framework);
                    }
                    if (ImGuiColoredButton(FontAwesomeIcon.DiceTwo, selectedInst == 2))
                    {
                        new TickScheduler(delegate { EnableInstance(2, GetYesAlreadyPlugin()); }, Svc.Framework);
                    }
                    if (ImGuiColoredButton(FontAwesomeIcon.DiceThree, selectedInst == 3))
                    {
                        new TickScheduler(delegate { EnableInstance(3, GetYesAlreadyPlugin()); }, Svc.Framework);
                    }
                    if (ImGuiIconButton(FontAwesomeIcon.TimesCircle))
                    {
                        new TickScheduler(delegate { DisableAllAndCreateIfNotExists(GetYesAlreadyPlugin()); }, Svc.Framework);
                    }
                }
                ImGui.End();
            }
        }

        bool ImGuiColoredButton(FontAwesomeIcon icon, bool colored)
        {
            if(colored)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DalamudRed);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGuiColors.DalamudOrange);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGuiColors.DPSRed);
            }
            var val = ImGuiIconButton(icon);
            if (colored) ImGui.PopStyleColor(3);
            return val;
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
                EnableInstance(1, GetYesAlreadyPlugin());
            }
            else if (arguments == "2")
            {
                EnableInstance(2, GetYesAlreadyPlugin());
            }
            else if (arguments == "3")
            {
                EnableInstance(3, GetYesAlreadyPlugin());
            }
        }

        void EnableInstance(int instance, object yaplugin)
        {
            DisableAllAndCreateIfNotExists(yaplugin);
            selectedInst = instance;
            var yaconfig = GetYesAlreadyConfig(yaplugin);
            var ListRootFolder = yaconfig.GetType().GetProperty("ListRootFolder").GetValue(yaconfig);
            var RootChildren = (System.Collections.IList)ListRootFolder.GetType().GetProperty("Children").GetValue(ListRootFolder);
            foreach (var e in RootChildren)
            {
                //PluginLog.Information(e.GetType().Name + "/" + (string)e.GetType().GetProperty("Name").GetValue(e));
                if (e.GetType().Name == "TextFolderNode" && (string)e.GetType().GetProperty("Name").GetValue(e) == "InstancinatorInternal")
                {
                    //PluginLog.Information("Found 1");
                    foreach (var i in (System.Collections.IList)e.GetType().GetProperty("Children").GetValue(e))
                    {
                        var txt = (string)i.GetType().GetProperty("Text").GetValue(i);
                        if (txt == Strings.Instances[instance-1] || txt == Strings.TravelToInstancedArea)
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
                ObjectList.Add(CreateListEntryNode(Strings.AetheryteTarget, Strings.Instances[0]));
                ObjectList.Add(CreateListEntryNode(Strings.AetheryteTarget, Strings.Instances[1]));
                ObjectList.Add(CreateListEntryNode(Strings.AetheryteTarget, Strings.Instances[2]));
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
            selectedInst = 0;
            var yaconfig = GetYesAlreadyConfig(yaplugin);
            var ListRootFolder = yaconfig.GetType().GetProperty("ListRootFolder").GetValue(yaconfig);
            var RootChildren = (System.Collections.IList)ListRootFolder.GetType().GetProperty("Children").GetValue(ListRootFolder);
            foreach (var e in RootChildren)
            {
                //PluginLog.Information(e.GetType().Name + "/" + (string)e.GetType().GetProperty("Name").GetValue(e));
                if (e.GetType().Name == "TextFolderNode" && (string)e.GetType().GetProperty("Name").GetValue(e) == "InstancinatorInternal")
                {
                    //PluginLog.Information("Found");
                    foreach (var i in (System.Collections.IList)e.GetType().GetProperty("Children").GetValue(e))
                    {
                        //PluginLog.Information((string)i.GetType().GetProperty("Text").GetValue(i));
                        i.GetType().GetProperty("Enabled").SetValue(i, false);
                    }
                    return true;
                }
            }
            return false;
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
