namespace Repository.Contracts;

public interface IRepositoryManager
{
    IUserRepository User { get; }
    IDeviceRepository Device { get; }
    void Save();
}
