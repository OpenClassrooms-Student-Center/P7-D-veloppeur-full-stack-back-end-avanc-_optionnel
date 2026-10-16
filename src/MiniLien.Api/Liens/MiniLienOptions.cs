namespace MiniLien.Api.Liens;

public class MiniLienOptions
{
    /// <summary>Adresse publique utilisée pour construire les liens courts.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Jeton exigé dans l'en-tête X-Admin-Token pour supprimer un lien.</summary>
    public string AdminToken { get; set; } = "";
}
