namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal interface IAsyncMapper<in TIn,TOut>
{
    Task<TOut> MapAsync(TIn toMap, CancellationToken cancellationToken = default);
}
