# Guide de contenu QuizzWare

Ce guide décrit comment écrire les cours et les quizz. **La liste de tous les sujets (catégories, chapitres, spécialisations) et leur état se trouvent dans la [carte des sujets](CARTE.md).**

Modèles à suivre : les troncs communs complets du C++ ([cpp/](../src/QuizzWare.Core/Content/cpp/)) et du C# ([csharp/](../src/QuizzWare.Core/Content/csharp/)), tout le tronc commun de Bases de données et les parcours de spécialisation déjà écrits (MongoDB, EF Core, PostgreSQL, ASP.NET Core, Kubernetes, FastAPI, System design).

## Principes

1. **Une catégorie est un parcours complet** : on doit pouvoir partir de zéro et arriver au niveau d'un entretien technique.
2. **On vérifie la théorie, puis on la met en pratique** : chaque leçon mélange questions de cours et lecture / débogage de code ou de configuration.
3. **Tout se joue sans clavier** : l'utilisateur est sur téléphone. On lui fait lire, choisir, toucher, placer ou ordonner, jamais écrire.
4. **L'explication est la vraie leçon** : elle dit pourquoi la bonne réponse est juste *et* pourquoi le piège principal est faux.

## Structure

```
Catégorie
├── Tronc commun : chapitres (2 à 4 leçons chacun)
│   ├── Leçon : 3 à 4 sections de cours courtes + ~6 questions (mélangées)
│   └── Défi entretien (optionnel) : ~6 questions dans l'ordre, débloqué quand les leçons du chapitre sont validées
└── Arbre des spécialisations : familles → technologies, bibliothèques, concepts et portes
```

- Les leçons se débloquent dans l'ordre du parcours : il faut **70 %** à la précédente.
- L'entraînement rapide ne pioche que dans les leçons déjà débloquées, avec une difficulté qui suit le niveau.
- Le test de niveau et le mode entretien tirent des questions au hasard : une question qui commence par « Cas pratique » ou par « Avec … de la leçon / précédente » en est exclue, car elle n'a de sens que dans son contexte. Les autres doivent se comprendre seules.
- Le défi garde l'ordre des questions : on peut enchaîner un **cas pratique** en plusieurs étapes (« Cas pratique 1/3 — trouve le bug », « 2/3 — quel correctif ? », « 3/3 — que se passe-t-il si… ? »).

### Contenu « à venir »

Une leçon sans `sections` ni `questions` est un **emplacement réservé** : elle s'affiche « 🕓 À venir », n'est pas jouable et **ne bloque pas** le déblocage des leçons suivantes. Idem pour un défi sans questions. On peut donc tracer tout un parcours, puis le remplir leçon par leçon dans n'importe quel ordre.

```json
{ "id": "cpp-copie", "title": "Copie profonde et superficielle", "summary": "Constructeur et affectation par copie.", "difficulty": 2 }
```

Pour **écrire** une leçon réservée : ajouter ses `sections` et ses `questions` en gardant le même `id`.

## Arbre des spécialisations

```
Bases de données (tronc commun)
├── Bases relationnelles        ← famille
│   ├── PostgreSQL              ← technologie (parcours écrit)
│   ├── Entity Framework Core   ← bibliothèque
│   └── SQLite                  ← technologie (bientôt disponible)
└── Autres horizons             ← famille de portes
    └── Intelligence artificielle ← porte vers une autre catégorie
```

| `kind` | Rôle |
|---|---|
| `famille` | Regroupe des spécialisations ; pas de parcours propre. |
| `techno`, `lib`, `concept` | Une spécialisation, avec son propre parcours de chapitres (même structure que le tronc commun). Sans chapitres jouables : « Bientôt disponible ». |
| `porte` | Mène à une **autre catégorie** : `"linkTo": "ia"`. Toujours ouverte, même vers une catégorie encore vide. |

- **Renvois** : une spécialisation avec `"linkTo": "bases-de-donnees/db-spe-efcore"` affiche le parcours d'une autre catégorie sans le dupliquer (progression partagée, déblocage par l'arbre d'origine). Un renvoi ou une porte n'a ni enfants ni chapitres ; un renvoi ne peut pas pointer vers un autre renvoi.
- `unlockedBy` : le chapitre du tronc commun qui ouvre l'arbre (seules ses leçons jouables comptent).
- `relatedCategory` relie un nœud à une autre catégorie (simple mention) ; `language` fixe le langage du code de tout le parcours.
- L'XP gagnée dans une spécialisation compte pour le niveau de la catégorie.

## Composition d'une leçon

Répartition conseillée pour ~6 questions :

| Nombre | Type | Rôle |
|---|---|---|
| 2 | `choice` | Théorie : définitions, « pourquoi », comparaisons |
| 1 | `choice` + `code` | Lire le code : « qu'affiche ce code ? », « quel est le type de… ? » |
| 1 | `bug` | Trouver la ligne fautive |
| 1 | `fill` ou `order` | Reconstituer du code |
| 1 | `multi` | Nuances, ou revue de code (« coche tous les problèmes ») |

Difficultés : environ 1/3 de 1, 1/2 de 2, et au moins une question de 3 par chapitre. Le niveau d'entraînement s'appuie dessus.

## Contraintes mobiles

- Code : **15 lignes maximum**, idéalement **moins de 45 caractères par ligne** (défilement horizontal au-delà).
- Choix de réponse courts : une ligne si possible.
- `order` : 4 à 9 lignes. L'indentation est conservée et aide à comprendre.
- `fill` : 1 à 3 trous, et 2 à 3 leurres plausibles.

