using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeneaGrab.Core.Models;

/// <summary>Registry provider</summary>
public abstract class Provider : IEquatable<Provider>
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static readonly string DefaultUserAgent =
        "Mozilla/5.0 (X11; Linux x86_64; rv:140.0) Gecko/20100101 Firefox/140.0";

    public abstract string Id { get; }
    public abstract string Url { get; }
    protected HttpClient HttpClient { get; }
    public override string ToString() => Id;

    protected Provider(HttpClient client = null)
    {
        HttpClient = client ?? GenerateHttpClient();
    }

    protected static HttpClient GenerateHttpClient() => new()
    {
        Timeout = DefaultTimeout,
        DefaultRequestHeaders =
        {
            { "User-Agent", DefaultUserAgent }
        }
    };


    public abstract Task<RegistryInfo> GetRegistryFromUrlAsync(Uri url);
    public abstract Task<(Registry registry, int pageNumber)> Infos(Uri url);
    public abstract Task<Stream> GetFrame(Frame page, Scale scale, Action<Progress> progress);
    public virtual Task<string> Ark(Frame page) => Task.FromResult(page.ArkUrl);


    public bool Equals(Provider other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as Provider);
    public static bool operator ==(Provider one, Provider two) => one?.Id == two?.Id;
    public static bool operator !=(Provider one, Provider two) => !(one == two);
    public override int GetHashCode() => Id.GetHashCode();

    public bool NeedsAuthentication(string method)
    {
        var property = GetType().GetMethod(method);
        return property is not null && Attribute.IsDefined(property, typeof(AuthentificationNeededAttribute));
    }
}
