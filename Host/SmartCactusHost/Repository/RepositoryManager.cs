using Repository.Contracts;

namespace Repository;

public class RepositoryManager : IRepositoryManager
{
    private readonly RepositoryContext _repositoryContext;
    private readonly Lazy<IUserRepository> _userRepository;
    private readonly Lazy<IDeviceRepository> _deviceRepository;

    public RepositoryManager(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
        _userRepository = new Lazy<IUserRepository>(() => new UserRepository(repositoryContext));
        _deviceRepository = new Lazy<IDeviceRepository>(() => new DeviceRepository(repositoryContext));
    }

    public IUserRepository User => _userRepository.Value;

    public IDeviceRepository Device => _deviceRepository.Value;

    public void Save()
    {
        _repositoryContext.SaveChanges();
    }
}
