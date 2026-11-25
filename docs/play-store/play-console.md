# Play Console : réponses formulaire par formulaire

Réponses à reporter dans la [Play Console](https://play.google.com/console) pour la version de test (Premium non proposé, aucune donnée collectée). Le parcours complet est dans [PUBLICATION.md](../PUBLICATION.md).

## 1. Créer l'application

Toutes les applications › **Créer une application**

| Champ | Réponse |
|---|---|
| Nom de l'application | `QuizzWare : code et entretiens` |
| Langue par défaut | Français (France) – fr-FR |
| Application ou jeu | Application |
| Gratuite ou payante | Gratuite (définitif : une app gratuite ne peut plus devenir payante ; les abonnements restent possibles) |
| Déclarations | Cocher les deux (Règles du programme pour les développeurs, lois américaines sur l'exportation) |

## 2. Contenu de l'application

Menu **Règles et programmes › Contenu de l'application** (ou la liste « Configurer votre application » du tableau de bord).

| Rubrique | Réponse |
|---|---|
| Règles de confidentialité | `https://github.com/brimetz/QuizzWare/blob/main/docs/confidentialite.md` |
| Accès à l'application | **Toutes les fonctionnalités sont disponibles sans accès spécial** (pas de compte, pas d'achat) |
| Annonces | **Non**, l'application ne contient pas d'annonces |
| Classification du contenu | Voir le point 3 |
| Public cible et contenu | Tranches **16-17 ans** et **18 ans et plus** (pas de moins de 13 ans : évite les règles « Familles »). L'app n'attire pas involontairement les enfants : Non |
| Sécurité des données | Voir le point 4 |
| Identifiant publicitaire | **Non**, l'application n'utilise pas l'identifiant publicitaire |
| Applications gouvernementales | Non |
| Fonctionnalités financières | Aucune |
| Santé | Aucune fonctionnalité liée à la santé |
| Application d'actualités | Non |

## 3. Classification du contenu (questionnaire IARC)

- Adresse e-mail : celle du compte développeur.
- Catégorie : **Référence, actualités ou éducation**.
- Toutes les questions sur la violence, la peur, la sexualité, le langage grossier, les substances, les jeux d'argent : **Non**.
- Interaction entre utilisateurs ou partage de contenu dans l'app : **Non** (« Signaler » ouvre le navigateur sur GitHub, hors de l'app).
- Partage de la position avec d'autres utilisateurs : **Non**.
- Achats de biens numériques : **Non** (Premium n'est pas proposé dans cette version).
- Accès sans restriction à internet, comme un navigateur : **Non**.

Résultat attendu : tout public (PEGI 3, ESRB Everyone).

## 4. Sécurité des données

| Question | Réponse |
|---|---|
| Votre application collecte-t-elle ou partage-t-elle des types de données utilisateur obligatoires ? | **Non** |

C'est la réponse exacte : la progression, les réglages et le journal des erreurs restent sur l'appareil, et l'app n'a pas accès à internet. Un rapport de plantage n'en sort que si l'utilisateur publie lui-même l'issue GitHub dans son navigateur, ce qui n'est pas une collecte par l'application. Sans collecte, il n'y a ni chiffrement en transit ni suppression de compte à déclarer.

À revoir si un jour l'app envoie quoi que ce soit (compte, synchronisation, remontée automatique des plantages, abonnement Premium : « Historique des achats » est alors à évaluer).

## 5. Fiche principale du Store

Menu **Développer l'audience › Présence sur le Store › Fiche principale**. Textes dans [fiche.md](fiche.md).

| Élément | Fichier ou texte |
|---|---|
| Nom (30 max) | `QuizzWare : code et entretiens` |
| Description courte (80 max) | Section « Description courte » de [fiche.md](fiche.md) |
| Description complète (4 000 max) | Section « Description complète » de [fiche.md](fiche.md) |
| Icône 512 × 512, PNG 32 bits | [icone-512.png](icone-512.png) |
| Image de présentation 1 024 × 500 | [banniere-1024x500.png](banniere-1024x500.png) |
| Captures d'écran de téléphone | 2 à 8, format portrait 9:16 (1 080 × 1 920), dossier [captures](captures/) |

**Paramètres du Store** (même menu) : catégorie **Éducation** ; adresse e-mail de contact (obligatoire, **affichée publiquement** : une adresse dédiée est conseillée) ; site web facultatif : `https://github.com/brimetz/QuizzWare`.

## 6. Test interne

**Tester et publier › Tests › Test interne**

1. Onglet **Testeurs** : Créer une liste d'adresses e-mail, nom `Proches`, y ajouter les adresses Gmail (la tienne d'abord). Enregistrer, puis cocher la liste.
2. **Créer une version** :
   - Signature d'application : **Choisir une clé de signature › Utiliser une clé générée par Google**.
   - Envoyer `src\QuizzWare\bin\Release\net10.0-android\publish\com.quizzware.app-Signed.aab`.
   - Nom de la version : `0.1.0 (1)`.
   - Notes de version :

     ```
     <fr-FR>
     Première version de test : 12 parcours complets (C, C++, C#, Java, JavaScript, Python, Go, Rust, algorithmes, bases de données, Git, tests), test de niveau, mode entretien, révision espacée et rappels.
     Une erreur dans une question ? Le bouton « ⚑ Signaler » est sous chaque correction.
     </fr-FR>
     ```

3. **Suivant**, lire les avertissements (ceux sur la désobfuscation ou les symboles natifs peuvent être ignorés), puis **Enregistrer et publier**.
4. Onglet **Testeurs** › **Copier le lien** d'inscription et l'envoyer aux testeurs. Sur le téléphone, avec le compte Google de la liste : ouvrir le lien, **Accepter l'invitation**, puis **Télécharger sur Google Play**.

Avant d'installer depuis le Play Store, désinstaller la version installée depuis l'ordinateur : elle est signée avec la clé de debug et le Play Store refuserait de la mettre à jour.

Après le premier envoi, **Configuration › Intégrité de l'application** affiche la clé d'importation : son empreinte SHA-256 doit être celle de `quizzware.keystore` (`keytool -list -v -keystore "$HOME\cles\quizzware.keystore"`).

## 7. Versions suivantes

1. Augmenter `ApplicationVersion` dans [QuizzWare.csproj](../../src/QuizzWare/QuizzWare.csproj) (2, 3…), et `ApplicationDisplayVersion` si besoin.
2. Reconstruire le `.aab` signé (point 3 de [PUBLICATION.md](../PUBLICATION.md)).
3. Test interne › **Créer une version** › envoyer le `.aab`, notes de version, publier. Les testeurs reçoivent la mise à jour par le Play Store.
