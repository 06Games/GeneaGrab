namespace GeneaGrab.Core.Tests.Providers.FR_AD17;

public class TestAD17
{
    private readonly AD17 instance;
    public TestAD17() {
        instance = new AD17();
    }

    [Theory(DisplayName = "Check information retriever")]
    [ClassData(typeof(DataAD17))]
    public async void CheckInfos(Data data)
    {
        var registryInfo = await instance.Infos(new Uri(data.URL));
        Assert.Equal(instance.GetType().Name, registryInfo.ProviderID);
        Assert.Equal(data.Id, registryInfo.RegistryID);
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
