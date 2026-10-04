# Points légaux à traiter (pas un avis juridique)

Je ne suis pas juriste. Cette liste sert de rappel ; à faire relire si le jeu gagne de l'argent.

1. **Taux de tirage affichés** : obligation Apple et Google pour les packs payants. Les taux viennent de `data/balance.json` (`PackService.DisplayedOdds`) : l'écran de la boutique doit les afficher tels quels.
2. **Packs à tirage payant** : certains pays les encadrent ou les interdisent (la Belgique notamment). `Compliance.PaidPackBlockedRegions` dans `data/balance.json` permet de désactiver l'ouverture avec des Watts achetés dans une liste de pays, via Remote Config, sans nouvelle version. Les packs gratuits restent ouverts. À vérifier pays par pays avant l'ouverture des marchés.
3. **Consentement publicitaire** : RGPD (Europe) et suivi publicitaire sur iOS (ATT). Utiliser le message de consentement intégré à AdMob (gratuit) avant la première publicité.
4. **Âge** : choisir la classification d'âge dans les stores ; `Compliance.MinimumAge` est prévu dans la config.
5. **Politique de confidentialité** : page publique obligatoire sur les deux stores. Elle doit citer Firebase (analytics, authentification anonyme) et AdMob.
6. **Nom** : « Échappée » est un mot courant. Chercher à l'INPI, à l'EUIPO (TMview) et dans les stores avant de communiquer.
7. **Aucun élément copié** d'un jeu existant (noms, interface, icônes, textes) : règle du projet.
