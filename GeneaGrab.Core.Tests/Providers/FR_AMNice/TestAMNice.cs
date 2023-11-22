using GeneaGrab.Core.Helpers;
using GeneaGrab.Core.Providers;
using Xunit.Abstractions;

namespace GeneaGrab.Core.Tests.Providers.FR_AMNice;

public class TestAMNice
{
    private readonly Nice instance = new();
    private readonly ITestOutputHelper output;
    public TestAMNice(ITestOutputHelper output) { this.output = output; }

    [Theory(DisplayName = "Check information retriever")]
    [ClassData(typeof(DataAMNice))]
    public async Task CheckInfos(Data data)
    {
        var registryInfo = await instance.Infos(new Uri(data.URL));
        output.WriteLine(await Json.StringifyAsync(registryInfo));
        Assert.Equal("AMNice", registryInfo.ProviderID);
        Assert.Equal(data.Id, registryInfo.RegistryID);
        Assert.Equal(data.Page, registryInfo.PageNumber);
        Assert.Equal(data.Cote, registryInfo.Registry.CallNumber);
        Assert.Equal(data.Rue, registryInfo.Registry.District);
        Assert.Equal(data.Ville, registryInfo.Registry.Location);
        Assert.Equal(data.DetailPosition, registryInfo.Registry.LocationDetails);
        Assert.Equal(data.Auteur, registryInfo.Registry.Author);
        Assert.Equal(data.From, registryInfo.Registry.From);
        Assert.Equal(data.To, registryInfo.Registry.To);

        var types = registryInfo.Registry.Types.ToArray();
        Assert.True(data.Types.Length == types.Length, $"{string.Join(", ", types)}\nExpected: {data.Types}");
        Assert.All(data.Types, type => Assert.Contains(type, types));
    }
}
