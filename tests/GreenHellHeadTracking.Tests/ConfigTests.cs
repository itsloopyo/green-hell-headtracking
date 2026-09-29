// SPDX-License-Identifier: MIT
// Copyright (c) 2026 itsloopyo
using System;
using System.IO;
using System.Text;
using CameraUnlock.Core.Config;
using Xunit;

namespace GreenHellHeadTracking.Tests
{
    public sealed class ConfigTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "green-hell-config-" + Guid.NewGuid());

        public ConfigTests() => Directory.CreateDirectory(directory);

        public void Dispose() => Directory.Delete(directory, true);

        private ConfigOwner<HeadTrackingConfigData> Owner() => new(new ConfigOwnerOptions<HeadTrackingConfigData>
        {
            Path = Path.Combine(directory, "CameraUnlock.ini"),
            Table = ModConfig.CreateTable(),
            Header = new RenderHeader("Green Hell"),
            Defaults = DefaultsFile.At(Path.Combine(directory, "Defaults.ini"))
        });

        private void WriteConfig(string replacements = "")
        {
            var bytes = ModConfig.CreateTable().RenderFresh(new RenderHeader("Green Hell"));
            File.WriteAllBytes(Path.Combine(directory, "CameraUnlock.ini"), bytes);
            File.WriteAllText(Path.Combine(directory, "Defaults.ini"),
                "[CameraUnlock]\nConfigFormat=1\n" + replacements);
        }

        [Fact]
        public void RenderConfig()
        {
            var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
            var path = Path.Combine(root, "config/CameraUnlock.ini");
            var bytes = ModConfig.CreateTable().RenderFresh(new RenderHeader("Green Hell"));
            var mode = Environment.GetEnvironmentVariable("CAMERAUNLOCK_RENDER_CONFIG");
            if (mode == "write")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, bytes);
            }
            else
            {
                Assert.True(string.IsNullOrEmpty(mode), "CAMERAUNLOCK_RENDER_CONFIG must be unset or write");
                Assert.Equal(bytes, File.ReadAllBytes(path));
            }
        }

        [Fact]
        public void FirstLaunchUsesExistingDefaults()
        {
            var config = Owner().Load().Config;
            Assert.Equal(4242, config.UdpPort);
            Assert.True(config.EnableOnStartup);
            Assert.True(config.RotationEnabled);
            Assert.True(config.PositionEnabled);
            Assert.False(config.WorldSpaceYaw);
            Assert.Equal(0f, config.LocalSmoothing);
            Assert.Equal(0.15f, config.RemoteSmoothing);
            Assert.Equal(0.3f, config.Position.LimitX);
            Assert.Equal(0.15f, config.Position.LimitY);
            Assert.Equal(0.05f, config.Position.LimitYDown);
            Assert.Equal(0.4f, config.Position.LimitZ);
            Assert.Equal(0.1f, config.Position.LimitZBack);
            if (OperatingSystem.IsWindows())
                Assert.Equal(ModConfig.CreateTable().RenderFresh(new RenderHeader("Green Hell")),
                    File.ReadAllBytes(Path.Combine(directory, "CameraUnlock.ini")));
        }

        [Fact]
        public void SharedDefaultsReachSupportedSettingsAndPreserveGameDefaults()
        {
            WriteConfig("[Network]\nUdpPort=5252\n[General]\nEnableOnStartup=false\nWorldSpaceYaw=true\nRotationEnabled=false\n[Smoothing]\nLocalSmoothing=0.1\nRemoteSmoothing=0.3\n" +
                "[Position]\nPositionEnabled=true\nPositionLimitX=0.45\nPositionLimitY=0.8\nPositionLimitYDown=0.7\nPositionLimitZ=0.6\nPositionLimitZBack=0.2\n" +
                "[Hotkeys]\nToggleKey=F8\nCycleTrackingModeKey=F9\nYawModeKey=F10\n");
            var loaded = Owner().Load();
            var config = loaded.Config;
            Assert.Equal(5252, config.UdpPort);
            Assert.False(config.EnableOnStartup);
            Assert.False(config.WorldSpaceYaw);
            Assert.False(config.RotationEnabled);
            Assert.True(config.PositionEnabled);
            Assert.Equal(0.1f, config.LocalSmoothing);
            Assert.Equal(0.3f, config.RemoteSmoothing);
            Assert.Equal(0.45f, config.Position.LimitX);
            Assert.Equal(0.15f, config.Position.LimitY);
            Assert.Equal(0.05f, config.Position.LimitYDown);
            Assert.Equal(0.6f, config.Position.LimitZ);
            Assert.Equal(0.2f, config.Position.LimitZBack);
            Assert.Equal("F8", config.ToggleKeyName);
            Assert.Equal("F9", config.CycleTrackingModeKeyName);
            Assert.Equal("F10", config.YawModeKeyName);
        }

        [Fact]
        public void GameOverridesWinAndInvalidValuesAreReported()
        {
            WriteConfig("[Network]\nUdpPort=5252\n");
            var path = Path.Combine(directory, "CameraUnlock.ini");
            var text = File.ReadAllText(path).Replace("UdpPort=default", "UdpPort=6262")
                .Replace("WorldSpaceYaw=false", "WorldSpaceYaw=true")
                .Replace("PositionLimitY=0.15", "PositionLimitY=0.25")
                .Replace("ToggleKey=default", "ToggleKey=NotAKey");
            File.WriteAllText(path, text, new UTF8Encoding(false));
            var loaded = Owner().Load();
            Assert.Equal(6262, loaded.Config.UdpPort);
            Assert.True(loaded.Config.WorldSpaceYaw);
            Assert.Equal(0.25f, loaded.Config.Position.LimitY);
            Assert.Equal("End, Ctrl+Shift+Y", loaded.Config.ToggleKeyName);
            Assert.NotEmpty(loaded.Diagnostics);
        }

        [Fact]
        public void HotkeySavePreservesUnrelatedSettings()
        {
            WriteConfig("[Network]\nUdpPort=5252\n");
            var path = Path.Combine(directory, "CameraUnlock.ini");
            File.AppendAllText(path, "\n; keep this comment\n[Custom]\nUnknown=keep\n");
            var owner = Owner();
            owner.Load();
            var saved = owner.Save(c => { c.RotationEnabled = false; c.PositionEnabled = true; c.WorldSpaceYaw = true; });
            if (!OperatingSystem.IsWindows())
            {
                Assert.NotEqual(ConfigSaveStatus.Saved, saved.Status);
                Assert.Contains("RotationEnabled=default", File.ReadAllText(path));
                return;
            }
            Assert.Equal(ConfigSaveStatus.Saved, saved.Status);
            var config = Owner().Load().Config;
            Assert.False(config.RotationEnabled);
            Assert.True(config.PositionEnabled);
            Assert.True(config.WorldSpaceYaw);
            Assert.Equal(5252, config.UdpPort);
            Assert.Contains("Unknown=keep", File.ReadAllText(path));
            Assert.Contains("; keep this comment", File.ReadAllText(path));
        }
    }
}
