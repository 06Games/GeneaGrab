using GeneaGrab.Core.Providers;

namespace GeneaGrab.Core.Tests.Providers.FR_AD17;

public class TestAD17
{
    private readonly AD17 instance = new();

    [Theory(DisplayName = "Check information retriever")]
    [ClassData(typeof(DataAD17))]
    public async Task CheckInfos(Data data)
    {
        var registryInfo = await instance.Infos(new Uri(data.URL));
        Assert.Equal(instance.GetType().Name, registryInfo.ProviderId);
        Assert.Equal(data.Id, registryInfo.RegistryId);
        Assert.Equal(data.Page, registryInfo.PageNumber);
        Assert.Equal(data.Cote, registryInfo.Registry.CallNumber);
        Assert.Equal(data.Ville, registryInfo.Registry.Location);
        Assert.Equal(data.From, registryInfo.Registry.From);
        Assert.Equal(data.To, registryInfo.Registry.To);

        var types = registryInfo.Registry.Types.ToArray();
        Assert.Equal(data.Types.Length, types.Length);
        Assert.All(data.Types, type => Assert.Contains(type, types));
    }
}
