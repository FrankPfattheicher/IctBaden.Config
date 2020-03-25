using System.IO;
using IctBaden.Config.Unit;

namespace IctBaden.Config.Namespace
{
    public interface IConfigurationNamespaceSerializer
    {
        ConfigurationUnit Load(TextReader reader);
        void Save(ConfigurationUnit rootUnit, TextWriter writer);
    }
}