## Formats JSON

Champs communs : `type`, `id` (unique dans toute l'app), `prompt`, `explanation`, `difficulty` (1 à 3), `code` (optionnel sauf pour `bug` et `fill`).

```jsonc
// QCM, réponse unique. Avec "code" : « lis le code / prédis la sortie ».
{ "type": "choice", "id": "cpp-x-q1", "prompt": "Qu'affiche ce code ?", "code": "...",
  "choices": ["A", "B", "C", "D"], "answerIndex": 0, "explanation": "...", "difficulty": 1 }

// Plusieurs bonnes réponses. Avec "code" : revue de code.
{ "type": "multi", "id": "...", "prompt": "Coche tous les problèmes.", "code": "...",
  "choices": ["...", "...", "...", "..."], "answerIndices": [0, 2], "explanation": "...", "difficulty": 3 }

// Trouve le bug : numéros de lignes acceptés, à partir de 1.
{ "type": "bug", "id": "...", "prompt": "Touche la ligne fautive.", "code": "ligne 1\nligne 2\nligne 3",
  "bugLines": [2], "explanation": "...", "difficulty": 2 }

// Code à trous : chaque ___ est un trou ; answers dans l'ordre des trous.
{ "type": "fill", "id": "...", "prompt": "Complète...", "code": "for (___ auto___ x : v)",
  "answers": ["const", "&"], "distractors": ["*", "&&"], "explanation": "...", "difficulty": 1 }

// Remettre dans l'ordre : "lines" contient l'ordre correct (l'app mélange).
{ "type": "order", "id": "...", "prompt": "Remets dans l'ordre...",
  "lines": ["for (...) {", "    ...", "}"], "explanation": "...", "difficulty": 2 }
```

Les choix, les mots et les lignes sont mélangés à l'affichage : l'ordre dans le JSON n'a pas d'importance. `dotnet test` valide tout le contenu (identifiants uniques, index dans les bornes, nombre de trous, renvois et portes…).

Langages de coloration (`language` de la catégorie, du nœud, de la section ou de la question) : `cpp`, `c`, `csharp`, `java`, `go`, `rust`, `python`, `javascript`, `sql`, `yaml`, `shell`, ou `text` pour du code sans coloration (commandes Redis, Cypher, sortie de commande).

## Organisation des fichiers

```
src/QuizzWare.Core/Content/
  <categorie>/
    category.json                ← métadonnées (sans chapitres)
    01-<chapitre>.json            ← tronc commun, chargé dans l'ordre alphabétique
    02-<chapitre>.json
    specialisations/
      arbre.json                  ← l'arbre (sans chapitres)
      <id du nœud>/01-….json      ← le parcours d'une spécialisation
```

Le chargeur accepte aussi un fichier unique `<categorie>.json` contenant tous les chapitres, pratique pour un prototype.

## Faire évoluer le contenu

La sauvegarde de l'utilisateur (`progress.json`, sur l'appareil) range sa progression par **identifiant** : leçons et défis validés, meilleurs scores, fiches de révision par question, XP par catégorie. Une mise à jour du contenu ne doit jamais lui faire perdre son avancée.

| Changement | Ce qu'il faut faire |
|---|---|
| Corriger un texte, une explication, une réponse | Rien : l'identifiant ne change pas |
| Ajouter une leçon, une question ou un chapitre | Rien de plus : l'utilisateur garde ce qu'il a validé, et une leçon insérée ne bloque pas ce qu'il avait déjà atteint |
| Renommer un identifiant publié | L'inscrire dans `src/QuizzWare.Core/Progress/renommages.json` : `"ancien": "nouveau"` |
| Supprimer un élément publié | L'inscrire dans `renommages.json` avec `null` : `"ancien": null` (l'XP déjà gagnée est conservée) |

Au chargement, la sauvegarde suit les renommages (y compris en chaîne) puis est réenregistrée. La liste `src/QuizzWare.Core/Progress/identifiants-publies.txt` contient tous les identifiants jouables déjà publiés ; elle ne fait que grandir et c'est `tools/GenerateurContenu` qui la tient à jour. Deux tests veillent :

- un identifiant de la liste absent du contenu et de `renommages.json` fait échouer `dotnet test` ;
- un identifiant du contenu absent de la liste aussi : lancer `dotnet run --project tools/GenerateurContenu` puis commiter.

Règles de déblocage qui protègent l'avancée : une leçon est ouverte si la précédente est validée **ou** si une leçon plus loin dans le même parcours l'est déjà ; un défi, si les leçons du chapitre sont validées ou si l'utilisateur a déjà avancé au-delà ; l'arbre des spécialisations reste ouvert dès qu'une leçon de spécialisation ou une leçon située après le chapitre qui l'ouvre a été validée.

## Relecture

Chaque question est relue techniquement avant d'être publiée : une réponse fausse dans une app pour ingénieurs fait perdre toute crédibilité. Pour les questions « qu'affiche ce code ? », on **compile et on exécute** l'extrait ; pour « que se passe-t-il ? », on vérifie que l'erreur de compilation annoncée est bien la bonne (en C++, sans le mode de compatibilité MSVC : `clang++ -std=c++23 -fno-ms-compatibility`).

Après publication, les utilisateurs signalent les erreurs depuis l'app (**⚑ Signaler**, sous chaque correction et chaque cours) : une issue GitHub est ouverte avec l'identifiant de la question ou de la leçon.
