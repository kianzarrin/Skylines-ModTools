﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ColossalFramework.UI;
using UnityEngine;

namespace ModTools
{
    internal sealed class Console : GUIWindow, ILogger
    {
        private static Configuration Config => ModTools.Instance.config;

        private readonly GUIArea headerArea;
        private readonly GUIArea consoleArea;
        private readonly GUIArea commandLineArea;
        private bool focusCommandLineArea;
        private bool emptyCommandLineArea = true;
        private bool setCommandLinePosition;
        private int commandLinePosition;

        private readonly float headerHeightCompact = 0.5f;
        private readonly float headerHeightExpanded = 8.0f;
        private bool headerExpanded;

        private readonly float commandLineAreaHeightCompact = 45.0f;
        private readonly float commandLineAreaHeightExpanded = 120.0f;

        private bool CommandLineAreaExpanded
        {
            get
            {
                var command = commandHistory[currentCommandHistoryIndex];
                return command.Length == 0 ? false : command.Contains('\n') || command.Length >= 64;
            }
        }

        private readonly object historyLock = new object();
        private readonly List<ConsoleMessage> history = new List<ConsoleMessage>();
        private readonly List<string> commandHistory = new List<string>() { string.Empty };
        private int currentCommandHistoryIndex;

        private Vector2 consoleScrollPosition = Vector2.zero;
        private Vector2 commandLineScrollPosition = Vector2.zero;

        private DebugOutputPanel vanillaPanel;
        private Transform oldVanillaPanelParent;

        private List<KeyValuePair<int, string>> userNotifications;

        public Console()
            : base("Debug console", Config.ConsoleRect, Skin)
        {
            headerArea = new GUIArea(this);
            consoleArea = new GUIArea(this);
            commandLineArea = new GUIArea(this);

            RecalculateAreas();
        }

        protected override void OnWindowDrawn()
        {
            var e = Event.current;
            if (e.type != EventType.KeyUp || GUI.GetNameOfFocusedControl() != "ModToolsConsoleCommandLine")
            {
                return;
            }
            if (setCommandLinePosition)
            {
                // return has been hit with control pressed in previous GUI event
                // reset the position to the remembered one
                setCommandLinePosition = false;
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                editor.selectIndex = commandLinePosition - 1;
                editor.cursorIndex = editor.selectIndex;
            }

            if (e.keyCode == KeyCode.Return)
            {
                if (e.shift)
                {
                    return;
                }

                // event.Use() does not consume the event, work around the enter being inserted into the textbox by
                // deleting the line break
                var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                var pos = editor.selectIndex;
                var currentCommand = commandHistory[currentCommandHistoryIndex];
                commandHistory[currentCommandHistoryIndex]
                    = currentCommand.Substring(0, pos - 1) + currentCommand.Substring(pos, currentCommand.Length - pos);

                // if control is pressed when hitting return, do not empty the command line area
                // remember the currently selected position and reset it after the next redraw
                if (e.control)
                {
                    emptyCommandLineArea = false;
                    setCommandLinePosition = true;
                    commandLinePosition = pos;
                }

                RunCommandLine();
            }
        }

        protected override void OnWindowDestroyed()
        {
            if (vanillaPanel != null)
            {
                vanillaPanel.transform.parent = oldVanillaPanelParent;
                vanillaPanel = null;
            }
        }

        public void Update()
        {
            if (vanillaPanel == null)
            {
                var panel = UIView.library?.Get<DebugOutputPanel>("DebugOutputPanel");
                if (panel == null)
                {
                    return;
                }
                vanillaPanel = panel;
                oldVanillaPanelParent = vanillaPanel.transform.parent;
                vanillaPanel.transform.parent = transform;
            }
        }

        public void Log(string message, LogType type)
        {
            lock (historyLock)
            {
                if (history.Count > 0)
                {
                    var lastMessage = history[history.Count - 1];
                    if (message == lastMessage.Message && type == lastMessage.Type)
                    {
                        lastMessage.Count++;
                        return;
                    }
                }
            }

            var caller = string.Empty;

            var trace = new StackTrace(8);

            for (var i = 0; i < trace.FrameCount; i++)
            {
                MethodBase callingMethod = null;

                var frame = trace.GetFrame(i);
                if (frame != null)
                {
                    callingMethod = frame.GetMethod();
                }

                if (callingMethod == null)
                {
                    continue;
                }
                caller = callingMethod.DeclaringType != null ? $"{callingMethod.DeclaringType}.{callingMethod.Name}()" : $"{callingMethod}()";
                break;
            }

            lock (historyLock)
            {
                history.Add(new ConsoleMessage(caller, message, type, trace));

                if (history.Count >= Config.ConsoleMaxHistoryLength)
                {
                    history.RemoveAt(0);
                }
            }

            if (type == LogType.Log && Config.ShowConsoleOnMessage)
            {
                Visible = true;
            }
            else if (type == LogType.Warning && Config.ShowConsoleOnWarning)
            {
                Visible = true;
            }
            else if ((type == LogType.Exception || type == LogType.Error) && Config.ShowConsoleOnError)
            {
                Visible = true;
            }

            if (Config.ConsoleAutoScrollToBottom)
            {
                consoleScrollPosition.y = float.MaxValue;
            }
        }

