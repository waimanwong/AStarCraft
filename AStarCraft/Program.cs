using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

public static class RandomNumber
{
    private static readonly RNGCryptoServiceProvider _generator = new RNGCryptoServiceProvider();

    public static int Between(int minimumValue, int maximumValue)
    {
        byte[] randomNumber = new byte[1];

        _generator.GetBytes(randomNumber);

        double asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

        // We are using Math.Max, and substracting 0.00000000001, 
        // to ensure "multiplier" will always be between 0.0 and .99999999999
        // Otherwise, it's possible for it to be "1", which causes problems in our rounding.
        double multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);

        // We need to add one to the range, to allow for the rounding done with Math.Floor
        int range = maximumValue - minimumValue + 1;

        double randomValueInRange = Math.Floor(multiplier * range);

        return (int)(minimumValue + randomValueInRange);
    }
}

struct Position
{
    public int x;
    public int y;
    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    //Return top, bottom, left, right
    public Position[] GetNeighbors()
    {
        Position[] neighbors = new Position[4];

        //top
        int yTop = this.y == 0 ? Map.Height - 1 : this.y - 1;
        neighbors[0] = new Position(this.x, yTop);

        //bottom
        int yBottom = this.y == Map.Height - 1 ? 0 : this.y + 1;
        neighbors[1] = new Position(this.x, yBottom);

        //Left
        int xLeft = this.x == 0 ? Map.Width - 1 : this.x - 1;
        neighbors[2] = new Position(xLeft, this.y);

        //Right
        int xRight = this.x == Map.Width - 1 ? 0 : this.x + 1;
        neighbors[3] = new Position(xRight, this.y);

        return neighbors;
    }

    public override string ToString()
    {
        return $"({x.ToString()},{y.ToString()})";
    }
}

class Map
{
    private string[] _rows;

    private readonly char[][] cells;
    public const int Height = 10;
    public const int Width = 19;

    public const char Void = '#';
    public const char Empty = '.';
    public const char UpArrow = 'U';
    public const char RightArrow = 'R';
    public const char DownArrow = 'D';
    public const char LeftArrow = 'L';

    public Map(params string[] rows)
    {
        this._rows = rows;
        this.cells = rows.Select(row => row.ToCharArray()).ToArray();
        InitializeState();
    }

    public char GetCell(Position pos)
    {
        return GetCell(pos.x, pos.y);
    }

    public char GetCell(int x, int y)
    {
        return cells[y][x];
    }

    public List<Position> EmptyCells = new List<Position>(19 * 10);
    public Dictionary<Position, int> EmptyNeighbors = new Dictionary<Position, int>();

    private void InitializeState()
    {
        
        for (int y = 0; y < Map.Height; y++)
        {
            for (int x = 0; x < Map.Width; x++)
            {
                if (cells[y][x] == Empty)
                {
                    var currentPosition = new Position(x, y);
                    EmptyCells.Add(currentPosition);

                    var neighbors = currentPosition.GetNeighbors();
                    var neighborCells = neighbors.Select(neighborPosition => this.GetCell(neighborPosition)).ToArray();
                    var neighborEmptyCellCount = neighborCells.Count(c => c == Empty);

                    EmptyNeighbors[currentPosition] = neighborEmptyCellCount;
                }
            }
        }
    }
    
    public Map Apply(IEnumerable<Arrow> arrows)
    {
        var newMap = new Map(this._rows);
        foreach (var arrow in arrows)
        {
            newMap.cells[arrow.y][arrow.x] = arrow.direction;
        }
        return newMap;
    }

    public void Debug()
    {
        var rows = cells.Select(charArray => new string(charArray)).ToArray();
        foreach (var row in rows)
        {
            Player.Debug(row);
        }
    }
}

