using System;
using System.Threading;
using System.Windows.Forms;

namespace ExtLume
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, AppInfo.MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    SignalExistingInstance();
                    return 0;
                }

                bool createdShowEvent;
                using (EventWaitHandle showEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    AppInfo.ShowEventName,
                    out createdShowEvent))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.SetUnhandledExceptionMode(
                        UnhandledExceptionMode.CatchException);
                    Application.ThreadException += ApplicationThreadException;

                    bool startHidden = HasArgument(args, "--startup")
                        || HasArgument(args, "--minimized");
                    Application.Run(
                        new MainForm(
                            showEvent,
                            startHidden,
                            GetLanguageOverride(args)));
                }
            }

            return 0;
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

        private static string GetLanguageOverride(string[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int index = 0; index < args.Length; index++)
            {
                const string prefix = "--language=";
                if (args[index] != null
                    && args[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string value = args[index].Substring(prefix.Length).Trim();
                    if (String.Equals(value, "en", StringComparison.OrdinalIgnoreCase)
                        || value.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static void SignalExistingInstance()
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    using (EventWaitHandle showEvent =
                        EventWaitHandle.OpenExisting(AppInfo.ShowEventName))
                    {
                        showEvent.Set();
                        return;
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    Thread.Sleep(80);
                }
            }
        }

        private static void ApplicationThreadException(
            object sender,
            ThreadExceptionEventArgs eventArgs)
        {
            UiText text = new UiText();
            MessageBox.Show(
                text.UnexpectedError,
                text.WindowTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
