using System;
using System.IO;
using IctBaden.Config.Namespace;

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

            var root = fromXaml
                ? ConfigurationNamespaceXmlSerializer.Load(new StringReader(srcText))
                : ConfigurationNamespaceJsonSerializer.Load(new StringReader(srcText));

            using (var wrt = new StreamWriter(dstFileName))
            {
                if (toXaml)
                    ConfigurationNamespaceXmlSerializer.Save(root, wrt);
                else
                    ConfigurationNamespaceJsonSerializer.Save(root, wrt);
            }

            Console.WriteLine("done.");
        }
    }
}