        private void RecalculateAreas()
        {
            var headerHeight = headerExpanded ? headerHeightExpanded : headerHeightCompact;
            headerHeight *= Config.FontSize;
            headerHeight += 32.0f;

            headerArea.RelativeSize.x = 1.0f;
            headerArea.AbsolutePosition.y = 16.0f;
            headerArea.AbsoluteSize.y = headerHeight;

            var commandLineAreaHeight = CommandLineAreaExpanded
                ? commandLineAreaHeightExpanded
                : commandLineAreaHeightCompact;

            consoleArea.AbsolutePosition.y = 16.0f + headerHeight;
            consoleArea.RelativeSize.x = 1.0f;
            consoleArea.RelativeSize.y = 1.0f;
            consoleArea.AbsoluteSize.y = -(commandLineAreaHeight + headerHeight + 16.0f);

            commandLineArea.RelativePosition.y = 1.0f;
            commandLineArea.AbsolutePosition.y = -commandLineAreaHeight;
            commandLineArea.RelativeSize.x = 1.0f;
            commandLineArea.AbsoluteSize.y = commandLineAreaHeight;
        }

        protected override void HandleException(Exception ex) => Log("Exception in ModTools Console - " + ex.Message, LogType.Exception);

        private void DrawCompactHeader()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("▼", GUILayout.ExpandWidth(false)))
            {
                headerExpanded = true;
                RecalculateAreas();
            }

