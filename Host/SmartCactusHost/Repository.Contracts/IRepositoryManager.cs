namespace Repository.Contracts;

public interface IRepositoryManager
{
    IUserRepository User { get; }
    void Save();
}
