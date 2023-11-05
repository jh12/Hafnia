using System.Diagnostics;
using MongoDB.Bson;

namespace Hafnia.DataAccess.MongoDB.Models.V2;

internal record Tag
(
    ObjectId Id,
    string Name,
    ObjectId[] Children,
    string[] Patterns
)
{
    public override string? ToString()
    {
        return Name;
    }
}
