using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MiniLien.Api.Liens;

public static class LienEndpoints
{
    public static void MapLiens(this WebApplication app)
    {
        var api = app.MapGroup("/api/liens");

        api.MapPost("", async (HttpRequest requete, LienStore store, IOptions<MiniLienOptions> options) =>
        {
            CreationLienRequete? corps;
            try
            {
                corps = await requete.ReadFromJsonAsync<CreationLienRequete>();
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException)
            {
                corps = null;
            }
            if (corps is null)
            {
                return Erreur("REQUETE_INVALIDE", "Le corps de la requête doit être un JSON de la forme {\"url\": \"...\"}.", 400);
            }
            try
            {
                var lien = store.Creer(corps.Url);
                return Results.Created($"/api/liens/{lien.Code}", LienDto.Depuis(lien, options.Value.BaseUrl));
            }
            catch (UrlInvalideException e)
            {
                return Erreur("URL_INVALIDE", e.Message, 400);
            }
        });

        api.MapGet("", (LienStore store, IOptions<MiniLienOptions> options) =>
            store.Lister().Select(lien => LienDto.Depuis(lien, options.Value.BaseUrl)));

        api.MapGet("/{code}", (string code, LienStore store, IOptions<MiniLienOptions> options) =>
            Introuvable404(() => Results.Ok(LienDto.Depuis(store.Trouver(code), options.Value.BaseUrl))));

        api.MapDelete("/{code}", (string code, HttpRequest requete, LienStore store, IOptions<MiniLienOptions> options) =>
        {
            var jeton = requete.Headers["X-Admin-Token"].ToString();
            if (jeton.Length == 0 || jeton != options.Value.AdminToken)
            {
                return Erreur("ACCES_REFUSE", "Jeton d'administration absent ou incorrect (en-tête X-Admin-Token).", 401);
            }
            return Introuvable404(() =>
            {
                store.Supprimer(code);
                return Results.NoContent();
            });
        });

        app.MapGet("/r/{code}", (string code, LienStore store) =>
            Introuvable404(() => Results.Redirect(store.Visiter(code).Url)));
    }

    private static IResult Erreur(string code, string message, int statut) =>
        Results.Json(new ErreurApi(code, message), statusCode: statut);

    private static IResult Introuvable404(Func<IResult> action)
    {
        try
        {
            return action();
        }
        catch (LienIntrouvableException e)
        {
            return Erreur("LIEN_INTROUVABLE", e.Message, 404);
        }
    }
}
