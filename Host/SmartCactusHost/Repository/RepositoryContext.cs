using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repository.Configuration;

namespace Repository;

public class RepositoryContext : DbContext
{
    public RepositoryContext (DbContextOptions options)
        : base (options)
    {
        Database.EnsureDeleted();
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DeviceConfiguration());
    }

    DbSet<User>? Users { get; set; }
    DbSet<Device>? Devices { get; set; }
    DbSet<Topic>? Topics { get; set; }
    DbSet<DeviceTopic>? DeviceTopics { get; set; }
    DbSet<MqttMessage>? MqttMessages { get; set; }
    DbSet<TelegramBrokerAction>? TelegramBrokerActions { get; set; }
}
