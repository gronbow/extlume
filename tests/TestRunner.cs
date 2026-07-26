using System;
using System.Collections.Generic;
using System.Drawing;
using ExtLume;

namespace ExtLume.Tests
{
    internal static class TestRunner
    {
        private static int passed;
        private static int failed;

        [STAThread]
        private static int Main(string[] args)
        {
            Run("ClampPercent", TestClampPercent);
            Run("RawToPercent", TestRawToPercent);
            Run("PercentToRaw", TestPercentToRaw);
            Run("SoftwareOpacity", TestSoftwareOpacity);
            Run("ModelExtraction", TestModelExtraction);
            Run("FriendlyNameFallback", TestFriendlyNameFallback);
            Run("StableId", TestStableId);
            Run("OutputClassification", TestOutputClassification);
            Run("UnknownClassificationSafety", TestUnknownClassificationSafety);
            Run("InteropLayout", TestInteropLayout);
            Run("DdcFallbackSafety", TestDdcFallbackSafety);
            Run("HighLevelHardwareWriteFlow", TestHighLevelHardwareWriteFlow);
            Run("VcpHardwareWriteFlow", TestVcpHardwareWriteFlow);
            Run("HardwareWriteFailures", TestHardwareWriteFailures);
            Run("CloneTopologyGrouping", TestCloneTopologyGrouping);
            Run("PhysicalTargetMapping", TestPhysicalTargetMapping);
            Run("SoftwareOverlayGuards", TestSoftwareOverlayGuards);
            Run("LanguageOverride", TestLanguageOverride);
            if (HasArgument(args, "--skip-live"))
            {
                Console.WriteLine("[SKIP] LiveDisplayDiscoveryReadOnly");
                Console.WriteLine("[SKIP] LiveMonitorRefreshReadOnly");
            }
            else
            {
                Run("LiveDisplayDiscoveryReadOnly", TestLiveDisplayDiscoveryReadOnly);
                Run("LiveMonitorRefreshReadOnly", TestLiveMonitorRefreshReadOnly);
            }

            Console.WriteLine();
            Console.WriteLine("Passed: {0}", passed);
            Console.WriteLine("Failed: {0}", failed);
            return failed == 0 ? 0 : 1;
        }

