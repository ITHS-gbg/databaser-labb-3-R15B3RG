using MongoDB.Bson;

namespace Labb3Quiz;

public class Question
{
    public ObjectId Id { get; set; }
    public string QuestionText { get; set; }

    public List<string> Options { get; set; }

    public string CorrectOption { get; set; }
}