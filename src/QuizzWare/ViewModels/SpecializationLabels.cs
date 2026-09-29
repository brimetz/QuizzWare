using QuizzWare.Core.Models;

namespace QuizzWare.ViewModels;

public static class SpecializationLabels
{
    public static string Kind(string kind) => kind switch
    {
        SpecializationKind.Family => "Famille",
        SpecializationKind.Library => "Bibliothèque",
        SpecializationKind.Concept => "Concept",
        SpecializationKind.Door => "Catégorie",
        _ => "Technologie",
    };
}
