using System.IO;
// ReSharper disable ConvertToUsingDeclaration

namespace IctBaden.Config.Test
{
    public class TestResources
    {
        public static string LoadResourceString(string name)
        {
            string text;
            var assembly = typeof(TestResources).Assembly;

            using (var stream = assembly.GetManifestResourceStream($"IctBaden.Config.Test.{name}"))
            {
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }
            }
            return text;
        }
    }
}
