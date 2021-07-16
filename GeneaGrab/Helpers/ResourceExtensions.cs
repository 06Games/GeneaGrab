using Windows.ApplicationModel.Resources;

namespace GeneaGrab.Helpers
{
    public enum Resource { Res, Core }
    internal static class ResourceExtensions
    {
        private static ResourceLoader res = new ResourceLoader("Resources");
        private static ResourceLoader core = new ResourceLoader("Core");

        public static string GetLocalized(this string resourceKey) => GetLocalized(Resource.Res, resourceKey);
        public static string GetLocalized(Resource view, string resourceKey)
        {
            ResourceLoader loader = view == Resource.Core ? core : res;
            return loader.GetString(resourceKey);
        }
    }
}
