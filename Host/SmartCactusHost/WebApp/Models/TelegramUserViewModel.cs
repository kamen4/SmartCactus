using Entities.Enums;

namespace WebApp.Models;

public class TelegramUserViewModel
{
    public Guid Id { get; set; }
    public UserRole Role { get; set; }
    public LoginStatus LoginStatus { get; set; }
    public long? TelegramId { get; set; }
    public string? TelegramUsername { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
