﻿using System;

namespace IctBaden.Config.Namespace
{
    public class NamespaceProviderFactory
    {
        //TODO: reflect classes

        public static NamespaceProvider Create(string namespaceUri)
        {
            var parts = namespaceUri.Split(new[] { "://" }, StringSplitOptions.None);
            if (parts.Length != 2)
                return null;

            var scheme = parts[0];
            var specification = parts[1];

            switch (scheme)
            {
                case "db":
                    return new NamespaceProviderDatabase(specification);
                case "file":
                    return new NamespaceProviderProfile(specification);
                case "memory":
                    return new NamespaceProviderMemory(specification);
                case "schema":
                    return new NamespaceProviderSchema(specification);
            }
            throw new ArgumentException("Unknown NamespaceProvider scheme: " + scheme);
        }
    }
}