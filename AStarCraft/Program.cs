using System;
using System.Collections.Generic;
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
    private readonly char[][] cells;
    public const int Height = 10;
    public const int Width = 19;

    public const char Void = '#';
    public const char Empty = '.';
    public const char UpArrow = 'U';
    public const char RightArrow = 'R';
    public const char DownArrow = 'D';
    public const char LeftArrow = 'L';
    
    public Map(string[] rows)
    {
        this.cells = rows.Select(row => row.ToCharArray()).ToArray();
        this.ComputeInternalState();
    }
    
    public char GetCell(Position pos)
    {
        return GetCell(pos.x, pos.y);
    }
    
    public char GetCell(int x, int y)
    {
        return cells[y][x];
    }

    public List<Position> VoidCells = new List<Position>(Map.Width * Map.Height);
    public List<Position> PlatformCells = new List<Position>(Map.Width * Map.Height);

    private void ComputeInternalState()
    {
        for (int y = 0; y < Map.Height; y++)
        {
            for (int x = 0; x < Map.Width; x++)
            {
                if (cells[y][x] == '.')
                {
                    PlatformCells.Add(new Position(x, y));
                }
                if (cells[y][x] == Map.Void)
                {
                    VoidCells.Add(new Position(x, y));
                }
            }
        }
    }
    
    public Map Apply(IEnumerable<Arrow> arrows)
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
    public Arrow(Position p, char direction) : this(p.x, p.y, direction)
    {  
    }

    public override string ToString()
    {
        return $"{x.ToString()} {y.ToString()} {direction}";
    }
}

struct RobotState
{
    public int x;
    public int y;
    public char direction;
    public RobotState(int x, int y, char direction)
    {
        this.x =x;
        this.y = y;
        this.direction = direction;
    }

    public override string ToString()
    {
        return $"({x.ToString()},{y.ToString()}) {direction.ToString()}";
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

    private Position GetNextPosition()
    {
        switch (direction)
        {
            case 'U':
                return new Position(x, y == 0 ? Map.Height - 1 : y - 1);
            case 'R':
                return new Position(x == Map.Width - 1 ? 0 : x + 1, y);
            case 'D':
                return new Position(x, y == Map.Height - 1 ? y = 0 : y + 1);
            case 'L':
                return new Position(x == 0 ? Map.Width - 1 : x - 1, y);
        }
        throw new NotSupportedException();
    }

    public RobotState GetNextState()
    {
        var nextPosition = GetNextPosition();
        return new RobotState(nextPosition.x, nextPosition.y, this.direction);
    }

    public RobotState DumpState()
    {
        return new RobotState(this.x, this.y, this.direction);
    }
}

static class ScoreCalculator
{
    public static int ComputeScore(Map map, Robot[] robots )
    {
        HashSet<RobotState> visitedStates = InitializeVisitedStates(map);
        int score = 0;

        foreach(var robot in robots)
        {
            score += ComputeScore(map, robot, visitedStates);
        }
        return score;
    }

