using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models.V2;

internal record Tag
(
    ObjectId Id,
    string Name,
    ObjectId? Parent,
    ObjectId[]? Ancestors,
    ObjectId[] AncestorsAndSelf,
    string[] Patterns
)
{
    public override string ToString()
    {
        return Name;
    }
}
