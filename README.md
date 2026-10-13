# RiftLens

Application compagnon personnelle pour League of Legends : profils, historique, tier list high-elo
et assistant de draft (LCU).

> RiftLens n'est pas approuvé par Riot Games et ne reflète pas les opinions de Riot Games ou de toute
> personne officiellement impliquée dans la production ou la gestion des propriétés de Riot Games.

## Installation (Windows 10/11)

1. Récupérer **`RiftLens-Setup.exe`** :
   - onglet **Actions** du dépôt → workflow « Installeur Windows » → *Run workflow*, puis l'artefact
     `RiftLens-Setup-x.y.z` une fois le build terminé ;
   - ou la page **Releases** (créée automatiquement pour chaque tag `vX.Y.Z`).
2. Lancer l'installeur (droits administrateur). Il installe, si nécessaire :
   - **Microsoft Edge WebView2 Runtime** (déjà présent sur la plupart des Windows 10/11) ;
   - **SQL Server 2022 Express**, instance dédiée `RIFTLENS` (inclus dans l'installeur, quelques minutes) ;
   - **RiftLens** : l'application et le service Windows **« RiftLens Server »** (démarrage automatique).
3. Au premier lancement, l'assistant demande votre **clé API Riot** :
   connectez-vous sur [developer.riotgames.com](https://developer.riotgames.com), copiez la *Development API Key*
   (`RGAPI-…`) et collez-la. Elle est vérifiée auprès de Riot puis stockée **chiffrée** (DPAPI) sur ce PC.
   Une clé de développement expire après 24 h : un bandeau vous le signale, il suffit d'en coller une nouvelle
   (Paramètres → Remplacer la clé).

Rien d'autre à lancer : le service démarre avec Windows, collecte les parties high-elo en arrière-plan et
l'application s'y connecte toute seule.

### Désinstallation

« Applications installées » → RiftLens. Le service et l'application sont supprimés ; sont conservés :
`%ProgramData%\RiftLens` (clés, journaux) et SQL Server Express (instance `RIFTLENS`, désinstallable séparément,
base `RiftLens`).

### Dépannage

| Symptôme | Piste |
|---|---|
| Bandeau « Serveur RiftLens injoignable » | `services.msc` → *RiftLens Server* démarré ? Journaux : `%ProgramData%\RiftLens\logs` |
| « Clé API Riot expirée » | Normal après 24 h avec une clé de développement : Paramètres → Remplacer la clé |
| Tier list vide | Le Collector a besoin de quelques heures de collecte (≈ 25 requêtes / min avec une clé perso) |
| Draft : « Client non détecté » | Le client LoL doit tourner sur ce PC ; installation hors `C:\Riot Games` : `LocalClient:InstallPaths` |
| Journaux de l'application | `%LocalAppData%\RiftLens\logs` |

### Vérification manuelle d'une version

1. Installer sur un Windows propre → WebView2, SQL Express et RiftLens s'installent, l'app se lance.
2. L'assistant s'ouvre → coller une clé invalide (refusée) puis une clé valide (« Tout est prêt »).
3. Ctrl+K → `Nom#TAG` → profil, historique, détail d'un match (graphe d'or).
4. Quelques heures plus tard : tier list et pages champion remplies.
5. Client LoL lancé → page Draft connectée ; imports runes / item set.
6. Redémarrer Windows → le service redémarre seul ; désinstaller → service supprimé.

## Architecture

| Projet | Rôle |
|---|---|
| `RiftLens.Domain` | Entités (Match, Participant, Player…), value objects (RiotId, Patch, Platform), score Wilson |
| `RiftLens.Contracts` | DTO partagés Api ↔ Desktop |
| `RiftLens.Infrastructure` | EF Core / SQL Server, client Riot API, rate limiter, Data Dragon |
| `RiftLens.Api` | Minimal API protégée par clé d'accès (`X-RiftLens-Key`) |
| `RiftLens.Collector` | Worker de collecte high-elo + agrégation |
| `RiftLens.LocalClient` | Client LoL local (LCU) : détection, champ select, import runes / item sets |
| `RiftLens.UI` | Razor Class Library : design system + pages |
| `RiftLens.Desktop` | Hôte WPF + BlazorWebView (Windows) |

La **clé Riot reste exclusivement côté serveur** (Api + Collector). Le Desktop ne connaît que l'URL du backend
et une clé d'accès RiftLens.

## Client Riot API

**Camille** (MingweiSamuel) a été évaluée puis écartée : générée automatiquement, sa maintenance .NET est irrégulière
et elle impose son propre rate limiter. RiftLens utilise un client typé `HttpClient` (`Infrastructure/Riot`) limité
aux endpoints utiles : Account-V1, Summoner-V4, League-V4, Champion-Mastery-V4, Match-V5 (ids, détail, timeline).

### Rate limiting

- File centralisée **par route** (`europe`, `euw1`…) : limite applicative + limite de chaque méthode.
- Limites par défaut `20:1,100:120` (clé perso), puis mises à jour dynamiquement depuis `X-App-Rate-Limit` /
  `X-Method-Rate-Limit`.
- Fenêtres glissantes (plus strictes que les fenêtres fixes de Riot : on ne peut pas les dépasser).
- 429 : respect de `Retry-After` ; une limite `application`/`method` gèle la file concernée, une limite `service`
  ne retarde que la requête ; 3 nouvelles tentatives maximum.
- L'Api et le Collector partagent la même clé dans deux processus : `Riot:RateLimit:BudgetShare` répartit le budget
  (Docker Compose : 0.4 / 0.6). Dans un processus, les requêtes interactives passent avant celles de fond.
- Un match déjà stocké n'est jamais retéléchargé ; la timeline est récupérée à la demande puis conservée (gzip).

### Fixtures de test

Les fixtures de `tests/RiftLens.Infrastructure.Tests/Fixtures` suivent fidèlement le schéma Riot / Data Dragon,
mais ont été écrites hors ligne. Pour les remplacer par de vraies réponses :

```powershell
$env:RIOT_API_KEY = "RGAPI-..."; ./tools/capture-riot-fixtures.ps1 -GameName "Nom" -TagLine "EUW"
```

## Collector & tier list

Le Collector (`RiftLens.Collector`) tourne en boucle :

1. **Seed** (toutes les 6 h) : ladders Challenger / Grandmaster / Master d'EUW (League-V4).
2. **Crawl** : les joueurs les moins récemment visités → ids des parties classées solo des 7 derniers jours,
   mis dans une file persistante (`rl.QueuedMatches`).
3. **Fetch** : téléchargement des matchs en attente (un match déjà en base n'est jamais redemandé ;
   3 tentatives avant abandon).
4. **Aggregate** (toutes les 30 min) : recalcul idempotent des statistiques des 2 derniers patchs.

Tout l'état est en base : un arrêt (Ctrl+C, redémarrage Docker) reprend exactement là où il s'était arrêté.
Avec 60 % d'une clé perso, comptez au plus ~25 requêtes/min (crawl + matchs) ; une tier list exploitable demande quelques heures de collecte.
Le calcul du tier est documenté dans [`docs/tier-score.md`](docs/tier-score.md).

Réglages : sections `Collector` et `Stats` de `src/RiftLens.Collector/appsettings.json`.

## Assistant de draft (client LoL local)

`RiftLens.LocalClient` tourne dans le Desktop, sur la machine du joueur :

- **Détection** : lecture du `lockfile` à côté de `LeagueClientUx.exe` en cours d'exécution, sinon dans
  `LocalClient:InstallPaths` (par défaut `C:\Riot Games\League of Legends`). Nouvel essai toutes les 3 s.
- **HTTPS local** : auth Basic `riot:<password>` ; le certificat auto-signé n'est accepté **que** pour `127.0.0.1`.
- **Événements** : WebSocket WAMP, abonnement à `OnJsonApiEvent_lol-champ-select_v1_session` (pas de polling).
- **Import** (sur clic uniquement) : page de runes via `/lol-perks/v1/pages` (remplace l'ancienne page « RiftLens… »,
  ne supprime jamais une page du joueur) et item set via `/lol-item-sets/v1/item-sets/{summonerId}/sets`
  (les sets du joueur sont conservés).
- **Règles Riot** : le modèle de champ select ne contient aucun nom, Riot ID ni PUUID ; aucune action de jeu
  n'est automatisée (pas d'auto-pick, d'auto-ban ni d'acceptation automatique).

