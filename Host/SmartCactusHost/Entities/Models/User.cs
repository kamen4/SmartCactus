using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Entities.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }
    public string? TelegramId { get; set; }
    public UserRole Role { get; set; }
}
