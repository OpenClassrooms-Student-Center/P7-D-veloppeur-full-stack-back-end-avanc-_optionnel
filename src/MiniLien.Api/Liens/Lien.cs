namespace MiniLien.Api.Liens;

/// <summary>Un lien court : un code de 6 caractères qui redirige vers une URL cible.</summary>
public class Lien(string code, string url, DateTimeOffset creeLe)
{
    private long _visites;

    public string Code { get; } = code;
    public string Url { get; } = url;
    public DateTimeOffset CreeLe { get; } = creeLe;
    public long Visites => Interlocked.Read(ref _visites);

    public void EnregistrerVisite() => Interlocked.Increment(ref _visites);
}

public record LienDto(string Code, string Url, string LienCourt, DateTime CreeLe, long Visites)
{
    public static LienDto Depuis(Lien lien, string baseUrl) =>
        new(lien.Code, lien.Url, $"{baseUrl}/r/{lien.Code}", lien.CreeLe.UtcDateTime, lien.Visites);
}

public record CreationLienRequete(string? Url);

/// <summary>Format commun des erreurs renvoyées par l'API.</summary>
public record ErreurApi(string Code, string Message);