        private static bool HasArgument(string[] args, string expected)
        {
            if (args == null)
            {
                return false;
            }

            for (int index = 0; index < args.Length; index++)
            {
                if (String.Equals(args[index], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("[PASS] {0}", name);
            }
            catch (Exception exception)
            {
                failed++;
                Console.WriteLine("[FAIL] {0}: {1}", name, exception.Message);
            }
        }

        private static void TestClampPercent()
        {
            Equal(0, BrightnessMath.ClampPercent(-20), "negative");
            Equal(0, BrightnessMath.ClampPercent(0), "zero");
            Equal(35, BrightnessMath.ClampPercent(35), "middle");
            Equal(100, BrightnessMath.ClampPercent(140), "above maximum");
        }

        private static void TestRawToPercent()
        {
            Equal(0, BrightnessMath.RawToPercent(20, 220, 20), "minimum");
            Equal(50, BrightnessMath.RawToPercent(20, 220, 120), "midpoint");
            Equal(100, BrightnessMath.RawToPercent(20, 220, 220), "maximum");
            Equal(0, BrightnessMath.RawToPercent(20, 220, 0), "below range");
            Equal(100, BrightnessMath.RawToPercent(20, 220, 400), "above range");
            Equal(0, BrightnessMath.RawToPercent(100, 100, 100), "invalid range");
        }

        private static void TestPercentToRaw()
        {
            Equal(20U, BrightnessMath.PercentToRaw(20, 220, 0), "minimum");
            Equal(120U, BrightnessMath.PercentToRaw(20, 220, 50), "midpoint");
            Equal(220U, BrightnessMath.PercentToRaw(20, 220, 100), "maximum");
            Equal(35U, BrightnessMath.PercentToRaw(0, 100, 35), "standard monitor");
            Equal(255U, BrightnessMath.PercentToRaw(0, 255, 100), "non-100 maximum");
        }

        private static void TestSoftwareOpacity()
        {
            Equal(0.0, BrightnessMath.SoftwareOpacity(100), "fully bright");
            Equal(0.425, BrightnessMath.SoftwareOpacity(50), "half");
            Equal(0.85, BrightnessMath.SoftwareOpacity(0), "recoverable minimum");
        }

        private static void TestModelExtraction()
        {
            Equal(
                "H25T7-3",
                MonitorIdentity.ExtractModelCode(@"\\?\DISPLAY#H25T7-3#5&123&0&UID1"),
                "display-config path");
            Equal(
                "EDO4179",
                MonitorIdentity.ExtractModelCode(@"DISPLAY\EDO4179\4&ABC&0&UID0_0"),
                "WMI path");
        }

        private static void TestFriendlyNameFallback()
        {
            Equal(
                "H25T7-3",
                MonitorIdentity.NormalizeFriendlyName(
                    "Generic PnP Monitor",
                    @"\\?\DISPLAY#H25T7-3#1"),
                "generic name");
            Equal(
                "Studio Display",
                MonitorIdentity.NormalizeFriendlyName(
                    "Studio Display",
                    @"\\?\DISPLAY#ABC123#1"),
                "EDID friendly name");
        }

        private static void TestStableId()
        {
            string first = MonitorIdentity.StableId("private-monitor-path");
            string second = MonitorIdentity.StableId("private-monitor-path");
            Equal(first, second, "deterministic");
            Equal(24, first.Length, "length");
            True(first.IndexOf("private", StringComparison.OrdinalIgnoreCase) < 0, "redacted");
        }

        private static void TestOutputClassification()
        {
            True(MonitorClassifier.IsInternalOutput(0x80000000), "internal");
            True(MonitorClassifier.IsInternalOutput(11), "embedded DisplayPort");
            True(MonitorClassifier.IsKnownExternalOutput(5), "HDMI");
            True(MonitorClassifier.IsKnownExternalOutput(10), "external DisplayPort");
            True(MonitorClassifier.IsKnownExternalOutput(16), "DisplayLink or indirect wired");
            True(MonitorClassifier.IsVirtualOutput(17), "indirect virtual");
            False(MonitorClassifier.IsKnownExternalOutput(17), "virtual is not external");
        }

        private static void TestUnknownClassificationSafety()
        {
            List<DisplayTarget> singleUnknown = new List<DisplayTarget>();
            singleUnknown.Add(new DisplayTarget
            {
                OutputTechnology = MonitorClassifier.OutputOther,
                Bounds = new Rectangle(0, 0, 1920, 1080)
            });
            MonitorClassifier.ClassifyTargets(singleUnknown);
            False(singleUnknown[0].IsExternal, "single unknown target remains unclassified");

            List<DisplayTarget> laptopTopology = new List<DisplayTarget>();
            laptopTopology.Add(new DisplayTarget
            {
                OutputTechnology = MonitorClassifier.OutputInternal,
                Bounds = new Rectangle(0, 0, 1920, 1080)
            });
            laptopTopology.Add(new DisplayTarget
            {
                OutputTechnology = MonitorClassifier.OutputOther,
                Bounds = new Rectangle(1920, 0, 1920, 1080)
            });
            MonitorClassifier.ClassifyTargets(laptopTopology);
            True(laptopTopology[0].IsInternal, "known internal remains internal");
            True(laptopTopology[1].IsExternal, "second physical target can be external");
        }

        private static void TestInteropLayout()
        {
            Equal(72, InteropLayout.DisplayPathSize, "DISPLAYCONFIG_PATH_INFO");
            Equal(84, InteropLayout.SourceNameSize, "DISPLAYCONFIG_SOURCE_DEVICE_NAME");
            Equal(420, InteropLayout.TargetNameSize, "DISPLAYCONFIG_TARGET_DEVICE_NAME");
            Equal(64, InteropLayout.DisplayModeBufferElementSize, "DISPLAYCONFIG_MODE_INFO");
        }

        private static void TestDdcFallbackSafety()
        {
            DdcBrightnessService service = new DdcBrightnessService();
            DisplayTarget internalTarget = new DisplayTarget
            {
                Id = "internal",
                DeviceName = @"\\.\DISPLAY999",
                FriendlyName = "Internal panel",
                IsInternal = true,
                IsExternal = false,
                Bounds = new Rectangle(0, 0, 100, 100)
            };
            Equal(0, service.Probe(internalTarget).Count, "internal display is never probed");

            DisplayTarget externalTarget = new DisplayTarget
            {
                Id = "external",
                DeviceName = @"\\.\DISPLAY999",
                FriendlyName = "Test monitor",
                IsInternal = false,
                IsExternal = true,
                Bounds = new Rectangle(100, 0, 100, 100)
            };
            List<MonitorDescriptor> descriptors = service.Probe(externalTarget);
            Equal(1, descriptors.Count, "fallback descriptor");
            Equal(
                BrightnessControlKind.SoftwareDimming,
                descriptors[0].ControlKind,
                "software fallback");
            False(descriptors[0].UsesHardware, "fallback does not claim hardware");
            Equal(
                BrightnessError.ControlUnavailable,
                service.SetBrightness(descriptors[0], 25).Error,
                "software descriptor cannot invoke DDC write");
        }

        private static void TestHighLevelHardwareWriteFlow()
        {
            FakeMonitorAdapter adapter = new FakeMonitorAdapter();
            adapter.HighLevelAvailable = true;
            adapter.Minimum = 20;
            adapter.Maximum = 220;
            adapter.Current = 120;

            BrightnessResult result =
                DdcBrightnessService.SetHighLevel(adapter, 25);
            True(result.Success, "high-level write succeeds");
            Equal(70U, adapter.LastHighLevelSet, "non-zero minimum is respected");
            Equal(2, adapter.HighLevelReadCount, "write is verified by readback");
            Equal(25, result.Percent, "verified percent");
            Equal(0, adapter.VcpSetCount, "VCP fallback was not used");
        }

        private static void TestVcpHardwareWriteFlow()
        {
            FakeMonitorAdapter adapter = new FakeMonitorAdapter();
            adapter.VcpAvailable = true;
            adapter.Maximum = 255;
            adapter.Current = 128;

            BrightnessResult result =
                DdcBrightnessService.SetVcp(adapter, 25);
            True(result.Success, "VCP write succeeds");
            Equal(64U, adapter.LastVcpSet, "non-100 VCP maximum is respected");
            Equal(2, adapter.VcpReadCount, "VCP write is verified by readback");
            Equal(25, result.Percent, "verified VCP percent");
            Equal(0, adapter.HighLevelSetCount, "high-level path was not used");
        }

        private static void TestHardwareWriteFailures()
        {
            FakeMonitorAdapter writeFailure = new FakeMonitorAdapter();
            writeFailure.HighLevelAvailable = true;
            writeFailure.Minimum = 0;
            writeFailure.Maximum = 100;
            writeFailure.Current = 50;
            writeFailure.FailHighLevelSet = true;
            writeFailure.LastErrorValue = 5;
            BrightnessResult failedWrite =
                DdcBrightnessService.SetHighLevel(writeFailure, 25);
            Equal(BrightnessError.WriteFailed, failedWrite.Error, "write failure");
            Equal(5, failedWrite.NativeError, "native write error");

            FakeMonitorAdapter vcpWriteFailure = new FakeMonitorAdapter();
            vcpWriteFailure.VcpAvailable = true;
            vcpWriteFailure.Maximum = 100;
            vcpWriteFailure.Current = 50;
            vcpWriteFailure.FailVcpSet = true;
            vcpWriteFailure.LastErrorValue = 87;
            BrightnessResult failedVcpWrite =
                DdcBrightnessService.SetVcp(vcpWriteFailure, 25);
            Equal(
                BrightnessError.WriteFailed,
                failedVcpWrite.Error,
                "VCP write failure");
            Equal(87, failedVcpWrite.NativeError, "native VCP write error");

            FakeMonitorAdapter readbackFailure = new FakeMonitorAdapter();
            readbackFailure.VcpAvailable = true;
            readbackFailure.Maximum = 100;
            readbackFailure.Current = 50;
            readbackFailure.FailReadback = true;
            readbackFailure.LastErrorValue = 31;
            BrightnessResult failedReadback =
                DdcBrightnessService.SetVcp(readbackFailure, 25);
            Equal(BrightnessError.ReadFailed, failedReadback.Error, "readback failure");
            Equal(31, failedReadback.NativeError, "native readback error");
        }

        private static void TestSoftwareOverlayGuards()
        {
            using (SoftwareDimmingService service = new SoftwareDimmingService())
            {
                service.SetLevel(
                    new DisplayTarget
                    {
                        Id = "internal",
                        IsInternal = true,
                        IsExternal = false,
                        Bounds = new Rectangle(0, 0, 1, 1)
                    },
                    50);
                Equal(0, service.ActiveOverlayCount, "internal display guard");

                service.SetLevel(
                    new DisplayTarget
                    {
                        Id = "virtual",
                        IsExternal = true,
                        IsVirtual = true,
                        Bounds = new Rectangle(0, 0, 1, 1)
                    },
                    50);
                Equal(0, service.ActiveOverlayCount, "virtual display guard");

                DisplayTarget mirroredExternal = new DisplayTarget
                {
                    Id = "mirrored-external",
                    IsExternal = true,
                    SharesSourceWithInternal = true,
                    Bounds = new Rectangle(0, 0, 1, 1)
                };
                service.SetLevel(mirroredExternal, 50);
                Equal(
                    0,
                    service.ActiveOverlayCount,
                    "internal-clone overlay guard");
                False(
                    SoftwareDimmingService.IsSafeTarget(mirroredExternal),
                    "internal clone is never a safe software target");
            }
        }

        private static void TestLanguageOverride()
        {
            UiText english = new UiText("en");
            UiText chinese = new UiText("zh-CN");
            False(english.IsChinese, "English override");
            True(chinese.IsChinese, "Chinese override");
            True(
                english.NoExternalDisplay.IndexOf("external", StringComparison.OrdinalIgnoreCase) >= 0,
                "English resource");
            True(
                chinese.NoExternalDisplay.IndexOf("外接", StringComparison.Ordinal) >= 0,
                "Chinese resource");
            True(
                english.DuplicateModeProtected.IndexOf(
                    "protected",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "duplicate-mode safety resource");
        }

        private static void TestCloneTopologyGrouping()
        {
            List<DisplayTarget> targets = new List<DisplayTarget>();
            targets.Add(new DisplayTarget
            {
                Id = "internal",
                DeviceName = @"\\.\DISPLAY1",
                IsInternal = true,
                IsExternal = false,
                Bounds = new Rectangle(0, 0, 1920, 1080)
            });
            targets.Add(new DisplayTarget
            {
                Id = "external-a",
                DeviceName = @"\\.\DISPLAY2",
                FriendlyName = "H25T7-3",
                IsExternal = true,
                Bounds = new Rectangle(1920, 0, 1920, 1080)
            });
            targets.Add(new DisplayTarget
            {
                Id = "external-b",
                DeviceName = @"\\.\DISPLAY2",
                FriendlyName = "DELL U2723QE",
                IsExternal = true,
                Bounds = new Rectangle(1920, 0, 1920, 1080)
            });

            List<List<DisplayTarget>> groups =
                MonitorManager.GroupExternalTargets(targets);
            Equal(1, groups.Count, "one cloned logical source");
            Equal(2, groups[0].Count, "both physical targets retained");
            False(
                groups[0][0].SharesSourceWithInternal,
                "external-only clone allows software dimming");

            MonitorManager manager = new MonitorManager();
            MonitorDescriptor fallback =
                manager.CreateSoftwareDescriptorGroup(groups[0]);
            True(
                fallback.DisplayName.IndexOf("H25T7-3", StringComparison.Ordinal) >= 0,
                "first model in group name");
            True(
                fallback.DisplayName.IndexOf("DELL U2723QE", StringComparison.Ordinal) >= 0,
                "second model in group name");

            List<DisplayTarget> mirroredWithInternal =
                new List<DisplayTarget>();
            mirroredWithInternal.Add(new DisplayTarget
            {
                Id = "mirrored-internal",
                DeviceName = @"\\.\DISPLAY3",
                IsInternal = true,
                Bounds = new Rectangle(0, 0, 1920, 1080)
            });
            mirroredWithInternal.Add(new DisplayTarget
            {
                Id = "mirrored-external",
                DeviceName = @"\\.\DISPLAY3",
                FriendlyName = "External monitor",
                IsExternal = true,
                Bounds = new Rectangle(0, 0, 1920, 1080)
            });
            List<List<DisplayTarget>> protectedGroups =
                MonitorManager.GroupExternalTargets(mirroredWithInternal);
            Equal(1, protectedGroups.Count, "mirrored external is detected");
            True(
                protectedGroups[0][0].SharesSourceWithInternal,
                "internal clone is marked");
            False(
                SoftwareDimmingService.IsSafeGroup(protectedGroups[0]),
                "software fallback is blocked for an internal clone");
            True(
                manager.CreateSoftwareDescriptorGroup(protectedGroups[0]) == null,
                "unsafe fallback descriptor is not created");
        }

        private static void TestPhysicalTargetMapping()
        {
            List<DisplayTarget> targets = new List<DisplayTarget>();
            targets.Add(new DisplayTarget
            {
                FriendlyName = "H25T7-3",
                DevicePath = @"\\?\DISPLAY#H25T7-3#A"
            });
            targets.Add(new DisplayTarget
            {
                FriendlyName = "DELL U2723QE",
                DevicePath = @"\\?\DISPLAY#DEL41B0#B"
            });

            int[] reordered = DdcBrightnessService.MapPhysicalTargets(
                new string[]
                {
                    "Dell U2723QE (DisplayPort)",
                    "H25T7-3"
                },
                targets);
            Equal(
                1,
                reordered[0],
                "friendly-name match beats position");
            Equal(
                0,
                reordered[1],
                "description match beats position");

            int[] partial = DdcBrightnessService.MapPhysicalTargets(
                new string[] { "H25T7-3", String.Empty },
                targets);
            Equal(
                1,
                partial[1],
                "remaining equal-count target is mapped safely");

            int[] positional = DdcBrightnessService.MapPhysicalTargets(
                new string[] { String.Empty, String.Empty },
                targets);
            Equal(
                1,
                positional[1],
                "equal-count positional fallback");

            int[] unequal = DdcBrightnessService.MapPhysicalTargets(
                new string[] { String.Empty },
                targets);
            Equal(
                -1,
                unequal[0],
                "unsafe unequal-count mapping rejected");

            int[] uniqueUnequal = DdcBrightnessService.MapPhysicalTargets(
                new string[] { "Dell U2723QE" },
                targets);
            Equal(
                1,
                uniqueUnequal[0],
                "unique name remains safe when counts differ");

            List<DisplayTarget> internalCloneTargets =
                new List<DisplayTarget>();
            internalCloneTargets.Add(new DisplayTarget
            {
                FriendlyName = "H25T7-3",
                DevicePath = @"\\?\DISPLAY#H25T7-3#A",
                SharesSourceWithInternal = true
            });
            int[] protectedPositional = DdcBrightnessService.MapPhysicalTargets(
                new string[] { String.Empty },
                internalCloneTargets);
            Equal(
                -1,
                protectedPositional[0],
                "internal clone never uses positional hardware mapping");
            int[] protectedNamed = DdcBrightnessService.MapPhysicalTargets(
                new string[] { "H25T7-3" },
                internalCloneTargets);
            Equal(
                0,
                protectedNamed[0],
                "internal clone permits unique model mapping");
        }

        private static void TestLiveDisplayDiscoveryReadOnly()
        {
            DisplayDiscoveryResult result =
                new DisplayDiscoveryService().DiscoverActiveTargets();
            True(result.Targets.Count > 0, "at least one active display");

            for (int index = 0; index < result.Targets.Count; index++)
            {
                DisplayTarget target = result.Targets[index];
                True(!String.IsNullOrEmpty(target.Id), "stable id");
                True(!String.IsNullOrEmpty(target.DeviceName), "GDI device");
                True(!String.IsNullOrEmpty(target.FriendlyName), "friendly name");
                True(!(target.IsInternal && target.IsExternal), "exclusive classification");
                True(!(target.IsVirtual && target.IsExternal), "virtual display excluded");
            }
        }

        private static void TestLiveMonitorRefreshReadOnly()
        {
            MonitorRefreshResult result = new MonitorManager().Refresh();
            True(result.ActiveDisplayCount > 0, "active display count");
            True(result.ExternalDisplayCount >= 0, "external count");
            for (int index = 0; index < result.Monitors.Count; index++)
            {
                MonitorDescriptor monitor = result.Monitors[index];
                True(monitor.Target.IsExternal, "only external targets become controls");
                True(!monitor.Target.IsInternal, "internal targets are excluded");
                True(monitor.CurrentPercent >= 0 && monitor.CurrentPercent <= 100, "percent range");
            }
        }

        private static void True(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void False(bool value, string message)
        {
            True(!value, message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message + " (expected " + expected + ", got " + actual + ")");
            }
        }

        private sealed class FakeMonitorAdapter : IMonitorHardwareAdapter
        {
            internal bool HighLevelAvailable;
            internal bool VcpAvailable;
            internal bool FailHighLevelSet;
            internal bool FailVcpSet;
            internal bool FailReadback;
            internal int LastErrorValue;
            internal uint Minimum;
            internal uint Current;
            internal uint Maximum;
            internal uint LastHighLevelSet;
            internal uint LastVcpSet;
            internal int HighLevelReadCount;
            internal int VcpReadCount;
            internal int HighLevelSetCount;
            internal int VcpSetCount;

            public int LastError
            {
                get { return LastErrorValue; }
            }

            public bool TryReadHighLevel(
                out uint minimum,
                out uint current,
                out uint maximum)
            {
                HighLevelReadCount++;
                minimum = Minimum;
                current = Current;
                maximum = Maximum;
                if (!HighLevelAvailable)
                {
                    return false;
                }

                return !(FailReadback && HighLevelReadCount > 1);
            }

            public bool SetHighLevel(uint value)
            {
                HighLevelSetCount++;
                LastHighLevelSet = value;
                if (FailHighLevelSet)
                {
                    return false;
                }

                Current = value;
                return true;
            }

            public bool TryReadVcp(out uint current, out uint maximum)
            {
                VcpReadCount++;
                current = Current;
                maximum = Maximum;
                if (!VcpAvailable)
                {
                    return false;
                }

                return !(FailReadback && VcpReadCount > 1);
            }

            public bool SetVcp(uint value)
            {
                VcpSetCount++;
                LastVcpSet = value;
                if (FailVcpSet)
                {
                    return false;
                }

                Current = value;
                return true;
            }
        }
    }
}
