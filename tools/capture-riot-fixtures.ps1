<#
.SYNOPSIS
  Remplace les fixtures Riot de test par de vraies réponses de l'API.
.EXAMPLE
  $env:RIOT_API_KEY = "RGAPI-..."; ./tools/capture-riot-fixtures.ps1 -GameName "Gazo" -TagLine "EUW"
.NOTES
  La clé n'est lue que depuis la variable d'environnement et n'est jamais écrite sur disque.
  Les réponses contiennent des PUUID et Riot ID réels : relisez-les avant de les commiter.
#>
param(
    [Parameter(Mandatory)] [string] $GameName,
    [Parameter(Mandatory)] [string] $TagLine,
    [string] $Platform = "euw1",
    [string] $Region = "europe"
)

$ErrorActionPreference = "Stop"
if (-not $env:RIOT_API_KEY) { throw "Définissez RIOT_API_KEY." }
$headers = @{ "X-Riot-Token" = $env:RIOT_API_KEY }
$out = Join-Path $PSScriptRoot "../tests/RiftLens.Infrastructure.Tests/Fixtures/riot"

function Save($url, $file) {
    Write-Host "GET $url"
    $raw = (Invoke-WebRequest -Uri $url -Headers $headers).Content
    Set-Content -Path (Join-Path $out $file) -Value $raw -Encoding utf8NoBOM
    Start-Sleep -Milliseconds 100 # reste sous 20 req/s
    return $raw | ConvertFrom-Json
}

$name = [uri]::EscapeDataString($GameName); $tag = [uri]::EscapeDataString($TagLine)
$account = Save "https://$Region.api.riotgames.com/riot/account/v1/accounts/by-riot-id/$name/$tag" "account.json"
$puuid = $account.puuid
Save "https://$Platform.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/$puuid" "summoner.json" | Out-Null
Save "https://$Platform.api.riotgames.com/lol/league/v4/entries/by-puuid/$puuid" "league-entries.json" | Out-Null
Save "https://$Platform.api.riotgames.com/lol/champion-mastery/v4/champion-masteries/by-puuid/$puuid/top?count=3" "masteries.json" | Out-Null
$ids = Save "https://$Region.api.riotgames.com/lol/match/v5/matches/by-puuid/$puuid/ids?queue=420&start=0&count=3" "match-ids.json"
Save "https://$Region.api.riotgames.com/lol/match/v5/matches/$($ids[0])" "match.json" | Out-Null
Save "https://$Region.api.riotgames.com/lol/match/v5/matches/$($ids[0])/timeline" "timeline.json" | Out-Null
Write-Host "Fixtures mises à jour. Certaines assertions (valeurs exactes) seront à ajuster."
