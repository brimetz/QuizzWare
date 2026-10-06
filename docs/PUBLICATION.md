# Publier QuizzWare sur le Play Store

Ce guide mène jusqu'au **test fermé** sur Google Play. Tout ce qui est dans le dépôt est prêt ; les étapes marquées 👤 demandent ton compte Google ou une décision de ta part.

## 1. Créer la clé de signature (une seule fois) 👤

La clé signe toutes les versions de l'app : **si tu la perds, tu ne peux plus publier de mise à jour** (sauf avec la signature gérée par Google, recommandée au point 4). Garde-la hors du dépôt, avec une sauvegarde.

```powershell
& "C:\Program Files\Android\jdk\jdk-8.0.302.8-hotspot\jdk8u302-b08\bin\keytool.exe" `
    -genkeypair -v -keystore "$HOME\cles\quizzware.keystore" -alias quizzware `
    -keyalg RSA -keysize 2048 -validity 10000
```

Le mot de passe demandé sert pour la keystore et pour la clé (le projet utilise le même pour les deux).

## 2. Construire le bundle signé

Le projet lit la keystore depuis deux variables d'environnement ([QuizzWare.csproj](../src/QuizzWare/QuizzWare.csproj)) : aucun secret n'est versionné.

```powershell
$env:QUIZZWARE_KEYSTORE = "$HOME\cles\quizzware.keystore"
$env:QUIZZWARE_KEYSTORE_PASS = "ton mot de passe"
dotnet publish src/QuizzWare -f net9.0-android -c Release
```

Résultat : `src/QuizzWare/bin/Release/net9.0-android/publish/com.quizzware.app-Signed.aab`.

Sans les variables, le bundle est signé avec la clé de debug : pratique pour tester, **refusé par le Play Store**.

À chaque nouvelle version envoyée, augmenter dans le `.csproj` :

- `ApplicationVersion` (entier, +1 à chaque envoi, exigé par Google) ;
- `ApplicationDisplayVersion` (version lisible, par exemple 0.2.0).

## 3. Tester la version Release sur un vrai téléphone

La version Release est compilée différemment de la version Debug (optimisations, suppression du code inutilisé) : il faut l'essayer avant de l'envoyer.

```powershell
dotnet build src/QuizzWare -t:Run -f net9.0-android -c Release
```

À vérifier : premier lancement (parcours de bienvenue), une leçon complète, les sons et vibrations, un rappel (régler l'heure à dans 2 minutes), le test de niveau et le mode entretien.

## 4. Play Console 👤

1. Créer un compte développeur sur [play.google.com/console](https://play.google.com/console) (25 $, une fois). Un compte **personnel** récent doit faire un **test fermé avec au moins 12 testeurs pendant 14 jours** avant de pouvoir publier en production.
2. **Créer l'application** : nom « QuizzWare », langue par défaut français, application gratuite.
3. **Signature** : accepter la signature d'application gérée par Google Play. Ta keystore devient alors une simple « clé d'importation », remplaçable si elle est perdue.
4. **Test fermé** : Tests › Test fermé › créer une version, envoyer le `.aab`, ajouter une liste de testeurs (adresses Gmail) et leur envoyer le lien d'inscription.

## 5. Fiche et déclarations 👤

| Rubrique | Quoi mettre |
|---|---|
| Fiche du Store | Textes de [play-store/fiche.md](play-store/fiche.md), icône `play-store/icone-512.png`, bannière `play-store/banniere-1024x500.png`, au moins 2 captures d'écran de téléphone |
| Règles de confidentialité | URL publique de [confidentialite.md](confidentialite.md), par exemple `https://github.com/brimetz/QuizzWare/blob/main/docs/confidentialite.md` (le dépôt doit être public) |
| Sécurité des données | **Aucune donnée collectée ni partagée** : tout reste sur l'appareil. Pas de chiffrement en transit à déclarer (aucune donnée envoyée). |
| Publicités | Non |
| Accès à l'application | Tout est accessible sans identifiants |
| Classification du contenu | Questionnaire IARC : catégorie « Référence, actualités ou éducation », aucune violence, aucun contenu sensible → classé tout public |
| Public cible | 16 ans et plus (l'app ne vise pas les enfants, ce qui évite les exigences « Familles ») |
| Catégorie | Éducation |
| Coordonnées | Une adresse e-mail de contact (obligatoire, affichée sur la fiche) |

Permissions déclarées par l'app (rien à justifier) : notifications, démarrage de l'appareil (reprogrammer les rappels), vibration. Aucun accès internet : le bouton « Signaler » ouvre simplement le navigateur.

## 6. Captures d'écran

Le Play Store demande au moins 2 captures de téléphone (format portrait, 1080 × 1920 conseillé). Écrans conseillés, sur un émulateur ou un téléphone :

1. L'accueil avec l'objectif du jour et les catégories ;
2. Une question « Trouve le bug » avec la correction ;
3. Le parcours d'une catégorie (C++) ;
4. Le bilan du mode entretien ;
5. L'arbre des spécialisations.
