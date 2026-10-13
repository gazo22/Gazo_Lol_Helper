# RiftLens

Application compagnon personnelle pour League of Legends : profils, historique, tier list high-elo,
assistant de draft (LCU) et overlay en jeu (Live Client Data).

> RiftLens n'est pas approuvé par Riot Games et ne reflète pas les opinions de Riot Games ou de toute
> personne officiellement impliquée dans la production ou la gestion des propriétés de Riot Games.

## Architecture

| Projet | Rôle |
|---|---|
| `RiftLens.Domain` | Entités (Match, Participant, Player…), value objects (RiotId, Patch, Platform), score Wilson |
| `RiftLens.Contracts` | DTO partagés Api ↔ Desktop |
| `RiftLens.Infrastructure` | EF Core / SQL Server, client Riot API, rate limiter, Data Dragon |
| `RiftLens.Api` | Minimal API protégée par clé d'accès (`X-RiftLens-Key`) |
| `RiftLens.Collector` | Worker de collecte high-elo + agrégation |
| `RiftLens.LocalClient` | LCU (champ select) + Live Client Data (overlay) |
| `RiftLens.UI` | Razor Class Library : design system + pages |
| `RiftLens.Desktop` | Hôte WPF + BlazorWebView (Windows) |

La **clé Riot reste exclusivement côté serveur** (Api + Collector). Le Desktop ne connaît que l'URL du backend
et une clé d'accès RiftLens.

## Démarrage

Prérequis : .NET 10 SDK, Docker, Windows 10/11 + WebView2 pour le Desktop.

```bash
cp .env.example .env            # renseigner SQL_SA_PASSWORD, RIOT_API_KEY, RIFTLENS_ACCESS_KEY
docker compose up -d --build    # SQL Server + Api (http://localhost:5080) + Collector
```

Le Desktop lit sa configuration dans `appsettings.json` (URL) et ses user-secrets (clé) :

```bash
dotnet user-secrets --project src/RiftLens.Desktop set "Backend:AccessKey" "<RIFTLENS_ACCESS_KEY>"
dotnet run --project src/RiftLens.Desktop
```

Pour lancer l'Api hors Docker :

```bash
dotnet user-secrets --project src/RiftLens.Api set "Riot:ApiKey" "RGAPI-..."
dotnet user-secrets --project src/RiftLens.Api set "Access:Keys:0" "<32+ caractères>"
dotnet user-secrets --project src/RiftLens.Api set "ConnectionStrings:RiftLens" "Server=localhost,1433;Database=RiftLens;User Id=sa;Password=...;TrustServerCertificate=True"
dotnet run --project src/RiftLens.Api
```

Un backend distant **doit** être servi en HTTPS (le Desktop refuse une URL HTTP non locale).

## Migrations

```bash
dotnet tool restore
dotnet ef migrations add <Nom> -p src/RiftLens.Infrastructure -o Persistence/Migrations
```

En environnement `Development`, l'Api applique les migrations au démarrage (`Database:MigrateOnStartup`).

## Tests

```bash
dotnet test
```

Le projet WPF se compile aussi sous Linux/macOS (`EnableWindowsTargeting`), mais ne s'exécute que sous Windows.
