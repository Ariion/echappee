# Échappée

Jeu mobile hybrid-casual de gestion de cyclisme (iOS et Android). Nom de travail, à vérifier INPI / EUIPO.
Le dossier complet (maquettes, tableaux, prévisions) est dans `docs/echappee-concept.html`. Ce fichier en est la version utile pour le code.

## Concept en une phrase
Course automatique de 60 secondes vue du dessus, revenus passifs, cartes de coureurs et de matériel, ligues hebdomadaires. Signature : le Plan de course, une stratégie programmée par le joueur avec des règles SI / ALORS.

## Décisions figées
- Sport : cyclisme, six disciplines : route, piste, BMX, VTT, cyclo-cross, gravel.
- Moteur : Unity 6, 2D, C#.
- Backend : Firebase formule Spark gratuite (auth anonyme, Remote Config pour l'économie, Analytics, Firestore au besoin). Pas de Cloud Functions (payantes) : les ligues sont simulées sur l'appareil.
- Pubs : AdMob seul au début (gratuit). Achats : Unity IAP.
- Budget : zéro euro hors comptes développeur. Tout est fait maison, pas de pub payante. Voir `docs/ZERO-COST.md`.
- Langue de base : français. Ensuite EN, ES, PT, IT, DE.
- Ne rien copier d'un jeu existant : ni noms, ni interface, ni icônes, ni textes. Mécaniques génériques seulement.

## Boucle de jeu
1. Course de 60 s, automatique, 12 équipes sur un circuit.
2. Selon la place : primes, points de ligue, parfois une carte.
3. Améliorer les infrastructures (sponsors, bus d'équipe, atelier mécano, fan zone, centre de récupération) : augmente le revenu par seconde.
4. Ouvrir des packs de coureurs et de matériel avec des Watts. Les doublons donnent des fragments qui montent les niveaux.
5. Régler le Plan de course. Retour à 1.

Rythmes : 60 s (course), un jour (revenu hors ligne plafonné à 4 h, 3 packs gratuits), 7 jours (ligue, promotion / relégation), 28 jours (Pass Saison, événement).

## Simulation de course (module central)
- Classe C# pure, sans dépendance Unity, déterministe : une graine donne toujours la même course. Tests unitaires obligatoires.
- Chaque coureur a quatre stats : Sprint, Grimpe, Rouleur, Technique.
- Le circuit est une suite de segments : plat, côte, descente, technique, sprint final.
- Effets à simuler : aspiration (économie d'effort derrière un autre coureur), fatigue, échappées, scission du peloton.
- La simulation produit un fil d'événements lisible (ex. « Attaque de T. Brandão dans la côte »).
- Les revenus sont des grands nombres : type dédié (K, M, B...), dès le premier jour.

## Poids des stats par discipline (0 à 3, fichier de configuration)
| Discipline | Sprint | Grimpe | Rouleur | Technique | Durée | Se débloque |
|---|---|---|---|---|---|---|
| Route | 2 | 3 | 3 | 1 | 60 s | départ |
| Piste | 3 | 0 | 2 | 1 | 30 s | départ |
| BMX | 3 | 1 | 1 | 3 | 20 s | Ligue Régionale |
| VTT | 1 | 3 | 2 | 3 | 60 s | Ligue Nationale |
| Cyclo-cross | 2 | 2 | 2 | 3 | 45 s | Ligue Continentale |
| Gravel | 1 | 2 | 3 | 2 | 90 s | Ligue World Tour |

## Plan de course
Liste de règles (3 à 5 emplacements selon le niveau), lues de haut en bas, chacune : condition + action + actif / inactif.
Conditions : pente, fatigue, kilomètres restants, écart avec une échappée. Actions : grimpeur attaque, reste dans la roue, sprinteur lancé, chasse groupée.
Prévoir une simulation sur 100 courses qui affiche le taux de victoire avant / après.

## Économie (tout en Remote Config, rien en dur)
Monnaies : Primes (souple), Watts (premium), Fragments.
Raretés : Amateur, Pro, Élite, Légende.

| Pack | Prix | Amateur | Pro | Élite | Légende | Garantie |
|---|---|---|---|---|---|---|
| Pack Pro | 15 W (x10 : 135 W) | 58 % | 33 % | 8,5 % | 0,5 % | Élite au plus tard au 15e tirage |
| Pack Élite | 45 W (x10 : 405 W) | 0 % | 66 % | 29 % | 5 % | Légende au plus tard au 25e tirage |

Les taux sont affichés dans le jeu (obligation Apple et Google).
Vidéos de bonus : environ 20 Watts par jour, 3 packs gratuits par jour.

## Monétisation
| Offre | Prix | Règle |
|---|---|---|
| Offre de départ | 2,99 € | une fois, 48 h, après la 2e session |
| Pass Saison | gratuit + 4,99 € | 30 paliers sur 28 jours, présenté dès le jour 3 |
| Sans pub | 4,99 € | enlève les interstitiels, garde les vidéos de bonus |
| Packs de Watts | 0,99 à 99,99 € | 6 paliers |
| Offres limitées | 4,99 à 9,99 € | au maximum une par semaine |
| Maillot Studio premium | 1,99 à 3,99 € | cosmétique uniquement |

Règles de publicité : jamais pendant une course, jamais avant le jour 3, interstitiel au maximum toutes les 6 minutes et seulement entre deux écrans. Vidéos récompensées toujours au choix.
Répartition visée du chiffre d'affaires : 45 % publicité, 55 % achats intégrés.

## Indicateurs à instrumenter dès le départ (Firebase Analytics)
Rétention J1 / J7 / J30, revenu par joueur actif et par jour, coût par installation, conversion en payeur, vidéos vues par joueur et par jour.
Seuils (une seule définition, `Gates` dans `data/balance.json`, testée) : soft launch si J1 au moins 35 % et J7 au moins 15 % ; en dessous on corrige ; sous 12 % de J7 on arrête. Cible : J1 38 %, J7 18 %, J30 7 %.

## Écrans (voir les maquettes dans le HTML)
Course (accueil), Équipe, Plan de course, Ligues, Boutique, Disciplines, Maillot Studio, Affiche de victoire.
Direction artistique : affiche de course vintage, aplats francs, contours de 2 px couleur encre, ombres décalées de 3 px.
Couleurs : encre #141A33, craie #EEF2F8, bleu #3C5BFF, rose #FF4F8B, jaune #FFC933, menthe #1FCB8E. Titres : Big Shoulders Display. Textes : Figtree. Chiffres : JetBrains Mono.
Raretés : Amateur gris-bleu, Pro bleu, Élite rose, Légende jaune.

## Ordre de travail
1. Créer le projet Unity 6 2D avec contrôle de version.
2. Module de simulation de course avec tests, puis un affichage minimal (circuit + points colorés).
3. Économie et grands nombres, revenu par seconde, revenu hors ligne.
4. Écrans de l'interface, dans l'ordre : Course, Équipe, Boutique.
5. Cartes, packs avec garantie, ligues simulées.
6. Plan de course, Maillot Studio, affiche de victoire.
7. Pubs, achats, consentement, analytics.

Critère de sortie de l'étape 2 : 5 personnes regardent la course 60 secondes sans décrocher.

## Règles de code
- Simulation et économie séparées de l'interface, testables sans Unity.
- Toutes les valeurs d'équilibrage dans des fichiers de configuration ou Remote Config.
- Pas de valeur de prix ou de taux de tirage en dur dans le code.
- Commits courts, un sujet par commit.

## Organisation du dépôt et commandes
- `src/Echappee.Core` : code C# pur (netstandard2.1, aucun `UnityEngine`). `data/*.json` : équilibrage, circuits, coureurs. `tests/` : xUnit. `tools/Echappee.Cli` : outil d'équilibrage. `unity/` : scripts Unity. `docs/` : concept, décisions (`DECISIONS.md`), coût zéro, légal.
- `dotnet test` doit rester vert avant chaque commit. Un test vérifie qu'aucun prix ni taux n'est en dur dans le cœur.
- Tout changement de règle ou d'équilibrage : mettre à jour `data/balance.json`, relancer les outils `stats`, `plan`, `eco`, puis `docs/DECISIONS.md` si une règle change.
- Après un changement du cœur ou des données : `./tools/sync-to-unity.sh`.
- Quatre stats partout (Sprint, Grimpe, Rouleur, Technique). La Prime n'a pas de symbole monétaire.