            GUILayout.Label("Show console configuration");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                lock (historyLock)
                {
                    history.Clear();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawExpandedHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Log message format:", GUILayout.ExpandWidth(false));
            Config.ConsoleFormatString = GUILayout.TextField(Config.ConsoleFormatString, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max items in history:", GUILayout.ExpandWidth(false));
            Config.ConsoleMaxHistoryLength = GUIControls.PrimitiveValueField("ConsoleMaxItemsInHistory", string.Empty, Config.ConsoleMaxHistoryLength);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show console on:", GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Message", GUILayout.ExpandWidth(false));
            Config.ShowConsoleOnMessage = GUILayout.Toggle(Config.ShowConsoleOnMessage, string.Empty, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Warning", GUILayout.ExpandWidth(false));
            Config.ShowConsoleOnWarning = GUILayout.Toggle(Config.ShowConsoleOnWarning, string.Empty, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Error", GUILayout.ExpandWidth(false));
            Config.ShowConsoleOnError = GUILayout.Toggle(Config.ShowConsoleOnError, string.Empty, GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Auto-scroll on new messages:");
            Config.ConsoleAutoScrollToBottom = GUILayout.Toggle(Config.ConsoleAutoScrollToBottom, string.Empty);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("▲", GUILayout.ExpandWidth(false)))
            {
                headerExpanded = false;
                RecalculateAreas();
            }

            GUILayout.Label("Hide console configuration");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save"))
            {
                ModTools.Instance.SaveConfig();
            }

            if (GUILayout.Button("Reset"))
            {
                var template = new Configuration();
                Config.ConsoleMaxHistoryLength = template.ConsoleMaxHistoryLength;
                Config.ConsoleFormatString = template.ConsoleFormatString;
                Config.ShowConsoleOnMessage = template.ShowConsoleOnMessage;
                Config.ShowConsoleOnWarning = template.ShowConsoleOnWarning;
                Config.ShowConsoleOnError = template.ShowConsoleOnError;

                ModTools.Instance.SaveConfig();
            }

            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                lock (historyLock)
                {
                    history.Clear();
                }
            }

            GUILayout.EndHorizontal();
        }

        public void DrawHeader()
        {
            headerArea.Begin();

            if (headerExpanded)
            {
                DrawExpandedHeader();
            }
            else
            {
                DrawCompactHeader();
            }

            headerArea.End();
        }

        private Color orangeColor = new Color(1.0f, 0.647f, 0.0f, 1.0f);

        private void DrawConsole()
        {
            userNotifications = UserNotifications.GetNotifications();

            consoleArea.Begin();

            consoleScrollPosition = GUILayout.BeginScrollView(consoleScrollPosition);

            foreach (var item in userNotifications)
            {
                GUILayout.BeginHorizontal(Skin.box);

                GUI.contentColor = Color.blue;
                GUILayout.Label(item.Value);
                GUI.contentColor = Color.white;

                if (GUILayout.Button("Hide"))
                {
                    UserNotifications.HideNotification(item.Key);
                }

                GUILayout.EndHorizontal();
            }

            ConsoleMessage[] messages = null;

            lock (historyLock)
            {
                messages = history.ToArray();
            }

            foreach (var item in messages)
            {
                GUILayout.BeginHorizontal(Skin.box);

                var msg = Config.ConsoleFormatString.Replace("{{type}}", item.Type.ToString())
                        .Replace("{{caller}}", item.Caller)
                        .Replace("{{message}}", item.Message);

                switch (item.Type)
                {
                    case LogType.Log:
                        GUI.contentColor = Config.ConsoleMessageColor;
                        break;

                    case LogType.Warning:
                        GUI.contentColor = Config.ConsoleWarningColor;
                        break;

                    case LogType.Error:
                        GUI.contentColor = Config.ConsoleErrorColor;
                        break;

                    case LogType.Assert:
                    case LogType.Exception:
                        GUI.contentColor = Config.ConsoleExceptionColor;
                        break;
                }

                GUILayout.Label(msg);

                GUILayout.FlexibleSpace();

                if (item.Count > 1)
                {
                    GUI.contentColor = orangeColor;
                    if (item.Count > 1024)
                    {
                        GUI.contentColor = Color.red;
                    }

                    GUILayout.Label(item.Count.ToString(), Skin.box);
                }
                else
                {
                    GUILayout.Label(string.Empty);
                }

                GUI.contentColor = Color.white;

                var stackTrace = item.Trace;
                if (stackTrace != null)
                {
                    GUIStackTrace.StackTraceButton(stackTrace);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            consoleArea.End();
        }

        private void DrawCommandLineArea()
        {
            commandLineArea.Begin();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            commandLineScrollPosition = GUILayout.BeginScrollView(commandLineScrollPosition, false, false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

            GUI.SetNextControlName("ModToolsConsoleCommandLine");

            commandHistory[currentCommandHistoryIndex] = GUILayout.TextArea(commandHistory[currentCommandHistoryIndex], GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (commandHistory[currentCommandHistoryIndex].Trim().Length == 0)
            {
                GUI.enabled = false;
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run", GUILayout.ExpandWidth(false)))
            {
                RunCommandLine();
            }

            GUI.enabled = true;

            if (currentCommandHistoryIndex == 0)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("↑", GUILayout.ExpandWidth(false)))
            {
                currentCommandHistoryIndex--;
            }

            GUI.enabled = true;

            if (currentCommandHistoryIndex >= commandHistory.Count - 1)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("↓", GUILayout.ExpandWidth(false)))
            {
                currentCommandHistoryIndex++;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            commandLineArea.End();

            // refocus the command line area after enter has been pressed
            if (focusCommandLineArea)
            {
                GUI.FocusControl("ModToolsConsoleCommandLine");
                focusCommandLineArea = false;
            }
        }

        private void RunCommandLine()
        {
            var commandLine = commandHistory[currentCommandHistoryIndex];

            if (emptyCommandLineArea)
            {
                if (commandHistory.Last() != string.Empty)
                {
                    commandHistory.Add(string.Empty);
                    currentCommandHistoryIndex = commandHistory.Count - 1;
                }
                else
                {
                    currentCommandHistoryIndex = commandHistory.Count - 1;
                }
            }
            emptyCommandLineArea = true;

            var source = string.Format(defaultSource, commandLine);
            var file = new ScriptEditorFile(source, "ModToolsCommandLineScript.cs");
            if (!ScriptCompiler.RunSource(new List<ScriptEditorFile>() { file }, out _, out var instance))
            {
                global::ModTools.Log.Error("Failed to compile command-line!");
            }
            else if (instance != null)
            {
                global::ModTools.Log.Message("Executing command-line..");
                instance.OnModLoaded();
            }
            else
            {
                global::ModTools.Log.Error("Error executing command-line..");
            }
            focusCommandLineArea = true;
        }

        protected override void DrawWindow()
        {
            RecalculateAreas();
            DrawHeader();
            DrawConsole();
            DrawCommandLineArea();
        }

        private readonly string defaultSource = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;

namespace ModTools
{{
    class ModToolsCommandLineRunner : IModEntryPoint
    {{
        public void OnModLoaded()
        {{
            {0}
        }}

        public void OnModUnloaded()
        {{
        }}
    }}
}}";
    }
}