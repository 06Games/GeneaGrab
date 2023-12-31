#nullable enable
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GeneaGrab.Core.Models;

/// <summary>Frame of the registry</summary>
/// <remarks>A frame can contain multiple pages</remarks>
[PrimaryKey(nameof(ProviderId), nameof(RegistryId), nameof(FrameNumber))]
public class Frame
{
    /// <summary>Associated provider id</summary>
    public string ProviderId { get; set; } = null!;
    /// <summary>Associated registry id</summary>
    public string RegistryId { get; set; } = null!;
    /// <summary>Associated registry</summary>
    public Registry? Registry { get; set; }
    /// <summary>Frame number</summary>
    public int FrameNumber { get; set; }

    /// <summary>Ark URL</summary>
    public string? ArkUrl { get; set; }
    /// <summary>Used internally to download the image</summary>
    public string? DownloadUrl { get; set; }

    /// <summary>Size of largest locally available image version</summary>
    public Scale ImageSize { get; set; }
    /// <summary>Total width of the original image</summary>
    public int? Width { get; set; }
    /// <summary>Total height of the original image</summary>
    public int? Height { get; set; }
    /// <summary>Tiles size (if applicable)</summary>
    public int? TileSize { get; set; }

    /// <summary>Notes about the page (user can edit this information)</summary>
    public string? Notes { get; set; }
    /// <summary>Any additional information the grabber needs</summary>
    [NotMapped] public object? Extra { get; set; }

    public override string ToString() => FrameNumber.ToString();
}
/// <summary>Available image size</summary>
public enum Scale
{
    /// <summary>The image isn't locally available</summary>
    /// <remarks>This means that the image has not been downloaded (and perhaps cannot be)</remarks>
    Unavailable,
    /// <summary>Only a reduced size for thumbnail use is locally available</summary>
    /// <remarks>~128-512 px</remarks>
    Thumbnail,
    /// <summary>A reduced image size suitable for navigation is locally available</summary>
    /// <remarks>~1024-2048 px</remarks>
    Navigation,
    /// <summary>Full-resolution image is locally available</summary>
    /// <remarks>Greater than 2048 px, or less if the original image is low resolution</remarks>
    Full
}
