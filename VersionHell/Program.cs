using System;

namespace VersionHell
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Give name of root assembly");
                return;
            }

            var root = DependencyNode.Build(args[0]);

            root.Report();

            //root.ReportAll();
        }
    }
}
