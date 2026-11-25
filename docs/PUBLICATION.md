# Publier QuizzWare sur le Play Store

Pour l'App Store : [PUBLICATION-IOS.md](PUBLICATION-IOS.md).

Ce guide mène jusqu'aux **tests sur Google Play**. Tout ce qui est dans le dépôt est prêt ; les étapes marquées 👤 demandent ton compte Google ou une décision de ta part.

**Version de test** : l'abonnement Premium n'est pas proposé (voir le point 6). L'application contient les 12 catégories gratuites ; les 8 autres sont visibles, marquées « 🕓 À venir ».

## 0. Prérequis

- SDK **.NET 10** et `dotnet workload install maui` (terminal administrateur), ou Visual Studio 2026 avec la charge « Développement .NET MAUI ».
- Plateforme **Android API 36** dans le SDK Android (.NET 10 compile pour Android 16, le niveau cible exigé par Google Play pour les nouvelles applications). Si elle manque (erreur `XA5207`) : Visual Studio › Outils › Android › Gestionnaire du SDK Android, ou

  ```powershell
  dotnet build src/QuizzWare -t:InstallAndroidDependencies -f net10.0-android -p:AcceptAndroidSDKLicenses=True
  ```

- L'application fonctionne à partir d'**Android 6.0** (API 23).

## 1. Essayer tout de suite sur ton téléphone, sans le Play Store

Pour un premier essai, inutile de passer par Google : sur le téléphone, activer les **Options pour les développeurs** (Paramètres › À propos › appuyer 7 fois sur « Numéro de build ») puis le **Débogage USB**, le brancher en USB et accepter l'empreinte de l'ordinateur.

```powershell
# Version Release (optimisée, comme celle du Play Store), installée et lancée sur le téléphone branché
dotnet build src/QuizzWare -t:Run -f net10.0-android -c Release
```

