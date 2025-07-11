using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameProject
{
    class GameMenu
    {
        static void Main(string[] args)
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine("=== GAME MENU ===");
                Console.WriteLine("1. Police Chase");
                Console.WriteLine("2. Jumping Platformer");
                Console.WriteLine("3. Snake");
                Console.WriteLine("4. Bomb Dodger");
                Console.WriteLine("0. Exit");
                Console.Write("Choose option: ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        PoliceGame.Start();
                        break;
                    case "2":
                        JumpingGame.Start();
                        break;
                    case "3":
                        SnakeGame.Start();
                        break;
                    case "4":
                        BombDodger.Start();
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Press any key...");
                        Console.ReadKey();
                        break;
                }
            }
        }
    }

    class PoliceGame
    {
        public static void Start()
        {
            Console.CursorVisible = false;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Thread inputThread = new Thread(WatchKeys);
            inputThread.IsBackground = true;
            inputThread.Start();

            LoadLevel(currentLevel);

            int interval = 120;
            int tick = 0;

            gameOver = false;
            while (!gameOver)
            {
                var start = DateTime.Now;
                DrawMap();
                if (tick % 2 == 0)
                {
                    MovePlayer();
                }
                MovePolice();
                if (CheckCollision()) break;
                tick++;
                int waitTime = interval - (int)(DateTime.Now - start).TotalMilliseconds;
                if (waitTime > 0) Thread.Sleep(waitTime);
            }

            Console.SetCursorPosition(0, height + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Game over. Press any key...");
            Console.ReadKey();
        }

        static string[][] levels = new string[][]
        {
            new string[]
            {
                "##############################",
                "#S   #     #####      #     G#",
                "### ### ###   ### ##  ### ####",
                "#         #   #   #       #  #",
                "# ### ### ##### ### ##### #  #",
                "#   #     #         #     ####",
                "##### ### # # ##### # ####### ",
                "#       #                    #",
                "##############################"
            },
            new string[]
            {
                "##############################",
                "#S    #     ###     ###     G#",
                "# ### # ### ### ### ### #####",
                "# #     #   #   #   #       #",
                "# # ### # ##### ##### ##### #",
                "#     #       ##     #   #",
                "### ####### # #####   # # ###",
                "#         # #         #     #",
                "##############################"
            }
        };

        static int currentLevel = 0;
        static char[,] map;
        static int width, height;
        static int playerX, playerY;
        static int goalX, goalY;
        static List<(int x, int y, int direction)> police = new List<(int x, int y, int direction)>();
        static Random random = new Random();

        static bool holdingW = false, holdingS = false, holdingA = false, holdingD = false;
        static bool gameOver = false;

        static void LoadLevel(int index)
        {
            var rows = levels[index];
            height = rows.Length;
            width = rows[0].Length;
            map = new char[height, width];
            police.Clear();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char character = rows[y][x];
                    map[y, x] = character;
                    if (character == 'S') { playerX = x; playerY = y; map[y, x] = ' '; }
                    if (character == 'G') { goalX = x; goalY = y; }
                }
            }

            police.Add((5, 2, 1));
            police.Add((10, 5, -1));
            police.Add((20, 3, 1));
        }

        static void DrawMap()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Level {currentLevel + 1}");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == playerX && y == playerY)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (x == goalX && y == goalY)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else if (police.Exists(p => p.x == x && p.y == y))
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    else
                        Console.ForegroundColor = ConsoleColor.White;

                    if (x == playerX && y == playerY) Console.Write("Z");
                    else if (x == goalX && y == goalY) Console.Write("G");
                    else if (police.Exists(p => p.x == x && p.y == y)) Console.Write("P");
                    else Console.Write(map[y, x]);
                }
                Console.WriteLine();
            }
        }

        static void WatchKeys()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.W) holdingW = true;
                if (key == ConsoleKey.S) holdingS = true;
                if (key == ConsoleKey.A) holdingA = true;
                if (key == ConsoleKey.D) holdingD = true;
                if (key == ConsoleKey.Escape) gameOver = true;
            }
        }

        static void MovePlayer()
        {
            int newX = playerX, newY = playerY;
            if (holdingW) newY--;
            else if (holdingS) newY++;
            else if (holdingA) newX--;
            else if (holdingD) newX++;

            if (IsFree(newX, newY))
            {
                playerX = newX;
                playerY = newY;
            }

            holdingW = holdingS = holdingA = holdingD = false;
        }

        static void MovePolice()
        {
            for (int i = 0; i < police.Count; i++)
            {
                int x = police[i].x;
                int y = police[i].y;
                int direction = police[i].direction;
                int newX = x + direction;

                if (IsFree(newX, y))
                {
                    police[i] = (newX, y, direction);
                }
                else
                {
                    direction *= -1;
                    newX = x + direction;
                    if (IsFree(newX, y)) police[i] = (newX, y, direction);
                    else police[i] = (x, y, direction);
                }
            }
        }

        static bool IsFree(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height && map[y, x] != '#';
        }

        static bool CheckCollision()
        {
            foreach (var p in police)
                if (p.x == playerX && p.y == playerY)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\u274C Police caught you!");
                    return true;
                }

            if (playerX == goalX && playerY == goalY)
            {
                currentLevel++;
                if (currentLevel < levels.Length)
                {
                    LoadLevel(currentLevel);
                    return false;
                }
                else
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\ud83c\udfc6 You won all levels!");
                    return true;
                }
            }
            return false;
        }
    }

    static class JumpingGame
    {
        static int targetFps = 50;
        static int frameTime = 1000 / targetFps;
        static int width, height;
        static double gravity = 0.2;
        static double jumpVelocity = -6;
        static double horizontalSpeed = 2;
        static int platformWidth = 7;
        static int platformCount = 10;
        static List<Platform> platforms;
        static Ball ball;
        static double scoreDistance;
        static Random random = new Random();
        static char[,] screenBuffer;

        const int VK_LEFT = 0x25;
        const int VK_RIGHT = 0x27;
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        public static void Start()
        {
            Console.CursorVisible = false;
            Console.Clear();
            width = Console.WindowWidth;
            height = Console.WindowHeight;
            screenBuffer = new char[height, width];
            scoreDistance = 0;
            Initialize();

            var stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Restart();
                Update();
                Render();
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                if (elapsed < frameTime)
                    Thread.Sleep(frameTime - elapsed);
            }
        }

        static void Initialize()
        {
            platforms = new List<Platform>();
            for (int i = 0; i < platformCount; i++)
            {
                int y = height - 1 - i * (height / platformCount);
                int x = random.Next(0, Math.Max(1, width - platformWidth));
                platforms.Add(new Platform(x, y));
            }
            var platform0 = platforms[0];
            ball = new Ball(platform0.X + platformWidth / 2, platform0.Y - 1);
        }

        static void Update()
        {
            ball.PreviousY = ball.Y;
            if (GetAsyncKeyState(VK_LEFT) < 0) ball.X -= horizontalSpeed;
            if (GetAsyncKeyState(VK_RIGHT) < 0) ball.X += horizontalSpeed;
            ball.VY += gravity;
            ball.Y += ball.VY;

            if (ball.VY > 0)
            {
                foreach (var platform in platforms)
                {
                    if (ball.PreviousY < platform.Y && ball.Y >= platform.Y && ball.X >= platform.X && ball.X <= platform.X + platformWidth)
                    {
                        ball.VY = jumpVelocity;
                        ball.Y = platform.Y - 1;
                        break;
                    }
                }
            }

            if (ball.Y < height / 3.0)
            {
                double shift = height / 3.0 - ball.Y;
                ball.Y = height / 3.0;
                scoreDistance += shift;
                for (int i = 0; i < platforms.Count; i++)
                {
                    platforms[i].Y += shift;
                    if (platforms[i].Y > height - 1)
                    {
                        platforms[i].Y = 0;
                        platforms[i].X = random.Next(0, Math.Max(1, width - platformWidth));
                    }
                }
            }

            if (ball.Y > height - 1)
            {
                Console.Clear();
                Console.SetCursorPosition(Math.Max(0, width / 2 - 5), Math.Max(0, height / 2));
                Console.Write("Game Over!");
                Thread.Sleep(2000);
                return;
            }

            ball.X = Math.Max(0, Math.Min(ball.X, width - 1));
        }

        static void Render()
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    screenBuffer[y, x] = ' ';

            foreach (var platform in platforms)
            {
                int px = (int)Math.Round(platform.X);
                int py = (int)Math.Round(platform.Y);
                if (py >= 0 && py < height)
                    for (int i = 0; i < platformWidth && px + i < width; i++)
                        screenBuffer[py, px + i] = '=';
            }

            int ballX = (int)Math.Round(ball.X);
            int ballY = (int)Math.Round(ball.Y);
            if (ballX >= 0 && ballX < width && ballY >= 0 && ballY < height)
                screenBuffer[ballY, ballX] = 'O';

            var scoreText = $"Score: {(int)scoreDistance}";
            for (int i = 0; i < scoreText.Length && i < width; i++)
                screenBuffer[0, i] = scoreText[i];

            for (int y = 0; y < height; y++)
            {
                Console.SetCursorPosition(0, y);
                for (int x = 0; x < width; x++)
                    Console.Write(screenBuffer[y, x]);
            }
        }
    }

    static class SnakeGame
    {
        static int width = 40;
        static int height = 20;
        static int snakeX, snakeY;
        static int foodX, foodY;
        static int directionX = 1, directionY = 0;
        static List<(int x, int y)> snakeBody = new List<(int x, int y)>();
        static int score = 0;
        static bool gameOver = false;
        static Random random = new Random();

        static int horizontalSpeed = 100;
        static int verticalSpeed = 200;
        static int currentSpeed = 100;

        public static void Start()
        {
            Console.CursorVisible = false;
            Console.Clear();

            snakeX = width / 2;
            snakeY = height / 2;
            snakeBody.Clear();
            snakeBody.Add((snakeX, snakeY));

            CreateFood();

            Thread inputThread = new Thread(WatchKeys);
            inputThread.IsBackground = true;
            inputThread.Start();

            gameOver = false;
            while (!gameOver)
            {
                var start = DateTime.Now;
                DrawGame();
                MoveSnake();
                CheckCollision();

                int waitTime = currentSpeed - (int)(DateTime.Now - start).TotalMilliseconds;
                if (waitTime > 0) Thread.Sleep(waitTime);
            }

            Console.SetCursorPosition(0, height + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Game over. Press any key...");
            Console.ReadKey();

            score = 0;
            directionX = 1;
            directionY = 0;
            currentSpeed = horizontalSpeed;
        }

        static void CreateFood()
        {
            do
            {
                foodX = random.Next(1, width - 1);
                foodY = random.Next(1, height - 1);
            } while (snakeBody.Exists(segment => segment.x == foodX && segment.y == foodY));
        }

        static void DrawGame()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Snake - Score: {score} | Speed: {(currentSpeed == horizontalSpeed ? "Fast" : "Slow")}");

            Console.Write("╔");
            for (int x = 0; x < width; x++) Console.Write("═");
            Console.WriteLine("╗");

            for (int y = 0; y < height; y++)
            {
                Console.Write("║");
                for (int x = 0; x < width; x++)
                {
                    if (snakeBody.Exists(segment => segment.x == x && segment.y == y))
                    {
                        if (snakeBody[0].x == x && snakeBody[0].y == y)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("█");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("█");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else if (x == foodX && y == foodY)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("●");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        Console.Write(" ");
                    }
                }
                Console.WriteLine("║");
            }

            Console.Write("╚");
            for (int x = 0; x < width; x++) Console.Write("═");
            Console.WriteLine("╝");

            Console.WriteLine("Controls: arrows or WASD, ESC = exit");
            Console.WriteLine("Horizontal movement is faster than vertical");
        }

        static void WatchKeys()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        if (directionY != 1)
                        {
                            directionX = 0; directionY = -1;
                            currentSpeed = verticalSpeed;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        if (directionY != -1)
                        {
                            directionX = 0; directionY = 1;
                            currentSpeed = verticalSpeed;
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        if (directionX != 1)
                        {
                            directionX = -1; directionY = 0;
                            currentSpeed = horizontalSpeed;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        if (directionX != -1)
                        {
                            directionX = 1; directionY = 0;
                            currentSpeed = horizontalSpeed;
                        }
                        break;
                    case ConsoleKey.Escape:
                        gameOver = true;
                        break;
                }
            }
        }

        static void MoveSnake()
        {
            int newX = snakeBody[0].x + directionX;
            int newY = snakeBody[0].y + directionY;

            snakeBody.Insert(0, (newX, newY));

            if (newX == foodX && newY == foodY)
            {
                score++;
                CreateFood();
            }
            else
            {
                snakeBody.RemoveAt(snakeBody.Count - 1);
            }
        }

        static void CheckCollision()
        {
            var head = snakeBody[0];

            if (head.x < 0 || head.x >= width || head.y < 0 || head.y >= height)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("💥 Hit the wall!");
                Console.WriteLine($"Your score: {score}");
                gameOver = true;
                return;
            }

            for (int i = 1; i < snakeBody.Count; i++)
            {
                if (snakeBody[i].x == head.x && snakeBody[i].y == head.y)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("💥 Snake bit its tail!");
                    Console.WriteLine($"Your score: {score}");
                    gameOver = true;
                    return;
                }
            }
        }
    }

    static class BombDodger
    {
        const int Width = 40;
        const int Height = 20;
        const char BorderChar = '#';
        const char PlayerChar = '@';
        const char BombChar = 'B';
        const char RockChar = 'K';
        const char ExplosionChar = '*';

        static int playerX = Width / 2;
        static int playerY = Height - 2;
        static int score = 0;
        static int timeAlive = 0;
        static bool gameOver = false;

        static List<Entity> bombs = new List<Entity>();
        static List<Entity> rocks = new List<Entity>();
        static List<Explosion> explosions = new List<Explosion>();

        static Random random = new Random();

        public static void Start()
        {
            Console.CursorVisible = false;
            Console.Clear();

            playerX = Width / 2;
            playerY = Height - 2;
            score = 0;
            timeAlive = 0;
            gameOver = false;
            bombs.Clear();
            rocks.Clear();
            explosions.Clear();

            Thread inputThread = new Thread(HandleInput);
            inputThread.IsBackground = true;
            inputThread.Start();

            while (!gameOver)
            {
                var start = DateTime.Now;

                SpawnEntities();
                UpdateEntities();
                UpdateExplosions();
                UpdateScore();

                if (CheckCollision())
                {
                    GameOver();
                    break;
                }

                Draw();

                int elapsed = (int)(DateTime.Now - start).TotalMilliseconds;
                int sleepTime = 100 - elapsed;
                if (sleepTime > 0) Thread.Sleep(sleepTime);
            }

            Console.SetCursorPosition(0, Height + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Game over. Press any key...");
            Console.ReadKey();
        }

        static void HandleInput()
        {
            while (!gameOver)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    MovePlayer(key);
                }
                Thread.Sleep(50);
            }
        }

        static void MovePlayer(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    if (playerX > 1) playerX--;
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    if (playerX < Width - 2) playerX++;
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    if (playerY > 1) playerY--;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    if (playerY < Height - 2) playerY++;
                    break;
                case ConsoleKey.Escape:
                    gameOver = true;
                    break;
            }
        }

        static void SpawnEntities()
        {
            double bombChance = 0.15 + (timeAlive / 1000.0) * 0.1;
            double rockChance = 0.08 + (timeAlive / 1000.0) * 0.05;

            if (random.NextDouble() < bombChance)
            {
                int x = random.Next(1, Width - 1);
                int dx = random.Next(-1, 2);
                bombs.Add(new Entity(x, 1, dx, 1, BombChar));
            }

            if (random.NextDouble() < rockChance)
            {
                int x = random.Next(1, Width - 1);
                int dx = random.Next(-1, 2);
                rocks.Add(new Entity(x, 1, dx, 1, RockChar));
            }
        }

        static void UpdateEntities()
        {
            List<Entity> toExplode = new List<Entity>();

            foreach (var bomb in bombs.ToList())
            {
                bomb.Move();
                if (bomb.Y >= Height - 1)
                {
                    toExplode.Add(bomb);
                    bombs.Remove(bomb);
                }
            }

            foreach (var rock in rocks.ToList())
            {
                rock.Move();
                if (rock.Y >= Height - 1 || rock.X <= 0 || rock.X >= Width - 1)
                    rocks.Remove(rock);
            }

            foreach (var bomb in toExplode)
            {
                explosions.Add(new Explosion(bomb.X, bomb.Y));
            }
        }

        static void UpdateExplosions()
        {
            explosions.RemoveAll(explosion => (DateTime.Now - explosion.StartTime).TotalMilliseconds > 500);
        }

        static void UpdateScore()
        {
            timeAlive++;
            if (timeAlive % 10 == 0) score++;
        }

        static bool CheckCollision()
        {
            foreach (var bomb in bombs)
                if (bomb.X == playerX && bomb.Y == playerY)
                    return true;

            foreach (var rock in rocks)
                if (rock.X == playerX && rock.Y == playerY)
                    return true;

            foreach (var explosion in explosions)
                if (explosion.X == playerX && explosion.Y == playerY)
                    return true;

            return false;
        }

        static void Draw()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Bomb Dodger - Score: {score} | Time: {timeAlive / 10}s");

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (y == 0 || y == Height - 1 || x == 0 || x == Width - 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(BorderChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    if (x == playerX && y == playerY)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(PlayerChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    var bomb = bombs.FirstOrDefault(b => b.X == x && b.Y == y);
                    if (bomb != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(BombChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    var rock = rocks.FirstOrDefault(r => r.X == x && r.Y == y);
                    if (rock != null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(RockChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    var explosion = explosions.FirstOrDefault(e => e.X == x && e.Y == y);
                    if (explosion != null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(ExplosionChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    Console.Write(' ');
                }
                Console.WriteLine();
            }

            Console.WriteLine("Controls: arrows or WASD, ESC = exit");
            Console.WriteLine("Avoid bombs (B), rocks (K) and explosions (*)!");
        }

        static void GameOver()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("💥 GAME OVER!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Your score: {score}");
            Console.WriteLine($"Survived: {timeAlive / 10} seconds");
            gameOver = true;
        }

        class Entity
        {
            public int X, Y;
            int deltaX, deltaY;
            char symbol;

            public Entity(int x, int y, int deltaX, int deltaY, char symbol)
            {
                X = x;
                Y = y;
                this.deltaX = deltaX;
                this.deltaY = deltaY;
                this.symbol = symbol;
            }

            public void Move()
            {
                X += deltaX;
                Y += deltaY;
            }

            public char Symbol => symbol;
        }

        class Explosion
        {
            public int X, Y;
            public DateTime StartTime;

            public Explosion(int x, int y)
            {
                X = x;
                Y = y;
                StartTime = DateTime.Now;
            }
        }
    }

    class Platform
    {
        public double X, Y;
        public Platform(double x, double y) { X = x; Y = y; }
    }

    class Ball
    {
        public double X, Y, VY;
        public double PreviousY;
        public Ball(double x, double y) { X = x; Y = y; VY = 0; }
    }
}
