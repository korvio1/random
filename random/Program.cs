using System;
using System.IO;

class NumberGuessingGame
{
    const int MinNumber = 0;
    const int MaxNumber = 100;
    const int MaxAttempts = 10;

    static void Main()
    {
        Console.WriteLine("Witaj w grze 'Zgadnij liczbę'!");

        string playerName = GetPlayerName();
        int bestScore = LoadBestScore(playerName);

        Console.WriteLine(bestScore >= 0
            ? $"Witaj {playerName}! Twój najlepszy wynik to {bestScore} prób."
            : $"Witaj {playerName}! Nie masz jeszcze żadnego zapisanego wyniku.");

        bool keepPlaying = true;
        while (keepPlaying)
        {
            int attempts = PlayGame();

            if (attempts == -1)
            {
                Console.WriteLine("Niestety, nie udało Ci się zgadnąć liczby w 10 próbach!");
            }
            else
            {
                Console.WriteLine($"Zgadłeś liczbę w {attempts} próbach!");

                if (bestScore < 0 || attempts < bestScore)
                {
                    Console.WriteLine("Gratulacje! To Twój nowy najlepszy wynik.");
                    bestScore = attempts;
                    SaveBestScore(playerName, bestScore);
                }
            }

            Console.WriteLine("Czy chcesz zagrać jeszcze raz? (t/n)");
            string answer = Console.ReadLine().Trim().ToLower();
            keepPlaying = (answer == "t" || answer == "tak");
        }

        Console.WriteLine("Dziękujemy za grę! Do zobaczenia.");
    }

    static string GetPlayerName()
    {
        Console.Write("Podaj swój nick: ");
        string name = Console.ReadLine().Trim();

        while (string.IsNullOrEmpty(name))
        {
            Console.Write("Nick nie może być pusty. Podaj swój nick: ");
            name = Console.ReadLine().Trim();
        }

        return name;
    }

    static int LoadBestScore(string playerName)
    {
        string fileName = playerName + ".txt";

        if (!File.Exists(fileName))
            return -1;

        try
        {
            string content = File.ReadAllText(fileName);
            if (int.TryParse(content, out int score))
                return score;
            else
                return -1;
        }
        catch
        {
            return -1;
        }
    }

    static void SaveBestScore(string playerName, int score)
    {
        string fileName = playerName + ".txt";

        try
        {
            File.WriteAllText(fileName, score.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu wyniku: {ex.Message}");
        }
    }

    static int PlayGame()
    {
        Random rnd = new Random();
        int numberToGuess = rnd.Next(MinNumber, MaxNumber + 1);
        int attempts = 0;
        int guess = -1;

        Console.WriteLine($"Zgadnij liczbę z zakresu {MinNumber} do {MaxNumber}. Masz {MaxAttempts} prób.");

        while (guess != numberToGuess && attempts < MaxAttempts)
        {
            Console.Write($"Próba {attempts + 1}/{MaxAttempts}: ");
            string input = Console.ReadLine();

            if (!int.TryParse(input, out guess))
            {
                Console.WriteLine("Proszę podaj poprawną liczbę.");
                continue;
            }

            if (guess < MinNumber || guess > MaxNumber)
            {
                Console.WriteLine($"Liczba musi być w zakresie od {MinNumber} do {MaxNumber}.");
                continue;
            }

            attempts++;

            if (guess > numberToGuess)
                Console.WriteLine("Za dużo!");
            else if (guess < numberToGuess)
                Console.WriteLine("Za mało!");
        }

        if (guess == numberToGuess)
            return attempts;
        else
            return -1;
    }
}
