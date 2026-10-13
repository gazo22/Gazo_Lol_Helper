# Calcul du tier RiftLens

## Données

Le Collector part des ladders **Challenger, Grandmaster et Master** (League-V4) d'EUW, parcourt les parties
classées solo (`queue=420`) des 7 derniers jours de ces joueurs et stocke chaque match une seule fois.
Chaque match est rattaché au *bucket d'elo* du joueur ladder qui l'a fait découvrir (`Match.SourceTier`).
Le filtre « Master+ » agrège les trois buckets, « Grandmaster+ » les deux plus hauts, « Challenger » un seul.

Sont exclus : remakes, participants sans rôle (`teamPosition` vide).

## Indicateurs (par patch, bucket, champion, rôle)

| Indicateur | Formule |
|---|---|
| Winrate | victoires / parties |
| Pickrate | parties du champion dans le rôle / nombre de matchs |
| Banrate | matchs où le champion est banni / nombre de matchs |
| Matchup | parties contre l'adversaire **du même rôle** dans l'équipe adverse |
| Core build | 3 premiers objets légendaires des slots 0-5 (hors bottes, composants, trinket) |
| Runes | 6 runes + 3 fragments + styles primaire/secondaire |

« Légendaire » (Data Dragon) : objet final (sans `into`), disponible sur la Faille (carte 11), coût total ≥ 2 200 PO.

## Score

```
score = WilsonLB95(victoires, parties) + 0,02 × log2(1 + pickrate / 0,01)
```

- **WilsonLB95** : borne basse de l'intervalle de confiance de Wilson à 95 % (z = 1,96).
  Elle « tire » les petits échantillons vers le bas : un 75 % sur 12 parties n'est pas un champion S+.
- **Bonus de popularité** : logarithmique, il départage les winrates proches au profit des champions
  réellement joués (pickrate 1 % → +0,02 ; 7 % → +0,06 ; 15 % → +0,08).

| Exemple | Winrate | Borne Wilson | Pickrate | Score |
|---|---|---|---|---|
| 9 / 12 | 75,0 % | 0,468 | 0,1 % | 0,470 |
| 530 / 1 000 | 53,0 % | 0,499 | 5 % | 0,551 |
| 5 300 / 10 000 | 53,0 % | 0,520 | 12 % | 0,594 |

## Tiers

Attribués **par rôle**, par percentile du score parmi les champions ayant au moins `Stats:MinGames`
(150 par défaut) parties dans ce rôle :

| Tier | Percentile |
|---|---|
| S+ | top 5 % |
| S | 5 – 20 % |
| A | 20 – 50 % |
| B | 50 – 80 % |
| C | 80 – 100 % |

Les champions sous le seuil ne participent pas aux percentiles ; ils reçoivent le tier correspondant à leur
score, **plafonné à B**, et sont signalés « échantillon faible ».

Les builds et pages de runes ne sont affichés qu'à partir de `Stats:MinBuildGames` (30) parties,
les matchups à partir de `Stats:MinMatchupGames` (20), classés par borne de Wilson (favorables) ou borne haute
(difficiles). La taille d'échantillon est toujours affichée.
