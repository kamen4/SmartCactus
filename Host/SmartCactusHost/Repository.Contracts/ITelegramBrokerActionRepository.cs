using Entities.Models;

namespace Repository.Contracts;

public interface ITelegramBrokerActionRepository
{
    TelegramBrokerAction? GetAction(Guid actionId, bool trackChanges);
    IEnumerable<TelegramBrokerAction> GetAllActions(bool trackChanges);
    IEnumerable<TelegramBrokerAction> GetSubscriptionActions(bool trackChanges);
    IEnumerable<TelegramBrokerAction> GetPublicationActions(bool trackChanges);
    void CreateAction(TelegramBrokerAction action);
    void DeleteAction(TelegramBrokerAction action);
}
