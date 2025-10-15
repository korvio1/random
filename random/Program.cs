using System;
using System.IO;

class NumberGuessingGame
{
    const int EasyMin = 0;
    const int EasyMax = 100;

    const int NormalMin = 0;
    const int NormalMax = 10000;

    const int HardMin = 0;
    const int HardMax = 1000000;

    const int MaxAttempts = 10;

    enum GameMode { Single, Reverse, Mixed }
    enum Difficulty { Easy, Normal, Hard }

    static void Main()
    {
        Console.WriteLine("Witaj w grze 'Zgadnij liczbę'!");
        string playerName = GetPlayerName();

        while (true)
        {
            Console.WriteLine("Wybierz tryb gry:\n1. Ty zgadujesz (Single)\n2. Program zgaduje (Reverse)\n3. Gra mieszana (Multi)\n0. Wyjście");
            string modeInput = Console.ReadLine().Trim();
            if (modeInput == "0") break;

            if (!Enum.TryParse<GameMode>(modeInput switch
            {
                "1" => "Single",
                "2" => "Reverse",
                "3" => "Mixed",
                _ => ""
            }, out GameMode mode))
            {
                Console.WriteLine("Nieznany tryb, wybierz 1, 2, 3 lub 0.");
                continue;
            }

            Difficulty difficulty = ChooseDifficulty();

            switch (mode)
            {
                case GameMode.Single:
                    PlaySingleGame(playerName, difficulty);
                    break;
                case GameMode.Reverse:
                    PlayReverseGame(playerName, difficulty);
                    break;
                case GameMode.Mixed:
                    PlayMixedGame(playerName, difficulty);
                    break;
            }
        }
        Console.WriteLine("Dzięki za grę!");
    }

    static Difficulty ChooseDifficulty()
    {
        Console.WriteLine("Wybierz poziom trudności:\n1. Łatwy (0-100)\n2. Normalny (0-10 000)\n3. Trudny (0-1 000 000)");
        while (true)
        {
            string input = Console.ReadLine().Trim();
            if (input == "1") return Difficulty.Easy;
            if (input == "2") return Difficulty.Normal;
            if (input == "3") return Difficulty.Hard;

            Console.WriteLine("Niepoprawny wybór. Wybierz 1, 2 lub 3.");
        }
    }

    static (int min, int max) GetRange(Difficulty diff) => diff switch
    {
        Difficulty.Easy => (EasyMin, EasyMax),
        Difficulty.Normal => (NormalMin, NormalMax),
        Difficulty.Hard => (HardMin, HardMax),
        _ => (EasyMin, EasyMax)
    };

    static string GetModeString(GameMode mode) => mode switch
    {
        GameMode.Single => "single",
        GameMode.Reverse => "reverse",
        GameMode.Mixed => "multi",
        _ => "single"
    };

    static string GetDifficultyString(Difficulty diff) => diff switch
    {
        Difficulty.Easy => "easy",
        Difficulty.Normal => "normal",
        Difficulty.Hard => "hard",
        _ => "easy"
    };

    static void PlaySingleGame(string playerName, Difficulty diff)
    {
        var (min, max) = GetRange(diff);
        int bestScore = LoadBestScore(playerName, GameMode.Single, diff);

        Console.WriteLine(bestScore >= 0
            ? $"Twój najlepszy wynik na poziomie {GetDifficultyString(diff)}: {bestScore} prób."
            : $"Nie masz jeszcze wyniku na poziomie {GetDifficultyString(diff)}.");

        int attempts = PlayGame(min, max);
        if (attempts == -1)
            Console.WriteLine("Nie udało się zgadnąć liczby.");
        else UpdateBestScore(playerName, GameMode.Single, diff, attempts);
    }

    static void PlayReverseGame(string playerName, Difficulty diff)
    {
        var (min, max) = GetRange(diff);
        int bestScore = LoadBestScore(playerName, GameMode.Reverse, diff);

        Console.WriteLine(bestScore >= 0
            ? $"Najlepszy wynik programu na poziomie {GetDifficultyString(diff)}: {bestScore} prób."
            : $"Program nie ma jeszcze wyniku na poziomie {GetDifficultyString(diff)}.");

        int attempts = PlayGameReverse(min, max);
        if (attempts == -1)
            Console.WriteLine("Program nie zgadł Twojej liczby.");
        else UpdateBestScore(playerName, GameMode.Reverse, diff, attempts);
    }

