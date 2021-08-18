using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneaGrab.Activation
{
    public interface ISchemeSupport
    {
        string UrlPath { get; }
        string GetIdFromParameters(Dictionary<string, string> Parameters);
        void Load(Dictionary<string, string> Parameters);
    }

    public static class SchemeActivationConfig
    {
        private static Dictionary<string, (Type, ISchemeSupport)> _activationPages;
        private static Dictionary<string, (Type, ISchemeSupport)> ActivationPages
        {
            get
            {
                if (_activationPages is null)
                    _activationPages = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                        .Where(t => t.GetInterfaces().Contains(typeof(ISchemeSupport)) && t.GetConstructor(Type.EmptyTypes) != null)
                        .Select(t => (t, Activator.CreateInstance(t) as ISchemeSupport))
                        .ToDictionary(t => t.Item2.UrlPath, t => t);
                return _activationPages;
            }
        }

        public static (Type, ISchemeSupport) GetPage(string pageKey) => ActivationPages.GetValueOrDefault(pageKey);
        public static string GetPageKey(Type pageType) => ActivationPages.FirstOrDefault(v => v.Value.Item1 == pageType).Key;
    }
}
