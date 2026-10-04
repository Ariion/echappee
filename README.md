# Échappée

Jeu mobile hybrid-casual de gestion de cyclisme (nom de travail). Vision complète : [`docs/echappee-concept.html`](docs/echappee-concept.html). Règles pour le code : [`CLAUDE.md`](CLAUDE.md). Contraintes : **budget zéro**, tout est fait maison ([`docs/ZERO-COST.md`](docs/ZERO-COST.md)).

## État d'avancement

| Étape (CLAUDE.md) | État |
|---|---|
| 1. Projet avec contrôle de version | Fait pour le code. Le projet Unity se crée sur ta machine (voir [`unity/README.md`](unity/README.md)). |
| 2. Simulation de course + affichage minimal | **Fait** : simulation déterministe testée, aperçu jouable dans le navigateur. |
| 3. Économie et grands nombres | **Fait** : `BigAmount`, revenu/s, hors ligne plafonné, boost vidéo, primes de course. |
| 4. Écrans (Course, Équipe, Boutique) | À faire dans Unity. Les textes des 6 langues (FR, EN, ES, PT, IT, DE) sont prêts dans `data/strings/` ; les traductions non françaises sont à faire relire par des natifs. |
| 5. Cartes, packs avec garantie, ligues | **Logique faite et testée** (packs, fragments, niveaux, ligue de 8 équipes). Écrans à faire. |
| 6. Plan de course, Maillot Studio, affiche | **Fait côté logique** : Plan de course (règles, analyseur avant/après), maillots (6 motifs, couleurs sécurisées) et affiche de victoire SVG. Écrans Unity à faire. |
| 7. Pubs, achats, consentement, analytics | **Règles faites et testées** (`AdPolicy`, `OfferPolicy`, seuils de rétention, noms d'événements). SDK AdMob / IAP / Firebase à brancher dans Unity. |

## Commandes

```bash
dotnet test                                                    # 71 tests, sans Unity
dotnet run --project tools/Echappee.Cli -- race 42 route 55    # regarder le fil d'une course
dotnet run --project tools/Echappee.Cli -- stats route 50      # mesurer l'équilibrage sur 300 courses
dotnet run --project tools/Echappee.Cli -- plan route 58       # taux de victoire avant / après un Plan
dotnet run --project tools/Echappee.Cli -- eco 6               # progression d'un joueur sur 6 h
dotnet run --project tools/Echappee.Cli -- preview 3 route 55  # écrit docs/preview/course.html
dotnet run --project tools/Echappee.Cli -- poster de          # écrit docs/preview/affiche.svg
./tools/sync-to-unity.sh                                       # copie le cœur et les données dans unity/
```

Ouvre `docs/preview/course.html` dans un navigateur : c'est une vraie course simulée, rejouable. C'est le test des « 5 personnes qui regardent 60 secondes sans décrocher ».

## Organisation

- `src/Echappee.Core` : tout le jeu hors interface (C# pur, netstandard2.1, sans Unity). Simulation, économie, packs, ligues, règles de pub et d'offres.
- `data/*.json` : **toutes** les valeurs d'équilibrage (prix, taux, seuils), les circuits et les coureurs. En production, `balance.json` sera servi par Remote Config.
- `tests/Echappee.Core.Tests` : tests unitaires (dont déterminisme, taux des packs, garanties, rythme de l'économie).
- `tools/Echappee.Cli` : outil d'équilibrage et générateur d'aperçu.
- `unity/` : scripts Unity (non testés dans l'éditeur, voir son README).
- `docs/` : concept, décisions, plan à zéro euro, points légaux.
