using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class Document
{
    public Guid DocumentId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? FileType { get; set; }

    public string? FileUrl { get; set; }

    public string? CoverType { get; set; }

    public string? CoverGradient { get; set; }

    public string? Pages { get; set; }

    public int? DownloadsCount { get; set; }

    public decimal? Rating { get; set; }

    public string? Tag { get; set; }

    public string? DocumentType { get; set; }

    public DateTime? CreatedAt { get; set; }
}
