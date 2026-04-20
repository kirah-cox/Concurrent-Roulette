class Program
{
    public static bool HaveWinner = false;
    public const int WinningAmount = 2000;
    static void Main()
    {
        List<Player> players = new List<Player>();
        List<Thread> threads = new List<Thread>();

        Console.CursorVisible = false;

        for (int i = 0; i < 4; i++)
        {
            int playerNumber = i + 1;
            players.Add(new Player(playerNumber));

            Thread player = new Thread(players[i].MakeBet);
            threads.Add(player);
            player.Start();
        }

        int roundNumber = 0;

        while (!HaveWinner)
        {
            roundNumber++;

            Console.WriteLine($"Round {roundNumber} Starts.");

            foreach (Player player in players)
            {
                player.GoPlayer.Set();
            }

            Thread.Sleep(2000);

            foreach (Player player in players)
            {
                player.Cts.Cancel();
            }

            Thread.Sleep(500);

            foreach (Player player in players)
            {
                player.Cts.Dispose();
                player.Cts = new CancellationTokenSource();
            }

            Console.WriteLine($"Round {roundNumber} Ends.");

            Console.WriteLine($"Round {roundNumber} Results:\n");

            foreach (Player player in players)
            {
                Console.WriteLine($"Player {player.Number}:");
                Console.WriteLine($"    Balance = {player.Balance}");
                Console.WriteLine($"    Amount Bet This Round = {player.RoundBetAmount}");
                Console.WriteLine($"    Amount Won This Round = {player.RoundWinAmount}");
                Console.WriteLine($"    Total Amount Bet = {player.TotalBetAmount}");
                Console.WriteLine($"    Total Amount Won = {player.TotalWinAmount}\n");
            }

            foreach (Player player in players)
            {
                if (player.Balance <= 0)
                {
                    Console.WriteLine($"Player {player.Number} is out of money.");
                }
            }

            if (players[0].Balance <= 0 && players[1].Balance <= 0 && players[2].Balance <= 0 && players[3].Balance <= 0)
            {
                HaveWinner = true;
                Console.WriteLine("All players are out of money. No winner.");
                break;
            }

            foreach (Player player in players)
            {
                if (player.Balance >= WinningAmount)
                {
                    HaveWinner = true;
                    Console.WriteLine($"Player {player.Number} wins with a balance of {player.Balance}!");
                }
            }
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }
    }
}

class Player
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

    public Player(int number)
    {
        Number = number;
    }

    public void MakeBet()
    {
        while (!Program.HaveWinner)
        {
            GoPlayer.WaitOne();
            RoundBetAmount = 0;
            RoundWinAmount = 0;
            while(Balance > 0 && !Program.HaveWinner && !Cts.Token.IsCancellationRequested)
            {
                if (GetRandom(4) == 1)
                {
                    IndividualBetAmount = GetRandom(51);

                    TotalBetAmount += IndividualBetAmount;
                    RoundBetAmount += IndividualBetAmount;

                    Balance -= IndividualBetAmount;

                    TotalWinAmount -= IndividualBetAmount;
                    RoundWinAmount -= IndividualBetAmount;

                    if (GetRandom(37) == 1)
                    {
                        Balance += IndividualBetAmount * 36;
                        TotalWinAmount += IndividualBetAmount * 36;
                        RoundWinAmount += IndividualBetAmount * 36;
                    }
                }

                Thread.Sleep(250);
            }
        }
    }

    public int GetRandom(int max)
    {
        Random random = new Random();
        return random.Next(1, max);
    }
}