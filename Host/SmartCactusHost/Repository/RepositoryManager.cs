using Repository.Contracts;

namespace Repository;

public class RepositoryManager : IRepositoryManager
{
    private readonly RepositoryContext _repositoryContext;
    private readonly Lazy<IUserRepository> _userRepository;
    private readonly Lazy<IDeviceRepository> _deviceRepository;
    private readonly Lazy<ITopicRepository> _topicRepositiry;
    private readonly Lazy<IDeviceTopicRepository> _deviceTopicRepository;
    private readonly Lazy<IMessageRepository> _messageRepository;

    public RepositoryManager(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
        _userRepository = new Lazy<IUserRepository>(() => new UserRepository(repositoryContext));
        _deviceRepository = new Lazy<IDeviceRepository>(() => new DeviceRepository(repositoryContext));
        _topicRepositiry = new Lazy<ITopicRepository>(() => new TopicRepository(repositoryContext));
        _deviceTopicRepository = new Lazy<IDeviceTopicRepository>(() => new DeviceTopicRepository(repositoryContext));
        _messageRepository = new Lazy<IMessageRepository>(() => new MessageRepository(repositoryContext));
    }

    public IUserRepository User => _userRepository.Value;
    public IDeviceRepository Device => _deviceRepository.Value;
    public ITopicRepository Topic => _topicRepositiry.Value;
    public IDeviceTopicRepository DeviceTopic => _deviceTopicRepository.Value;
    public IMessageRepository Message => _messageRepository.Value;

    public void Save()
    {
        _repositoryContext.SaveChanges();
    }
}
