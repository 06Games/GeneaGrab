using System;
using System.Net.Http;
using System.Threading.Tasks;
using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Models;

namespace GeneaGrab.Core.Providers;

public class Nice : Bach
{
    public override string Id => "AMNice";
    public override string Url => "https://archives.nicecotedazur.org/";
    protected override string BaseUrl => "https://recherche.archives.nicecotedazur.org";

    public Nice(HttpClient client = null) : base(client)
    {
    }

    public override async Task<RegistryInfo> GetRegistryFromUrlAsync(Uri url)
    {
        if (url.Host != "recherche.archives.nicecotedazur.org" ||
            !url.AbsolutePath.StartsWith("/viewer/series/")) return null;
        return await base.GetRegistryFromUrlAsync(url);
    }

    protected override RegistryType ParseTag(string tag) => tag switch
    {
        "naissance" => RegistryType.Birth,
        "mariage" => RegistryType.Marriage,
        "décès" => RegistryType.Death,
        "publication périodique" => RegistryType.Newspaper,
        "ouvrage imprimé" => RegistryType.Book,
        "carte postale" => RegistryType.Other,
        "document photographique" => RegistryType.Other,
        _ => RegistryType.Unknown
    };
}
