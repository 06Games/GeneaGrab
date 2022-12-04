using Xunit.Abstractions;

namespace GeneaGrab.Core.Tests.Providers.FR_AD79_86;

public class TestAD79_86
{
    private readonly AD79_86 instance;
    private readonly ITestOutputHelper _output;
    public TestAD79_86(ITestOutputHelper output) {
        instance = new AD79_86();
        _output = output;
    }

    [Theory(DisplayName = "Check information retriever")]
    [ClassData(typeof(DataAD79_86))]
    public async void CheckInfos(Data data)
    {
        var registryInfo = await instance.Infos(new Uri(data.URL));
        Assert.Equal("AD79-86", registryInfo.ProviderID);
        Assert.Equal(data.Id, registryInfo.RegistryID);
        Assert.Equal(data.Page, registryInfo.PageNumber);
        Assert.Equal(data.Cote, registryInfo.Registry.CallNumber);
        Assert.Equal(data.Ville, registryInfo.Registry.Location);
        Assert.Equal(data.Paroisse, registryInfo.Registry.District);
        Assert.Equal(data.From, registryInfo.Registry.From);
        Assert.Equal(data.To, registryInfo.Registry.To);

        var types = registryInfo.Registry.Types.ToArray();
        Assert.True(data.Types.Length == types.Length, $"{string.Join(", ", types)}\nExpected: {data.Types}");
        Assert.All(data.Types, type => Assert.Contains(type, types));
    }
}
