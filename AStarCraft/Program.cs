using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

struct Position
{
    public int x;
    public int y;
    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

class Map
{
    private readonly char[][] cells;
    public const int Height = 10;
    public const int Width = 19;


    public Map(string[] rows)
    {
        this.cells = rows.Select(row => row.ToCharArray()).ToArray();
    }
    
    public char GetCell(int x, int y)
    {
        return cells[y][x];
    }

    static List<Position> _platformCells = new List<Position>(19 * 10);

    public Position[] GetPlatformCells()
    {
        _platformCells.Clear();

        for (int y = 0; y < Map.Height; y++)
        {
            for (int x = 0; x < Map.Width; x++)
            {
                if (cells[y][x] == '.')
                {
                    _platformCells.Add(new Position(x, y));
                }
            }
        }
        return _platformCells.ToArray();
    }
    
    public Map Apply(List<Arrow> arrows)
    {
        var newMap = (Map)this.MemberwiseClone();
        foreach(var arrow in arrows)
        {
            this.cells[arrow.y][arrow.x] = arrow.direction;
        }
        return newMap;    
    }

    public void Debug()
    {
        var rows = cells.Select(charArray => new string(charArray)).ToArray();
        foreach(var row in rows)
        {
            Player.Debug(row);
        }
    }
}

struct Arrow
{
    public int x, y;
    public char direction;
    public Arrow(int x, int y, char direction)
    {
        this.x = x;
        this.y = y;
        this.direction = direction;
    }
    public override string ToString()
    {
        return $"{x.ToString()} {y.ToString()} {direction}";
    }
}

class Robot
{
    public int x;
    public int y;
    public char direction;

    public Robot(int x, int y, char direction)
    {
        this.x = x;
        this.y = y;
        this.direction = direction;
    }

    public Robot Clone()
    {
        return new Robot(x, y, direction);
    }

    
    public void Move()
    {
        switch(direction)
        {
            case 'U':
                y = y == 0 ? Map.Height - 1 : y - 1;
                break;
            case 'R':
                x = x == Map.Width - 1 ? 0 : x + 1;
                break;
            case 'D':
                y = y == Map.Height - 1 ? y = 0 : y + 1;
                break;
            case 'L':
                x = x == 0 ? Map.Width - 1 : x - 1;
                break;
        }
    }

}

static class ScoreCalculator
{
    public static int ComputeScore(Map map, Robot[] robots )
    {
        int score = 0;

        foreach(var robot in robots)
        {
            score += ComputeScore(map, robot);
        }
        return score;
    }

    private static int ComputeScore(Map map, Robot robot)
    {
        bool robotIsAlive = true;
        int score = 0;
        HashSet<int> visitedArrows = new HashSet<int>();

        visitedArrows.Add(robot.y * Map.Width + robot.x);

        while (robotIsAlive )
        {
            score++;

            robot.Move();

            var cellContent = map.GetCell(robot.x, robot.y);
            switch(cellContent)
            {
                case '#':
                    robotIsAlive = false;
                    break;
                case '.':
                    break;
                case 'U':
                case 'R':
                case 'D':
                case 'L':
                    int hashedArrow = robot.y * Map.Width + robot.x;

                    if (visitedArrows.Contains(hashedArrow))
                    {
                        robotIsAlive = false;
                    }
                    else
                    {
                        visitedArrows.Add(hashedArrow);
                        robot.direction = cellContent;
                    }
                    break;
            }
        }

        return score;
    }
}

static class SolutionOptimizer
{
    
    public static Tuple<List<Arrow>, int> FindBestSolution(Map map, Robot[] robots)
    {
        var emptyCells = map.GetPlatformCells();
        int bestScore = 0;
        List<Arrow> bestSolution = new List<Arrow>();

        Stopwatch stopwatch = Stopwatch.StartNew();

        while(stopwatch.ElapsedMilliseconds < 900)
        {
            var solution = BuildSolution(emptyCells);

            var newMap = map.Apply(solution);

            var score = ScoreCalculator.ComputeScore(newMap, robots.Select(r => r.Clone()).ToArray());
            if(score > bestScore)
            {
                bestScore = score;
                bestSolution = solution;
            }
        }

        return new Tuple<List<Arrow>, int>(bestSolution, bestScore);
    }

    private static List<Arrow> BuildSolution(Position[] emptyCells)
    {
        List<Arrow> solution = new List<Arrow>(emptyCells.Length);
        Random rand = new Random();

        for(int i = 0; i < emptyCells.Length; i++)
        {
            var currentCell = emptyCells[i];

            //Place a arrow or not ?
            if(rand.NextDouble() < 0.5)
            {
                //Which arrow ?
                var randomArrow = rand.NextDouble();
                if(randomArrow <= 0.25)
                {
                    solution.Add(new Arrow(currentCell.x, currentCell.y, 'U'));
                }
                else if(randomArrow <= 0.5)
                {
                    solution.Add(new Arrow(currentCell.x, currentCell.y, 'R'));
                }
                else if (randomArrow <= 0.75)
                {
                    solution.Add(new Arrow(currentCell.x, currentCell.y, 'D'));
                }
                else
                {
                    solution.Add(new Arrow(currentCell.x, currentCell.y, 'L'));
                }
            }
        }

        return solution;
    }

}

class Player
{
    public static void Debug(string message)
    {
        Console.Error.WriteLine(message);
    }

    static void Main(string[] args)
    {
       
        string[] lines = new string[10];

        for (int i = 0; i < 10; i++)
        {
            string line = Console.ReadLine();
            lines[i] = line;
        }
        Map map = new Map(lines);
        
        int robotCount = int.Parse(Console.ReadLine());
        Robot[] robots = new Robot[robotCount];

        for (int i = 0; i < robotCount; i++)
        {
            string[] inputs = Console.ReadLine().Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            string direction = inputs[2];

            robots[i] = new Robot(x, y, direction[0]);
        }

        var bestSolution = SolutionOptimizer.FindBestSolution(map, robots);
        Player.Debug($"Expected score {bestSolution.Item2.ToString()}");

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");

        var output = string.Join(" ", bestSolution.Item1.Select(arrow => arrow.ToString()).ToArray());
        
        Console.WriteLine(output);
    }
}