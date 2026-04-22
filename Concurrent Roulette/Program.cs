class Program
{
    public static bool HaveWinner = false;
    public const int WinningAmount = 1000;
    static void Main()
    {
        Game game1 = new Game();
        game1.DisplayRoundInfo = true;
        game1.PlayGame(4);
    }
}

public class Player
{
    public int Balance { get; set; } = 500;
    public int IndividualBetAmount { get; set; }
    public int RoundBetAmount { get; set; }
    public int RoundWinAmount { get; set; }
    public int TotalBetAmount { get; set; }
    public int TotalWinAmount { get; set; }
    public int Number { get; set; }
    public CancellationTokenSource Cts = new();
    public AutoResetEvent GoPlayer = new(false);
    public AutoResetEvent GoAttendant = new(false);

    public Player(int number)
    {
        Number = number;
    }

    public void Play()
    {
        while (!Program.HaveWinner)
        {
            GoPlayer.WaitOne();
            RoundBetAmount = 0;
            RoundWinAmount = 0;
            while (Balance > 0 && !Program.HaveWinner && !Cts.Token.IsCancellationRequested)
            {
                if (ShouldIPlaceBet())
                {
                    IndividualBetAmount = HowMuchToBet();

                    AdjustBetAndWinAmounts();

                    WhereToPlaceBet();
                }

                Thread.Sleep(250);
            }

            GoAttendant.Set();
        }
    }

    private void AdjustBetAndWinAmounts()
    {
        TotalBetAmount += IndividualBetAmount;
        RoundBetAmount += IndividualBetAmount;

        Balance -= IndividualBetAmount;

        TotalWinAmount -= IndividualBetAmount;
        RoundWinAmount -= IndividualBetAmount;
    }

    private static int HowMuchToBet()
    {
        return Random.Shared.Next(5, 50);
    }

    private static bool ShouldIPlaceBet()
    {
        return Random.Shared.Next(5) == 0;
    }

    private void WhereToPlaceBet()
    {
        int betType = Random.Shared.Next(13);

        if (betType == 0)
        {
            BettingMat.OneToEighteen[this] = IndividualBetAmount;
        }
        else if (betType == 1)
        {
            BettingMat.NineteenToThirtySix[this] = IndividualBetAmount;
        }
        else if (betType == 2)
        {
            BettingMat.Even[this] = IndividualBetAmount;
        }
        else if (betType == 3)
        {
            BettingMat.Odd[this] = IndividualBetAmount;
        }
        else if (betType == 4)
        {
            BettingMat.Red[this] = IndividualBetAmount;
        }
        else if (betType == 5)
        {
            BettingMat.Black[this] = IndividualBetAmount;
        }
        else if (betType == 6)
        {
            BettingMat.FirstDozen[this] = IndividualBetAmount;
        }
        else if (betType == 7)
        {
            BettingMat.SecondDozen[this] = IndividualBetAmount;
        }
        else if (betType == 8)
        {
            BettingMat.ThirdDozen[this] = IndividualBetAmount;
        }
        else if (betType == 9)
        {
            BettingMat.FirstColumn[this] = IndividualBetAmount;
        }
        else if (betType == 10)
        {
            BettingMat.SecondColumn[this] = IndividualBetAmount;
        }
        else if (betType == 11)
        {
            BettingMat.ThirdColumn[this] = IndividualBetAmount;
        }
        else
        {
            int numberBetOn = Random.Shared.Next(1, 37);
            BettingMat.SpecificNumber[(this, numberBetOn)] = IndividualBetAmount;
        }
    }
}

public class Game
{
    List<Player> Players = new List<Player>();
    List<Thread> Threads = new List<Thread>();
    IndividualNumber WinningNumber = new(-1);
    public bool DisplayRoundInfo = false;
    int RoundNumber = 0;

    public void PlayGame(int numberOfPlayers)
    {
        StartPlayers(numberOfPlayers);

        while (!Program.HaveWinner)
        {
            RoundNumber++;

            Console.WriteLine($"\n--- Round {RoundNumber} ---\n");

            SignalPlayersToStartBetting();

            Thread.Sleep(2000);

            TellPlayersToStopBetting();

            WaitForPlayersToFinishBetting();

            SpinWheel();

            DetermineWinners();

            DisplayBets();

            GivePlayersNewCancellationTokens();

            Console.ReadKey();
            BettingMat.Reset();
        }

        foreach (Thread thread in Threads)
        {
            thread.Join();
        }
    }

    private void GivePlayersNewCancellationTokens()
    {
        foreach (Player player in Players)
        {
            player.Cts.Dispose();
            player.Cts = new CancellationTokenSource();
        }
    }

