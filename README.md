# QuizzWare

Application mobile d'apprentissage pour développeurs et ingénieurs software, façon Duolingo : des cours courts suivis de quizz, organisés par catégorie, avec un niveau qui progresse par catégorie.

**20 catégories** : Architecture système · Architecture infra · Réseau · C++ · C# · C · Python · Bases de données · Algorithmes · JavaScript / TypeScript · Java · Go · Rust · Sécurité · Systèmes d'exploitation · Git · Tests et qualité · Frontend · Mobile · Intelligence artificielle. La liste complète des sujets et leur état : **[docs/CARTE.md](docs/CARTE.md)**.

Chaque catégorie a un **tronc commun** en chapitres (des leçons, cours + quizz, puis un **défi entretien**) et un **arbre de spécialisations** (technologies, bibliothèques, concepts, et portes vers d'autres catégories). Les questions se jouent sans clavier :

| Type | Interaction |
|---|---|
| QCM / « lis le code » | Choisir une réponse, éventuellement à partir d'un extrait de code |
| Plusieurs réponses / revue de code | Cocher toutes les bonnes réponses |
| Trouve le bug | Toucher la ligne fautive |
| Complète le code | Placer des mots dans les trous |
| Remets dans l'ordre | Toucher les lignes dans le bon ordre |

## Stack

- **.NET 9 / .NET MAUI** (Android, iOS, Windows pour le dev)
- **MVVM** avec CommunityToolkit.Mvvm
- **100 % local** : contenu embarqué en JSON, progression sauvegardée dans `progress.json` (dossier de données de l'app)

## Structure

```
src/
  QuizzWare.Core/          Logique métier, sans dépendance à MAUI (testable)
    Content/<categorie>/   Cours et quizz : un fichier par chapitre + specialisations/
    Content/               Chargement + validation du contenu
    Leveling/              Règles d'XP et de niveaux
    Models/                Catégorie → chapitres → leçons / défi, types de questions
    Quiz/                  Préparation (mélange), correction et déroulement d'un quizz
    Progress/              Progression, déblocage, série de jours, persistance
  QuizzWare/               Application MAUI
    ViewModels/Questions/  Un ViewModel par type de question
    ViewModels/Code/       Coloration syntaxique, lignes de code
    Views/Controls/        Bloc de code réutilisable, sélection du template de question
tests/
  QuizzWare.Core.Tests/    Tests xUnit du Core (y compris validation du contenu JSON)
docs/
  CONTENU.md               Guide de rédaction (formats, règles, contraintes mobiles)
  CARTE.md                 Carte des sujets (générée)
tools/
  plan-des-sujets.txt      Plan du grand arbre : catégories, chapitres, spécialisations
  GenerateurContenu/       Génère la structure du contenu et la carte à partir du plan
```

## Règles de progression

| Règle | Valeur |
|---|---|
| XP par bonne réponse | 10 × difficulté de la question (1 à 3) |
| Bonus quizz sans faute | +20 XP |
| XP pour passer du niveau N à N+1 | 100 + 50 × (N − 1) |
| Niveau max | 10 (≈ 2 700 XP) |
| Titres | Débutant (1-2), Junior (3-4), Confirmé (5-6), Senior (7-8), Expert (9), Maître (10) |
| Validation d'une leçon | ≥ 70 % de bonnes réponses, débloque la leçon suivante (y compris d'un chapitre à l'autre) |
| Défi entretien | Débloqué quand toutes les leçons du chapitre sont validées ; facultatif pour avancer |
| Entraînement rapide | 5 questions tirées des leçons débloquées ; difficulté max 1 (niv. 1-3), 2 (niv. 4-6), 3 (niv. 7+) |
| Série (streak) | +1 par jour consécutif avec au moins un quizz |

Les règles sont dans [LevelSystem.cs](src/QuizzWare.Core/Leveling/LevelSystem.cs) et [ProgressService.cs](src/QuizzWare.Core/Progress/ProgressService.cs).

## Démarrer

Prérequis : SDK .NET 9 et le workload MAUI (une fois, dans un terminal **administrateur**) :

```powershell
dotnet workload install maui
```

Pour Android, il faut aussi le SDK Android (installé avec Visual Studio « Développement .NET MAUI », ou via Android Studio).

```powershell
# Tests du Core (ne nécessite pas MAUI)
dotnet test tests/QuizzWare.Core.Tests

# Lancer sur Windows (le plus rapide pour itérer)
dotnet build src/QuizzWare -t:Run -f net9.0-windows10.0.19041.0

# Lancer sur un émulateur / téléphone Android connecté
dotnet build src/QuizzWare -t:Run -f net9.0-android
```

iOS nécessite un Mac (appairé depuis Visual Studio ou build directement sur macOS).

## Ajouter du contenu

- **Écrire une leçon** (remplir une leçon « à venir ») : modifier son fichier JSON dans `src/QuizzWare.Core/Content/`. Voir **[docs/CONTENU.md](docs/CONTENU.md)** pour le format de chaque type de question et les règles de rédaction.
- **Modifier la structure** (catégories, chapitres, arbres de spécialisations) : modifier `tools/plan-des-sujets.txt` puis lancer `dotnet run --project tools/GenerateurContenu`. Voir **[tools/README.md](tools/README.md)**.

Le contenu est validé au démarrage et par `dotnet test` (identifiants uniques, index valides, nombre de trous, etc.) : une erreur dans un JSON est détectée avant d'arriver sur le téléphone.
