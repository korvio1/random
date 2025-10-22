using System;
using System.IO;
using System.Collections.Generic;

class Player
{
    public string Name { get; set; }
    public Player(string name)
    {
        Name = name;
    }
}

class NumberGuessingGame
{
    const int EasyMin = 0;
    const int EasyMax = 100;

    const int NormalMin = 0;
    const int NormalMax = 10000;

    const int HardMin = 0;
    const int HardMax = 1000000;

    const int MaxAttempts = 10;

    enum GameMode { Single, Reverse, Mixed, MultiPlayer }
    enum Difficulty { Easy, Normal, Hard }

    static void Main()
    {
        Console.WriteLine("Witaj w grze 'Zgadnij liczbę'!");
        Player humanPlayer = new Player(GetPlayerName());
        Player programPlayer = new Player("Komputer");

        while (true)
        {
            Console.WriteLine("Wybierz tryb gry:\n1. Ty zgadujesz (Single)\n2. Program zgaduje (Reverse)\n3. Gra mieszana (Multi)\n4. Gra wieloosobowa (3 graczy)\n0. Wyjście");
            string modeInput = Console.ReadLine().Trim();
            if (modeInput == "0") break;

            if (!Enum.TryParse<GameMode>(modeInput switch
            {
                "1" => "Single",
                "2" => "Reverse",
                "3" => "Mixed",
                "4" => "MultiPlayer",
                _ => ""
            }, out GameMode mode))
            {
                Console.WriteLine("Nieznany tryb, wybierz 0,1,2,3 lub 4.");
                continue;
            }

            Difficulty difficulty = ChooseDifficulty();

            switch (mode)
            {
                case GameMode.Single:
                    PlaySingleGame(humanPlayer, difficulty);
                    break;
                case GameMode.Reverse:
                    PlayReverseGame(humanPlayer, programPlayer, difficulty);
                    break;
                case GameMode.Mixed:
                    PlayMixedGame(humanPlayer, programPlayer, difficulty);
                    break;
                case GameMode.MultiPlayer:
                    PlayMultiPlayerGame(difficulty);
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
        GameMode.MultiPlayer => "multiplayer",
        _ => "single"
    };

    static string GetDifficultyString(Difficulty diff) => diff switch
    {
        Difficulty.Easy => "easy",
        Difficulty.Normal => "normal",
        Difficulty.Hard => "hard",
        _ => "easy"
    };

    static void PlaySingleGame(Player player, Difficulty diff)
    {
        var (min, max) = GetRange(diff);
        int bestScore = LoadBestScore(player.Name, GameMode.Single, diff);

        Console.WriteLine(bestScore >= 0
            ? $"Twój najlepszy wynik na poziomie {GetDifficultyString(diff)}: {bestScore} prób."
            : $"Nie masz jeszcze wyniku na poziomie {GetDifficultyString(diff)}.");

        int attempts = PlayGame(min, max);
        if (attempts == -1)
            Console.WriteLine("Nie udało się zgadnąć liczby.");
        else UpdateBestScore(player.Name, GameMode.Single, diff, attempts);
    }

    static void PlayReverseGame(Player humanPlayer, Player programPlayer, Difficulty diff)
    {
        var (min, max) = GetRange(diff);
        int bestScore = LoadBestScore(programPlayer.Name, GameMode.Reverse, diff);

        Console.WriteLine(bestScore >= 0
            ? $"Najlepszy wynik programu na poziomie {GetDifficultyString(diff)}: {bestScore} prób."
            : $"Program nie ma jeszcze wyniku na poziomie {GetDifficultyString(diff)}.");

        int attempts = PlayGameReverse(min, max);
        if (attempts == -1)
            Console.WriteLine("Program nie zgadł Twojej liczby.");
        else UpdateBestScore(programPlayer.Name, GameMode.Reverse, diff, attempts);
    }

    static void PlayMixedGame(Player humanPlayer, Player programPlayer, Difficulty diff)
    {
        var (min, max) = GetRange(diff);
        (int wins, int losses) = LoadMultiStats(humanPlayer.Name, diff);

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
                    SaveMultiStats(humanPlayer.Name, diff, wins + 1, losses);
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
                    SaveMultiStats(humanPlayer.Name, diff, wins, losses + 1);
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
        SaveMultiStats(humanPlayer.Name, diff, wins, losses + 1);
    }

    static void PlayMultiPlayerGame(Difficulty diff)
    {
        var (min, max) = GetRange(diff);

        List<Player> players = new List<Player>();
        for (int i = 1; i <= 3; i++)
        {
            Console.Write($"Podaj nazwę gracza {i}: ");
            string name;
            do
            {
                name = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(name)) Console.WriteLine("Nazwa nie może być pusta.");
            } while (string.IsNullOrEmpty(name));
            players.Add(new Player(name));
        }

        int numberToGuess = new Random().Next(min, max + 1);
        Console.WriteLine($"Została wylosowana liczba z zakresu {min}-{max}. Gracze na zmianę będą zgadywać.");

        int[] attemptsPerPlayer = new int[3];
        bool someoneWon = false;
        int winnerIndex = -1;

        for (int round = 1; round <= MaxAttempts; round++)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (round > MaxAttempts) break;

                attemptsPerPlayer[i]++;
                Console.Write($"{players[i].Name}, próba {attemptsPerPlayer[i]}/{MaxAttempts}: ");
                if (!int.TryParse(Console.ReadLine(), out int guess) || guess < min || guess > max)
                {
                    Console.WriteLine($"Liczba musi być z zakresu {min}-{max}");
                    attemptsPerPlayer[i]--;
                    i--;
                    continue;
                }
                if (guess == numberToGuess)
                {
                    Console.WriteLine($"{players[i].Name} wygrał w {attemptsPerPlayer[i]} próbach!");
                    someoneWon = true;
                    winnerIndex = i;
                    break;
                }
                Console.WriteLine(guess > numberToGuess ? "Za dużo!" : "Za mało!");
            }
            if (someoneWon) break;
        }

