using System;
using System.Collections.Generic;
using System.Linq;
using Labb3Quiz;
using MongoDB.Bson;
using MongoDB.Driver;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    private static IMongoCollection<Question> questionCollection;
    private static IMongoCollection<Quiz> quizCollection;
    private static Dictionary<string, ObjectId> existingQuestions = new Dictionary<string, ObjectId>();
    private static Dictionary<string, ObjectId> existingQuizzes = new Dictionary<string, ObjectId>();

    public static List<Option> options;
    static void Main()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("QuizDatabase");

        questionCollection = database.GetCollection<Question>("questions");
        quizCollection = database.GetCollection<Quiz>("quizzes");



        DisplayMenu(database);

        Console.ReadLine();

        static void DisplayMenu(IMongoDatabase database)
        {

            options = new List<Option>
            {
                new Option("Lista frågor", () => ListQuestions(database)),
                new Option("Lägg till fråga", () =>  AddQuestion(database)),
                new Option("Skapa nytt quiz", () =>  CreateQuiz(database)),
                new Option("Lägg till fråga i quiz", () =>  AddQuestionToQuiz(database)),
                new Option("Ta bort fråga från quiz", () =>  RemoveQuestionFromQuiz(database)),
                new Option("Exit", () => Environment.Exit(0)),
            };


            int index = 0;


            WriteMenu(options, options[index]);


            ConsoleKeyInfo keyinfo;
            do
            {
                keyinfo = Console.ReadKey();


                if (keyinfo.Key == ConsoleKey.DownArrow)
                {
                    if (index + 1 < options.Count)
                    {
                        index++;
                        WriteMenu(options, options[index]);
                    }
                }
                if (keyinfo.Key == ConsoleKey.UpArrow)
                {
                    if (index - 1 >= 0)
                    {
                        index--;
                        WriteMenu(options, options[index]);
                    }
                }

                if (keyinfo.Key == ConsoleKey.Enter)
                {
                    options[index].Selected.Invoke();
                    index = 0;
                }
            }
            while (keyinfo.Key != ConsoleKey.X);

            Console.ReadKey();

        }

        static void WriteTemporaryMessage(string message)
        {
            Console.Clear();
            Console.WriteLine(message);
            Thread.Sleep(3000);
            WriteMenu(options, options.First());
        }



        static void WriteMenu(List<Option> options, Option selectedOption)
        {
            Console.Clear();

            Console.WriteLine("Välkommen till Quiz!\n");

            foreach (Option option in options)
            {
                if (option == selectedOption)
                {
                    Console.Write("--> ");
                }
                else
                {
                    Console.Write(" ");
                }

                Console.WriteLine(option.Name);
            }
        }
    }

    public class Option
    {
        public string Name { get; }
        public Action Selected { get; }

        public Option(string name, Action selected)
        {
            Name = name;
            Selected = selected;
        }
    }


    static void ListQuestions(IMongoDatabase database)
    {
        var questions = questionCollection.Find(_ => true).ToList();

        Console.Clear();
        Console.WriteLine("Frågebank:");
        foreach (var question in questions)
        {
            Console.WriteLine($"ID: {question.Id}, Text: {question.QuestionText}");
            Console.WriteLine("Svarsalternativ:");
            foreach (var option in question.Options)
            {
                Console.WriteLine($"- {option}");
            }

            Console.WriteLine($"Rätt svar: {question.CorrectOption}");
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("Tryck på valfri knapp för att återvända till menyn.");
        Console.ReadKey();
        Main();
    }

    static void AddQuestion(IMongoDatabase database)
    {
        Console.Clear();
        Console.Write("Ange frågetext: ");
        string questionText = Console.ReadLine();


        if (string.IsNullOrEmpty(questionText))
        {
            Console.WriteLine("Fältet kan inte vara tomt!");
            Console.ReadKey();
            Main();
        }
        else if (existingQuestions.ContainsKey(questionText.ToLower()))
        {
            Console.WriteLine("Frågan finns redan. Kan inte skapa dubbletter.");
            Console.ReadKey();
            Main();
        }

        List<string> options = new List<string>();

        for (int i = 0; i < 3; i++)
        {
            while (true)
            {
                Console.Write($"Ange svarsalternativ {i + 1}: ");
                string option = Console.ReadLine();
                if (!string.IsNullOrEmpty(option))
                {
                    options.Add(option);
                    break;
                }
                else
                {
                    Console.WriteLine("Fältet kan inte vara tomt!");
                }
            }
        }

        int correctOptionIndex;

        while (true)
        {
            Console.Write("Ange vilket alternativ som är rätt (1-3): ");
            string correctOptionInput = Console.ReadLine();

            if (int.TryParse(correctOptionInput, out correctOptionIndex))
            {
                if (correctOptionIndex >= 1 && correctOptionIndex <= options.Count)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Finns bara {options.Count} svarsalternativ. Välj ett av dessa. Försök igen!");
                }
            }
            else
            {
                Console.WriteLine("Du måste använda siffror! Försök igen!");
            }
        }

        string correctOption = options[correctOptionIndex - 1];

        var newQuestion = new Question
        {
            QuestionText = questionText,
            Options = options,
            CorrectOption = correctOption
        };

        questionCollection.InsertOne(newQuestion);

        existingQuestions.Add(questionText.ToLower(), newQuestion.Id);

        Console.WriteLine("Frågan har lagts till i frågebanken!");
        Console.WriteLine("Tryck på valfri knapp för att återvända till menyn.");
        Console.ReadKey();
        Main();
    }

    static void AddQuestionToQuiz(IMongoDatabase database)
    {
        Console.Clear();
        var quizzes = quizCollection.Find(_ => true).ToList();

        Console.WriteLine("Tillgängliga quizzar:");
        foreach (var quiz in quizzes)
        {
            Console.WriteLine($"ID: {quiz.Id}, Namn: {quiz.Name}");
        }

        Console.Write("Välj ett quiz genom att ange dess ID: ");
        string selectedQuizId = Console.ReadLine();

        if (ObjectId.TryParse(selectedQuizId, out var quizObjectId))
        {
            var selectedQuiz = quizCollection.Find(q => q.Id == quizObjectId).FirstOrDefault();

            if (selectedQuiz != null)
            {
                Console.Write("Vill du lägga till en befintlig fråga i quizen? (ja/nej): ");
                string addExistingQuestionChoice = Console.ReadLine().ToLower();

                if (addExistingQuestionChoice == "ja")
                {
                    AddExistingQuestionToQuiz(database, selectedQuiz);
                }
                else
                {
                    Console.WriteLine("Ange frågetext för den nya frågan: ");
                    string questionText = Console.ReadLine();

                    if (string.IsNullOrEmpty(questionText))
                    {
                        Console.WriteLine("Fältet kan inte vara tomt!");
                        Console.ReadKey();
                        Main();
                    }
                    else if (existingQuestions.ContainsKey(questionText.ToLower()))
                    {
                        Console.WriteLine("Frågan finns redan. Kan inte skapa dubbletter.");
                        Console.ReadKey();
                        Main();
                    }


                    List<string> options = new List<string>();
                    Console.WriteLine("Ange tre olika svarsalternativ:");
                    for (int i = 0; i < 3; i++)
                    {
                        Console.Write($"Svarsalternativ {i + 1}: ");
                        options.Add(Console.ReadLine());
                    }

                    Console.Write("Ange vilket alternativ som är rätt (1-3): ");
                    int correctOptionIndex = int.Parse(Console.ReadLine()) - 1;
                    string correctOption = options[correctOptionIndex];

                    var newQuestion = new Question
                    {
                        QuestionText = questionText,
                        Options = options,
                        CorrectOption = correctOption
                    };

                    questionCollection.InsertOne(newQuestion);

                    selectedQuiz.Questions.Add(newQuestion.Id);

                    Console.WriteLine("Frågan har lagts till i quizen!");
                }
            }
            else
            {
                Console.WriteLine("Ogiltigt quiz-ID.");
            }
        }
        else
        {
            Console.WriteLine("Ogiltigt quiz-ID.");
        }

        Console.WriteLine("Tryck på valfri knapp för att återvända till menyn.");
        Console.ReadKey();
        Main();
    }

    static void AddExistingQuestionToQuiz(IMongoDatabase database, Quiz quiz)
    {
        Console.Clear();
        var questions = questionCollection.Find(_ => true).ToList();

        Console.WriteLine("Tillgängliga frågor att lägga till i quizen:");
        int selectedQuestionIndex = 0;

        do
        {
            Console.Clear();

            Console.WriteLine("Tillgängliga frågor att lägga till i quizen:");
            for (int i = 0; i < questions.Count; i++)
            {
                if (i == selectedQuestionIndex)
                {
                    Console.Write("--> ");
                }
                else
                {
                    Console.Write("    ");
                }

                Console.WriteLine($"ID: {questions[i].Id}, Text: {questions[i].QuestionText}");
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.UpArrow && selectedQuestionIndex > 0)
            {
                selectedQuestionIndex--;
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow && selectedQuestionIndex < questions.Count - 1)
            {
                selectedQuestionIndex++;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                var selectedQuestion = questions[selectedQuestionIndex];
                quiz.Questions.Add(selectedQuestion.Id);
                quizCollection.ReplaceOne(q => q.Id == quiz.Id, quiz);
                Console.WriteLine("Frågan har lagts till i quizen!");
                Console.ReadKey();
                Main();
            }
        } while (true);
    }

    static void RemoveQuestionFromQuiz(IMongoDatabase database)
    {
        Console.Clear();
        var questions = questionCollection.Find(_ => true).ToList();

        if (questions.Count == 0)
        {
            Console.WriteLine("Det finns inga frågor i frågebanken.");
            Console.WriteLine("Tryck på en tangent för att återgå till huvudmenyn.");
            Console.ReadKey();
            Main(); // Eller anropa en funktion som tar dig tillbaka till huvudmenyn
            return;
        }

        Console.WriteLine("Frågebank:");
        int selectedQuestionIndex = 0;

        do
        {
            Console.Clear();

            Console.WriteLine("Frågebank:");
            for (int i = 0; i < questions.Count; i++)
            {
                if (i == selectedQuestionIndex)
                {
                    
                    Console.Write("--> ");
                }
                else
                {
                    
                    Console.Write("    ");
                }

                Console.WriteLine($"ID: {questions[i].Id}, Text: {questions[i].QuestionText}");
                Console.WriteLine("Svarsalternativ:");
                foreach (var option in questions[i].Options)
                {
                    Console.WriteLine($"- {option}");
                }

                Console.WriteLine($"Rätt svar: {questions[i].CorrectOption}");
                Console.WriteLine();
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.UpArrow && selectedQuestionIndex > 0)
            {
                selectedQuestionIndex--;
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow && selectedQuestionIndex < questions.Count - 1)
            {
                selectedQuestionIndex++;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                var selectedQuestion = questions[selectedQuestionIndex];
                questionCollection.DeleteOne(q => q.Id == selectedQuestion.Id);
                Console.Clear();
                Console.WriteLine("Frågan har tagits bort från frågebanken!");
                Console.ReadKey();
                Main();
            }
        } while (true);
    }



    static void CreateQuiz(IMongoDatabase database)
    {
        Console.Clear();
        Console.Write("Ange namn för det nya quizet: ");
        string quizName = Console.ReadLine();

        if (string.IsNullOrEmpty(quizName))
        {
            Console.WriteLine("Fältet kan inte vara tomt!");
            Console.ReadKey();
            Main();
        }
        else if (existingQuizzes.ContainsKey(quizName.ToLower()))
        {
            Console.WriteLine("Quizzet finns redan. Kan inte skapa dubbletter.");
            Console.ReadKey();
            Main();
        }

        Console.Write("Ange beskrivning för det nya quizet: ");
        string quizDescription = Console.ReadLine();

        if (string.IsNullOrEmpty(quizDescription))
        {
            Console.WriteLine("Fältet kan inte vara tomt!");
            Console.ReadKey();
            Main();
        }
        else if (existingQuizzes.ContainsKey(quizDescription.ToLower()))
        {
            Console.WriteLine("Quizzet finns redan. Kan inte skapa dubbletter.");
            Console.ReadKey();
            Main();
        }
        else
        {

            var newQuiz = new Quiz
            {
                Name = quizName,
                Description = quizDescription,
                Questions = new List<ObjectId>()
            };

            quizCollection.InsertOne(newQuiz);

            existingQuizzes.Add(quizName.ToLower(), newQuiz.Id);

            Console.Write("Vill du lägga till en befintlig fråga i quizen? (ja/nej): ");
            string addExistingQuestionChoice = Console.ReadLine().ToLower();

            if (addExistingQuestionChoice == "ja")
            {
                AddExistingQuestionToQuiz(database, newQuiz);
            }
            else
            {
                Console.WriteLine("Quizet har skapats och lagts till i databasen!");
                Console.WriteLine("Tryck på valfri knapp för att återvända till menyn.");
                Console.ReadKey();
                Main();
            }
        }


    }

}
