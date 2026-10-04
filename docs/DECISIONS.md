# Décisions et corrections par rapport au dossier de concept

Le dossier `docs/echappee-concept.html` reste la référence de la vision. Ce fichier note ce qui a été tranché depuis, pour ne pas laisser deux versions contradictoires.

## Incohérences levées

| Sujet | Dans le dossier | Décision |
|---|---|---|
| Seuils de rétention à 7 jours | 18 % (encadré), 15 % (CLAUDE.md), 12 % (tableau) | **Un seul jeu de seuils**, dans `data/balance.json` section `Gates`, testé : arrêt si J7 < 12 % ; correction si J1 < 35 % ou J7 < 15 % ; soft launch validé au-dessus ; objectif J1 38 %, J7 18 %, J30 7 %. |
| Stats des coureurs | 4 stats dans les règles, 3 sur les maquettes de cartes | **4 stats partout** (Sprint, Grimpe, Rouleur, Technique). Les maquettes de cartes sont à refaire avec Technique quand on dessinera l'écran Équipe (Technique domine en BMX, VTT, cyclo-cross). |
| Devise sur la maquette | « +$126 » | Corrigé : l'unité du jeu est la Prime, sans symbole monétaire. |
| Médiation publicitaire | LevelPlay ou AppLovin MAX | **AdMob seul au début** (gratuit, plus simple). Médiation plus tard si les revenus le justifient. |
| Budget | 12 000 € | **0 €** (voir `ZERO-COST.md`). |

## Changement de plateforme (Unity → web)

Unity ne peut pas être installé ni lancé dans l'environnement de développement, et publier sur les magasins demande des comptes payants à ton nom. J'ai donc choisi une **PWA en Blazor WebAssembly** : même cœur C# testé, jouable immédiatement sur téléphone, publication gratuite. Conséquences : vidéos et paiements simulés tant que tu n'as pas ouvert de vrais comptes ; Unity reste possible plus tard (les scripts de `unity/` sont conservés mais non maintenus).

## Règles de jeu précisées en écrivant le code

- Une « équipe » est une unité du peloton ; sa force par segment mélange le meilleur coureur (60 %) et la moyenne des titulaires (40 %). Le meilleur coureur du segment est celui nommé dans le fil d'événements.
- Plan de course : la **première règle active dont la condition est vraie s'applique** (lecture de haut en bas). Sans règle applicable, l'équipe du joueur reste dans le peloton (elle n'attaque pas seule) : c'est ce qui donne de la valeur au Plan. Un plan raisonnable fait gagner quelques points de victoire, « toujours dans la roue » est un mauvais plan (testé dans `PlanBalanceTests`).
- Revenu hors ligne : plafonné à 4 h ; la vidéo multiplie par 3 mais ne lève pas le plafond.
- Garantie des packs : compteur par pack ; le tirage numéro N est forcé quand N-1 tirages consécutifs n'ont pas donné la rareté garantie (Élite sous 15 tirages, Légende sous 25).
- Ligues : 8 équipes (le joueur + 7 fantômes simulés), 3 montent, 2 descendent, 7 jours. Pas de descente sous la ligue Amateur ni de montée au-dessus de Légende.
- Départ : 4 Amateurs + 1 Élite garanti, 15 Primes (de quoi acheter la première amélioration), 30 Watts, bonus de connexion quotidien de 10 Watts.
- Plan de course : le bouton « plan conseillé » insère les trois règles testées en simulation ; une règle ajoutée seule par le joueur peut être mauvaise, c'est voulu (c'est là que le talent compte).
- Rythme de l'économie : un joueur parfaitement glouton achète la 2ᵉ infrastructure en ~1,5 min et la 5ᵉ en ~1 h (test `PacingTests`).

## Ce qui reste à décider (ne pas deviner)

- Nombre exact de cartes de matériel (le dossier parle de « coureurs et matériel » ; seuls les coureurs sont implémentés).
- Contenu du Pass Saison (30 paliers) et des événements de 28 jours.
- Équilibrage final des rangs de ligue : à régler en soft launch avec de vraies données.
