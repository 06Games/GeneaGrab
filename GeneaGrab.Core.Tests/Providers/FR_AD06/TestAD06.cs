using GeneaGrab.Core.Models;
using GeneaGrab.Core.Providers;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace GeneaGrab.Core.Tests.Providers.FR_AD06;

public class TestAD06
{
    private readonly AD06 instance;
    private static int timeoutCount;
    private readonly ITestOutputHelper output;

    public TestAD06(ITestOutputHelper output)
    {
        instance = new AD06();
        this.output = output;
    }

    [Theory(DisplayName = "Check information retriever")]
    [ClassData(typeof(DataAD06))]
    public async Task CheckInfos(Data data)
    {
        if (timeoutCount >= 3) return; // AD06 is geo-restricted, so if the API times out 3 times, we assume it's because the location is blocked.
        RegistryInfo registryInfo;
        try { registryInfo = await instance.Infos(new Uri(data.URL)); }
        catch (Exception? e)
        {
            while (e is not TimeoutException or TaskCanceledException or null) e = e?.InnerException;
            if (e is not (TimeoutException or TaskCanceledException)) throw;

            timeoutCount++;
            output.WriteLine($"Timed-out ({timeoutCount})");
            throw;
        }

        output.WriteLine(JsonConvert.SerializeObject(registryInfo.Registry, Formatting.Indented));

        Assert.Equal(instance.GetType().Name, registryInfo.ProviderId);
        Assert.Equal(data.Id, registryInfo.RegistryId);
        Assert.Equal(data.Page, registryInfo.PageNumber);
        Assert.Equal(data.Cote, registryInfo.Registry.CallNumber);
        Assert.Equal(data.Ville, registryInfo.Registry.Location);
        Assert.Equal(data.Paroisse, registryInfo.Registry.District);
        Assert.Equal(data.Details, registryInfo.Registry.LocationDetails);
        Assert.Equal(data.Titre, registryInfo.Registry.Title);
        Assert.Equal(data.SousTitre, registryInfo.Registry.Subtitle);
        Assert.Equal(data.Auteur, registryInfo.Registry.Author);
        Assert.Equal(data.From, registryInfo.Registry.From);
        Assert.Equal(data.To, registryInfo.Registry.To);

        var types = registryInfo.Registry.Types.ToArray();
        Assert.All(data.Types, type => Assert.Contains(type, types));
        Assert.Equal(data.Types.Length, types.Length);
    }
}
