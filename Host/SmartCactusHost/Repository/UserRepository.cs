using Repository.Contracts;
using Entities.Models;
using System.Linq.Expressions;

namespace Repository;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {
    }

    public void CreateUser(User user)
    {
        Create(user);
    }

    public void DeleteUser(User user)
    {
        Delete(user);
    }

    public IEnumerable<User> GetAllUsers(bool trackChanges)
    {
        return FindAll(trackChanges).OrderBy(u => u.TelegramId).ToList();
    }

    public IEnumerable<User> GetByIds(IEnumerable<Guid> ids, bool trackChanges)
    {
        return FindByCondition(u => ids.Contains(u.Id), trackChanges).ToList();
    }

    public User? GetUser(Guid userId, bool trackChanges)
    {
        return FindByCondition(u => u.Id.Equals(userId), trackChanges).SingleOrDefault();
    }

    public User? GetUserByCondition(Expression<Func<User, bool>> expression, bool trackChanges)
    {
        return FindByCondition(expression, trackChanges).SingleOrDefault();
    }
}
