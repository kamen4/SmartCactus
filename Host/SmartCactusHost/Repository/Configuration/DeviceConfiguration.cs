using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configuration;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder
            .HasMany(d => d.Topics)
            .WithMany(t => t.Devices)
            .UsingEntity<DeviceTopic>();

        builder.HasData(new Device()
        {
            Id = Guid.NewGuid(),
            MqttClientId = "TELEGRAM_DEVICE",
        });
    }
}
