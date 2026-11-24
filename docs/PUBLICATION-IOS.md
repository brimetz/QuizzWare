# Publier QuizzWare sur l'App Store

Ce guide mène jusqu'aux **tests TestFlight**, puis à la soumission. Les étapes marquées 👤 demandent ton compte Apple ou une décision de ta part. Pour Google Play : [PUBLICATION.md](PUBLICATION.md).

**Version de test** : l'abonnement Premium n'est pas proposé (`PremiumOffered` = `false` dans [QuizzWare.csproj](../src/QuizzWare/QuizzWare.csproj)). Les catégories Premium restent visibles, marquées « 🕓 À venir », et l'application ne contient aucun code de paiement : rien à configurer côté achats intégrés pour l'instant.

## 1. Prérequis 👤

- **Apple Developer Program** : [developer.apple.com/programs](https://developer.apple.com/programs) (99 € par an). Compte individuel : ton nom apparaît comme vendeur sur l'App Store.
- **Un Mac** avec la dernière version de **Xcode** (App Store Connect n'accepte que les builds faits avec le SDK iOS récent) et le SDK .NET 10 + `dotnet workload install maui`. Le build iOS ne peut pas se faire depuis Windows seul.

## 2. Identifiant, certificat et profil 👤

Sur [developer.apple.com/account](https://developer.apple.com/account) › Certificates, Identifiers & Profiles :

1. **Identifier** › App IDs › `+` : type App, Bundle ID explicite **`com.quizzware.app`** (doit correspondre à `ApplicationId` du `.csproj`). Aucune capacité particulière : les rappels sont des notifications locales, pas des notifications push.
2. **Certificat** › `+` › *Apple Distribution*. Le plus simple : Xcode › Réglages › Comptes › *Manage Certificates* › `+` › Apple Distribution. Le certificat et sa clé privée restent dans le trousseau du Mac. **Ne jamais les mettre dans le dépôt** (le `.gitignore` exclut `.p12`, `.cer`, `.mobileprovision`, `.p8`).
3. **Profil** › `+` › *App Store Connect*, pour `com.quizzware.app` et ce certificat. Nom conseillé : `QuizzWare App Store`. Le télécharger et l'ouvrir (double-clic) pour l'installer.

## 3. Construire l'archive `.ipa`

Sur le Mac, à la racine du dépôt :

```bash
dotnet publish src/QuizzWare -f net10.0-ios -c Release \
  -p:RuntimeIdentifier=ios-arm64 -p:ArchiveOnBuild=true \
  -p:CodesignKey="Apple Distribution: Prénom Nom (EQUIPE)" \
  -p:CodesignProvision="QuizzWare App Store"
```

Résultat : `src/QuizzWare/bin/Release/net10.0-ios/ios-arm64/publish/QuizzWare.ipa`.

À chaque envoi, augmenter dans le `.csproj` :

- `ApplicationVersion` (devient `CFBundleVersion`, doit augmenter à chaque envoi) ;
- `ApplicationDisplayVersion` (devient `CFBundleShortVersionString`, la version affichée, par exemple 0.2.0).

Ce que le dépôt fournit déjà pour Apple :

| Fichier | Rôle |
|---|---|
| [PrivacyInfo.xcprivacy](../src/QuizzWare/Platforms/iOS/PrivacyInfo.xcprivacy) | Manifeste de confidentialité exigé à l'envoi : aucun suivi, aucune donnée collectée, et les raisons d'usage des API système qu'utilisent .NET et MAUI (préférences, dates de fichiers, espace disque, temps depuis le démarrage). Sans lui, App Store Connect refuse le build. |
| [Info.plist](../src/QuizzWare/Platforms/iOS/Info.plist) | `ITSAppUsesNonExemptEncryption` = `false` : l'app n'utilise pas de chiffrement propre, la question d'export n'est plus posée à chaque build. |

## 4. App Store Connect 👤

1. [appstoreconnect.apple.com](https://appstoreconnect.apple.com) › Apps › `+` › Nouvelle app : plateforme iOS, nom « QuizzWare » (s'il est pris, « QuizzWare : quiz de code » par exemple), langue principale **français**, bundle ID `com.quizzware.app`, SKU libre (`quizzware`).
2. **Envoyer le `.ipa`** avec l'app **Transporter** (gratuite sur le Mac App Store) : glisser le fichier, *Deliver*. Le build apparaît dans l'onglet TestFlight après 10 à 30 minutes de traitement.

## 5. TestFlight : faire tester 👤

- **Testeurs internes** (jusqu'à 100 membres de ton équipe App Store Connect) : disponible dès la fin du traitement, sans revue d'Apple.
- **Testeurs externes** (jusqu'à 10 000, par adresse e-mail ou lien public) : le premier build passe une *revue bêta*, rapide et plus souple que la revue de l'App Store. Renseigner « Informations de test » : e-mail de contact, description, et « aucun identifiant nécessaire ».

Les testeurs installent l'app **TestFlight** puis QuizzWare depuis l'invitation. Un build TestFlight expire après 90 jours.

À vérifier sur un vrai iPhone : premier lancement (parcours de bienvenue), une leçon complète, les sons et vibrations, un rappel (demande d'autorisation, puis heure réglée à dans 2 minutes), le test de niveau, le mode entretien, une catégorie « À venir » et le bouton « ⚑ Signaler ».

## 6. Fiche et déclarations 👤

| Rubrique | Quoi mettre |
|---|---|
| URL des règles de confidentialité (obligatoire) | URL publique de [confidentialite.md](confidentialite.md), par exemple `https://github.com/brimetz/QuizzWare/blob/main/docs/confidentialite.md` (le dépôt doit être public) |
| URL d'assistance (obligatoire) | `https://github.com/brimetz/QuizzWare/issues` |
| Confidentialité de l'app | **Données non collectées** : tout reste sur l'appareil, les rapports ne partent que si l'utilisateur publie lui-même l'issue GitHub dans son navigateur |
| Classification par âge | Questionnaire : aucun contenu sensible → 4+ |
| Catégorie | Éducation (secondaire : Productivité ou Référence) |
| Prix | Gratuit |
| Captures d'écran | iPhone 6,9 pouces (1320 × 2868) obligatoires. L'app déclare aussi l'iPad (`UIDeviceFamily` 1 et 2) : il faut alors des captures **iPad 13 pouces** et la tester sur iPad. Pour ne publier que sur iPhone, retirer `<integer>2</integer>` de `UIDeviceFamily` dans `Info.plist`. |
| Textes | Reprendre [play-store/fiche.md](play-store/fiche.md) **sans mentionner Google Play ni Android** (règle 2.3.10) |

## 7. Avant la soumission publique : points de revue

TestFlight tolère une version incomplète ; la revue de l'App Store est plus stricte (règle **2.1, complétude de l'app**) et refuse souvent les fonctions affichées « à venir » ou « bientôt disponible ». Deux endroits sont concernés :

- les catégories Premium marquées « 🕓 À venir » (accueil, parcours de bienvenue, spécialisations, réglages) ;
- la carte « 👤 Compte · Bientôt disponible » des réglages.

Pour la sortie publique, soit Premium est prêt (point 8), soit ces éléments sont masqués plutôt qu'annoncés.

## 8. Plus tard : l'abonnement Premium sur iOS 👤

1. App Store Connect › Accords, taxes et opérations bancaires : signer l'accord **Apps payantes**, renseigner banque et fiscalité.
2. L'app › Abonnements : créer un groupe « QuizzWare Premium », puis un abonnement auto-renouvelable d'ID produit **`quizzware_premium`** (identique à `PremiumPolicy.SubscriptionProductId`), durée 1 mois, prix, et éventuellement un essai gratuit de 7 jours.
3. Passer `PremiumOffered` à `true` dans le `.csproj` : le plugin de paiement (StoreKit) est réintégré et les écrans Premium réapparaissent.
4. Règle **3.1.2** : l'écran d'achat doit afficher, en plus du prix et de la durée, des liens vers les **conditions d'utilisation** (le contrat de licence standard d'Apple suffit) et les **règles de confidentialité**. À ajouter à [PremiumPage.xaml](../src/QuizzWare/Views/PremiumPage.xaml) avant de soumettre.
5. Tester avec un compte **Sandbox** (App Store Connect › Utilisateurs › Sandbox) sur un build TestFlight.