        if (!someoneWon)
            Console.WriteLine("Nikt nie wygrał w podanej liczbie prób.");
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
        string filename = $"scores_{playerName}_{GetModeString(mode)}_{GetDifficultyString(diff)}.txt";
        if (!File.Exists(filename)) return -1;
        if (int.TryParse(File.ReadAllText(filename), out int best)) return best;
        return -1;
    }

    static void SaveBestScore(string playerName, GameMode mode, Difficulty diff, int attempts)
    {
        string filename = $"scores_{playerName}_{GetModeString(mode)}_{GetDifficultyString(diff)}.txt";
        File.WriteAllText(filename, attempts.ToString());
    }

    static (int wins, int losses) LoadMultiStats(string playerName, Difficulty diff)
    {
        string filename = $"multi_stats_{playerName}_{GetDifficultyString(diff)}.txt";
        if (!File.Exists(filename)) return (0, 0);
        string[] lines = File.ReadAllLines(filename);
        if (lines.Length < 2) return (0, 0);
        if (int.TryParse(lines[0], out int wins) && int.TryParse(lines[1], out int losses))
            return (wins, losses);
        return (0, 0);
    }

    static void SaveMultiStats(string playerName, Difficulty diff, int wins, int losses)
    {
        string filename = $"multi_stats_{playerName}_{GetDifficultyString(diff)}.txt";
        File.WriteAllLines(filename, new string[] { wins.ToString(), losses.ToString() });
    }

    static string GetPlayerName()
    {
        Console.Write("Podaj swoją nazwę: ");
        string name;
        do
        {
            name = Console.ReadLine().Trim();
            if (string.IsNullOrEmpty(name))
                Console.Write("Nazwa nie może być pusta. Podaj nazwę: ");
        } while (string.IsNullOrEmpty(name));
        return name;
    }
}
