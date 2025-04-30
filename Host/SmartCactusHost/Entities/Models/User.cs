using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Entities.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }
    public UserRole Role { get; set; }
    public LoginStatus LoginStatus { get; set; }
    
    // Telegram data
    public long? TelegramId { get; set; }
    public long? TelegramChatId { get; set; }
    public string? TelegramUsername { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
