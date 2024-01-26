using MongoDB.Bson;

namespace Labb3Quiz;

public class Quiz
{
    public ObjectId Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public List<ObjectId> Questions { get; set; }
}