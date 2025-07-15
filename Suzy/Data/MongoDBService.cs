using MongoDB.Driver;
using MongoDB.Bson;

public class MongoDBService
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoDBService()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("MyStudyApp");
        _collection = database.GetCollection<BsonDocument>("users");
    }

    public async Task InsertSampleDataAsync()
    {
        var document = new BsonDocument
        {
            { "name", "Mai" },
            { "email", "mai@example.com" }
        };
        await _collection.InsertOneAsync(document);
    }
}
