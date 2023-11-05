namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal interface IMapper<in TIn, out TOut>
{
    TOut Map(TIn toMap);
}