Hors publication, la version Release est un `.apk` installé par mise à jour : la progression est conservée d'une installation à l'autre (un `.aab` installé depuis l'ordinateur passerait par une désinstallation).

Pour l'envoyer à quelqu'un sans câble, construire un `.apk` et le partager (le destinataire autorise l'installation d'applications inconnues) :

```powershell
dotnet publish src/QuizzWare -f net10.0-android -c Release -p:AndroidPackageFormat=apk
# → src/QuizzWare/bin/Release/net10.0-android/publish/com.quizzware.app-Signed.apk
```

À vérifier : premier lancement (parcours de bienvenue), une leçon complète, les sons et vibrations, un rappel (Réglages › Rappels › « Envoyer un rappel de test » : la notification arrive 10 secondes plus tard ; un rappel réglé à dans 2 minutes ne vient pas si l'objectif du jour est déjà atteint), le test de niveau, le mode entretien, une catégorie « À venir » et le bouton « ⚑ Signaler ».

## 2. Créer la clé de signature (une seule fois) 👤

La clé signe toutes les versions envoyées au Play Store : **si tu la perds, tu ne peux plus publier de mise à jour** (sauf avec la signature gérée par Google, recommandée au point 4). Garde-la hors du dépôt, avec une sauvegarde.

```powershell
# keytool est fourni avec le JDK (celui de Visual Studio, ou celui installé par InstallAndroidDependencies)
& (Get-ChildItem "C:\Program Files\Android\jdk", "C:\Program Files\Microsoft", "$env:LOCALAPPDATA\Android" -Recurse -Filter keytool.exe -ErrorAction SilentlyContinue | Select-Object -First 1).FullName `
    -genkeypair -v -keystore "$HOME\cles\quizzware.keystore" -alias quizzware `
    -keyalg RSA -keysize 2048 -validity 10000
```

Le mot de passe demandé sert pour la keystore et pour la clé (le projet utilise le même pour les deux).

## 3. Construire le bundle signé

Le projet lit la keystore depuis deux variables d'environnement ([QuizzWare.csproj](../src/QuizzWare/QuizzWare.csproj)) : aucun secret n'est versionné.

```powershell
$env:QUIZZWARE_KEYSTORE = "$HOME\cles\quizzware.keystore"
# Saisi sans écho : le mot de passe ne reste pas dans l'historique du terminal.
$env:QUIZZWARE_KEYSTORE_PASS = [Net.NetworkCredential]::new('', (Read-Host "Mot de passe de la keystore" -AsSecureString)).Password
dotnet publish src/QuizzWare -f net10.0-android -c Release
Remove-Item Env:QUIZZWARE_KEYSTORE_PASS
```

Résultat : `src/QuizzWare/bin/Release/net10.0-android/publish/com.quizzware.app-Signed.aab`.

Sans les variables, le bundle est signé avec la clé de debug : pratique pour tester, **refusé par le Play Store**.

À chaque nouvelle version envoyée, augmenter dans le `.csproj` :

- `ApplicationVersion` (entier, +1 à chaque envoi, exigé par Google) ;
- `ApplicationDisplayVersion` (version lisible, par exemple 0.2.0).

## 4. Play Console : tester via Google Play 👤

1. Créer un compte développeur sur [play.google.com/console](https://play.google.com/console) (25 $, une fois). Google vérifie ton identité (pièce d'identité, et un téléphone Android pour un compte personnel) : compter quelques jours.
2. **Créer l'application** : nom « QuizzWare », langue par défaut français, application, gratuite. Le nom de package `com.quizzware.app` est fixé définitivement au premier envoi.
3. **Signature** : accepter la signature d'application gérée par Google Play. Ta keystore devient alors une simple « clé d'importation », remplaçable si elle est perdue.
4. **Test interne** (le plus rapide) : Tester et publier › Tests › Test interne › créer une liste de testeurs (ton adresse Gmail et celles de tes proches, 100 au maximum), créer une version, envoyer le `.aab`. Pas d'examen préalable : la version est disponible en quelques minutes. Chaque testeur ouvre le **lien d'inscription**, accepte, puis installe QuizzWare depuis le Play Store, avec ce même compte Google.
5. **Test fermé**, pour aller vers la production : un compte **personnel** récent doit faire tester l'app par **au moins 12 testeurs inscrits pendant 14 jours d'affilée** avant de pouvoir demander l'accès à la production (le test interne ne compte pas). Tests › Test fermé › créer une version avec le même `.aab`, ajouter les testeurs, leur envoyer le lien. Cette piste passe par l'examen de Google (de quelques heures à quelques jours), qui demande que les rubriques du point 5 soient remplies.

## 5. Fiche et déclarations 👤

Réponses détaillées, formulaire par formulaire (création, contenu de l'application, classification, sécurité des données, fiche, test interne) : **[play-store/play-console.md](play-store/play-console.md)**.

| Rubrique | Quoi mettre |
|---|---|
| Fiche du Store | Textes de [play-store/fiche.md](play-store/fiche.md), icône `play-store/icone-512.png`, bannière `play-store/banniere-1024x500.png`, au moins 2 captures d'écran de téléphone |
| Règles de confidentialité | URL publique de [confidentialite.md](confidentialite.md), par exemple `https://github.com/brimetz/QuizzWare/blob/main/docs/confidentialite.md` (le dépôt doit être public) |
| Sécurité des données | **Aucune donnée collectée ni partagée** : tout reste sur l'appareil. Pas de chiffrement en transit à déclarer (aucune donnée envoyée). Les rapports de plantage restent sur l'appareil ; ils ne sont transmis que si l'utilisateur les publie lui-même sur GitHub depuis son navigateur, ce qui ne constitue pas une collecte par l'application. Si un service de remontée automatique (Sentry…) était ajouté un jour, il faudrait déclarer « Plantages » et « Diagnostics ». |
| Publicités | Non |
| Accès à l'application | Tout est accessible sans identifiants : pas de compte, pas d'achat |
| Classification du contenu | Questionnaire IARC : catégorie « Référence, actualités ou éducation », aucune violence, aucun contenu sensible → classé tout public |
| Public cible | 16 ans et plus (l'app ne vise pas les enfants, ce qui évite les exigences « Familles ») |
| Catégorie | Éducation |
| Coordonnées | Une adresse e-mail de contact (obligatoire, affichée sur la fiche) |

Permissions déclarées par l'app (rien à justifier) : notifications, démarrage de l'appareil (reprogrammer les rappels), vibration. Aucun accès internet : le bouton « Signaler » ouvre simplement le navigateur.

## 6. Abonnement Premium 👤

**Pour l'instant, Premium n'est pas proposé** : `PremiumOffered` vaut `false` dans [QuizzWare.csproj](../src/QuizzWare/QuizzWare.csproj). Les catégories Premium restent visibles, marquées « 🕓 À venir », sans achat possible, et le plugin de paiement n'est pas inclus dans l'application. Passer `PremiumOffered` à `true` rétablit tout ce qui suit.

Ce qui est gratuit et ce qui est Premium est défini à un seul endroit : [PremiumPolicy.cs](../src/QuizzWare.Core/Premium/PremiumPolicy.cs) (catégories gratuites, nombre d'entretiens gratuits par semaine, identifiant de l'abonnement). En **Debug**, l'abonnement est simulé (aucun paiement, bouton d'achat qui demande une confirmation) ; en **Release** sur Android, il passe par Google Play Billing.

**Protection du droit Premium** : l'application ne garde jamais un simple « oui ». Elle n'accepte que les achats **signés par Google** pour `com.quizzware.app` et `quizzware_premium` ([PlayPurchaseVerifier.cs](../src/QuizzWare.Core/Premium/PlayPurchaseVerifier.cs)), garde la date de la dernière vérification **chiffrée** par le Keystore d'Android (`SecureStorage`), exclue des sauvegardes, et la considère comme expirée après **3 jours** sans nouvelle vérification ([PremiumEntitlement.cs](../src/QuizzWare.Core/Premium/PremiumEntitlement.cs)). Cela bloque la modification des fichiers et des sauvegardes, pas une application modifiée (il faudrait pour cela un serveur, et que le contenu Premium soit téléchargé plutôt qu'embarqué).

1. **Profil de paiement** : Play Console › Paramètres › Profil de paiement. Renseigner les coordonnées bancaires et fiscales. Vendre impose le statut de **professionnel** (règlement européen DSA) : adresse, téléphone et e-mail de contact deviennent publics sur la fiche.
2. **Envoyer d'abord un `.aab`** contenant la facturation (cette version) sur une piste de test : la Play Console ne permet de créer des abonnements qu'après.
3. **Clé de licence** : Monétiser › Configuration de la monétisation › Licences : copier la clé publique (base 64) dans `PlayLicenseKey` de [StoreSubscriptionService.cs](../src/QuizzWare/Services/StoreSubscriptionService.cs). Elle est publique : ce n'est pas un secret. **Tant qu'elle est vide, aucun achat n'est accepté.**
4. **Créer l'abonnement** : Monétiser › Produits › Abonnements › Créer.
   - ID produit : `quizzware_premium` (doit correspondre exactement à `PremiumPolicy.SubscriptionProductId`).
   - Forfait de base : renouvellement automatique, période **1 mois**, prix (par exemple 4,99 €). Google convertit les prix pour les autres pays.
   - Offre conseillée : **essai gratuit de 7 jours** pour les nouveaux abonnés.
   - Activer le forfait de base et l'offre.
5. **Testeurs de licence** : Paramètres › Test de licence › ajouter ton adresse Gmail. Leurs achats ne sont pas débités, et un abonnement mensuel se renouvelle toutes les 5 minutes, ce qui permet de tester renouvellement et expiration.
6. **Tester** sur un téléphone avec un compte testeur, en installant l'appli **depuis le lien de la piste de test** (pas avec `dotnet build -t:Run`, que Google Play Billing ne reconnaît pas) :
   - une catégorie Premium (par exemple Architecture système) ouvre la page Premium ;
   - le prix s'affiche, l'achat aboutit, les catégories se débloquent ;
   - « Restaurer mes achats » après une réinstallation ;
   - après résiliation dans Google Play et expiration, les catégories se reverrouillent au retour dans l'appli ;
   - hors ligne, Premium reste actif 3 jours, puis se reverrouille jusqu'à la prochaine connexion.
7. **Fiche du Store** : préciser « Achats via l'application » et mentionner Premium dans la description.

## 7. Captures d'écran

Le Play Store demande au moins 2 captures de téléphone (format portrait, 1080 × 1920 conseillé). Écrans conseillés, sur un émulateur ou un téléphone :

1. L'accueil avec l'objectif du jour et les catégories ;
2. Une question « Trouve le bug » avec la correction ;
3. Le parcours d'une catégorie (C++) ;
4. Le bilan du mode entretien ;
5. L'arbre des spécialisations.
