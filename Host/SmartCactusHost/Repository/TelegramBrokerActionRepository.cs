using Entities.Models;
using Repository.Contracts;

namespace Repository;

public class TelegramBrokerActionRepository : RepositoryBase<TelegramBrokerAction>, ITelegramBrokerActionRepository
{
    public TelegramBrokerActionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
    {
    }

    public void CreateAction(TelegramBrokerAction action)
    {
        Create(action);
    }

    public void DeleteAction(TelegramBrokerAction action)
    {
        Delete(action);
    }

    public TelegramBrokerAction? GetAction(Guid actionId, bool trackChanges)
    {
        return FindByCondition(a => a.Id == actionId, trackChanges).SingleOrDefault();
    }

    public IEnumerable<TelegramBrokerAction> GetAllActions(bool trackChanges)
    {
        return FindAll(trackChanges).ToList();
    }

    public IEnumerable<TelegramBrokerAction> GetPublicationActions(bool trackChanges)
    {
        return FindByCondition(a => a.EventType == Entities.Enums.EventType.Publication, trackChanges).ToList();
    }

    public IEnumerable<TelegramBrokerAction> GetSubscriptionActions(bool trackChanges)
    {
        return FindByCondition(a => a.EventType == Entities.Enums.EventType.Subscription, trackChanges).ToList();
    }
}
