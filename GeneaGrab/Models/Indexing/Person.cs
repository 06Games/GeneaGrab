using System.Diagnostics.CodeAnalysis;

namespace GeneaGrab.Models.Indexing;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Person
{
    public int Id { get; private set; }
    public int RecordId { get; set; }
    public Record? Record { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Sex? Sex { get; set; }
    public string? Age { get; set; }
    public string? CivilStatus { get; set; }
    public string? PlaceOrigin { get; set; }
    public string? Notes { get; set; }
}
public enum Sex { Other = -1, Male, Female }
