// SPDX-License-Identifier: MIT
// Copyright (c) 2026 itsloopyo
using CameraUnlock.Core.Config;

namespace GreenHellHeadTracking
{
    public static class ModConfig
    {
        public static ConfigTable<HeadTrackingConfigData> CreateTable()
        {
            return HeadTrackingConfigTable.Create(
                    ConfigConcepts.UdpPort, ConfigConcepts.EnableOnStartup,
                    ConfigConcepts.LocalSmoothing, ConfigConcepts.RemoteSmoothing,
                    ConfigConcepts.WorldSpaceYaw, ConfigConcepts.RotationEnabled,
                    ConfigConcepts.PositionEnabled, ConfigConcepts.PositionLimitX,
                    ConfigConcepts.PositionLimitY, ConfigConcepts.PositionLimitYDown,
                    ConfigConcepts.PositionLimitZ, ConfigConcepts.PositionLimitZBack,
                    ConfigConcepts.ToggleKey, ConfigConcepts.CycleTrackingModeKey, ConfigConcepts.YawModeKey)
                .Select(ConfigConcepts.WorldSpaceYaw).PerGame("false").Writable()
                .Select(ConfigConcepts.PositionLimitY).PerGame("0.15")
                .Select(ConfigConcepts.PositionLimitYDown).PerGame("0.05")
                .Select(ConfigConcepts.RotationEnabled).Writable()
                .Select(ConfigConcepts.PositionEnabled).Writable();
        }
    }
}