    private void TellPlayersToStopBetting()
    {
        foreach (Player player in Players)
        {
            player.Cts.Cancel();
        }
    }

    private void WaitForPlayersToFinishBetting()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            Players[i].GoAttendant.WaitOne();
        }
    }

    private void SignalPlayersToStartBetting()
    {
        foreach (Player player in Players)
        {
            player.GoPlayer.Set();
        }
    }

    private void StartPlayers(int numberOfPlayers)
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            int playerNumber = i + 1;
            Players.Add(new Player(playerNumber));

            Thread player = new Thread(Players[i].Play);
            Threads.Add(player);
            player.Start();
        }
    }

    private void SpinWheel()
    {
        WinningNumber = new IndividualNumber(Random.Shared.Next(37));
    }

    private void DetermineWinners()
    {
        if (!DisplayRoundInfo)
            return;

        foreach (Player player in Players)
        {
            if (BettingMat.OneToEighteen.TryGetValue(player, out int betAmount))
            {
                if (WinningNumber.Number >= 1 && WinningNumber.Number <= 18)
                {
                    player.RoundWinAmount += betAmount * 2;
                    player.TotalWinAmount += betAmount * 2;
                    player.Balance += betAmount * 2;
                }
            }
            if (BettingMat.NineteenToThirtySix.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number >= 19 && WinningNumber.Number <= 36)
                {
                    player.RoundWinAmount += betAmount * 2;
                    player.TotalWinAmount += betAmount * 2;
                    player.Balance += betAmount * 2;
                }
            }
            if (BettingMat.Even.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number != 0 && WinningNumber.Number % 2 == 0)
                {
                    player.RoundWinAmount += betAmount * 2;
                    player.TotalWinAmount += betAmount * 2;
                    player.Balance += betAmount * 2;
                }
            }
            if (BettingMat.Odd.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number % 2 == 1)
                {
                    player.RoundWinAmount += betAmount * 2;
                    player.TotalWinAmount += betAmount * 2;
                    player.Balance += betAmount * 2;
                }
            }
            if (BettingMat.Red.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Color == "Red")
                {
                    player.RoundWinAmount += betAmount * 2;
                    player.TotalWinAmount += betAmount * 2;
                    player.Balance += betAmount * 2;
                }
            }
            if (BettingMat.Black.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Color == "Black")
                {
                    player.RoundWinAmount += betAmount * 2;
                    player.TotalWinAmount += betAmount * 2;
                    player.Balance += betAmount * 2;
                }
            }
            if (BettingMat.FirstDozen.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number >= 1 && WinningNumber.Number <= 12)
                {
                    player.RoundWinAmount += betAmount * 3;
                    player.TotalWinAmount += betAmount * 3;
                    player.Balance += betAmount * 3;
                }
            }
            if (BettingMat.SecondDozen.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number >= 13 && WinningNumber.Number <= 24)
                {
                    player.RoundWinAmount += betAmount * 3;
                    player.TotalWinAmount += betAmount * 3;
                    player.Balance += betAmount * 3;
                }
            }
            if (BettingMat.ThirdDozen.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number >= 25 && WinningNumber.Number <= 36)
                {
                    player.RoundWinAmount += betAmount * 3;
                    player.TotalWinAmount += betAmount * 3;
                    player.Balance += betAmount * 3;
                }
            }
            if (BettingMat.FirstColumn.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number % 3 == 1)
                {
                    player.RoundWinAmount += betAmount * 3;
                    player.TotalWinAmount += betAmount * 3;
                    player.Balance += betAmount * 3;
                }
            }
            if (BettingMat.SecondColumn.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number % 3 == 2)
                {
                    player.RoundWinAmount += betAmount * 3;
                    player.TotalWinAmount += betAmount * 3;
                    player.Balance += betAmount * 3;
                }
            }
            if (BettingMat.ThirdColumn.TryGetValue(player, out betAmount))
            {
                if (WinningNumber.Number % 3 == 0)
                {
                    player.RoundWinAmount += betAmount * 3;
                    player.TotalWinAmount += betAmount * 3;
                    player.Balance += betAmount * 3;
                }
            }
            if (BettingMat.SpecificNumber.TryGetValue((player, WinningNumber.Number), out betAmount))
            {
                player.RoundWinAmount += betAmount * 36;
                player.TotalWinAmount += betAmount * 36;
                player.Balance += betAmount * 36;
            }

            if (player.Balance >= Program.WinningAmount)
            {
                Program.HaveWinner = true;
            }
        }
    }

    private void DisplayBets()
    {
        Console.WriteLine($"Winning Number: {WinningNumber.Number} ({WinningNumber.Color})");
        Console.WriteLine($"Round {RoundNumber} Bets:");

        if (BettingMat.OneToEighteen.Count > 0)
        {
            Console.WriteLine("\nOne to Eighteen:");
            foreach (var bet in BettingMat.OneToEighteen)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.NineteenToThirtySix.Count > 0)
        {
            Console.WriteLine("\nNineteen to Thirty-Six:");
            foreach (var bet in BettingMat.NineteenToThirtySix)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.Even.Count > 0)
        {
            Console.WriteLine("\nEven:");
            foreach (var bet in BettingMat.Even)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.Odd.Count > 0)
        {
            Console.WriteLine("\nOdd:");
            foreach (var bet in BettingMat.Odd)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.Red.Count > 0)
        {
            Console.WriteLine("\nRed:");
            foreach (var bet in BettingMat.Red)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.Black.Count > 0)
        {
            Console.WriteLine("\nBlack:");
            foreach (var bet in BettingMat.Black)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.FirstDozen.Count > 0)
        {
            Console.WriteLine("\nFirst Dozen:");
            foreach (var bet in BettingMat.FirstDozen)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.SecondDozen.Count > 0)
        {
            Console.WriteLine("\nSecond Dozen:");
            foreach (var bet in BettingMat.SecondDozen)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.ThirdDozen.Count > 0)
        {
            Console.WriteLine("\nThird Dozen:");
            foreach (var bet in BettingMat.ThirdDozen)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.FirstColumn.Count > 0)
        {
            Console.WriteLine("\nFirst Column:");
            foreach (var bet in BettingMat.FirstColumn)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.SecondColumn.Count > 0)
        {
            Console.WriteLine("\nSecond Column:");
            foreach (var bet in BettingMat.SecondColumn)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.ThirdColumn.Count > 0)
        {
            Console.WriteLine("\nThird Column:");
            foreach (var bet in BettingMat.ThirdColumn)
            {
                Console.WriteLine($"Player {bet.Key.Number}: {bet.Value:$0.00}");
            }
        }
        if (BettingMat.SpecificNumber.Count > 0)
        {
            Console.WriteLine("\nSpecific Number:");
            foreach (var bet in BettingMat.SpecificNumber)
            {
                Console.WriteLine($"Player {bet.Key.Item1.Number} bet on {bet.Key.Item2}: {bet.Value:$0.00}");
            }
        }

        Console.WriteLine("\nPlayer Standings:");

        foreach (Player player in Players)
        {
            Console.WriteLine($"Player {player.Number}: Balance: {player.Balance:$0.00}, Round Bet Amount: {player.RoundBetAmount:$0.00}, Round Win Amount: {player.RoundWinAmount:$0.00}, Total Bet Amount: {player.TotalBetAmount:$0.00}, Total Win Amount: {player.TotalWinAmount:$0.00}");
        }

        if (Program.HaveWinner)
        {
            Console.WriteLine("\nWe have a winner!");
            foreach (Player player in Players)
            {
                if (player.Balance >= Program.WinningAmount)
                {
                    Console.WriteLine($"Player {player.Number} wins with a balance of {player.Balance:$0.00}!");
                }
            }

        }
    }
}

