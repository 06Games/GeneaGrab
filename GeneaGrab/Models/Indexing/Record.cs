using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using GeneaGrab.Core.Models;

namespace GeneaGrab.Models.Indexing;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Record
{
    public Record(Registry registry, RPage page) : this(registry.ProviderID, registry.ID, page.Number)
    {
        CallNumber = registry.CallNumber;
    }
    public Record(string providerId, string registryId, int frameNumber)
    {
        ProviderId = providerId;
        RegistryId = registryId;
        FrameNumber = frameNumber;
    }

    /// <summary>Automatically generated record id</summary>
    public int Id { get; private set; }

    // ============ Document ============
    /// <summary>(Internal) Id of the platform of the document</summary>
    public string ProviderId { get; private set; }
    /// <summary>(Internal) Id of the document</summary>
    public string RegistryId { get; private set; }
    /// <summary>Call number of the document (if applicable)</summary>
    public string? CallNumber { get; set; }
    /// <summary>Frame number</summary>
    /// <remarks>A frame can contain multiple pages</remarks>
    public int FrameNumber { get; private set; }
    /// <summary>Ark url</summary>
    public Uri? ArkUrl { get; set; }
    /// <summary>Page number (if applicable)</summary>
    public string? PageNumber { get; set; }


    // ============ Record ============
    /// <summary>Sequence number (if applicable)</summary>
    public string? SequenceNumber { get; set; }
    /// <summary>Position of the record on the vue</summary>
    public Rect? Position { get; set; }
    /// <summary>Type of record</summary>
    public RegistryType Type { get; set; }
    /// <summary>Date of the record</summary>
    public DateTime? Date { get; set; }

    /// <summary>City of record</summary>
    public string? City { get; set; }
    /// <summary>Parish of record (if applicable)</summary>
    public string? Parish { get; set; }
    /// <summary>District of record (if applicable)</summary>
    public string? District { get; set; }
    /// <summary>Road of record (if applicable)</summary>
    public string? Road { get; set; }

    /// <summary>Persons linked to the record</summary>
    public IEnumerable<Person> Persons { get; private set; } = new List<Person>();
    /// <summary>Field for any remaining info</summary>
    public string? Notes { get; set; }


    public override string ToString() => $"#{Id} {Position}";
}