### Vérification manuelle (Windows)

1. Lancer le client LoL puis RiftLens → page Draft : « Client connecté ».
2. Lancer une partie personnalisée ou une file normale → le panneau apparaît à l'entrée en sélection.
3. Survoler un champion → stats, matchups et recommandations se mettent à jour.
4. « Importer la page de runes » → une page « RiftLens <champion> » est créée et sélectionnée.
5. « Importer l'item set » → visible dans la boutique en jeu, onglet des objets recommandés.
6. Fermer le client → retour à « Client non détecté », puis reconnexion automatique au redémarrage.

## Développement

Prérequis : .NET 10 SDK, Docker, Windows 10/11 + WebView2 pour le Desktop.

L'Api (`src/RiftLens.Api`, assembly `RiftLens.Server`) fonctionne en deux modes :

- **local** (`--LocalMode:Enabled=true`, utilisé par l'installeur) : service Windows sur `127.0.0.1:5080`,
  Collector intégré au processus, SQL Express `.\RIFTLENS`, clé d'accès générée dans `%ProgramData%\RiftLens\access.key`,
  clé Riot saisie dans l'assistant ;
- **serveur** (Docker Compose ci-dessous) : Api et Collector séparés, secrets par variables d'environnement.

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

## Installeur

`installer/` contient un projet WiX 5 : `Package` (MSI : fichiers, raccourcis, service) et `Bundle`
(`RiftLens-Setup.exe` : WebView2 + SQL Express + MSI). WiX ne fonctionne que sous Windows ; le build est fait par
`.github/workflows/release.yml`. En local sous Windows :

```powershell
dotnet publish src/RiftLens.Desktop -c Release -r win-x64 --self-contained -o artifacts/app
dotnet publish src/RiftLens.Api -c Release -r win-x64 --self-contained -o artifacts/server
# préparer installer/Bundle/redist comme indiqué dans installer/Bundle/redist/README.md, puis :
dotnet build installer/Bundle/RiftLens.Bundle.wixproj -c Release
```
