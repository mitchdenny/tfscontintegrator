using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ContinuousIntegrator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].ToLower() == "/console")
            {
                ChangesetWatcher watcher = new ChangesetWatcher();
                watcher.Start();

                Console.WriteLine("Press ENTER to stop Continuous Integrator.");
                Console.ReadLine();

                watcher.Stop();
            }
            else
            {
                ContinuousIntegratorService service = new ContinuousIntegratorService();
                ContinuousIntegratorService.Run(service);
            }
        }
    }
}
