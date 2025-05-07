using System.Linq.Expressions;
using Entities.Models;

namespace Repository.Contracts;

public interface IUserRepository
{
    IEnumerable<User> GetAllUsers(bool trackChanges);
    User? GetUser(Guid userId, bool trackChanges);
    User? GetUserByCondition(Expression<Func<User, bool>> expression, bool trackChanges);
    void CreateUser(User user);
    void DeleteUser(User user);
}
