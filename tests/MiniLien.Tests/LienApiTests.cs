using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniLien.Tests;

public class LienApiTests : IDisposable
{
    private const string Code = "^[A-Za-z0-9]{6}$";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LienApiTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("MiniLien:AdminToken", "jeton-de-test"));
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> CreerLien(string url)
    {
        var reponse = await _client.PostAsJsonAsync("/api/liens", new { url });
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var corps = await reponse.Content.ReadFromJsonAsync<JsonElement>();
        return corps.GetProperty("code").GetString()!;
    }

    private async Task<HttpResponseMessage> Supprimer(string code, string? jeton)
    {
        var requete = new HttpRequestMessage(HttpMethod.Delete, $"/api/liens/{code}");
        if (jeton is not null)
        {
            requete.Headers.Add("X-Admin-Token", jeton);
        }
        return await _client.SendAsync(requete);
    }

    [Fact]
    public async Task CreeUnLienCourtPourUneUrlValide()
    {
        var reponse = await _client.PostAsJsonAsync("/api/liens", new { url = "https://openclassrooms.com/fr/" });

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var corps = await reponse.Content.ReadFromJsonAsync<JsonElement>();
        var code = corps.GetProperty("code").GetString()!;
        Assert.Matches(Code, code);
        Assert.Equal($"/api/liens/{code}", reponse.Headers.Location!.OriginalString);
        Assert.Equal("https://openclassrooms.com/fr/", corps.GetProperty("url").GetString());
        Assert.Equal($"http://localhost:8080/r/{code}", corps.GetProperty("lienCourt").GetString());
        Assert.Equal(0, corps.GetProperty("visites").GetInt64());
    }

    [Fact]
    public async Task RefuseUneUrlInvalide()
    {
        var reponse = await _client.PostAsJsonAsync("/api/liens", new { url = "ftp://exemple.fr" });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        var corps = await reponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("URL_INVALIDE", corps.GetProperty("code").GetString());
    }

    [Fact]
    public async Task RedirigeEtCompteLesVisites()
    {
        var code = await CreerLien("https://exemple.fr/page");

        var reponse = await _client.GetAsync($"/r/{code}");
        await _client.GetAsync($"/r/{code}");

        Assert.Equal(HttpStatusCode.Found, reponse.StatusCode);
        Assert.Equal("https://exemple.fr/page", reponse.Headers.Location!.OriginalString);
        var lien = await _client.GetFromJsonAsync<JsonElement>($"/api/liens/{code}");
        Assert.Equal(2, lien.GetProperty("visites").GetInt64());
    }

    [Fact]
    public async Task ListeLesLiens()
    {
        await CreerLien("https://exemple.fr/1");
        await CreerLien("https://exemple.fr/2");

        var liens = await _client.GetFromJsonAsync<JsonElement>("/api/liens");

        Assert.Equal(2, liens.GetArrayLength());
    }

    [Fact]
    public async Task Renvoie404PourUnCodeInconnu()
    {
        var reponse = await _client.GetAsync("/r/XXXXXX");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        var corps = await reponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("LIEN_INTROUVABLE", corps.GetProperty("code").GetString());
    }

    [Fact]
    public async Task RefuseLaSuppressionSansJetonValide()
    {
        var code = await CreerLien("https://exemple.fr");

        Assert.Equal(HttpStatusCode.Unauthorized, (await Supprimer(code, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Supprimer(code, "mauvais")).StatusCode);
    }

    [Fact]
    public async Task SupprimeUnLienAvecLeBonJeton()
    {
        var code = await CreerLien("https://exemple.fr");

        var reponse = await Supprimer(code, "jeton-de-test");

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/liens/{code}")).StatusCode);
    }
}