struct Arrow
{
    public int x, y;
    public char direction;
    public Arrow(Position p, char direction)
    {
        this.x = p.x;
        this.y = p.y;
        this.direction = direction;
    }
    public override string ToString()
    {
        return $"{x.ToString()} {y.ToString()} {direction}";
    }
}

struct Robot
{
    public int id;
    public int x;
    public int y;
    public char direction;

    public Robot(int id,  int x, int y, char direction)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.direction = direction;
    }

    public Robot Clone()
    {
        return new Robot(id, x, y, direction);
    }
    
    public void Move()
    {
        switch (direction)
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
    public static int ComputeScore(Map map, Robot[] robots)
    {
        int score = 0;

        foreach (var robot in robots.Select(r => r.Clone()).ToArray())
        {
            score += ComputeScore(map, robot);
        }
        return score;
    }

    private static int ComputeScore(Map map, Robot robot)
    {
        ChangeDirectionIfLocatedOnAnArrow(map, ref robot);

        bool robotIsAlive = true;
        int score = 0;
        HashSet<Robot> visitedStates = new HashSet<Robot>(1000);

        visitedStates.Add(robot.Clone());

        while (robotIsAlive)
        {
            //Score is incremented by the number of robots in function.
            score++;

            //Automaton2000 robots move by 1 cell in the direction they're facing.
            robot.Move();

            //Automaton2000 robots change their direction if they're located on an arrow.
            ChangeDirectionIfLocatedOnAnArrow(map, ref robot);

            //Automaton2000 robots stop functioning if they're located on a void cell or if they've entered a state(position, direction) they've been in before. (Automaton2000 robots don't share their state history)
            var robotState = robot.Clone();
            var cellContent = map.GetCell(robot.x, robot.y);
            if (cellContent == Map.Void || visitedStates.Contains(robotState))
            {
                robotIsAlive = false;
            }
            else
            {
                visitedStates.Add(robotState);
            }
        }

        return score;
    }

    private static void ChangeDirectionIfLocatedOnAnArrow(Map map, ref Robot robot)
    {
        //Automaton2000 robots change their direction if they're located on an arrow.
        var cellContent = map.GetCell(robot.x, robot.y);
        switch (cellContent)
        {
            case 'U':
            case 'R':
            case 'D':
            case 'L':
                robot.direction = cellContent;
                break;
            default:
                //Do nothing
                break;
        }
    }
}

class Solution
{
    private List<Arrow> _arrowsToPlace = new List<Arrow>();

    public Arrow[] Arrows => _arrowsToPlace.ToArray();

    public void Add(Arrow arrow)
    {
        this._arrowsToPlace.Add(arrow);
    }

    public override string ToString()
    {
        return string.Join(" ", this._arrowsToPlace.Select(arrow => arrow.ToString()).ToArray());
    }
}

static class SolutionFinder
{
    public static Solution FindBestSolution(Map map, Robot[] robots)
    {
        var platformCells = map.EmptyCells.ToArray();

        var solutionClockWise = GetSolution(map, platformCells, robots, clockWise: true);
        var newMapClockWise = map.Apply(solutionClockWise.Arrows);
        int scoreClockWise = ScoreCalculator.ComputeScore(newMapClockWise, robots);
        Player.Debug($"Expected score clockwise {scoreClockWise.ToString()}");

        var solutionAntiClockWise = GetSolution(map, platformCells, robots, clockWise: false);
        var newMapAntiClockWise = map.Apply(solutionAntiClockWise.Arrows);
        int scoreAntiClockWise = ScoreCalculator.ComputeScore(newMapAntiClockWise, robots);
        Player.Debug($"Expected score anti clock wise {scoreAntiClockWise.ToString()}");

        return scoreAntiClockWise < scoreClockWise ? solutionClockWise : solutionAntiClockWise;
    }

