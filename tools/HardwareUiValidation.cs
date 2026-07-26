using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExtLume;

namespace ExtLume.Tools
{
    internal static class HardwareUiValidation
    {
        private static int exitCode = 1;

        [STAThread]
        private static int Main(string[] args)
        {
            string expectedModel = GetArgument(args, "--model=");
            bool captureOnly = HasArgument(args, "--capture-only");
            int testPercent = 0;
            int restorePercent = 0;
            if (String.IsNullOrEmpty(expectedModel)
                || (!captureOnly
                    && (!Int32.TryParse(
                        GetArgument(args, "--test-percent="),
                        out testPercent)
                        || !Int32.TryParse(
                            GetArgument(args, "--restore-percent="),
                            out restorePercent))))
            {
                Console.Error.WriteLine(
                    "Usage: --model=<exact model> "
                    + "(--capture-only | --test-percent=<0-100> "
                    + "--restore-percent=<0-100>)");
                return 2;
            }

            testPercent = captureOnly ? 0 : testPercent;
            restorePercent = captureOnly ? 0 : restorePercent;
            testPercent = BrightnessMath.ClampPercent(testPercent);
            restorePercent = BrightnessMath.ClampPercent(restorePercent);
            string capturePath = GetArgument(args, "--capture=");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm form = new MainForm(null, false, "en");
            form.Location = new Point(-30000, -30000);
            form.Opacity = 0.01;
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Shown += async delegate
            {
                try
                {
                    await RunValidationAsync(
                        form,
                        expectedModel,
                        testPercent,
                        restorePercent,
                        capturePath,
                        captureOnly);
                    exitCode = 0;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception.ToString());
                    if (!captureOnly)
                    {
                        RestoreDirectly(expectedModel, restorePercent);
                    }

                    exitCode = 3;
                }
                finally
                {
                    form.Dispose();
                    Application.ExitThread();
                }
            };
            Application.Run(form);
            return exitCode;
        }

        private static async Task RunValidationAsync(
            MainForm form,
            string expectedModel,
            int testPercent,
            int restorePercent,
            string capturePath,
            bool captureOnly)
        {
            MonitorCard card = null;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                card = FindControl<MonitorCard>(form);
                if (card != null)
                {
                    break;
                }

                await Task.Delay(200);
            }

            if (card == null)
            {
                throw new InvalidOperationException(
                    "The live monitor card did not appear.");
            }

            if (!String.Equals(
                    card.Monitor.DisplayName,
                    expectedModel,
                    StringComparison.OrdinalIgnoreCase)
                || !card.Monitor.UsesHardware
                || card.Monitor.Target == null
                || !card.Monitor.Target.IsExternal
                || card.Monitor.Target.IsInternal)
            {
                throw new InvalidOperationException(
                    "The visible card is not the exact safe hardware target.");
            }

            BrightnessSlider slider = FindControl<BrightnessSlider>(card);
            if (slider == null)
            {
                throw new InvalidOperationException(
                    "The custom brightness slider was not found.");
            }

            Console.WriteLine(
                "Visible card before input: "
                + card.Monitor.DisplayName
                + " | "
                + slider.Value
                + "%");
            if (captureOnly)
            {
                VerifyFreshRead(
                    expectedModel,
                    slider.Value,
                    "capture-only");
                if (!String.IsNullOrEmpty(capturePath))
                {
                    CaptureForm(form, capturePath);
                }

                return;
            }

            int observedPercent = -1;
            card.BrightnessRequested += delegate(
                object sender,
                BrightnessRequestEventArgs eventArgs)
            {
                observedPercent = eventArgs.Percent;
            };

            ClickSlider(slider, testPercent);
            await WaitForApplyAsync(
                slider,
                delegate { return observedPercent; },
                testPercent);
            VerifyFreshRead(expectedModel, testPercent, "test");

            observedPercent = -1;
            ClickSlider(slider, restorePercent);
            await WaitForApplyAsync(
                slider,
                delegate { return observedPercent; },
                restorePercent);
            VerifyFreshRead(expectedModel, restorePercent, "restore");
            if (!String.IsNullOrEmpty(capturePath))
            {
                CaptureForm(form, capturePath);
            }

