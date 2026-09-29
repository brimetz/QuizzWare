# Outils

## Générateur de la structure du contenu

`plan-des-sujets.txt` décrit **tout le grand arbre** de QuizzWare : les catégories, les chapitres et leçons du tronc commun, et les arbres de spécialisations. `GenerateurContenu` le transforme en fichiers JSON dans `src/QuizzWare.Core/Content/` et régénère la [carte des sujets](../docs/CARTE.md).

```powershell
dotnet run --project tools/GenerateurContenu
dotnet test tests/QuizzWare.Core.Tests   # valide le résultat (identifiants, renvois, portes…)
```

### Ce que fait le générateur, et ce qu'il ne fait pas

| Élément | Comportement |
|---|---|
| Nouvelle catégorie (`@cat`) | `category.json` créé s'il n'existe pas encore |
| Chapitre (`#ch`) absent | Créé avec ses leçons « à venir » |
| Chapitre déjà présent (`#ch` ou `#keep`) | **Jamais modifié** : seulement renuméroté selon le plan. Le contenu écrit est préservé. |
| Arbre de spécialisations | `specialisations/arbre.json` **entièrement régénéré** depuis le plan |
| Parcours d'une spécialisation (`specialisations/<id>/`) | Jamais touché |
| `docs/CARTE.md` | Régénérée à partir du plan et du contenu réel (✅ / 🟡 / 🕓) |

Conséquences :

- Pour **modifier un arbre** (ajouter une spécialisation, une porte…), on modifie le **plan**, puis on relance le générateur. Une modification faite à la main dans `arbre.json` serait écrasée.
- Pour **écrire une leçon** ou modifier un chapitre existant, on modifie directement son **fichier JSON** (voir le [guide de contenu](../docs/CONTENU.md)). Le générateur n'y touchera plus.
- Pour **ajouter un chapitre**, on l'ajoute au plan à la bonne place : les chapitres suivants sont renumérotés automatiquement.

### Syntaxe du plan

```
// commentaire
@cat id | ordre | nom | icône | couleur | langage (ou -) | description     nouvelle catégorie
@existing id                                                              catégorie existante
@unlock chapitre-id                                                       chapitre qui ouvre l'arbre
#keep NN chapitre-id                                                      chapitre existant, en position NN
#ch NN chapitre-id | titre | résumé                                       chapitre (créé « à venir » s'il manque)
- lecon-id | titre | résumé | difficulté (1 à 3)                          leçon du chapitre précédent
+ famille-id | titre | résumé | icône                                     famille de spécialisations
* noeud-id | titre | résumé | icône | kind [| rel=… | lang=… | link=…]    spécialisation de la famille précédente
```

`kind` : `techno`, `lib`, `concept` ou `porte`. Options : `rel=` catégorie liée, `lang=` langage du code, `link=` renvoi (`categorie/noeud`) ou, pour une porte, catégorie visée (`categorie`).
