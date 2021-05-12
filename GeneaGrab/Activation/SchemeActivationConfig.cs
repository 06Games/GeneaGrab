using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneaGrab.Activation
{
    public static class SchemeActivationConfig
    {
        private static readonly Dictionary<string, Type> _activationPages = new Dictionary<string, Type>
        {
            { "registry", typeof(Views.Registry) }
        };

        public static Type GetPage(string pageKey) => _activationPages.FirstOrDefault(p => p.Key == pageKey).Value;
        public static string GetPageKey(Type pageType) => _activationPages.FirstOrDefault(v => v.Value == pageType).Key;
    }
}
