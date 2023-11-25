namespace Hafnia.DataAccess.MongoDB.Cache;

internal interface IEntityCache<T>
{
    Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default);

    Task Clear();
}
