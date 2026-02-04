using System.Linq;
using System.Threading;

const string helper = "Expected format: S(single) / D(double) / T(triple) + value (1-20 or 25), or M for miss.";
const string exitLine = "Input stream closed. Exiting game.";

int[] board = new int[21];
for (int i = 0; i < 20; i++)
{
    board[i] = i + 1;
}
board[20] = 25;

string? playerCount = "1";
int noPlayers;
bool doubleOut = false;
bool playAgain = true;
var allThrows = AllPossibleThrows(board);

Console.Clear();
do
{
    //read and validate number of players
    Console.WriteLine("How many players are playing this round? (Max 4)");
    while (true)
    {
        playerCount = Console.ReadLine();
        if (playerCount == null)
        {
            Console.WriteLine(exitLine);
            return;
        }
        bool isValidInput = int.TryParse(playerCount, out noPlayers);
        if (!isValidInput || (noPlayers < 1 || noPlayers > 4))
        {
            Console.WriteLine("Please enter a number between 1 and 4.");
            continue;
        }
        break;
    }
    //sets n.of players and their initial score
    string[] players = new string[noPlayers];
    int[] score = new int[noPlayers];

    for (int i = 0; i < noPlayers; i++)
    {
        players[i] = $"Player {i + 1}";
        score[i] = 501;
    }
    //declaration and validation of double-out rule
    Console.WriteLine("Do you want to apply double-out rule? Y(yes) / N(no)");
    while (true)
    {
        string? choice = Console.ReadLine();
        if (choice == null)
        {
            Console.WriteLine(exitLine);
            return;
        }

        choice = choice.Trim().ToLower();
        if (!DecideValidator(choice))
        {
            Console.WriteLine("You entered wrong value, please enter Y for double out, or N if you don't want to apply double out rule.");
            continue;
        }
        doubleOut = choice == "y";
        break;
    }
    int roundNumber = 1;
    int currentPlayer = 0;
    Console.WriteLine(new string('-', Console.WindowWidth));
    //main loop for game until someone wins
    while (true)
    {
        if (currentPlayer == 0)
        {
            Console.WriteLine($"Round {roundNumber}");
            Console.WriteLine($"Insert values of your throw one by one. {helper}");
            Console.WriteLine(new string('-', Console.WindowWidth));
        }
        Console.WriteLine($"Turn of {players[currentPlayer]}. Current score: {score[currentPlayer]}");

        int sumOfThrow = 0;
        int oneThrow = 0;
        int tempScore = score[currentPlayer];
        char multiplier = ' ';
        //throw cycle handling up to 3 darts
        for (int i = 0; i < 3; i++)
        {
            string? valueOfThrow = "";
            bool ok = false;
            //printing possible checkouts if checkout is viable, with and withotu double-out
            if (!doubleOut && tempScore <= 180)
                PrintCheckout(allThrows, tempScore, i, doubleOut);
            if (doubleOut && tempScore <= 170)
                PrintCheckout(allThrows, tempScore, i, doubleOut);
            //loop for inserting values of darts and checking if it's valid input
            do
            {
                Console.Write($"Dart {i + 1}: ");
                valueOfThrow = Console.ReadLine();
                if (valueOfThrow == null)
                {
                    Console.WriteLine(exitLine);
                    return;
                }

                valueOfThrow = valueOfThrow.Trim().ToLower();
                try
                {
                    ok = ThrowInputCheck(board, valueOfThrow, helper);
                }
                catch (FormatException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            } while (!ok);

            //getting value of throw and adding to current score of a throw
            multiplier = CheckThrowMultiplier(valueOfThrow);
            if (multiplier != 'm')
            {
                oneThrow = int.Parse(CheckThrowValue(valueOfThrow));
                sumOfThrow += GetThrowScore(oneThrow, multiplier);
            }
            tempScore = score[currentPlayer] - sumOfThrow;
            //stop early if score would bust or become unfinishable or win
            if (doubleOut && tempScore <= 1)
                break;
            if (tempScore <= 0)
                break;
        }
        score[currentPlayer] = ResolveThrowScore(score[currentPlayer], sumOfThrow, doubleOut, multiplier);

        Console.WriteLine(new string('-', Console.WindowWidth));

        if (score[currentPlayer] == 0)
        {
            Console.WriteLine($"Congratulations! {players[currentPlayer]} won!!!");
            Console.WriteLine(new string('-', Console.WindowWidth));
            break;
        }

        Console.WriteLine("Press Enter for next player.");
        Console.ReadLine();

        currentPlayer = (currentPlayer + 1) % noPlayers;

        if (currentPlayer == 0)
            roundNumber = EndOfRound(roundNumber, noPlayers, players, score);

    }
    Console.WriteLine($"Do you want to play again? Y(yes) / N(no)");

    while (true)
    {
        string? choice = Console.ReadLine();
        if (choice == null)
        {
            Console.WriteLine(exitLine);
            return;
        }

        choice = choice.Trim().ToLower();
        if (!DecideValidator(choice))
        {
            Console.WriteLine("Please enter Y or N");
            continue;
        }
        if (choice == "n")
            playAgain = false;
        break;
    }
} while (playAgain);
//validates user's throw input
static bool ThrowInputCheck(int[] board, string valueOfThrow, string helper)
{
    if (valueOfThrow == "m")
        return true;

    if (valueOfThrow == "")
        throw new FormatException($"Input is empty. {helper}");

    if (valueOfThrow.Length < 2)
        throw new FormatException($"Format of input is invalid in '{valueOfThrow}'. {helper}");

    char mult = CheckThrowMultiplier(valueOfThrow);
    if (mult != 's' && mult != 'd' && mult != 't')
        throw new FormatException($"First character needs to be a valid multiplier, currently it's '{mult}'. {helper}");

    if (!int.TryParse(CheckThrowValue(valueOfThrow), out int value))
        throw new FormatException($"Numeric part '{valueOfThrow.Substring(1)}' is not valid. {helper}");

    if (!board.Contains(value))
        throw new FormatException($"Value '{value}' is not on dart board. {helper}");

    if (value == 25 && mult == 't')
        throw new FormatException($"Bull can't be triple. {helper}");

    return true;
}
//calculates score of a single throw
static int GetThrowScore(int value, char multiplier)
{
    int multiplierValue = 0;

    switch (multiplier)
    {
        case 's':
            multiplierValue = 1;
            break;
        case 'd':
            multiplierValue = 2;
            break;
        case 't':
            multiplierValue = 3;
            break;
    }
    return value * multiplierValue;
}
//applies and resolves score after a throw
static int ResolveThrowScore(int score, int sumOfThrow, bool doubleOut, char multiplier)
{
    score -= sumOfThrow;
    switch (score)
    {
        case > 0:
            if (score == 1 && doubleOut)
            {
                Console.WriteLine($"\nYour score is currently {score}. You can't close this round with double.");
                score += sumOfThrow;
                Console.WriteLine($"Your current score is again: {score}");
                break;
            }
            Console.WriteLine($"\nYou threw {sumOfThrow}. Your current score is: {score}");
            break;

        case 0:
            if (doubleOut && multiplier == 'd')
            {
                Console.WriteLine($"\nYour score is exactly: {score}.");
                break;
            }
            if (doubleOut && multiplier != 'd')
            {
                Console.WriteLine($"\nYour score is {score}, but you need to close with double.");
                score += sumOfThrow;
                Console.WriteLine($"Your current score is again: {score}");
                break;
            }

            Console.WriteLine($"\nYour score is exactly: {score}.");
            break;

        case < 0:
            score += sumOfThrow;
            Console.WriteLine($"\nYou threw {sumOfThrow} and busted. Last round of throws is not valid. Your current score is again: {score}");
            break;
    }
    return score;
}
//printing round scores and increments the round counter
static int EndOfRound(int currentRound, int noPlayers, string[] players, int[] score)
{
    Console.WriteLine($"Score after round {currentRound} is:");
    const int colWidth = 10;

    for (int i = 0; i < noPlayers; i++)
    {
        Console.Write($"|  {players[i],-colWidth}");
    }
    Console.WriteLine();
    for (int i = 0; i < noPlayers; i++)
    {
        Console.Write($"|  {score[i],-colWidth}");
    }
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine("Starting next round");
    Thread.Sleep(2000);
    Console.WriteLine(new string('-', Console.WindowWidth));
    currentRound++;
    return currentRound;

}
//prints a possible checkout based on darts left and double-out rule
static void PrintCheckout(DartThrow[] allThrows, int score, int actualDart, bool doubleOut)
{
    const string checkout = "Possible checkout with: ";
    for (int a = allThrows.Length - 1; a >= 0; a--)
    {
        var d1 = allThrows[a];

        if (doubleOut && d1.Multiplier == 'D' && d1.Score == score)
        {
            Console.WriteLine(checkout + d1.FullValue);
            return;
        }
        if (!doubleOut && d1.Score == score)
        {
            Console.WriteLine(checkout + d1.FullValue);
            return;
        }
        for (int b = allThrows.Length - 1; b >= 0 && actualDart < 2; b--)
        {
            var d2 = allThrows[b];

            if (doubleOut && d2.Multiplier == 'D' && d1.Score + d2.Score == score)
            {
                Console.WriteLine($"{checkout}{d1.FullValue}, {d2.FullValue}");
                return;
            }

            if (!doubleOut && d1.Score + d2.Score == score)
            {
                Console.WriteLine($"{checkout}{d1.FullValue}, {d2.FullValue}");
                return;
            }
            for (int c = allThrows.Length - 1; c >= 0 && actualDart < 1; c--)
            {
                var d3 = allThrows[c];

                if (doubleOut && d3.Multiplier == 'D' && d1.Score + d2.Score + d3.Score == score)
                {
                    Console.WriteLine($"{checkout}{d1.FullValue}, {d2.FullValue}, {d3.FullValue}");
                    return;
                }

                if (!doubleOut && d1.Score + d2.Score + d3.Score == score)
                {
                    Console.WriteLine($"{checkout}{d1.FullValue}, {d2.FullValue}, {d3.FullValue}");
                    return;
                }
            }
        }
    }
}
//generates all possible dart throws using struct
static DartThrow[] AllPossibleThrows(int[] board)
{
    DartThrow[] possibleThrows = new DartThrow[(board.Length * 3) - 1];
    int index = 0;
    for (int i = 0; i < board.Length - 1; i++)
    {
        possibleThrows[index++] = new DartThrow(board[i], 's');
        possibleThrows[index++] = new DartThrow(board[i], 'd');
        possibleThrows[index++] = new DartThrow(board[i], 't');

    }
    possibleThrows[index++] = new DartThrow(board[20], 's');
    possibleThrows[index++] = new DartThrow(board[20], 'd');

    return possibleThrows;
}
//returns multiplier of the throw input
static char CheckThrowMultiplier(string valueOfThrow)
{
    return valueOfThrow[0];
}

//returns the numeric value part of the throw input
static string CheckThrowValue(string valueOfThrow)
{
    return valueOfThrow.Substring(1);
}

static bool DecideValidator(string choice)
{
    return choice == "y" || choice == "n";
}
//holds data for one possible throw used to generate checkout combinations
struct DartThrow
{
    public int BaseValue;
    public char Multiplier;
    public string FullValue;
    public int Score;
    public DartThrow(int value, char multiplier)
    {
        BaseValue = value;
        int MultiplierValue = 0;

        switch (multiplier)
        {
            case 's':
                Multiplier = 'S';
                MultiplierValue = 1;
                break;
            case 'd':
                Multiplier = 'D';
                MultiplierValue = 2;
                break;
            case 't':
                Multiplier = 'T';
                MultiplierValue = 3;
                break;
            default:
                throw new ArgumentException("Invalid multiplier", nameof(multiplier));
        }
        Score = BaseValue * MultiplierValue;
        FullValue = Multiplier.ToString() + BaseValue;
    }
}