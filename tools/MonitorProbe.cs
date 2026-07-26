using System;
using ExtLume;

namespace ExtLume.Tools
{
    internal static class MonitorProbe
    {
        [STAThread]
        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("ExtLume - read-only probe");
            Console.WriteLine("Display path struct: {0} bytes", InteropLayout.DisplayPathSize);
            Console.WriteLine("Source name struct: {0} bytes", InteropLayout.SourceNameSize);
            Console.WriteLine("Target name struct: {0} bytes", InteropLayout.TargetNameSize);

            DisplayDiscoveryResult discovery =
                new DisplayDiscoveryService().DiscoverActiveTargets();
            Console.WriteLine("Active targets: {0}", discovery.Targets.Count);

            int externalCount = 0;
            for (int index = 0; index < discovery.Targets.Count; index++)
            {
                DisplayTarget target = discovery.Targets[index];
                if (target.IsExternal)
                {
                    externalCount++;
                }

                Console.WriteLine(
                    "- {0} | {1} | {2} | {3}",
                    target.FriendlyName,
                    MonitorClassifier.OutputTechnologyName(target.OutputTechnology),
                    target.IsInternal ? "internal" : (target.IsExternal ? "external" : "unclassified"),
                    target.IsVirtual ? "virtual" : "physical");
            }

            Console.WriteLine("External targets: {0}", externalCount);
            Console.WriteLine("Warnings: {0}", discovery.Warnings.Count);

            if (InteropLayout.DisplayPathSize != 72
                || InteropLayout.SourceNameSize != 84
                || InteropLayout.TargetNameSize != 420)
            {
                Console.Error.WriteLine("Interop layout validation failed.");
                return 2;
            }

            return discovery.Targets.Count > 0 ? 0 : 3;
        }
    }
}
