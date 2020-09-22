using System.IO;

namespace IctBaden.Config.Test
{
    public class TestResources
    {
        public static string LoadResourceString(string name)
        {
            var assembly = typeof(TestResources).Assembly;
            using var stream = assembly.GetManifestResourceStream($"IctBaden.Config.Test.{name}");
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            return text;
        }
    }
}