            Console.WriteLine(
                "UI chain verified; final brightness "
                + restorePercent
                + "%.");
        }

        private static void CaptureForm(Form form, string capturePath)
        {
            string fullPath = Path.GetFullPath(capturePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (Bitmap bitmap = new Bitmap(
                form.Width,
                form.Height))
            {
                form.DrawToBitmap(
                    bitmap,
                    new Rectangle(Point.Empty, form.Size));
                bitmap.Save(
                    fullPath,
                    System.Drawing.Imaging.ImageFormat.Png);
            }

            Console.WriteLine("Captured live UI: " + fullPath);
        }

        private static async Task WaitForApplyAsync(
            BrightnessSlider slider,
            Func<int> observedPercent,
            int expectedPercent)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                await Task.Delay(150);
                if (observedPercent() == expectedPercent && slider.Enabled)
                {
                    await Task.Delay(250);
                    return;
                }
            }

            throw new InvalidOperationException(
                "Timed out waiting for the UI brightness request.");
        }

        private static void ClickSlider(
            BrightnessSlider slider,
            int requestedPercent)
        {
            int trackWidth = Math.Max(1, slider.ClientSize.Width - 26);
            int x = 13 + (int)Math.Round(
                trackWidth * (requestedPercent / 100.0),
                MidpointRounding.AwayFromZero);
            int y = slider.ClientSize.Height / 2;
            int mappedPercent = slider.ValueFromClientX(x);
            if (mappedPercent != requestedPercent)
            {
                throw new InvalidOperationException(
                    "Slider coordinate mapped to "
                    + mappedPercent
                    + "% instead of "
                    + requestedPercent
                    + "%.");
            }

            MethodInfo mouseDown = typeof(BrightnessSlider).GetMethod(
                "OnMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo mouseUp = typeof(BrightnessSlider).GetMethod(
                "OnMouseUp",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (mouseDown == null || mouseUp == null)
            {
                throw new InvalidOperationException(
                    "Slider mouse handlers were not found.");
            }

            mouseDown.Invoke(
                slider,
                new object[]
                {
                    new MouseEventArgs(
                        MouseButtons.Left,
                        1,
                        x,
                        y,
                        0)
                });
            mouseUp.Invoke(
                slider,
                new object[]
                {
                    new MouseEventArgs(
                        MouseButtons.Left,
                        1,
                        x,
                        y,
                        0)
                });
        }

        private static void VerifyFreshRead(
            string expectedModel,
            int expectedPercent,
            string phase)
        {
            MonitorDescriptor monitor = FindExactMonitor(
                new MonitorManager().Refresh().Monitors,
                expectedModel);
            if (monitor == null
                || !monitor.UsesHardware
                || monitor.CurrentPercent != expectedPercent)
            {
                throw new InvalidOperationException(
                    "Fresh DDC read failed after "
                    + phase
                    + "; expected "
                    + expectedPercent
                    + "%.");
            }

            Console.WriteLine(
                "Fresh read after "
                + phase
                + ": "
                + monitor.CurrentPercent
                + "% via "
                + monitor.ControlKind);
        }

        private static void RestoreDirectly(
            string expectedModel,
            int restorePercent)
        {
            try
            {
                MonitorManager manager = new MonitorManager();
                MonitorDescriptor monitor = FindExactMonitor(
                    manager.Refresh().Monitors,
                    expectedModel);
                if (monitor != null && monitor.UsesHardware)
                {
                    BrightnessResult result = manager.SetHardwareBrightness(
                        monitor,
                        restorePercent);
                    Console.Error.WriteLine(
                        "Direct safety restore: success="
                        + result.Success
                        + " percent="
                        + result.Percent);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(
                    "Direct safety restore failed: "
                    + exception.Message);
            }
        }

        private static MonitorDescriptor FindExactMonitor(
            IList<MonitorDescriptor> monitors,
            string expectedModel)
        {
            MonitorDescriptor match = null;
            for (int index = 0; index < monitors.Count; index++)
            {
                MonitorDescriptor monitor = monitors[index];
                if (monitor.Target != null
                    && monitor.Target.IsExternal
                    && !monitor.Target.IsInternal
                    && String.Equals(
                        monitor.DisplayName,
                        expectedModel,
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (match != null)
                    {
                        return null;
                    }

                    match = monitor;
                }
            }

            return match;
        }

        private static T FindControl<T>(Control parent)
            where T : Control
        {
            for (int index = 0; index < parent.Controls.Count; index++)
            {
                T match = parent.Controls[index] as T;
                if (match != null)
                {
                    return match;
                }

                match = FindControl<T>(parent.Controls[index]);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static string GetArgument(string[] args, string prefix)
        {
            if (args == null)
            {
                return String.Empty;
            }

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] != null
                    && args[index].StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return args[index].Substring(prefix.Length);
                }
            }

            return String.Empty;
        }

        private static bool HasArgument(string[] args, string expected)
        {
            if (args == null)
            {
                return false;
            }

            for (int index = 0; index < args.Length; index++)
            {
                if (String.Equals(
                    args[index],
                    expected,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
