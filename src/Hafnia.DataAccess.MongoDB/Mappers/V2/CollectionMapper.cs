using Hafnia.DataAccess.MongoDB.Models.V2;

namespace Hafnia.DataAccess.MongoDB.Mappers.V2;

internal class CollectionMapper : IMapper<Collection, DTOs.Collection>
{
    public DTOs.Collection Map(Collection toMap)
    {
        return new DTOs.Collection
        (
            Id: toMap.Id.ToString(),
            Name: toMap.Name,
            ThumbnailId: toMap.Thumbnail.ToString(),
            IncludedTags: toMap.IncludedTags?.Select(t => t.ToString()).ToArray() ?? Array.Empty<string>(),
            ExcludedTags: toMap.ExcludedTags?.Select(t => t.ToString()).ToArray() ?? Array.Empty<string>(),
            Children: toMap.Children?.Select(c => c.ToString()).ToArray() ?? Array.Empty<string>(),
            IsRoot: toMap.IsRoot
        );
    }
}
