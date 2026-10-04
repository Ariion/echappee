# Échappée

Jeu mobile hybrid-casual de gestion de cyclisme (nom de travail). Vision complète : [`docs/echappee-concept.html`](docs/echappee-concept.html). Règles pour le code : [`CLAUDE.md`](CLAUDE.md). Contraintes : **budget zéro**, tout est fait maison ([`docs/ZERO-COST.md`](docs/ZERO-COST.md)).

## Jouer

L'application est une **PWA** (site installable, fonctionne hors ligne) en Blazor WebAssembly : même code C# testé que le reste du projet.

```bash
dotnet publish src/Echappee.Web -c Release -o /tmp/site
cd /tmp/site/wwwroot && python3 -m http.server 8099      # puis ouvrir http://localhost:8099
```

Publication gratuite : le workflow `.github/workflows/pages.yml` publie sur GitHub Pages à chaque fusion dans `main` (réglage unique à faire dans GitHub : *Settings → Pages → Source : GitHub Actions*). Sur téléphone : ouvrir l'adresse, puis « Ajouter à l'écran d'accueil ».

## État d'avancement

| Étape (CLAUDE.md) | État |
|---|---|
| 1. Projet avec contrôle de version | **Fait** (solution .NET, CI, déploiement Pages). |
| 2. Simulation de course + affichage minimal | **Fait** : simulation déterministe testée, course animée dans l'app. |
| 3. Économie et grands nombres | **Fait** : revenu/s, hors ligne plafonné 4 h, boost, améliorations ×1/×10/MAX. |
| 4. Écrans Course, Équipe, Boutique | **Faits**, plus Plan de course, Ligues, Disciplines, Maillot Studio, Réglages. |
| 5. Cartes, packs avec garantie, ligues | **Fait** : packs (taux affichés, compteur de garantie), fragments, niveaux, ligue hebdomadaire. |
| 6. Plan de course, Maillot Studio, affiche | **Fait** : règles SI/ALORS, simulation 100 courses, 6 motifs, affiche partageable. |
| 7. Pubs, achats, consentement, analytics | **Règles faites et testées**. Dans l'app, vidéos et achats sont **simulés** (mode test). Vraie pub / vrais paiements : voir `docs/ZERO-COST.md`. |

## Commandes

```bash
dotnet test tests/Echappee.Core.Tests                          # 78 tests, sans navigateur, sans Unity
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
- `src/Echappee.Web` : l'application jouable (Blazor WebAssembly).
- `tests/e2e` : parcours complet dans un vrai navigateur.
- `unity/` : scripts Unity optionnels (non testés), gardés pour une éventuelle sortie sur les magasins.
- `docs/` : concept, décisions, plan à zéro euro, points légaux.
