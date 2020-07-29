using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;

// ReSharper disable ConvertToUsingDeclaration

namespace IctBaden.Config.TestApp
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("IctBaden Config TestApp");
            
            string json;
            var assembly = typeof(Program).Assembly;
            using (var stream = assembly.GetManifestResourceStream("IctBaden.Config.TestApp.schema.json"))
            {
                using (var reader = new StreamReader(stream ?? throw new FileNotFoundException()))
                {
                    json = reader.ReadToEnd();
                }
            }
            var session = new ConfigurationSession();
            var jsonSerializer = new ConfigurationNamespaceJsonSerializer(session);
            var schema = jsonSerializer.Load(new StringReader(json));
            
            //var provider = NamespaceProviderFactory.Create("mongo://mongodb:27017");
            var provider = NamespaceProviderFactory.Create("file://C:\\Temp\\Config.cfg");
            session.Namespace.AddChild(schema);
            session.RegisterNamespaceProvider("test", provider);
            
            Console.WriteLine(session.GetNamespaceProvider("test").GetPersistenceInfo());

            const string logPath = "C:\\Temp";
            var unit = session.Namespace.GetUnitById("Logging/LogPath");
            unit.SetValue(logPath);

            var value = unit.GetValue<string>();
            Debug.Assert(value == logPath);
        
            
            var channels = session.Namespace.GetUnitById("Channels");
            var type = session.Namespace.GetUnitById("ChannelVoIP");

            var children = channels.GetUserUnits(null);
            if (children.Count == 0)
            {
                channels.CreateItem(type, "TestVoIP");
            }

            children = channels.GetUserUnits(null);
            Debug.Assert(children.Count == 1);

            var user = children.First();
            user.Delete();
            
            children = channels.GetUserUnits(null);
            Debug.Assert(children.Count == 0);
            
            Console.WriteLine("OK.");
        }
    }
}