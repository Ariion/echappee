# Plan à zéro euro

Objectif : tout faire nous-mêmes, sans rien payer en dehors de ce qui est imposé par les magasins d'applications.

## Ce qui est gratuit (et ce qu'on utilise)

| Besoin | Outil | Remarque |
|---|---|---|
| Moteur | **Unity Personal** (Unity 6) | Gratuit tant que le revenu / financement du studio reste sous le seuil fixé par Unity (à vérifier sur unity.com au moment de publier). |
| Code, tests, CI | .NET SDK, xUnit, GitHub Actions | Gratuit. Tout le cœur du jeu se teste sans Unity (`dotnet test`). |
| Analytics, Remote Config, Auth anonyme, Firestore | **Firebase, formule Spark** | Gratuite, sans carte bancaire, avec quotas largement suffisants pour un lancement. |
| Publicité | **AdMob** (Google) | Gratuit. Une seule régie au début ; la médiation (LevelPlay, MAX) viendra si les revenus le justifient. |
| Achats intégrés | Unity IAP / stores | Pas de frais d'outil ; les stores prélèvent une commission sur les ventes (15 % petits studios). |
| Polices | Big Shoulders Display, Figtree, JetBrains Mono | Licence libre (OFL). |
| Illustrations, icônes, affiches | SVG et code (direction artistique « affiche vintage » : aplats, contours 2 px) | Fait maison, aucun fichier acheté. |
| Sons, musique | Sons générés / dépôts sous licence CC0 (Kenney, Freesound CC0, OpenGameArt CC0) | Noter la source de chaque fichier dans `docs/CREDITS.md` (à créer au premier asset). |
| Traductions | Faites ici (FR → EN, ES, PT, IT, DE), relues par des natifs bénévoles | La qualité de la traduction automatique n'est pas garantie : prévoir une relecture. |
| Recherche d'antériorité du nom | Bases INPI et EUIPO (TMview), recherche dans les stores | Consultation gratuite. Le dépôt de marque, lui, est payant : à ne faire qu'après le soft launch. |
| Publicité du jeu | Organique : vidéos courtes, affiches de victoire partagées, communautés cyclisme | Pas d'achat d'installations. |

## Ce qui n'est PAS gratuit et qu'on ne peut pas contourner

- **Google Play** : compte développeur, **paiement unique** (25 USD au moment de la rédaction).
- **App Store (Apple)** : programme développeur, **abonnement annuel** (99 USD au moment de la rédaction).
- Sans ces comptes, on ne peut pas publier. Conseil : **sortir d'abord sur Android** (un seul paiement), mesurer la rétention, et n'ouvrir iOS que si les chiffres le justifient.
- Un ordinateur pour compiler la version iOS doit être un Mac (ou un service de build payant). Tant que ce n'est pas le cas, rester sur Android.

Les montants des stores changent : à revérifier avant paiement.

## Ce qu'on a retiré du plan initial pour rester à zéro

- **Cloud Functions** (Firebase) : exigent la formule payante Blaze. Les ligues restent donc **simulées côté appareil** (équipes fantômes calculées localement), sans serveur. Les « fantômes de vrais joueurs » viendront plus tard, avec Firestore seul, si besoin.
- **Budget de lancement de 12 000 €** (publicité payante, créateurs, juridique, musique, traductions) : remplacé par du travail à la main et des ressources libres. **Pas de publicité payante.**
- **Vérification juridique par un professionnel** : remplacée par un garde-fou technique (blocage des packs payants par pays, réglable à distance) et par `docs/LEGAL.md`. Ce n'est pas un avis juridique.

## Conséquence honnête sur les prévisions

Les scénarios du dossier de concept (« Cible » : 104 000 installations dont 57 000 payantes) supposaient environ 63 k€ de publicité. **Sans publicité payante, ces chiffres ne s'appliquent plus.** Les installations viendront uniquement de l'organique (bouche-à-oreille, partages des affiches, vidéos). Il faut s'attendre à des volumes beaucoup plus faibles, mais aussi à un risque financier de zéro euro hors comptes développeur. Les seuils de rétention restent valables : ils décident si on continue, pas combien on gagne.
