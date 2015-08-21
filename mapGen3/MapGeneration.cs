/***********************************************************
* Primary author:           Anthony Nguyen
* Secondary author(s):      Luke Thompson
* Date Created:           	2/3/15
* Last Modification Date: 	5/1/15
* Filename:               	MapGeneration.cs
*
* Overview:
* 	This program will randomly generate a tower defense map that
 * 	has a walkable path, tower plots, and decoration
*
* Input:
* 	Desired number of towers, path length, resources, difficulty, split path
*
* Output:
* 	A tower defense map with the appropiate length, tower plots,
 * 	and resources
************************************************************/

// Maps:
//  Small:  10
//      Path Length:
//          Small:   10-19
//          Medium:  20-24
//          Large:   25-35
//  Medium: 15
//      Path Length:
//          Small:   20-34
//          Medium:  35-49
//          Large:   50-70
//  Large:  20
//   Path Length:
//          Small:   30-49
//          Medium:  50-74
//          Large:   75-100

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Assets.Code.MapGeneration
{
    /// <summary>
    /// Tile struct that will be the data type for the grid
    /// </summary>
    public struct Tile
    {
        // tile is given direction when placed
        public Direction dir;
        // grid's tile
        public TileType tile;
        // used in the recursive search
        public bool searched;
        // check if tile placed is split tile
        public bool split;
    }

    /// <summary>
    /// ***Tower is always last in enum***                   if changed, adjust array size in Display() method
    /// 
    /// Different tile types to be placed on grid
    /// </summary>
    public enum TileType
    {
        EndPointUD, EndPointLR, StartPointUD, StartPointLR, HorizPath, VertPath,
        ElbowLeftUp, ElbowRightUp, ElbowLeftDown, ElbowRightDown,
        SplitUDL, SplitUDR, SplitULR, SplitDLR, Split4Ways,
        EmptySpace, CurPosition, Decor, Decor2, Tower
    };

    /// <summary>
    /// Available directions 
    /// </summary>
    public enum Direction
    {
        Up, Down, Left, Right, None
    };

    public enum MapSize
    {
        Small = 10, Medium = 15, Large = 20
    };

    public enum MapLength
    {
        Short, Medium, Long
    };

    class MapGeneration
    {
        #region Fields
        // CONSTANTS ==============================
        public static int GRID_WIDTH
        {
            get { return (int)MapSize.Large; }
        }
        public static int GRID_HEIGHT
        {
            get { return (int)MapSize.Large; }
        }
        const float TOO_MANY_TILES = .87f;
        public static int NUM_OF_TOWERS = 0;
        public static int NUM_OF_DECORS = 0;
        const int NUM_OF_DIRECTIONS = 4;
        private static float _decorPercent = 1.0f;
        private static float _towerPercent = 1.0f;

        /// <summary>
        /// Coordinate points for grid
        /// </summary>
        public struct Pair
        {
            public int x;
            public int y;

            // c'tor
            public Pair(int pt1, int pt2)
            {
                x = pt1;
                y = pt2;
            }
        }

        // seed for random number generator
        private static int _generationSeed = (int)DateTime.Now.Ticks;
        // random number generator
        private static System.Random rand = new System.Random(_generationSeed); 

        // direction for map generation
        private static Direction _dir;

        // position for map generation
        public static Pair _start;
        public static Pair _end;
        private static Pair _position;
        private static Pair _positionSplit;

        // array that contains map
        private static Tile[,] grid = new Tile[GRID_WIDTH, GRID_HEIGHT];
        private static TileType[,] tileMap = new TileType[GRID_WIDTH, GRID_HEIGHT];

        // bool to check for adjacency
        private static bool _isTurnTile = false;

        // int array for counting distance till path tile
        private static int[] _emptyCount = new int[NUM_OF_DIRECTIONS];

        #endregion

        #region Properties
        public static float DecorPercent
        {
            get { return _decorPercent; }
            set { _decorPercent = value; }
        }

        public static float TowerPercent
        {
            get { return _towerPercent; }
            set { _towerPercent = value; }
        }

        public static Pair Start
        {
            get { return _start; }
            set { _start = value; }
        }

        public static Pair End
        {
            get { return _end; }
            set { _end = value; }
        }

        public static int GenerationSeed
        {
            get { return _generationSeed; }
            set { _generationSeed = value; }
        }
        #endregion

        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            for (int i = 0; i < 1; i++)
            {
                GenerateMap(MapSize.Large, MapLength.Medium, false, 0.2f, 0.3f);

                // Display the map
                Console.WriteLine();
                Display();
                Console.WriteLine("Iteration: {0}", (i + 1));
                Console.WriteLine("Path Tile Count: {0}", PathTileCount());
                Console.WriteLine("Split-Tower-Decor Count: {0}-{1}-{2}", SplitTileCount(), TowerCount(), DecorCount());
                Console.WriteLine();
            }

            timer.Stop();
            System.TimeSpan time = timer.Elapsed;

            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    time.Hours, time.Minutes, time.Seconds, time.Milliseconds / 10);

            Console.WriteLine("Run Time " + elapsedTime);
        }

        /// <summary>
        /// Get the original generated map 
        /// </summary>
        /// <returns>Original generated map as TileType</returns>
        public static TileType[,] GetGeneratedMap()
        {
            TileType[,] generatedMap = new TileType[GRID_WIDTH, GRID_HEIGHT];

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    generatedMap[x, y] = grid[x, y].tile;
                }
            }

            return generatedMap;
        }

        /// <summary>
        /// Get the tile at a specific coordinate on map of TileTypes
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <returns></returns>
        public static TileType GetTile(int x, int y)
        {
            return tileMap[x, y];
        }
        /// <summary>
        /// Set the tile at a specific coordinate on map of TileTypes
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="tile"></param>
        public static void SetTile(int x, int y, TileType tile)
        {
            tileMap[x, y] = tile;
        }

        /// <summary>
        /// Get tiles from generated map and return TileType variable. 
        /// The returned map is modifiable
        /// </summary>
        /// <returns></returns>
        public static TileType[,] GetMapAsTileType()
        {
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    tileMap[x, y] = grid[x, y].tile;
                }
            }

            return tileMap;
        }

        /// <summary>
        /// Randomly generate a map with the specified parameters
        /// </summary>
        /// <returns>A 2D array of type TileType</returns>
        //public static TileType[,] GenerateMap(MapLength length, bool splitPath, float towerPercent, float decorPercent)
        public static TileType[,] GenerateMap()
        {
            TowerPercent = .81f;
            DecorPercent = .0f;
            //TowerPercent = towerPercent;
            //DecorPercent = decorPercent;


            // Generate the map
            do
            {
                InitializeGrid();
            } while (!RandomMapGenerator(75, 100, 20) || !GenerateSplitPath());

            // If not enough towers, place more
            NUM_OF_TOWERS = CalculateTowerAmount(TowerPercent);
            if (_towerPercent > 0.7f)
                PlaceTowerSequentially();
            else
                while (TowerCount() < NUM_OF_TOWERS)
                    SingleTower();

            Console.WriteLine("{0}/{1}", TowerCount(), NUM_OF_TOWERS);

            // Place decorative objects
            if (DecorPercent > 0.8f)
                PlaceDecorSequentially();
            else
                PlaceDecor2(CalculateDecorAmount(DecorPercent));

            return GetMapAsTileType();
        }

        /// <summary>
        /// Generates a map on a 2D array of TileType.
        /// The map varies in size and length and may include a mix of any of the following:
        /// split pathing, tower plots, and decoration.
        /// </summary>
        /// <param name="size">Size of the map</param>
        /// <param name="length">Length of the path</param>
        /// <param name="splitPath">Whether to add split pathing or not</param>
        /// <param name="towerPercent">Percent of empty spaces adjacent to path tiles for tower plots</param>
        /// <param name="decorPercent">Percent of empty space in map for decoration tiles</param>
        /// <returns>A 2D array of TileType</returns>
        public static TileType[,] GenerateMap(MapSize size, MapLength length, bool splitPath, float towerPercent, float decorPercent)
        {
            StartUp();
            TowerPercent = towerPercent;
            DecorPercent = decorPercent;
            bool goodMap = false;
            bool goodSplitPath = true;
            int startEndDistance = 0;
            int numOfTowers = 0;

            // Generate the map
            do
            {
                // initialize grid
                InitializeGrid();
                // create path according to desired length
                goodMap = MakePath(size, length);
                // check the start and end point distance
                startEndDistance = StartEndDistance();
                // if split pathing is desired
                if (splitPath)
                    goodSplitPath = GenerateSplitPath();
            } while (!goodMap || !goodSplitPath || startEndDistance < (int)size / 2);

            // calculate number of desired towers
            numOfTowers = CalculateTowerAmount(TowerPercent);
            // check if user wants more than 90% of empty space
            if (TowerPercent > TOO_MANY_TILES) PlaceTowerSequentially();
            // else random placement
            else
                while (TowerCount() < numOfTowers) SingleTower();

            // Place decorative objects
            // check if user wants more than 90% of empty space
            if (DecorPercent > TOO_MANY_TILES) PlaceDecorSequentially();
            else PlaceDecor2(CalculateDecorAmount(DecorPercent));

            return GetMapAsTileType();
        }

        #region Tile Path Methods
        private static void StartUp()
        {
            // array that contains map
            grid = new Tile[GRID_WIDTH, GRID_HEIGHT];
            tileMap = new TileType[GRID_WIDTH, GRID_HEIGHT];

            rand = new System.Random();
            // flag for turn tiles
            _isTurnTile = false;

            // int array for counting distance till path tile
            _emptyCount = new int[NUM_OF_DIRECTIONS];

            // percent of empty space to be used
            TowerPercent = 0.2f;
            DecorPercent = 0.4f;
        }

        /// <summary>
        /// Calls the method to generate tile path.
        /// Parameters for map generation are dependant upon map size and path length desired
        /// </summary>
        /// <param name="size">Desired map size</param>
        /// <param name="length">Desired path length</param>
        /// <returns>true or false, whether a good map was generated</returns>
        private static bool MakePath(MapSize size, MapLength length)
        {
            bool map = false;

            // check for map size
            switch (size)
            {
                // small
                case MapSize.Small:
                    // check length
                    switch (length)
                    {
                        // short
                        case MapLength.Short:
                            map = RandomMapGenerator(10, 20, 20);
                            break;
                        // medium
                        case MapLength.Medium:
                            map = RandomMapGenerator(20, 35, 20);
                            break;
                        // long
                        case MapLength.Long:
                            map = RandomMapGenerator(30, 50, 20);
                            break;
                        default:
                            break;
                    }
                    break;
                // medium
                case MapSize.Medium:
                    switch (length)
                    {
                        case MapLength.Short:
                            map = RandomMapGenerator(20, 24, 20);
                            break;
                        case MapLength.Medium:
                            map = RandomMapGenerator(35, 50, 20);
                            break;
                        case MapLength.Long:
                            map = RandomMapGenerator(50, 75, 20);
                            break;
                        default:
                            break;
                    }
                    break;
                // large
                case MapSize.Large:
                    switch (length)
                    {
                        case MapLength.Short:
                            map = RandomMapGenerator(25, 35, 20);
                            break;
                        case MapLength.Medium:
                            map = RandomMapGenerator(50, 70, 20);
                            break;
                        case MapLength.Long:
                            map = RandomMapGenerator(75, 100, 20);
                            break;
                        default:
                            break;
                    }
                    break;
            }
            return map;
        }
        private static int PathTileCount()
        {
            int count = 0;

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (grid[x, y].tile < TileType.Split4Ways)
                        count++;
                }
            }

            return count;
        }
        /// <summary>
        /// Find start and end points on grid
        /// </summary>
        /// <returns></returns>
        private static string StartEndFinder()
        {
            string strFound = "Finding... ";
            bool[] finding = new bool[2];

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (grid[x, y].tile == TileType.EndPointUD || grid[x, y].tile == TileType.EndPointLR)
                    {
                        strFound += "EndPoint Found!\t";
                        finding[0] = true;
                    }
                    if (grid[x, y].tile == TileType.StartPointUD || grid[x, y].tile == TileType.StartPointLR)
                    {
                        strFound += "StartPoint Found!\t";
                        finding[1] = true;
                    }
                }
            }

            if (!finding[0])
                throw new Exception("EndPoint not found!");
            if (!finding[1])
                throw new Exception("StartPoint not found!");

            return strFound;
        }

        /// <summary>
        /// Map generation algorithm
        /// </summary>
        /// <param name="min">Minimum path length</param>
        /// <param name="max">Maximum path length</param>
        /// <param name="tower_freq">Number of tiles per tower</param>
        /// <returns></returns>
        private static bool RandomMapGenerator(int min, int max, int tower_freq)
        {
            Pair start;             // start position
            int path_length;        // current path length
            int failures = 0;       // failed iterations
            bool generating = true; // flag for map generation
            bool put_tower = false; // flag for tower placement

            // start point on the UD of map
            if (rand.Next(0, 2) == 1)
            {
                start.x = rand.Next(0, GRID_HEIGHT);
                start.y = 0;
                // set start tile
                grid[start.y, start.x].tile = TileType.StartPointUD;
                grid[start.y, start.x].dir = Direction.Down;
                start.y += 1;
                // set next tile
                grid[start.y, start.x].tile = TileType.VertPath;
                // set direction
                _dir = Direction.Down;
            }
            // start point on the LR side of map
            else
            {
                start.x = 0;
                start.y = rand.Next(0, GRID_HEIGHT);
                grid[start.y, start.x].tile = TileType.StartPointLR;
                grid[start.y, start.x].dir = Direction.Right;
                start.x += 1;
                grid[start.y, start.x].tile = TileType.HorizPath;
                _dir = Direction.Right;
            }

            // set current position to start point
            _position = start;
            path_length = 1;

            // for navigations
            _start = _position;

            // generation loop
            while (generating)
            {
                if (PlaceTile()) // If a tile is successfully placed
                {
                    ++path_length;
                    if (path_length % tower_freq == 0) // Check if a tower needs placing
                        put_tower = true;
                    failures = 0; // reset failure count
                }
                else
                    failures++; // increment failure count

                if (failures > 30) // presumed catastrophic failure
                    return false;

                if (TowerCount() < NUM_OF_TOWERS && put_tower && PlaceTower()) // Attempt to place a tower if the put_tower flag is set
                    put_tower = false;         // Disable the flag if the placement is successful


                // Attempt to end the path
                // side of grid
                if (path_length > min)
                {
                    // end point is on top/bottom
                    if (_position.y == 0 || _position.y == GRID_HEIGHT - 1)
                    {
                        // stops map generation
                        generating = false;
                        // place end tile
                        grid[_position.y, _position.x].tile = TileType.EndPointUD;
                        // for navigation
                        End = _position;
                    }
                    // end point is on left/right
                    else if (_position.x == 0 || _position.x == GRID_WIDTH - 1)
                    {
                        generating = false;
                        grid[_position.y, _position.x].tile = TileType.EndPointLR;
                        End = _position;
                    }
                }

            }
            // If map is within bounds and no tile adjacency, return true
            if (path_length < max && !CheckTileAdjacency()) return true;
            return false;
        }

        /// <summary>
        /// Find the distance between the start and end point
        /// </summary>
        /// <returns></returns>
        private static int StartEndDistance()
        {
            return Math.Abs(Start.x - End.x) + Math.Abs(Start.y - End.y);
        }

        /// <summary>
        /// Outer switch statement that controls where to go according to '_dir' variable
        /// </summary>
        private static bool PlaceTile()
        {
            // set all of the tile search flags to false
            UnsearchGrid();
            // grid contains Direction datatype, so after a tile is placed and
            // variable '_dir' is adjusted, change direction for grid.
            grid[_position.y, _position.x].dir = _dir;
            grid[_position.y, _position.x].tile = TileType.CurPosition;

            // determine current direction of travel
            switch (_dir)
            {
                case Direction.Up:
                    return GoUp();
                case Direction.Down:
                    return GoDown();
                case Direction.Left:
                    return GoLeft();
                case Direction.Right:
                    return GoRight();
                default: return false;
            }
        }

        /// <summary>
        /// Changes values inside grid for path going up and considers every possible direction
        /// </summary>
        private static bool GoUp()
        {
            int rNum = rand.Next(0, 3);
            if (_isTurnTile) rNum = 0;

            // Randomly choose next possible direction
            switch (rNum)
            {
                // move up
                case 0:
                    // check if in bounds and tile is empty
                    if (_position.y > 0 && IsSafe(_position.y - 1, _position.x))
                    {
                        // place horizontal tile
                        grid[_position.y, _position.x].tile = TileType.VertPath;
                        // set turn tile flag, done before position is adjusted
                        _isTurnTile = IsTurnTile();
                        // adjust position after placing tile
                        _position.y -= 1;
                        // change direction to move up
                        _dir = Direction.Up;
                        return true;
                    }
                    break;
                // move left
                case 1:
                    // check if in bounds and tile is empty
                    if (_position.x > 0 && IsSafe(_position.y, _position.x - 1))
                    {
                        // place elbow right down tile
                        grid[_position.y, _position.x].tile = TileType.ElbowLeftDown;
                        _isTurnTile = IsTurnTile();
                        // adjust position after placing tile
                        _position.x -= 1;
                        // change direction to move left
                        _dir = Direction.Left;
                        return true;
                    }
                    break;
                // move right
                case 2:
                    // check if in bounds and tile is empty
                    if (_position.x < GRID_WIDTH - 1 && IsSafe(_position.y, _position.x + 1))
                    {
                        // place elbow left down tile
                        grid[_position.y, _position.x].tile = TileType.ElbowRightDown;
                        _isTurnTile = IsTurnTile();
                        // adjust position after placing tile
                        _position.x += 1;
                        // change direction to move right
                        _dir = Direction.Right;
                        return true;
                    }
                    break;
            }
            return false;
        }
        /// <summary>
        /// Changes values inside grid for path going down and considers every possible direction
        /// </summary>
        private static bool GoDown()
        {
            int rNum = rand.Next(0, 3);
            if (_isTurnTile) rNum = 0;

            // Randomly choose next possible direction
            switch (rNum)
            {
                // down
                case 0:
                    if (_position.y < GRID_HEIGHT - 1 && IsSafe(_position.y + 1, _position.x))
                    {
                        grid[_position.y, _position.x].tile = TileType.VertPath;
                        _isTurnTile = IsTurnTile();
                        _position.y += 1;
                        _dir = Direction.Down;
                        return true;
                    }
                    break;
                // left
                case 1:
                    if (_position.x > 0 && IsSafe(_position.y, _position.x - 1))
                    {
                        grid[_position.y, _position.x].tile = TileType.ElbowLeftUp;
                        _isTurnTile = IsTurnTile();
                        _position.x -= 1;
                        _dir = Direction.Left;
                        return true;
                    }
                    break;
                // right
                case 2:
                    if (_position.x < GRID_WIDTH - 1 && IsSafe(_position.y, _position.x + 1))
                    {
                        grid[_position.y, _position.x].tile = TileType.ElbowRightUp;
                        _isTurnTile = IsTurnTile();
                        _position.x += 1;
                        _dir = Direction.Right;
                        return true;
                    }
                    break;
            }
            return false;
        }
        /// <summary>
        /// Changes values inside grid for path going left and considers every possible direction
        /// </summary>
        private static bool GoLeft()
        {
            int rNum = rand.Next(0, 3);
            if (_isTurnTile) rNum = 2;

            // Randomly choose next possible direction
            switch (rNum)
            {
                // up
                case 0:
                    if (_position.y > 0 && IsSafe(_position.y - 1, _position.x))
                    {
                        grid[_position.y, _position.x].tile = TileType.ElbowRightUp;
                        _isTurnTile = IsTurnTile();
                        _position.y -= 1;
                        _dir = Direction.Up;
                        return true;
                    }
                    break;
                // down
                case 1:
                    if (_position.y < GRID_HEIGHT - 1 && IsSafe(_position.y + 1, _position.x))
                    {
                        grid[_position.y, _position.x].tile = TileType.ElbowRightDown;
                        _isTurnTile = IsTurnTile();
                        _position.y += 1;
                        _dir = Direction.Down;
                        return true;
                    }
                    break;
                // left
                case 2:
                    if (_position.x > 0 && IsSafe(_position.y, _position.x - 1))
                    {
                        grid[_position.y, _position.x].tile = TileType.HorizPath;
                        _isTurnTile = IsTurnTile();
                        _position.x -= 1;
                        _dir = Direction.Left;
                        return true;
                    }
                    break;
            }
            return false;
        }
        /// <summary>
        /// Changes values inside grid for path going right and considers every possible direction
        /// </summary>
        private static bool GoRight()
        {
            int rNum = rand.Next(0, 3);
            if (_isTurnTile) rNum = 2;

            // Randomly choose next possible direction
            switch (rNum)
            {
                // up
                case 0:
                    if (_position.y > 0 && IsSafe(_position.y - 1, _position.x))
                    {
                        grid[_position.y, _position.x].tile = TileType.ElbowLeftUp;
                        _isTurnTile = IsTurnTile();
                        _position.y -= 1;
                        _dir = Direction.Up;
                        return true;
                    }
                    break;
                // down
                case 1:
                    if (_position.y < GRID_HEIGHT - 1 && IsSafe(_position.y + 1, _position.x))
                    {
                        grid[_position.y, _position.x].tile = TileType.ElbowLeftDown;
                        _isTurnTile = IsTurnTile();
                        _position.y += 1;
                        _dir = Direction.Down;
                        return true;
                    }
                    break;
                // right
                case 2:
                    if (_position.x < GRID_WIDTH - 1 && IsSafe(_position.y, _position.x + 1))
                    {
                        grid[_position.y, _position.x].tile = TileType.HorizPath;
                        _isTurnTile = IsTurnTile();
                        _position.x += 1;
                        _dir = Direction.Right;
                        return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// Checks to see if there is a path from a given square to the edge of the map
        /// </summary>
        /// <param name="y"> y position value </param>
        /// <param name="x"> x position value </param>
        /// <returns> true if a path exists
        ///           false otherwise       </returns>
        private static bool IsSafe(int y, int x)
        {
            //Check if the tile itself is empty
            if (grid[y, x].tile != TileType.EmptySpace)
                return false;
            //Check if the current position is an edge tile
            if (y == 0 || y == GRID_HEIGHT - 1 || x == 0 || x == GRID_WIDTH - 1)
                return true;
            //Search the current tile
            if (grid[y, x].tile == TileType.EmptySpace)
            {
                grid[y, x].searched = true;

                //Recursively search surrounding tile
                if (grid[y + 1, x].tile == TileType.EmptySpace && !grid[y + 1, x].searched)
                    if (IsSafe(y + 1, x)) return true; //A path was found
                if (grid[y, x + 1].tile == TileType.EmptySpace && !grid[y, x + 1].searched)
                    if (IsSafe(y, x + 1)) return true;
                if (grid[y - 1, x].tile == TileType.EmptySpace && !grid[y - 1, x].searched)
                    if (IsSafe(y - 1, x)) return true;
                if (grid[y, x - 1].tile == TileType.EmptySpace && !grid[y, x - 1].searched)
                    if (IsSafe(y, x - 1)) return true;
            }
            return false;
        }

        /// <summary>
        /// Check if the tile in the current position to be a turn tile.
        /// This will flag that a straight has to be placed
        /// </summary>
        /// <returns> true on turn tiles
        ///           false on all other tiles</returns>
        private static bool IsTurnTile()
        {
            TileType tile = grid[_position.y, _position.x].tile;

            // all turn tiles return true
            switch (tile)
            {
                case TileType.SplitUDL:
                case TileType.SplitUDR:
                case TileType.SplitULR:
                case TileType.SplitDLR:
                case TileType.ElbowLeftUp:
                case TileType.ElbowRightUp:
                case TileType.ElbowLeftDown:
                case TileType.ElbowRightDown:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Traverse the grid and check if tiles have adjacent tiles
        /// </summary>
        /// <returns></returns>
        private static bool CheckTileAdjacency()
        {
            for (int i = 0; i < GRID_HEIGHT; i++)
            {
                for (int j = 0; j < GRID_WIDTH; j++)
                {
                    if (grid[i, j].tile <= TileType.Split4Ways)
                        if (HasAdjacentTile(i, j))
                            return true;
                }
            }
            return false;
        }

        /// <summary>
        /// A simpler version of TileCheck() method.
        /// If a non-connecting tile is next to current tile then invalid tile is placed
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param> 
        /// <returns></returns>
        private static bool HasAdjacentTile(int x, int y)
        {
            // checks for non-connective tiles to the top
            if (x > 0)
                if (grid[x - 1, y].tile == TileType.HorizPath || grid[x - 1, y].tile == TileType.ElbowLeftUp || grid[x - 1, y].tile == TileType.ElbowRightUp ||
                    grid[x - 1, y].tile == TileType.SplitULR)
                    return true;
            // checks for non-connective tiles to the bottom
            if (x < GRID_HEIGHT - 1)
                if (grid[x + 1, y].tile == TileType.HorizPath || grid[x + 1, y].tile == TileType.ElbowLeftDown || grid[x + 1, y].tile == TileType.ElbowRightDown ||
                    grid[x + 1, y].tile == TileType.SplitDLR)
                    return true;
            // checks for non-connective tiles to the left
            if (y > 0)
                if (grid[x, y - 1].tile == TileType.VertPath || grid[x, y - 1].tile == TileType.ElbowLeftDown || grid[x, y - 1].tile == TileType.ElbowLeftUp ||
                    grid[x, y - 1].tile == TileType.SplitUDL)
                    return true;
            // checks for non-connective tiles to the right
            if (y < GRID_WIDTH - 1)
                if (grid[x, y + 1].tile == TileType.VertPath || grid[x, y + 1].tile == TileType.ElbowRightDown || grid[x, y + 1].tile == TileType.ElbowRightUp ||
                    grid[x, y + 1].tile == TileType.SplitUDR)
                    return true;
            //// checks for path tiles to the top left
            //if (x > 0 && y > 0)
            //    if (grid[x - 1, y - 1].tile <= TileType.Split4Ways)
            //        return true;
            //// checks for path tiles to the top right
            //if (x > 0 && y < GRID_WIDTH - 1)
            //    if (grid[x - 1, y + 1].tile <= TileType.Split4Ways)
            //        return true;
            //// checks for path tiles to the bottom left
            //if (x < GRID_HEIGHT - 1 && y > 0)
            //    if (grid[x + 1, y - 1].tile <= TileType.Split4Ways)
            //        return true;
            //// checks for path tiles to the bottom right
            //if (x < GRID_HEIGHT - 1 && y < GRID_WIDTH - 1)
            //    if (grid[x + 1, y + 1].tile <= TileType.Split4Ways)
            //        return true;

            // everything else
            return false;
        }

        /// <summary>
        /// Doesn't seem very necessary, but used for testing
        /// </summary>
        private static void InitializeGrid()
        {
            for (int i = 0; i < GRID_WIDTH; ++i)
                for (int j = 0; j < GRID_HEIGHT; ++j)
                {
                    grid[i, j].tile = TileType.EmptySpace;
                    grid[i, j].searched = false;
                    grid[i, j].split = false;
                }
        }

        private static void UnsearchGrid()
        {
            for (int i = 0; i < GRID_WIDTH; ++i)
                for (int j = 0; j < GRID_HEIGHT; ++j)
                    grid[i, j].searched = false;
        }

        /// <summary>
        /// Takes in an array of integers and display a map with corresponding path tiles.
        /// </summary>
        public static void Display()
        {
            // size 'Tower + 1' because Tower will always be last enum
            char[] tileset = new char[(int)TileType.Tower + 1];

            /*
            U = Up, D = Down, L = Left, R = Right
             
            End point UD     - 0
            End point LR     - 1
            Start point UD   - 2
            Start point LR   - 3
            horizontal path  - 4
            vertical path    - 5
            elbow left/up    - 6
            elbow right/up   - 7
            elbow left/down  - 8
            elbow right/down - 9
            UDL split        - 10
            UDR split        - 11
            ULR split        - 12
            DLR split        - 13
            4 way split      - 14
            empty space      - 15
            decoration       - 16
            decoration2      - 17
            tower            - 18
            */

            tileset[(int)TileType.StartPointUD] = 'A';
            tileset[(int)TileType.StartPointLR] = 'B';
            tileset[(int)TileType.EndPointUD] = 'Y';
            tileset[(int)TileType.EndPointLR] = 'Z';
            tileset[(int)TileType.HorizPath] = '─';
            tileset[(int)TileType.VertPath] = '│';
            tileset[(int)TileType.ElbowLeftUp] = '┘';
            tileset[(int)TileType.ElbowRightUp] = '└';
            tileset[(int)TileType.ElbowLeftDown] = '┐';
            tileset[(int)TileType.ElbowRightDown] = '┌';
            tileset[(int)TileType.SplitUDL] = '┤';
            tileset[(int)TileType.SplitUDR] = '├';
            tileset[(int)TileType.SplitULR] = '┴';
            tileset[(int)TileType.SplitDLR] = '┬';
            tileset[(int)TileType.Split4Ways] = '┼';
            tileset[(int)TileType.EmptySpace] = ' ';
            tileset[(int)TileType.CurPosition] = 'C';
            tileset[(int)TileType.Decor] = 'D';
            tileset[(int)TileType.Decor2] = '2';
            tileset[(int)TileType.Tower] = 'T';

            for (int x = 0; x < GRID_WIDTH; ++x)
            {
                for (int y = 0; y < GRID_HEIGHT; ++y)
                {
                    Console.Write("{0}", tileset[(int)grid[x, y].tile]);
                }
                Console.WriteLine();
            }
        }
        #endregion

        #region Split Path Methods
        /// <summary>
        /// Create a random split path on the randomly generated map
        /// </summary>
        private static bool GenerateSplitPath()
        {
            // create two points
            Pair Start = new Pair(rand.Next(1, GRID_WIDTH - 1), rand.Next(1, GRID_HEIGHT - 1));
            Pair End = new Pair(rand.Next(1, GRID_WIDTH - 1), rand.Next(1, GRID_HEIGHT - 1));
            int failed = 0;

            // outer while loop to check if split path is placed
            while (SplitTileCount() == 0)
            {
                Start = new Pair(rand.Next(1, GRID_WIDTH - 1), rand.Next(1, GRID_HEIGHT - 1));
                End = new Pair(rand.Next(1, GRID_WIDTH - 1), rand.Next(1, GRID_HEIGHT - 1));
                failed = 0;

                // find a path tile that is not on the border
                while (grid[Start.y, Start.x].tile > TileType.Split4Ways)
                {
                    // x-coordinate may be anywhere left or right
                    Start.x = rand.Next(1, GRID_WIDTH - 1);
                    // y-coordinate may be anywhere up or down
                    Start.y = rand.Next(1, GRID_WIDTH - 1);

                    failed++;
                    if (failed > 100)
                        return false;
                    //throw new Exception("TOO MUCH!");
                }
                // find a path tile that is not on the border
                while (grid[End.y, End.x].tile > TileType.Split4Ways || (Start.y == End.y && Start.x == End.x))
                {
                    End.x = rand.Next(1, GRID_WIDTH - 1);
                    End.y = rand.Next(1, GRID_WIDTH - 1);

                    failed++;
                    if (failed > 100)
                        return false;
                    //throw new Exception("TOO MUCH!");
                }

                //grid[Start.y, Start.x].tile = TileType.Split4Ways;
                //grid[End.y, End.x].tile = TileType.Split4Ways;
                // set split attribute to true for start/end points
                grid[Start.y, Start.x].split = true;
                grid[End.y, End.x].split = true;

                // set position to start
                _positionSplit = Start;

                // create path between start/end points
                while (_positionSplit.y != End.y || _positionSplit.x != End.x)
                {
                    int rNum = rand.Next(0, NUM_OF_DIRECTIONS);
                    // steps are always toward end point
                    // randomly choose next step
                    switch (rNum)
                    {
                        // move up
                        case (int)Direction.Up:
                            // check the difference between next step position and end point
                            if (_positionSplit.y > 0 && Math.Abs(_positionSplit.y - 1 - End.y) < Math.Abs(_positionSplit.y - End.y))
                                _positionSplit.y -= 1;
                            break;
                        // move down
                        case (int)Direction.Down:
                            if (_positionSplit.y < GRID_HEIGHT - 1 && Math.Abs(_positionSplit.y + 1 - End.y) < Math.Abs(_positionSplit.y - End.y))
                                _positionSplit.y += 1;
                            break;
                        // move left
                        case (int)Direction.Left:
                            if (_positionSplit.x > 0 && Math.Abs(_positionSplit.x - 1 - End.x) < Math.Abs(_positionSplit.x - End.x))
                                _positionSplit.x -= 1;
                            break;
                        // move right
                        case (int)Direction.Right:
                            if (_positionSplit.x < GRID_WIDTH - 1 && Math.Abs(_positionSplit.x + 1 - End.x) < Math.Abs(_positionSplit.x - End.x))
                                _positionSplit.x += 1;
                            break;
                    }
                    // set temporary tile
                    //grid[_positionSplit.y, _positionSplit.x].tile = TileType.Split4Ways;

                    // set split attribute to true
                    grid[_positionSplit.y, _positionSplit.x].split = true;
                }
                // reconfigure tiles
                PlaceSplitTile();
            }
            return true;
        }

        /// <summary>
        /// Iterates through grid and check if split attribute 
        /// is set to true to reconfigure tile to split tile
        /// </summary>
        private static void PlaceSplitTile()
        {
            for (int x = 0; x < GRID_HEIGHT; x++)
            {
                for (int y = 0; y < GRID_WIDTH; y++)
                {
                    if (grid[x, y].split == true && grid[x, y].tile > TileType.StartPointLR)
                        grid[x, y].tile = ConfigureTile(x, y, TileCheck(x, y));
                }
            }

            // make a 2nd run through because TileCheck modifies split 
            for (int x = 0; x < GRID_HEIGHT; x++)
            {
                for (int y = 0; y < GRID_WIDTH; y++)
                {
                    if (grid[x, y].split == true && grid[x, y].tile > TileType.StartPointLR)
                        grid[x, y].tile = ConfigureTile(x, y, TileCheck(x, y));
                }
            }
        }

        /// <summary>
        /// Checks each side of the current tile for non-connective tiles
        /// and if so, set bool to false.
        /// Reverse check is done because less comparisons are needed
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>bool array where true mean tile is clear of non-connective tiles
        /// and false means there are</returns>
        private static bool[] TileCheck(int x, int y)
        {
            // create an array of bools for each side of the current tile
            bool[] tileFlags = new bool[NUM_OF_DIRECTIONS];

            // initialize each bool to true
            //for (int i = 0; i < NUM_OF_DIRECTIONS; i++)
            //tileFlags[i] = true;


            // checks for tiles above
            if (x > 0)
                if (grid[x - 1, y].tile <= TileType.Split4Ways || grid[x - 1, y].split)
                {
                    grid[x - 1, y].split = true;
                    tileFlags[0] = true;
                }
            // checks for tiles below
            if (x < GRID_HEIGHT - 1)
                if (grid[x + 1, y].tile <= TileType.Split4Ways || grid[x + 1, y].split)
                {
                    grid[x + 1, y].split = true;
                    tileFlags[1] = true;
                }
            // checks for tiles to the left
            if (y > 0)
                if (grid[x, y - 1].tile <= TileType.Split4Ways || grid[x, y - 1].split)
                {
                    grid[x, y - 1].split = true;
                    tileFlags[2] = true;
                }
            // checks for tiles to the right
            if (y < GRID_WIDTH - 1)
                if (grid[x, y + 1].tile <= TileType.Split4Ways || grid[x, y + 1].split)
                {
                    grid[x, y + 1].split = true;
                    tileFlags[3] = true;
                }

            //// checks for non-connective tiles above
            //if (x > 0)
            //    if (grid[x - 1, y].tile == TileType.HorizPath || grid[x - 1, y].tile == TileType.ElbowLeftUp || grid[x - 1, y].tile == TileType.ElbowRightUp ||
            //        grid[x - 1, y].tile == TileType.SplitULR || grid[x - 1, y].tile > TileType.Split4Ways)
            //        tileFlags[0] = false;
            //// checks for non-connective tiles below
            //if (x < GRID_HEIGHT - 1)
            //    if (grid[x + 1, y].tile == TileType.HorizPath || grid[x + 1, y].tile == TileType.ElbowLeftDown || grid[x + 1, y].tile == TileType.ElbowRightDown ||
            //        grid[x + 1, y].tile == TileType.SplitDLR || grid[x + 1, y].tile > TileType.Split4Ways)
            //        tileFlags[1] = false;
            //// checks for non-connective tiles to the left
            //if (y > 0)
            //    if (grid[x, y - 1].tile == TileType.VertPath || grid[x, y - 1].tile == TileType.ElbowLeftDown || grid[x, y - 1].tile == TileType.ElbowLeftUp ||
            //        grid[x, y - 1].tile == TileType.SplitUDL || grid[x, y - 1].tile > TileType.Split4Ways)
            //        tileFlags[2] = false;
            //// checks for non-connective tiles to the right
            //if (y < GRID_WIDTH - 1)
            //    if (grid[x, y + 1].tile == TileType.VertPath || grid[x, y + 1].tile == TileType.ElbowRightDown || grid[x, y + 1].tile == TileType.ElbowRightUp ||
            //        grid[x, y + 1].tile == TileType.SplitUDR || grid[x, y + 1].tile > TileType.Split4Ways)
            //        tileFlags[3] = false;

            // return array of bools
            return tileFlags;
        }

        /// <summary>
        /// Takes in an array of bools and depending on which
        /// and how many bools are true, return the specific tile
        /// </summary>
        /// <param name="tileFlags">array of bools that represent each side</param>
        /// <returns>TileType</returns>
        private static TileType ConfigureTile(int x, int y, bool[] tileFlags)
        {
            // tileFlags[0] = up
            // tileFlags[1] = down
            // tileFlags[2] = left
            // tileFlags[3] = right

            // Split 4 Ways tile, not necessary because temp tile is TileType.Split4Ways
            if (tileFlags[0] && tileFlags[1] && tileFlags[2] && tileFlags[3])
                return TileType.Split4Ways;

            // Split up down left
            else if (tileFlags[0] && tileFlags[1] && tileFlags[2])
                return TileType.SplitUDL;

            // Split up down right
            else if (tileFlags[0] && tileFlags[1] && tileFlags[3])
                return TileType.SplitUDR;

            // Split up left right
            else if (tileFlags[0] && tileFlags[2] && tileFlags[3])
                return TileType.SplitULR;

            // Split down left right
            else if (tileFlags[1] && tileFlags[2] && tileFlags[3])
                return TileType.SplitDLR;

            // Elbow left up
            else if (tileFlags[0] && tileFlags[2])
                return TileType.ElbowLeftUp;

            // Elbow right up
            else if (tileFlags[0] && tileFlags[3])
                return TileType.ElbowRightUp;

            // Elbow down left
            else if (tileFlags[1] && tileFlags[2])
                return TileType.ElbowLeftDown;

            // Elbow down right
            else if (tileFlags[1] && tileFlags[3])
                return TileType.ElbowRightDown;

            // Horiz path
            else if (tileFlags[2] && tileFlags[3])
                return TileType.HorizPath;

            // Vert path
            else if (tileFlags[0] && tileFlags[1])
                return TileType.VertPath;

            // Should never get called
            else
                return grid[x, y].tile;
        }

        /// <summary>
        /// Counts the number of split tiles on grid
        /// </summary>
        /// <returns></returns>
        private static int SplitTileCount()
        {
            int count = 0;

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (grid[x, y].tile >= TileType.SplitUDL && grid[x, y].tile <= TileType.Split4Ways)
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Splits the grid into four quadrants 
        /// and then checks if a path tile exists.
        /// </summary>
        /// <param name="quandrant"></param>
        /// <returns>true if a non-start/end point exists</returns>
        private static bool QuandrantCheck(int quandrant)
        {
            // Quandrants
            //   4 | 1
            //   -----
            //   3 | 2

            switch (quandrant)
            {
                // check if path tile exists in first quadrant of grid
                case 1:
                    // lower half, start at 1 to exclude border
                    for (int y = 1; y < GRID_HEIGHT / 2; y++)
                    {
                        // upper half, subtract 1 to exclude border
                        for (int x = GRID_WIDTH / 2; x < GRID_WIDTH - 1; x++)
                        {
                            // I dont want to overwrite start/end points
                            if (grid[y, x].tile < TileType.EmptySpace && grid[y, x].tile > TileType.StartPointLR)
                                return true;
                        }
                    }
                    break;
                // check if path tile exists in second quadrant of grid
                case 2:
                    // upper half
                    for (int y = GRID_HEIGHT / 2; y < GRID_HEIGHT - 1; y++)
                    {
                        // upper half
                        for (int x = GRID_WIDTH / 2; x < GRID_WIDTH - 1; x++)
                        {
                            if (grid[y, x].tile < TileType.EmptySpace && grid[y, x].tile > TileType.StartPointLR)
                                return true;
                        }
                    }
                    break;
                // check if path tile exists in third quadrant of grid
                case 3:
                    // upper half
                    for (int y = GRID_HEIGHT / 2; y < GRID_HEIGHT - 1; y++)
                    {
                        // lower half
                        for (int x = 1; x < GRID_WIDTH / 2; x++)
                        {
                            if (grid[y, x].tile < TileType.EmptySpace && grid[y, x].tile > TileType.StartPointLR)
                                return true;
                        }
                    }
                    break;
                // check if path tile exists in fourth quadrant of grid
                case 4:
                    // lower half
                    for (int y = 1; y < GRID_HEIGHT / 2; y++)
                    {
                        // lower half
                        for (int x = 1; x < GRID_WIDTH / 2; x++)
                        {
                            if (grid[y, x].tile < TileType.EmptySpace && grid[y, x].tile > TileType.StartPointLR)
                                return true;
                        }
                    }
                    break;
            }
            return false;
        }
        #endregion

        #region Decoration Functions
        /// <summary>
        /// Does a simple calculation to determine the number
        /// of decorations to be placed
        /// </summary>
        /// <param name="decorPercent">Percentage of empty spaces to be used</param>
        /// <returns>number of decorations as an int</returns>
        private static int CalculateDecorAmount(float decorPercent)
        {
            float numOfDecors = 0;
            float numOfEmpty = 0;

            // go through the whole grid and count all the empty spaces
            for (int x = 0; x < GRID_HEIGHT; x++)
            {
                for (int y = 0; y < GRID_WIDTH; y++)
                {
                    if (grid[x, y].tile == TileType.EmptySpace)
                        numOfEmpty++;
                }
            }

            // number of decorations will be a certain percentage of empty spaces.
            numOfDecors = numOfEmpty * decorPercent;

            return (int)numOfDecors;
        }

        /// <summary>
        /// Places decorations sequentially using a for loop
        /// </summary>
        private static void PlaceDecorSequentially()
        {
            int numOfDecors = CalculateDecorAmount(DecorPercent);

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (grid[x, y].tile == TileType.EmptySpace && numOfDecors > 0)
                    {
                        // 20% to place a mountain
                        if (rand.Next(0, 5) == 0)
                        {
                            grid[x, y].tile = TileType.Decor2;
                            numOfDecors--;
                        }
                        else
                        {
                            grid[x, y].tile = TileType.Decor;
                            numOfDecors--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Randomly check for empty space on grid; if empty, changes the tile to Decoration tile type 
        /// </summary>
        private static void PlaceDecor(int num)
        {
            // loop for decoration placement
            while (num > 0)
            {
                if (PutDecor(rand.Next(0, GRID_HEIGHT - 1), rand.Next(0, GRID_WIDTH - 1)))
                    --num; // Decrement count if a decoration is successfully placed
            }
        }

        private static bool PutDecor(int y, int x)
        {
            if (grid[y, x].tile == TileType.EmptySpace)
            {
                grid[y, x].tile = TileType.Decor;
                return true; // If a decoration is placed, return true
            }
            return false; // If placement fails, return false
        }

        private static int DecorCount()
        {
            int count = 0;

            // height of the grid
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                // width of the grid
                for (int y = 0; y < GRID_WIDTH; y++)
                {
                    if (grid[x, y].tile == TileType.Decor || grid[x, y].tile == TileType.Decor2)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private static void PlaceDecor2(int num)
        {
            while (DecorCount() < num)
            {
                //Display();
                PutDecor2(rand.Next(0, GRID_HEIGHT - 1), rand.Next(0, GRID_WIDTH - 1), TileType.Decor2);
                //PutDecor2(rand.Next(0, GRID_HEIGHT - 1), rand.Next(0, GRID_WIDTH - 1), TileType.Decor);
                PutDecor(rand.Next(0, GRID_HEIGHT - 1), rand.Next(0, GRID_WIDTH - 1));
            }
        }

        private static void PutDecor2(int x, int y, TileType decor)
        {
            if (grid[x, y].tile == TileType.EmptySpace)
            {
                EmptyTileCount(x, y, Direction.Up);
                EmptyTileCount(x, y, Direction.Down);
                EmptyTileCount(x, y, Direction.Left);
                EmptyTileCount(x, y, Direction.Right);

                // place first decoration
                grid[x, y].tile = decor;

                // starting from the top, go clockwise
                //switch (rand.Next(0, 2))
                //{

                //    case 0:
                //        // up
                //        for (int newX = x; newX > (x - _emptyCount[0] % 3); newX--)
                //        {
                //            if (grid[newX, y].tile == TileType.EmptySpace)
                //                grid[newX, y].tile = decor;
                //        }
                //        // down
                //        for (int newX = x; newX < (x + _emptyCount[2] % 3); newX++)
                //        {
                //            if (grid[newX, y].tile == TileType.EmptySpace)
                //                grid[newX, y].tile = TileType.Decor2;
                //        }
                //        break;
                //    case 1:

                //        // left 
                //        for (int newY = y; newY > (y - _emptyCount[3] % 3); newY--)
                //        {
                //            if (grid[x, newY].tile == TileType.EmptySpace)
                //                grid[x, newY].tile = TileType.Decor2;
                //        }
                //        // right
                //        for (int newY = y; newY < (y + _emptyCount[1] % 3); newY++)
                //        {
                //            if (grid[x, newY].tile == TileType.EmptySpace)
                //                grid[x, newY].tile = TileType.Decor2;
                //        }
                //        break;
                //}

                // up
                if (_emptyCount[0] > 4)
                {
                    _emptyCount[0] = 0;
                    PutDecor2(x - 1, y, decor);
                }
                // bottom
                if (_emptyCount[1] > 4)
                {
                    _emptyCount[1] = 0;
                    PutDecor2(x + 1, y, decor);
                }
                // left
                if (_emptyCount[2] > 4)
                {
                    _emptyCount[2] = 0;
                    PutDecor2(x, y - 1, decor);
                }
                // right
                if (_emptyCount[3] > 4)
                {
                    _emptyCount[3] = 0;
                    PutDecor2(x, y + 1, decor);
                }

                //// check every direction, but the opposite
                //// up
                //if (_emptyCount[0] > 4 && _emptyCount[2] > 0 && _emptyCount[3] > 0)
                //{
                //    _emptyCount[0] = 0;
                //    _emptyCount[2] = 0;
                //    _emptyCount[3] = 0;
                //    PutDecor2(x - 1, y, decor);
                //}
                //// bottom
                //if (_emptyCount[1] > 4 && _emptyCount[2] > 0 && _emptyCount[3] > 0)
                //{
                //    _emptyCount[1] = 0;
                //    _emptyCount[2] = 0;
                //    _emptyCount[3] = 0;
                //    PutDecor2(x + 1, y , decor);
                //}
                //// left
                //if (_emptyCount[2] > 4 && _emptyCount[0] > 0 && _emptyCount[1] > 0)
                //{
                //    _emptyCount[0] = 0;
                //    _emptyCount[1] = 0;
                //    _emptyCount[2] = 0;
                //    PutDecor2(x, y - 1, decor);
                //}
                //// right
                //if (_emptyCount[3] > 4 && _emptyCount[0] > 0 && _emptyCount[1] > 0)
                //{
                //    _emptyCount[0] = 0;
                //    _emptyCount[1] = 0;
                //    _emptyCount[3] = 0;
                //    PutDecor2(x, y + 1, decor);
                //}

                for (int i = 0; i < NUM_OF_DIRECTIONS; i++)
                {
                    _emptyCount[i] = 0;
                }
            }
        }

        private static void EmptyTileCount(int x, int y, Direction dir)
        {
            // check to the top
            if (x > 0 && dir == Direction.Up)
                if (grid[x - 1, y].tile == TileType.EmptySpace)
                {
                    _emptyCount[0]++;
                    EmptyTileCount(x - 1, y, Direction.Up);
                }
            // check to the bottom
            if (x < GRID_WIDTH - 1 && dir == Direction.Down)
                if (grid[x + 1, y].tile == TileType.EmptySpace)
                {
                    _emptyCount[1]++;
                    EmptyTileCount(x + 1, y, Direction.Down);
                }
            // check to the left
            if (y > 0 && dir == Direction.Left)
                if (grid[x, y - 1].tile == TileType.EmptySpace)
                {
                    _emptyCount[2]++;
                    EmptyTileCount(x, y - 1, Direction.Left);
                }
            // check to the right
            if (y < GRID_HEIGHT - 1 && dir == Direction.Right)
                if (grid[x, y + 1].tile == TileType.EmptySpace)
                {
                    _emptyCount[3]++;
                    EmptyTileCount(x, y + 1, Direction.Right);
                }

            //// check to the top
            //if (x > 0 && dir == Direction.Up)
            //    if (grid[x - 1, y].tile == TileType.EmptySpace || grid[x - 1, y].tile == TileType.Decor || grid[x - 1, y].tile == TileType.Decor2)
            //    {
            //        _emptyCount[0]++;
            //        EmptyTileCount(x - 1, y, Direction.Up);
            //    }

            //// check to the bottom
            //if (x < GRID_WIDTH - 1 && dir == Direction.Down)
            //    if (grid[x + 1, y].tile == TileType.EmptySpace || grid[x + 1, y].tile == TileType.Decor || grid[x + 1, y].tile == TileType.Decor2)
            //    {
            //        _emptyCount[1]++;
            //        EmptyTileCount(x + 1, y, Direction.Down);
            //    }
            //// check to the left
            //if (y > 0 && dir == Direction.Left)
            //    if (grid[x, y - 1].tile == TileType.EmptySpace || grid[x, y - 1].tile == TileType.Decor || grid[x, y - 1].tile == TileType.Decor2)
            //    {
            //        _emptyCount[2]++;
            //        EmptyTileCount(x, y - 1, Direction.Left);
            //    }
            //// check to the right
            //if (y < GRID_HEIGHT - 1 && dir == Direction.Right)
            //    if (grid[x, y + 1].tile == TileType.EmptySpace || grid[x, y + 1].tile == TileType.Decor || grid[x, y + 1].tile == TileType.Decor2)
            //    {
            //        _emptyCount[3]++;
            //        EmptyTileCount(x, y + 1, Direction.Right);
            //    }
        }

        /// <summary>
        /// Is given a point and checks all surrounding tiles if it's empty
        /// </summary>
        /// <param name="x">First coordinate point</param>
        /// <param name="y">Second coordinate point</param>
        /// <returns>array of bools where index 0 is the top and continues clockwise</returns>
        private static bool[] HasEmptySurroundingTile(int x, int y)
        {
            bool[] eightSides = new bool[8];

            // check to the top
            if (x > 0)
                if (grid[x - 1, y].tile == TileType.EmptySpace)
                    eightSides[0] = true;
            // check to the top right
            if (x > 0 && y < GRID_HEIGHT - 1)
                if (grid[x - 1, y + 1].tile == TileType.EmptySpace)
                    eightSides[1] = true;
            // check to the right
            if (y < GRID_HEIGHT - 1)
                if (grid[x, y + 1].tile == TileType.EmptySpace)
                    eightSides[2] = true;
            // check to the bottom right
            if (x < GRID_WIDTH - 1 && y < GRID_HEIGHT - 1)
                if (grid[x + 1, y + 1].tile == TileType.EmptySpace)
                    eightSides[3] = true;
            // check to the bottomn
            if (x < GRID_WIDTH - 1)
                if (grid[x + 1, y].tile == TileType.EmptySpace)
                    eightSides[4] = true;
            // check to the bottom left
            if (x < GRID_WIDTH - 1 && y > 0)
                if (grid[x + 1, y - 1].tile == TileType.EmptySpace)
                    eightSides[5] = true;
            // check to the left
            if (y > 0)
                if (grid[x, y - 1].tile == TileType.EmptySpace)
                    eightSides[6] = true;
            // check to the top left
            if (x > 0 && y > 0)
                if (grid[x - 1, y - 1].tile == TileType.EmptySpace)
                    eightSides[7] = true;


            return eightSides;
        }

        #endregion

        #region Tower Functions
        /// <summary>
        /// Does a simple calculation to determine the number
        /// of towers to be placed
        /// </summary>
        /// <param name="towerPercent">Percentage of empty spaces to be used</param>
        /// <returns>number of decorations as an int</returns>
        private static int CalculateTowerAmount(float towerPercent)
        {
            float numOfTowers = 0;
            float numOfEmpty = 0;

            // use the searched field as a flag for being counted
            UnsearchGrid();
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    // check if it's a path tile
                    if (grid[x, y].tile <= TileType.Split4Ways)
                    {
                        // look at surrounding tiles 
                        // bounds check
                        // up
                        if (x > 0)
                            // check if tile is empty and been searched
                            if (grid[x - 1, y].tile == TileType.EmptySpace && !grid[x - 1, y].searched)
                            {
                                // increment empty count
                                numOfEmpty++;
                                // set tile as searched
                                grid[x - 1, y].searched = true;
                            }
                        // down
                        if (x < GRID_WIDTH - 1)
                            if (grid[x + 1, y].tile == TileType.EmptySpace && !grid[x + 1, y].searched)
                            {
                                numOfEmpty++;
                                grid[x + 1, y].searched = true;
                            }
                        // left
                        if (y > 0)
                            if (grid[x, y - 1].tile == TileType.EmptySpace && !grid[x, y - 1].searched)
                            {
                                numOfEmpty++;
                                grid[x, y - 1].searched = true;
                            }

                        // right
                        if (y < GRID_HEIGHT - 1)
                            if (grid[x, y + 1].tile == TileType.EmptySpace && !grid[x, y + 1].searched)
                            {
                                numOfEmpty++;
                                grid[x, y + 1].searched = true;
                            }
                        // top left
                        if (x > 0 && y > 0)
                            if (grid[x - 1, y - 1].tile == TileType.EmptySpace && !grid[x - 1, y - 1].searched)
                            {
                                numOfEmpty++;
                                grid[x - 1, y - 1].searched = true;
                            }
                        // top right
                        if (x > 0 && y < GRID_HEIGHT - 1)
                            if (grid[x - 1, y + 1].tile == TileType.EmptySpace && !grid[x - 1, y + 1].searched)
                            {
                                numOfEmpty++;
                                grid[x - 1, y + 1].searched = true;
                            }
                        // bottom left
                        if (x < GRID_WIDTH - 1 && y > 0)
                            if (grid[x + 1, y - 1].tile == TileType.EmptySpace && !grid[x + 1, y - 1].searched)
                            {
                                numOfEmpty++;
                                grid[x + 1, y - 1].searched = true;
                            }
                        // bottom right
                        if (x < GRID_WIDTH - 1 && y < GRID_HEIGHT - 1)
                            if (grid[x + 1, y + 1].tile == TileType.EmptySpace && !grid[x + 1, y + 1].searched)
                            {
                                numOfEmpty++;
                                grid[x + 1, y + 1].searched = true;
                            }
                    }
                }
            }
            UnsearchGrid();

            numOfTowers = numOfEmpty * towerPercent;
            return (int)numOfTowers;
        }

        /// <summary>
        /// Places towers sequentially using a for loop
        /// </summary>
        private static void PlaceTowerSequentially()
        {
            int numOfTowers = CalculateTowerAmount(TowerPercent);

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    // check if it's a path tile
                    if (grid[x, y].tile <= TileType.Split4Ways)
                    {
                        // look at surrounding tiles 
                        // bounds check
                        // up
                        if (x > 0)
                            // check if tile is empty
                            if (grid[x - 1, y].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                // place tower
                                grid[x - 1, y].tile = TileType.Tower;
                                numOfTowers--;
                            }
                        // down
                        if (x < GRID_WIDTH - 1)
                            if (grid[x + 1, y].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                grid[x + 1, y].tile = TileType.Tower;
                                numOfTowers--;
                            }
                        // left
                        if (y > 0)
                            if (grid[x, y - 1].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                grid[x, y - 1].tile = TileType.Tower;
                                numOfTowers--;
                            }
                        // right
                        if (y < GRID_HEIGHT - 1)
                            if (grid[x, y + 1].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                grid[x, y + 1].tile = TileType.Tower;
                                numOfTowers--;
                            }

                        // top left
                        if (x > 0 && y > 0)
                            if (grid[x - 1, y - 1].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                grid[x - 1, y - 1].tile = TileType.Tower;
                                numOfTowers--;
                            }
                        // top right
                        if (x > 0 && y < GRID_HEIGHT - 1)
                            if (grid[x - 1, y + 1].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                grid[x - 1, y + 1].tile = TileType.Tower;
                                numOfTowers--;
                            }
                        // bottom left
                        if (x < GRID_WIDTH - 1 && y > 0)
                            if (grid[x + 1, y - 1].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                grid[x + 1, y - 1].tile = TileType.Tower;
                                numOfTowers--;
                            }
                        // bottom right
                        if (x < GRID_WIDTH - 1 && y < GRID_HEIGHT - 1)
                            if (grid[x + 1, y + 1].tile == TileType.EmptySpace && numOfTowers > 0)
                            {
                                grid[x + 1, y + 1].tile = TileType.Tower;
                                numOfTowers--;
                            }
                    }
                }
            }

        }

        /// <summary>
        /// Randomly check for empty space on grid next to path; if empty, changes the tile to Tower 
        /// </summary>
        private static void SingleTower()
        {
            // flag to find new position
            bool towerPlaced = false;

            // initialize a random point
            Pair randomPos = new Pair(rand.Next(0, GRID_WIDTH), rand.Next(0, GRID_HEIGHT));

            // check if a tower has been placed within the 8 directions
            while (!towerPlaced)
            {
                // if iterator reaches end and no matches made or tower placement flag raised, find new position
                if (grid[randomPos.x, randomPos.y].tile > TileType.Split4Ways || towerPlaced)
                {
                    randomPos.x = rand.Next(0, GRID_WIDTH);
                    randomPos.y = rand.Next(0, GRID_HEIGHT);
                    towerPlaced = false;
                }

                // check if position in bounds and if position is a path tile 
                if (grid[randomPos.x, randomPos.y].tile <= TileType.Split4Ways)
                {
                    // bounds check and check if top tile is empty
                    if (randomPos.x > 0 && grid[randomPos.x - 1, randomPos.y].tile == TileType.EmptySpace)
                        // place tower
                        grid[randomPos.x - 1, randomPos.y].tile = TileType.Tower;
                    // down
                    else if (randomPos.x < GRID_WIDTH - 1 && grid[randomPos.x + 1, randomPos.y].tile == TileType.EmptySpace)
                        grid[randomPos.x + 1, randomPos.y].tile = TileType.Tower;
                    // left
                    else if (randomPos.y > 0 && grid[randomPos.x, randomPos.y - 1].tile == TileType.EmptySpace)
                        grid[randomPos.x, randomPos.y - 1].tile = TileType.Tower;
                    // right
                    else if (randomPos.y < GRID_HEIGHT - 1 && grid[randomPos.x, randomPos.y + 1].tile == TileType.EmptySpace)
                        grid[randomPos.x, randomPos.y + 1].tile = TileType.Tower;

                    // top left
                    else if (randomPos.x > 0 && randomPos.y > 0 && grid[randomPos.x - 1, randomPos.y - 1].tile == TileType.EmptySpace)
                        grid[randomPos.x - 1, randomPos.y - 1].tile = TileType.Tower;
                    // top right
                    else if (randomPos.x > 0 && randomPos.y < GRID_HEIGHT - 1 && grid[randomPos.x - 1, randomPos.y + 1].tile == TileType.EmptySpace)
                        grid[randomPos.x - 1, randomPos.y + 1].tile = TileType.Tower;
                    // bottom left
                    else if (randomPos.x < GRID_WIDTH - 1 && randomPos.y > 0 && grid[randomPos.x + 1, randomPos.y - 1].tile == TileType.EmptySpace)
                        grid[randomPos.x + 1, randomPos.y - 1].tile = TileType.Tower;
                    // bottom right
                    else if (randomPos.x < GRID_WIDTH - 1 && randomPos.y < GRID_HEIGHT - 1 && grid[randomPos.x + 1, randomPos.y + 1].tile == TileType.EmptySpace)
                        grid[randomPos.x + 1, randomPos.y + 1].tile = TileType.Tower;

                    // if all tiles occupied, break
                    else
                        break;

                    // once a tower is placed, raise flag to find new row
                    towerPlaced = true;
                }
            }
        }

        /// <summary>
        /// Randomly check for empty space on grid next to path; if empty, changes the tile to Tower.
        /// </summary>
        /// <param name="NUM_OF_TOWERS">number of desired towers</param>
        private static void PlaceTowers()
        {
            // flag to find new rower
            bool towerPlaced = false;

            // random row and column
            Pair randomPos = new Pair(rand.Next(0, GRID_WIDTH), rand.Next(0, GRID_HEIGHT));

            // loop that finds a valid tower placement
            while (TowerCount() != NUM_OF_TOWERS)
            {
                // if iterator reaches end and no matches made or tower placement flag raised, find new row
                if (grid[randomPos.x, randomPos.y].tile > TileType.Split4Ways || towerPlaced)
                {
                    randomPos.x = rand.Next(0, GRID_WIDTH);
                    randomPos.y = rand.Next(0, GRID_HEIGHT);
                    towerPlaced = false;
                }

                // check if position in bounds and if position is a path tile 
                if (grid[randomPos.x, randomPos.y].tile <= TileType.Split4Ways)
                {
                    // check to the left of the tile
                    if (randomPos.y > 0 && grid[randomPos.x, randomPos.y - 1].tile == TileType.EmptySpace)
                        grid[randomPos.x, randomPos.y - 1].tile = TileType.Tower;

                    // check to the right of tile, if empty place tower
                    else if (randomPos.y < GRID_HEIGHT - 1 && grid[randomPos.x, randomPos.y + 1].tile == TileType.EmptySpace)
                        grid[randomPos.x, randomPos.y + 1].tile = TileType.Tower;

                    // check to the bottom of the tile
                    else if (randomPos.x < GRID_WIDTH - 1 && grid[randomPos.x + 1, randomPos.y].tile == TileType.EmptySpace)
                        grid[randomPos.x + 1, randomPos.y].tile = TileType.Tower;

                    // check to the top of the tile
                    else if (randomPos.x > 0 && grid[randomPos.x - 1, randomPos.y].tile == TileType.EmptySpace)
                        grid[randomPos.x - 1, randomPos.y].tile = TileType.Tower;

                    // if all tiles occupied, break
                    else
                        break;

                    // once a tower is placed, raise flag to find new row
                    towerPlaced = true;
                }
            }
        }

        /// <summary>
        /// Count towers
        /// </summary>
        /// <returns></returns>
        private static int TowerCount()
        {
            int count = 0;

            // height of the grid
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                // width of the grid
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    if (grid[y, x].tile == TileType.Tower)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// PlaceTower(): Attempts to place a tower on a square diagonal to the current position. Returns true if
        /// successful, false otherwise
        /// </summary>
        /// <returns></returns>
        private static bool PlaceTower()
        {
            if (_position.y > 0 && _position.y < GRID_HEIGHT - 1 && _position.x > 0 && _position.x < GRID_WIDTH - 1)
            {
                switch (rand.Next(0, NUM_OF_DIRECTIONS))
                {
                    case 0: // Lower Right
                        if (grid[_position.y + 1, _position.x + 1].tile == TileType.EmptySpace)
                        {
                            grid[_position.y + 1, _position.x + 1].tile = TileType.Tower;
                            return true;
                        }
                        break;
                    case 1: // Lower Left
                        if (grid[_position.y + 1, _position.x - 1].tile == TileType.EmptySpace)
                        {
                            grid[_position.y + 1, _position.x - 1].tile = TileType.Tower;
                            return true;
                        }
                        break;
                    case 2: // Upper Right
                        if (grid[_position.y - 1, _position.x + 1].tile == TileType.EmptySpace)
                        {
                            grid[_position.y - 1, _position.x + 1].tile = TileType.Tower;
                            return true;
                        }
                        break;
                    case 3: // Upper Left
                        if (grid[_position.y - 1, _position.x - 1].tile == TileType.EmptySpace)
                        {
                            grid[_position.y - 1, _position.x - 1].tile = TileType.Tower;
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        #endregion
    }
}
