using System.Collections.Generic;

namespace CustomPackages.Package.Extensions
{
    public static class MetaDataCreator
    {
        public static KeyValuePair<string, object> Create(string key, object val) =>
            new(key, val);
    }
}