    private static Solution GetSolution(Map map, Position[] platformCells,Robot[] robots,  bool clockWise)
    {
        var solution = new Solution();

        foreach (var platformCell in platformCells)
        {
            var neighbors = platformCell.GetNeighbors();
            var neighborCells = neighbors.Select(neighborPosition => map.GetCell(neighborPosition)).ToArray();
            var neighborEmptyCellCount = neighborCells.Count(c => c == '#');

            if (neighborEmptyCellCount == 3)
            {
                //Dead ends
                HandleDeadEnds(ref solution, map, neighbors, platformCell);
            }
            else if (neighborEmptyCellCount == 2)
            {
                //Corners
                HandleCorner(ref solution, map, neighbors, platformCell, clockWise);
            }
        }
        return solution;
    }

    private static void HandleCorner(ref Solution solution, Map map, Position[] neighbors, Position platformCell, bool clockWise)
    {
        //top right corner
        if ((map.GetCell(neighbors[0]) == Map.Void) && (map.GetCell(neighbors[3]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.DownArrow));
            else
                solution.Add(new Arrow(platformCell, Map.LeftArrow));
        }

        //top left corner
        if ((map.GetCell(neighbors[0]) == Map.Void) && (map.GetCell(neighbors[2]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.RightArrow));
            else
                solution.Add(new Arrow(platformCell, Map.DownArrow));
        }

        //bottom left corner
        if ((map.GetCell(neighbors[1]) == Map.Void) && (map.GetCell(neighbors[2]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.UpArrow));
            else
                solution.Add(new Arrow(platformCell, Map.RightArrow));
        }

        //bottom right corner
        if ((map.GetCell(neighbors[1]) == Map.Void) && (map.GetCell(neighbors[3]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.LeftArrow));
            else
                solution.Add(new Arrow(platformCell, Map.UpArrow));
        }
    }

    private static void HandleDeadEnds(ref Solution solution, Map map, Position[] neighbors, Position platformCell)
    {
        //Return top
        if (map.GetCell(neighbors[0]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.UpArrow));
        }
        //Return bottom
        if (map.GetCell(neighbors[1]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.DownArrow));
        }
        //Return left
        if (map.GetCell(neighbors[2]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.LeftArrow));
        }
        //Return right
        if (map.GetCell(neighbors[3]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.RightArrow));
        }
    }
}

class RandomSolutionFinder
{
    public static Solution GenerateRandomSolution(Map map, List<Position> positions)
    {
        Solution s = new Solution();
        
        foreach (var position in positions)
        {
            var neighborEmptyCellCount = map.EmptyNeighbors[position];
            var neighbors = position.GetNeighbors();
            
            if (neighborEmptyCellCount == 1)
            {
                int rand = RandomNumber.Between(0, 10);
                if (rand < 9)
                {
                    //Dead ends
                    HandleDeadEnds(ref s, map, neighbors, position);
                }
            }
            else if (neighborEmptyCellCount == 2)
            {
                int rand = RandomNumber.Between(0, 10);
                bool clockWise = rand <= 5;

                //Corners
                HandleCorner(ref s, map, neighbors, position, clockWise);
            }
            else if(neighborEmptyCellCount == 3)
            {
                int p = RandomNumber.Between(0, 10);
                
                if (p <= 2)
                {
                    //Do nothing
                }
                else if (p <= 4)
                {
                    //bottom
                    int yBottom = position.y == Map.Height - 1 ? 0 : position.y + 1;
                   
                    if (map.GetCell(position.x, yBottom) != Map.Void)
                    {
                        s.Add(new Arrow(position, Map.DownArrow));
                    }
                }
                else if (p <= 6)
                {
                    //Right
                    int xRight = position.x == Map.Width - 1 ? 0 : position.x + 1;
                    
                    if (map.GetCell(xRight, position.y ) != Map.Void)
                    {
                        s.Add(new Arrow(position, Map.RightArrow));
                    }
                }
                else if (p <= 8)
                {
                    //top
                    int yTop = position.y == 0 ? Map.Height - 1 : position.y - 1;
                    
                    if (map.GetCell(position.x, yTop) != Map.Void)
                    {
                        s.Add(new Arrow(position, Map.UpArrow));
                    }
                }
                else
                {
                    //Left
                    int xLeft = position.x == 0 ? Map.Width - 1 : position.x - 1;
                    if (map.GetCell(xLeft, position.y) != Map.Void)
                    {
                        s.Add(new Arrow(position, Map.LeftArrow));
                    }
                }
            }
            else
            {
                //Do nothing
            }
        }
        return s;
    }

