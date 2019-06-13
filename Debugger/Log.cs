﻿using UnityEngine;

namespace ModTools
{
    internal static class Log
    {
        private static readonly object syncObject = new object();
        private static ILogger customLogger;

        public static void SetCustomLogger(ILogger logger)
        {
            lock (syncObject)
            {
                customLogger = logger;
            }
        }

        public static void Message(string s)
        {
            ILogger logger;
            lock (syncObject)
            {
                logger = customLogger;
            }

            if (logger != null)
            {
                logger.Log(s, LogType.Log);
            }
            else
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, s);
            }
        }

        public static void Error(string s)
        {
            ILogger logger;
            lock (syncObject)
            {
                logger = customLogger;
            }

            if (logger != null)
            {
                logger.Log(s, LogType.Error);
            }
            else
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Error, s);
            }
        }

        public static void Warning(string s)
        {
            ILogger logger;
            lock (syncObject)
            {
                logger = customLogger;
            }

            if (logger != null)
            {
                logger.Log(s, LogType.Warning);
            }
            else
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Warning, s);
            }
        }
    }
}