using System;
using System.IO;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;

namespace IctBaden.Config.Converter
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Config Converter");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: IctBaden.Config.Converter SourceFileName");
                return;
            }

            var srcFileName = args[0];
            if (!File.Exists(srcFileName))
            {
                Console.WriteLine($"Source file '{srcFileName}' does not exist.");
                return;
            }

            var fromXaml = srcFileName.Contains("xaml", StringComparison.InvariantCultureIgnoreCase);
            var toXaml = !fromXaml;

            static string Format(bool isXaml) => isXaml ? "XAML" : "JSON";

            var dstFileName = srcFileName.Replace(Format(fromXaml), Format(toXaml).ToLower(), StringComparison.InvariantCultureIgnoreCase);

            Console.WriteLine($"Converting {srcFileName}"); 
            Console.WriteLine($"Source format {Format(fromXaml)}");
            Console.WriteLine($"Destination format {Format(toXaml)}");
            Console.WriteLine($"Output {dstFileName}");

            var srcText = File.ReadAllText(srcFileName);

            var session = new ConfigurationSession();
            var jsonSerializer = new ConfigurationNamespaceJsonSerializer(session);
            var xmlSerializer = new ConfigurationNamespaceXamlSerializer(session);
            var root = fromXaml
                ? xmlSerializer.Load(new StringReader(srcText))
                : jsonSerializer.Load(new StringReader(srcText));

            using (var wrt = new StreamWriter(dstFileName))
            {
                if (toXaml)
                    xmlSerializer.Save(root, wrt);
                else
                    jsonSerializer.Save(root, wrt);
            }

            Console.WriteLine("done.");
        }
    }
}
