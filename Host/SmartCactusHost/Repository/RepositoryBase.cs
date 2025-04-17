using Repository.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Repository;

public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected RepositoryContext _RepositoryContext { get; set; }

    public RepositoryBase(RepositoryContext repositoryContext)
        => _RepositoryContext = repositoryContext;

    public IQueryable<T> FindAll(bool trackChanges)
    {
        if (!trackChanges)
            return _RepositoryContext.Set<T>().AsNoTracking();
        return _RepositoryContext.Set<T>();
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges)
    {
        if (!trackChanges)
            return _RepositoryContext.Set<T>().Where(expression).AsNoTracking();
        return _RepositoryContext.Set<T>().Where(expression);
    }

    public void Create(T entity)
    {
        _RepositoryContext.Set<T>().Add(entity);
    }

    public void Update(T entity)
    {
        _RepositoryContext.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        _RepositoryContext.Set<T>().Remove(entity);
    }
}
