// See https://aka.ms/new-console-template for more information
using Eccord;
using Eccord.Patches;

CreditScoreGood creditScoreGood = new();
CreditScoreBad creditScoreBad = new();

User userA = new PatchedUser("Lillie", "Norway");
User userB = new PatchedUser("Fomx", "Sweden");
User userC = new PatchedUser("Amanda", "Norway");
User userD = new PatchedUser("Hello", "USA");

Console.WriteLine(userA);
Console.WriteLine(userB);
Console.WriteLine(userC);
Console.WriteLine(userD);
Console.WriteLine("--------");

creditScoreBad.Dispose();
Console.WriteLine($"Disposed {creditScoreBad}");
Console.WriteLine("--------");
Console.WriteLine(userA);
Console.WriteLine(userB);
Console.WriteLine(userC);
Console.WriteLine(userD);

