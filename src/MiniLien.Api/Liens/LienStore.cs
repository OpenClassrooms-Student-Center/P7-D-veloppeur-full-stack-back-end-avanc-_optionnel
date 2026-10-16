using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MiniLien.Api.Liens;

public class UrlInvalideException(string message) : Exception(message);

public class LienIntrouvableException(string code) : Exception($"Aucun lien ne correspond au code « {code} ».");

/// <summary>Stockage en mémoire des liens courts (les données sont perdues au redémarrage).</summary>
public class LienStore
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int LongueurCode = 6;
    private const int LongueurMaxUrl = 2048;

    private readonly ConcurrentDictionary<string, Lien> _liens = new();

    public Lien Creer(string? url)
    {
        var urlVerifiee = VerifierUrl(url);
        while (true)
        {
            var lien = new Lien(RandomNumberGenerator.GetString(Alphabet, LongueurCode), urlVerifiee, DateTimeOffset.UtcNow);
            if (_liens.TryAdd(lien.Code, lien))
            {
                return lien;
            }
        }
    }

    public IEnumerable<Lien> Lister() => _liens.Values.OrderByDescending(l => l.CreeLe);

    public Lien Trouver(string code) =>
        _liens.TryGetValue(code, out var lien) ? lien : throw new LienIntrouvableException(code);

    public Lien Visiter(string code)
    {
        var lien = Trouver(code);
        lien.EnregistrerVisite();
        return lien;
    }

    public void Supprimer(string code)
    {
        if (!_liens.TryRemove(code, out _))
        {
            throw new LienIntrouvableException(code);
        }
    }

    private static string VerifierUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new UrlInvalideException("Le champ « url » est obligatoire.");
        }
        url = url.Trim();
        if (url.Length > LongueurMaxUrl)
        {
            throw new UrlInvalideException($"L'URL dépasse {LongueurMaxUrl} caractères.");
        }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host))
        {
            throw new UrlInvalideException("L'URL doit commencer par http:// ou https:// et contenir un domaine.");
        }
        return url;
    }
}
