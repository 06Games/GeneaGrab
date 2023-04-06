namespace GeneaGrab.Core.Tests.Providers.FR_AD06;

public class TestAD06
{
    private readonly AD06 instance;
    public TestAD06() {
        instance = new AD06();
    }

    [Theory(DisplayName = "Check information retriever")]
    [ClassData(typeof(DataAD06))]
    public async void CheckInfos(Data data)
    {
        var registryInfo = await instance.Infos(new Uri(data.URL));
        Assert.Equal(instance.GetType().Name, registryInfo.ProviderID);
        Assert.Equal(data.Id, registryInfo.RegistryID);
        Assert.Equal(data.Page, registryInfo.PageNumber);
        Assert.Equal(data.Cote, registryInfo.Registry.CallNumber);
        Assert.Equal(data.Ville, registryInfo.Registry.Location);
        Assert.Equal(data.Paroisse, registryInfo.Registry.District);
        Assert.Equal(data.From, registryInfo.Registry.From);
        Assert.Equal(data.To, registryInfo.Registry.To);
        if(data.Notes != null) Assert.Equal(data.Notes, registryInfo.Registry.Notes);

        var types = registryInfo.Registry.Types.ToArray();
        Assert.All(data.Types, type => Assert.Contains(type, types));
        Assert.Equal(data.Types.Length, types.Length);
    }
}
