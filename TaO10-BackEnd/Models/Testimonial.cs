using System;
using System.Collections.Generic;

namespace TaO10_BackEnd.Models;

public partial class Testimonial
{
    public Guid TestimonialId { get; set; }

    public string Text { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Meta { get; set; }

    public char? Initial { get; set; }

    public string? AvatarBg { get; set; }

    public int? Rating { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsFeatured { get; set; }
}