    private static void HandleCorner(ref Solution solution, Map map, Position[] neighbors, Position platformCell, bool clockWise)
    {
        //neigbors top bottom left right

        //top right corner
        if ((map.GetCell(neighbors[0]) == Map.Void) && (map.GetCell(neighbors[3]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.DownArrow));
            else
                solution.Add(new Arrow(platformCell, Map.LeftArrow));
        }

        //top left corner
        if ((map.GetCell(neighbors[0]) == Map.Void) && (map.GetCell(neighbors[2]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.RightArrow));
            else
                solution.Add(new Arrow(platformCell, Map.DownArrow));
        }

        //bottom left corner
        if ((map.GetCell(neighbors[1]) == Map.Void) && (map.GetCell(neighbors[2]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.UpArrow));
            else
                solution.Add(new Arrow(platformCell, Map.RightArrow));
        }

        //bottom right corner
        if ((map.GetCell(neighbors[1]) == Map.Void) && (map.GetCell(neighbors[3]) == Map.Void))
        {
            if (clockWise)
                solution.Add(new Arrow(platformCell, Map.LeftArrow));
            else
                solution.Add(new Arrow(platformCell, Map.UpArrow));
        }
    }

    private static void HandleDeadEnds(ref Solution solution, Map map, Position[] neighbors, Position platformCell)
    {

        //Return top
        if (map.GetCell(neighbors[0]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.UpArrow));
        }
        //Return bottom
        if (map.GetCell(neighbors[1]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.DownArrow));
        }
        //Return left
        if (map.GetCell(neighbors[2]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.LeftArrow));
        }
        //Return right
        if (map.GetCell(neighbors[3]) == Map.Empty)
        {
            solution.Add(new Arrow(platformCell, Map.RightArrow));
        }
    }

    public static Tuple<Solution, int> FindSolution(Map map, Robot[] robots)
    {
        var platformCells = map.EmptyCells;

        Solution bestSolution = null;
        int bestScore = -1;
        Stopwatch watch = Stopwatch.StartNew();

        int simulationCount = 0;

        while (watch.ElapsedMilliseconds < 970)
        {
            var solution = RandomSolutionFinder.GenerateRandomSolution(map, platformCells);
            
            //Evaluate
            var newMap = map.Apply(solution.Arrows);
            int score = ScoreCalculator.ComputeScore(newMap, robots);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestSolution = solution;
            }

            simulationCount++;
        }

        Player.Debug($"Nb Simulation {simulationCount.ToString()} in {watch.ElapsedMilliseconds.ToString()}ms");
        Player.Debug($"Best score achieved: {bestScore.ToString()}");

        return new Tuple<Solution, int>(bestSolution, bestScore);
    }
}

static class Test
{
    public static void Run()
    {
        var map = new Map(
            "###################",
            "###################",
            "###################",
            "###################",
            "###............####",
            "###################",
            "###################",
            "###################",
            "###################",
            "###################");
        var robots = new Robot[]
        {
            new Robot(0,3,4,'R')
        };

        var bestSolution = RandomSolutionFinder.FindSolution(map, robots);
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
        //Test.Run();

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

            robots[i] = new Robot(i, x, y, direction[0]);
        }

        var bestSolution = RandomSolutionFinder.FindSolution(map, robots);

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");
        var output = bestSolution.Item1.ToString();
        Player.Debug(output);

        Console.WriteLine(output);
    }
}