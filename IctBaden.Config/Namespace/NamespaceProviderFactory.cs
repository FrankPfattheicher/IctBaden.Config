using System;
using Microsoft.Extensions.Logging;

namespace IctBaden.Config.Namespace
{
    public static class NamespaceProviderFactory
    {
        //TODO: reflect classes

        public static NamespaceProvider? Create(ILogger logger, string namespaceUri)
        {
            var parts = namespaceUri.Split(new[] { "://" }, StringSplitOptions.None);
            if (parts.Length != 2)
                return null;

            var scheme = parts[0];
            var specification = parts[1];

            switch (scheme)
            {
                case "sql":
                    return new NamespaceProviderSqlServer(logger, specification);
                case "mongo":
                    return new NamespaceProviderMongoDb(logger, specification);
                case "file":
                    return new NamespaceProviderProfile(logger, specification);
                case "memory":
                    return new NamespaceProviderMemory(logger, specification);
            }
            throw new ArgumentException("Unknown NamespaceProvider scheme: " + scheme);
        }
    }
}