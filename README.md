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

Les troncs communs **C++** (12 chapitres) et **C#** (9 chapitres) sont entièrement écrits, chaque chapitre avec son défi entretien. Les extraits « qu'affiche ce code ? » ont été compilés et exécutés (clang++ en C++23, Roslyn en C# 13). Chaque question et chaque cours a un lien **⚑ Signaler** qui ouvre une issue GitHub pré-remplie.

Au-delà des leçons :

- **Premier lancement** : profil (étudiant → senior), centres d'intérêt (affichés en premier sur l'accueil) et objectif quotidien, puis un parcours conseillé.
- **Test de niveau** : 2 questions par chapitre du tronc commun, sans correction en cours de route ; chaque chapitre réussi sans faute est validé, et le test s'arrête au premier chapitre raté. Proposé tant qu'aucune leçon de la catégorie n'est validée.
- **Mode entretien** : simulation chronométrée (Junior 10 questions / 8 min, Confirmé 15 / 12 min, Senior 15 / 10 min), questions réparties entre les chapitres, verdict et sujets à retravailler. Les erreurs rejoignent la révision espacée.

Publication sur le Play Store : **[docs/PUBLICATION.md](docs/PUBLICATION.md)**.

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
    Progress/              Progression, déblocage, série, objectif du jour, révision espacée, rappels, persistance
  QuizzWare/               Application MAUI
    ViewModels/Questions/  Un ViewModel par type de question
    ViewModels/Code/       Coloration syntaxique, lignes de code
    Views/Controls/        Bloc de code réutilisable, sélection du template de question
    Services/              Notifications locales (Android / iOS), sons et vibrations, signalement d'erreurs
    Resources/Raw/sons/    Sons générés par tools/GenerateurSons.ps1
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
| Erreurs rejouées | Une question ratée revient en fin de quizz (remélangée, 2 reprises max) ; seule la première réponse compte pour le score, l'XP et la révision espacée |
| Objectif du jour | 10, 30 (par défaut), 50 ou 100 XP, réglable dans ⚙️ Réglages |
| Révision espacée | Chaque question répondue entre dans un système de Leitner à 6 boîtes (intervalles 1, 3, 7, 14, 30, 60 jours). Bonne réponse à l'échéance → boîte suivante ; erreur → boîte 1, à revoir demain. « 🧠 Réviser » propose jusqu'à 10 questions dues, toutes catégories confondues ; leur XP va à la catégorie de chaque question (sans bonus sans faute) |
| Rappels | Notification locale quotidienne à l'heure choisie (Android / iOS), sur 3 jours glissants ; pas de rappel le jour même si l'objectif est atteint |

Les règles sont dans [LevelSystem.cs](src/QuizzWare.Core/Leveling/LevelSystem.cs), [ProgressService.cs](src/QuizzWare.Core/Progress/ProgressService.cs), [SpacedRepetition.cs](src/QuizzWare.Core/Progress/SpacedRepetition.cs) et [ReminderPlanner.cs](src/QuizzWare.Core/Progress/ReminderPlanner.cs).

La sauvegarde (`progress.json`) porte un `SchemaVersion` et une date `UpdatedAt` : elle est prête à être synchronisée avec un futur compte utilisateur.

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
