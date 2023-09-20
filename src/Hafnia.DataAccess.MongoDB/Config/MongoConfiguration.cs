namespace Hafnia.DataAccess.MongoDB.Config;

public class MongoConfiguration
{
    public const string Section = "mongo";

    public required string ConnectionString { get; init; }
    public required string Database { get; init; }
}