    private static HashSet<RobotState> InitializeVisitedStates(Map map)
    {
        var voidCells = map.VoidCells;
        HashSet<RobotState> visitedStates = new HashSet<RobotState>(Map.Height * Map.Width * 4);

        foreach (var voidCell in voidCells)
        {
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.DownArrow));
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.UpArrow));
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.LeftArrow));
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.RightArrow));
        }
        return visitedStates;
    }

    private static int ComputeScore(Map map, Robot robot, HashSet<RobotState> initializedVisitedStates)
    {
        ChangeDirectionIfLocatedOnAnArrow(map, robot);

        bool robotIsAlive = true;
        int score = 0;
        HashSet<RobotState> visitedStates = initializedVisitedStates.ToHashSet();
        
        visitedStates.Add(robot.DumpState());

        while (robotIsAlive)
        {
            //Score is incremented by the number of robots in function.
            score++;

            //Automaton2000 robots move by 1 cell in the direction they're facing.
            robot.Move();

            //Automaton2000 robots change their direction if they're located on an arrow.
            ChangeDirectionIfLocatedOnAnArrow(map, robot);

            //Automaton2000 robots stop functioning if they're located on a void cell or if they've entered a state(position, direction) they've been in before. (Automaton2000 robots don't share their state history)
            var robotState = robot.DumpState();
            if (visitedStates.Contains(robotState))
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

    private static void ChangeDirectionIfLocatedOnAnArrow(Map map, Robot robot)
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
    private HashSet<Arrow> _arrowsToPlace = new HashSet<Arrow>();

    public Arrow[] Arrows => _arrowsToPlace.ToArray();
    
    public void Add(Arrow arrow)
    {
        this._arrowsToPlace.Add(arrow);
    }

    public override string ToString()
    {
        return string.Join(" ", this._arrowsToPlace.Select(arrow => arrow.ToString()).ToArray());
    }

    internal void Combine(Solution otherSolution)
    {
        foreach(var arrow in otherSolution._arrowsToPlace)
        {
            _arrowsToPlace.Add(arrow);
        }
    }
}

static class SolutionFinder
{
    
    public static Solution FindBestSolution(Map map, Robot[] robots)
    {
        var platformCells = map.PlatformCells.ToArray();

        var solutionClockWise = GetSolution(map, platformCells, clockWise: true);
        var newMapClockWise = map.Apply(solutionClockWise.Arrows);
        int scoreClockWise = ScoreCalculator.ComputeScore(newMapClockWise, robots);
        Player.Debug($"Expected score clockwise {scoreClockWise.ToString()}");

        var solutionAntiClockWise = GetSolution(map, platformCells, clockWise: false);
        var newMapAntiClockWise = map.Apply(solutionAntiClockWise.Arrows);
        int scoreAntiClockWise = ScoreCalculator.ComputeScore(newMapAntiClockWise, robots);
        Player.Debug($"Expected score anti clock wise {scoreAntiClockWise.ToString()}");

        return scoreAntiClockWise < scoreClockWise ? solutionClockWise : solutionAntiClockWise;
    }

    private static Solution GetSolution(Map map, Position[] platformCells, bool clockWise)
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
            if(clockWise)
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


static class SolutionFinder2
{
    public static Solution FindBestSolution(Map map, Robot[] robots)
    {
        HashSet<RobotState> visitedStates = InitializeVisitedStates(map);
        var overallSolution = new Solution();

        foreach (var robot in robots)
        {
            var bestSolutionForRobot = FindBestSolutionForRobot(map, robot, visitedStates);

            overallSolution.Combine(bestSolutionForRobot);
        }

        return overallSolution;

    }

    private static HashSet<RobotState> InitializeVisitedStates(Map map)
    {
        HashSet<RobotState> visitedStates = new HashSet<RobotState>(Map.Height * Map.Width * 4);

        var voidCells = map.VoidCells;
        foreach (var voidCell in voidCells)
        {
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.DownArrow));
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.UpArrow));
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.LeftArrow));
            visitedStates.Add(new RobotState(voidCell.x, voidCell.y, Map.RightArrow));
        }

        return visitedStates;
    }


    public static Solution FindBestSolutionForRobot(Map map, Robot robot, HashSet<RobotState> initialisedVisitedStates)
    {
        var clockWiseDirections = "URDL"; //clock wise
        
        Solution solution = new Solution();

        HashSet<RobotState> visitedStates = initialisedVisitedStates.ToHashSet();

        bool isAlive = true;

        while (isAlive)
        {
            var nextRobotState = robot.GetNextState();
            
            if (visitedStates.Contains(nextRobotState))
            {
                isAlive = false;

                //Is there other alternatives
                foreach (var tryDirection in clockWiseDirections)
                {
                    if (robot.direction != tryDirection)
                    {
                        robot.direction = tryDirection;
                        var tryNextState = robot.GetNextState();

                        if(visitedStates.Contains(tryNextState) == false)
                        {
                            solution.Add(new Arrow(robot.x, robot.y, tryDirection));

                            robot.Move();
                            visitedStates.Add(robot.DumpState());

                            isAlive = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                robot.Move();
                visitedStates.Add(robot.DumpState());
            }
           
        }
        return solution;
    }

    private static void ChangeDirectionIfLocatedOnAnArrow(Map map, Robot robot)
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
        
        var bestSolution = SolutionFinder2.FindBestSolution(map, robots);
        
        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");
        var output = bestSolution.ToString();
        Player.Debug(output);

        Console.WriteLine(output);
    }
}