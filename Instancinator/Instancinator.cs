﻿using Dalamud.Game;
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
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Internal.Notifications;
using ECommons;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.Schedulers;

namespace Instancinator
{
    public class Instancinator : IDalamudPlugin
    {
        public string Name => "Instancinator";
        bool draw = false;
        int selectedInst = 0;
        long nextKeypress = 0;
        Config Cfg;
        bool open = false;

        public Instancinator(DalamudPluginInterface pluginInterface)
        {
            ECommonsMain.Init(pluginInterface, this);
            Cfg = Svc.PluginInterface.GetPluginConfig() as Config ?? new Config();
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Svc.ClientState.TerritoryChanged += TerrCh;
            Svc.Commands.AddHandler("/inst", new CommandInfo(Cmd));
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { open = true; };
            Svc.Toasts.ErrorToast += ToastHandler;
        }

        public void Dispose()
        {
            Safe(delegate { DisableAllEntries(GetYesAlreadyPlugin()); });
            Svc.Framework.Update -= Tick;
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            Svc.Commands.RemoveHandler("/inst");
            Svc.ClientState.TerritoryChanged -= TerrCh;
            Svc.Toasts.ErrorToast -= ToastHandler;
        }

        private void ToastHandler(ref SeString message, ref bool isHandled)
        {
            if(selectedInst != 0 && message.ToString().Contains(Strings.Signature))
            {
                nextKeypress = Environment.TickCount64 + 100 + Cfg.ExtraDelay;
            }
        }

        private void TerrCh(ushort e)
        {
            Safe(delegate
            {
                if (selectedInst != 0) DisableAllEntries(GetYesAlreadyPlugin());
            });
        }

        private void Tick(object framework)
        {
            draw = false;
            if (Svc.ClientState.LocalPlayer != null && !Svc.Condition[ConditionFlag.BoundByDuty] && Strings.Territories.Contains(Svc.ClientState.TerritoryType))
            {
                Safe(delegate 
                { 
                    foreach (var i in Svc.Objects)
                    {
                        if (i.ObjectKind == ObjectKind.Aetheryte
                            && i.Name.ToString() == Strings.AetheryteTarget
                            && Vector3.Distance(Svc.ClientState.LocalPlayer.Position, i.Position) < 10f)
                        {
                            draw = true;
                            if (selectedInst != 0)
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
                                        if (Svc.Targets.Target == null || Svc.Targets.Target.Name.ToString() != Strings.AetheryteTarget)
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
                                                Keypress.SendKeycode(hwnd, Cfg.KeyCode);
                                            }
                                            nextKeypress = Environment.TickCount64 + 500;
                                        }
                                    }
                                    else if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent])
                                    {
                                        nextKeypress = Environment.TickCount64 + 1000 + Cfg.ExtraDelay;
                                    }
                                }
                            }
                            break;
                        }
                    }
                });
            }
            if (selectedInst != 0 && !draw)
            {
                Safe(delegate { DisableAllAndCreateIfNotExists(GetYesAlreadyPlugin()); });
            }
        }

        private void Draw()
        {
            if (open)
            {
                if(ImGui.Begin("Instancinator configuration", ref open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("Interact keycode", ref Cfg.KeyCode, float.Epsilon, 0, 1000);
                    if(Cfg.KeyCode <= 0) Cfg.KeyCode = 0x60;
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("Extra delay, MS", ref Cfg.ExtraDelay, 1f, 0, 2000);
                    if (Cfg.ExtraDelay < 0) Cfg.ExtraDelay = 0;
                }
                ImGui.End();
                if (!open)
                {
                    Svc.PluginInterface.SavePluginConfig(Cfg);
                    Svc.PluginInterface.UiBuilder.AddNotification("Configuration saved", "Instancinator", NotificationType.Success);
                }
            }
            if (draw)
            {
                if(ImGui.Begin("Instancinator", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
                {
                    ImGui.SetWindowFontScale(1f);
                    ImGui.Text($"Sel: {selectedInst}");
                    ImGui.SetWindowFontScale(2f);
                    if (ImGuiColoredButton(FontAwesomeIcon.DiceOne, selectedInst == 1))
                    {
                        new TickScheduler(delegate { Safe(delegate { EnableInstance(1, GetYesAlreadyPlugin()); }); });
                    }
                    if (ImGuiColoredButton(FontAwesomeIcon.DiceTwo, selectedInst == 2))
                    {
                        new TickScheduler(delegate { Safe(delegate { EnableInstance(2, GetYesAlreadyPlugin()); }); });
                    }
                    if (ImGuiColoredButton(FontAwesomeIcon.DiceThree, selectedInst == 3))
                    {
                        new TickScheduler(delegate { Safe(delegate { EnableInstance(3, GetYesAlreadyPlugin()); }); });
                    }
                    if (ImGuiIconButton(FontAwesomeIcon.TimesCircle))
                    {
                        new TickScheduler(delegate { Safe(delegate { DisableAllAndCreateIfNotExists(GetYesAlreadyPlugin()); }); });
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
            Safe(delegate
            {
                if(arguments == "key")
                {
                    Svc.Chat.Print($"Current confirm key code: {Cfg.KeyCode}");
                }
                else if (arguments.StartsWith("key"))
                {
                    if(int.TryParse(arguments.Replace("key ", ""), out var newKey) && newKey > 0)
                    {
                        Cfg.KeyCode = newKey;
                        Svc.PluginInterface.SavePluginConfig(Cfg);
                        Svc.Chat.Print($"New confirm key code: {Cfg.KeyCode}");
                    }
                    else
                    {
                        Svc.Chat.PrintError("Invalid argument");
                    }
                }
                else if (arguments == "check")
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
                else
                {
                    open = true;
                }
            });
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
