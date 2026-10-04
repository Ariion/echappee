# Projet Unity

Le dossier `Assets/Scripts/Game` contient les premiers scripts d'affichage. **Ils ont été écrits sans Unity sous la main et ne sont pas testés dans l'éditeur** : prévois de corriger deux ou trois détails à la première ouverture.

## Création (gratuite)

1. Installe **Unity Hub**, puis **Unity 6** avec les modules Android (et iOS si tu as un Mac). Licence **Personal** (gratuite).
2. Dans Unity Hub, crée un projet **2D** dans ce dossier `unity/` (ou crée-le ailleurs et copie `Assets/Scripts/Game`).
3. `Window > Package Manager` : installe `com.unity.nuget.newtonsoft-json` (gratuit).
4. Lance `./tools/sync-to-unity.sh` à la racine du dépôt : le cœur C# et les JSON d'équilibrage sont copiés dans `Assets/Scripts/Core` et `Assets/Resources/Data` (dossiers non versionnés, à régénérer après chaque changement).

## Première scène (étape 2 : circuit + points colorés)

1. Crée un sprite rond (carré blanc/rond de `GameObject > 2D Object > Sprites`), fais-en un prefab.
2. Place une dizaine de petits objets vides autour du circuit, dans l'ordre : ce sont les points de passage.
3. Crée un GameObject `Course` avec `RaceView` : assigne les points de passage et le prefab.
4. Crée un GameObject `Jeu` avec `GameBootstrap` : assigne `view`. Appuie sur Play.

Critère de sortie de l'étape : 5 personnes regardent la course 60 secondes sans décrocher.

## SDK à ajouter plus tard (tous gratuits)

- Firebase Unity SDK (Analytics, Remote Config, Auth, Firestore) : formule Spark. Place `google-services.json` en dehors du dépôt (il est ignoré par Git).
- Google Mobile Ads (AdMob) et son message de consentement.
- Unity IAP.

Appelle toujours `AdPolicy` / `OfferPolicy` avant d'afficher une pub ou une offre.
