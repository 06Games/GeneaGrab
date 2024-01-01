using GeneaGrab.Core.Providers;

namespace GeneaGrab.Core.Tests.Providers.FR_AD17;

public class TestAD17
{
    private readonly AD17 instance = new();

    [Theory(DisplayName = "Check information retriever")]
    [ClassData(typeof(DataAD17))]
    public async Task CheckInfos(Data data)
    {
        var (registry, pageNumber) = await instance.Infos(new Uri(data.URL));
        Assert.Equal(instance.GetType().Name, registry.ProviderId);
        Assert.Equal(data.Id, registry.Id);
        Assert.Equal(data.Page, pageNumber);
        Assert.Equal(data.Cote, registry.CallNumber);
        Assert.Equal(new[] { data.Ville }, registry.Location);
        Assert.Equal(data.From, registry.From);
        Assert.Equal(data.To, registry.To);

        var types = registry.Types.ToArray();
        Assert.Equal(data.Types.Length, types.Length);
        Assert.All(data.Types, type => Assert.Contains(type, types));
    }
}
