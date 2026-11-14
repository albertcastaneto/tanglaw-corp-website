using System.ComponentModel.DataAnnotations;

namespace TanglawSite.Models;

public class ContactFormModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Company { get; set; }

    [Required(ErrorMessage = "Please enter a message.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Honeypot field: real users leave it empty, bots tend to fill it.</summary>
    public string? Website { get; set; }
}
