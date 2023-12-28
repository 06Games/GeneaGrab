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
    public Relation Relation { get; set; }
    public string? Notes { get; set; }
}
public enum Sex { Other = -1, Male, Female }
public enum Relation
{
    /// <summary>Any other relation</summary>
    Other = -1,

    /// <summary>Main individual of the record</summary>
    Main,
    /// <summary>Father of the main individual</summary>
    Father,
    /// <summary>Mother of the main individual</summary>
    Mother,
    /// <summary>Spouse of the main individual</summary>
    Spouse,
    /// <summary>Child of the main individual</summary>
    Child,
    /// <summary>A family member of the main individual</summary>
    Family,
    /// <summary>A neighbor of the main individual</summary>
    Neighbor,
    /// <summary>An acquaintance of the main individual</summary>
    Acquaintance
}