    static void PlayMixedGame(string playerName, Difficulty diff)
    {
        var (min, max) = GetRange(diff);
        (int wins, int losses) = LoadMultiStats(playerName, diff);

        Console.WriteLine($"Twoje dotychczasowe wyniki (wygrane/przegrane) na poziomie {GetDifficultyString(diff)}: {wins}/{losses}");

        Random rnd = new();
        bool playerTurn = rnd.Next(2) == 1;
        Console.WriteLine(playerTurn ? "Ty zaczynasz!" : "Program zaczyna!");

        int low = min, high = max, playerAttempts = 0, programAttempts = 0;
        int numberToGuessByPlayer = new Random().Next(min, max + 1);

        for (int tries = 0; tries < MaxAttempts;)
        {
            if (playerTurn)
            {
                playerAttempts++;
                Console.Write($"Twoja próba {playerAttempts}/{MaxAttempts}: ");
                if (!int.TryParse(Console.ReadLine(), out int guess) || guess < min || guess > max)
                {
                    Console.WriteLine($"Liczba musi być z zakresu {min}-{max}");
                    playerAttempts--;
                    continue;
                }
                if (guess == numberToGuessByPlayer)
                {
                    Console.WriteLine($"Wygrałeś w {playerAttempts} próbach!");
                    SaveMultiStats(playerName, diff, wins + 1, losses);
                    return;
                }
                Console.WriteLine(guess > numberToGuessByPlayer ? "Za dużo!" : "Za mało!");
                tries++;
            }
            else
            {
                programAttempts++;
                int progGuess = (low + high) / 2;
                Console.WriteLine($"Program zgaduje: {progGuess}");
                Console.WriteLine("Podaj: 'w' (więcej), 'm' (mniej), 't' (trafione)");
                string resp = Console.ReadLine().Trim().ToLower();
                if (resp == "t")
                {
                    Console.WriteLine($"Program wygrał w {programAttempts} próbach!");
                    SaveMultiStats(playerName, diff, wins, losses + 1);
                    return;
                }
                if (resp == "w") low = progGuess + 1;
                else if (resp == "m") high = progGuess - 1;
                else
                {
                    Console.WriteLine("Niepoprawna odpowiedź.");
                    programAttempts--;
                    continue;
                }
                tries++;
            }
            playerTurn = !playerTurn;
        }
        Console.WriteLine("Nikt nie wygrał w podanej liczbie prób.");
        SaveMultiStats(playerName, diff, wins, losses + 1);
    }

    static int PlayGame(int min, int max)
    {
        int target = new Random().Next(min, max + 1);
        for (int i = 1; i <= MaxAttempts; i++)
        {
            Console.Write($"Próba {i}/{MaxAttempts}: ");
            if (!int.TryParse(Console.ReadLine(), out int guess))
            {
                Console.WriteLine("Podaj liczbę.");
                i--;
                continue;
            }
            if (guess < min || guess > max)
            {
                Console.WriteLine($"Liczba musi być z zakresu {min}-{max}.");
                i--;
                continue;
            }
            if (guess == target) return i;
            Console.WriteLine(guess > target ? "Za dużo!" : "Za mało!");
        }
        return -1;
    }

    static int PlayGameReverse(int min, int max)
    {
        Console.WriteLine($"Pomyśl liczbę z {min} do {max}, a ja zgadnę.");
        int low = min, high = max;
        for (int attempts = 1; attempts <= MaxAttempts && low <= high; attempts++)
        {
            int guess = (low + high) / 2;
            Console.WriteLine($"Moja próba {attempts}/{MaxAttempts}: {guess} (w/m/t)");
            string resp = Console.ReadLine().Trim().ToLower();
            if (resp == "t") return attempts;
            if (resp == "w") low = guess + 1;
            else if (resp == "m") high = guess - 1;
            else
            {
                Console.WriteLine("Niepoprawna odpowiedź.");
                attempts--;
            }
        }
        return -1;
    }

    static void UpdateBestScore(string playerName, GameMode mode, Difficulty diff, int attempts)
    {
        Console.WriteLine($"Zgadnięto w {attempts} próbach!");
        int bestScore = LoadBestScore(playerName, mode, diff);
        if (bestScore < 0 || attempts < bestScore)
        {
            Console.WriteLine("Nowy najlepszy wynik!");
            SaveBestScore(playerName, mode, diff, attempts);
        }
    }

    static int LoadBestScore(string playerName, GameMode mode, Difficulty diff)
    {
        string fileName = $"{playerName}_{GetDifficultyString(diff)}_{GetModeString(mode)}.txt";
        if (!File.Exists(fileName)) return -1;

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

    static void SaveBestScore(string playerName, GameMode mode, Difficulty diff, int score)
    {
        string fileName = $"{playerName}_{GetDifficultyString(diff)}_{GetModeString(mode)}.txt";
        try
        {
            File.WriteAllText(fileName, score.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu wyniku: {ex.Message}");
        }
    }

    static (int wins, int losses) LoadMultiStats(string playerName, Difficulty diff)
    {
        string fileName = $"{playerName}_{GetDifficultyString(diff)}_multi_stats.txt";
        if (!File.Exists(fileName))
            return (0, 0);

        try
        {
            string content = File.ReadAllText(fileName);
            string[] parts = content.Split(';');
            int wins = 0, losses = 0;

            foreach (var part in parts)
            {
                if (part.StartsWith("wins=") && int.TryParse(part.Substring(5), out int w))
                    wins = w;
                else if (part.StartsWith("losses=") && int.TryParse(part.Substring(7), out int l))
                    losses = l;
            }

            return (wins, losses);
        }
        catch
        {
            return (0, 0);
        }
    }

    static void SaveMultiStats(string playerName, Difficulty diff, int wins, int losses)
    {
        string fileName = $"{playerName}_{GetDifficultyString(diff)}_multi_stats.txt";
        string content = $"wins={wins};losses={losses}";

        try
        {
            File.WriteAllText(fileName, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu statystyk multi: {ex.Message}");
        }
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
}
