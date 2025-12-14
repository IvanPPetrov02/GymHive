using MongoDB.Driver;
using MongoDB.Bson;

namespace MembershipService;

public class MigrateMemberships
{
    public static async Task Main(string[] args)
    {
        var connectionString = "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GymHiveMembershipsV2");
        var collection = database.GetCollection<BsonDocument>("memberships");

        // Add AutoRenew field to all documents that don't have it
        var filter = Builders<BsonDocument>.Filter.Exists("AutoRenew", false);
        var update = Builders<BsonDocument>.Update.Set("AutoRenew", false);
        
        var result = await collection.UpdateManyAsync(filter, update);
        
        Console.WriteLine($"Migration completed!");
        Console.WriteLine($"Matched: {result.MatchedCount} documents");
        Console.WriteLine($"Modified: {result.ModifiedCount} documents");
    }
}
