# MiniLien — C# / ASP.NET Core

MiniLien est un petit service de raccourcissement d'URL : on lui donne une adresse longue, il renvoie un lien court
(`http://localhost:8080/r/aZ3k9Q`) qui redirige vers l'adresse d'origine et compte les visites.

Le périmètre est volontairement réduit. L'application fonctionne en local sur votre poste, **et c'est tout** :
pas de conteneur, pas d'automatisation, pas de monitoring. C'est à vous de les ajouter.

## Prérequis

- SDK .NET 8

## Lancer l'application

```bash
dotnet build
```

```bash
dotnet run --project src/MiniLien.Api
```

L'application répond sur http://localhost:8080.

## Lancer les tests

```bash
dotnet test
```

## L'API

| Méthode | Route | Rôle | Réponses |
|---|---|---|---|
| `POST` | `/api/liens` | Crée un lien court. Corps : `{"url": "https://..."}` | `201` + lien ; `400` |
| `GET` | `/api/liens` | Liste les liens, du plus récent au plus ancien | `200` |
| `GET` | `/api/liens/{code}` | Détail d'un lien, avec son nombre de visites | `200` ; `404` |
| `DELETE` | `/api/liens/{code}` | Supprime un lien. En-tête `X-Admin-Token` obligatoire | `204` ; `401` ; `404` |
| `GET` | `/r/{code}` | Redirige vers l'URL d'origine et compte la visite | `302` ; `404` |

Les erreurs ont toujours le même format : `{"code": "URL_INVALIDE", "message": "..."}`.
Codes possibles : `REQUETE_INVALIDE`, `URL_INVALIDE`, `LIEN_INTROUVABLE`, `ACCES_REFUSE`.

Les liens sont gardés **en mémoire** : ils disparaissent quand l'application redémarre. Il n'y a pas de base de données.

### Essayer avec curl

```bash
curl -i -X POST http://localhost:8080/api/liens -H "Content-Type: application/json" -d '{"url": "https://openclassrooms.com"}'
```

```bash
curl -i http://localhost:8080/r/<code>
```

```bash
curl http://localhost:8080/api/liens
```

## Configuration

La configuration se trouve dans `src/MiniLien.Api/appsettings.json` : adresse publique des liens courts (`MiniLien:BaseUrl`) et jeton d'administration (`MiniLien:AdminToken`). Le port local (8080) est défini dans `Properties/launchSettings.json`.

## Organisation du code

```
MiniLien.sln
src/MiniLien.Api/
├── Program.cs                point d'entrée (API minimale)
└── Liens/
    ├── LienEndpoints.cs      routes /api/liens et /r/{code}
    ├── LienStore.cs          création, recherche, visites (stockage en mémoire)
    └── Lien.cs               modèle et objets échangés
tests/MiniLien.Tests/         tests de l'API (xUnit + WebApplicationFactory)
```
