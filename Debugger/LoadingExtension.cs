﻿using ColossalFramework;
using ICities;

namespace ModTools
{
    public sealed class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            CustomPrefabs.Bootstrap();
            var appMode = Singleton<ToolManager>.instance.m_properties.m_mode;
            var modTools = ModTools.Instance;
            if (modTools == null)
            {
                UnityEngine.Debug.LogError("ModTools instance wasn't present");
                return;
            }
            if (modTools.config.ExtendGamePanels && appMode == ItemClass.Availability.Game)
            {
                modTools.gameObject.AddComponent<GamePanelExtender>();
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            CustomPrefabs.Revert();
        }
    }
}