public static class BettingMat
{
    public static Dictionary<Player, int> OneToEighteen { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> NineteenToThirtySix { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> Even { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> Odd { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> Red { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> Black { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> FirstDozen { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> SecondDozen { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> ThirdDozen { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> FirstColumn { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> SecondColumn { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<Player, int> ThirdColumn { get; set; } = new Dictionary<Player, int>();
    public static Dictionary<(Player, int), int> SpecificNumber { get; set; } = new Dictionary<(Player, int), int>();

    public static void Reset()
    {
        OneToEighteen.Clear();
        NineteenToThirtySix.Clear();
        Even.Clear();
        Odd.Clear();
        Red.Clear();
        Black.Clear();
        FirstDozen.Clear();
        SecondDozen.Clear();
        ThirdDozen.Clear();
        FirstColumn.Clear();
        SecondColumn.Clear();
        ThirdColumn.Clear();
        SpecificNumber.Clear();
    }
}

public class IndividualNumber
{
    public int Number { get; set; }
    public string Color { get; set; }

    public IndividualNumber(int number)
    {
        Number = number;
        if (number == 0)
        {
            Color = "Green";
        }
        else if (number == 1 || number == 3 || number == 5 || number == 7 || number == 9 || number == 12
            || number == 14 || number == 16 || number == 18 || number == 19 || number == 21 || number == 23
            || number == 25 || number == 27 || number == 30 || number == 32 || number == 34 || number == 36)
        {
            Color = "Red";
        }
        else
        {
            Color = "Black";
        }
    }
}