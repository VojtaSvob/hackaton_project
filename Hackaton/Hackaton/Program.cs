using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameProject
{
    /// <summary>
    /// Hlavní třída obsahující menu pro výběr her
    /// </summary>
    class GameMenu
    {
        /// <summary>
        /// Vstupní bod aplikace - zobrazuje hlavní menu a spouští vybranou hru
        /// </summary>
        static void Main(string[] args)
        {
            bool running = true; // Kontrola běhu aplikace

            // Hlavní smyčka menu
            while (running)
            {
                // Vymazání obrazovky a zobrazení menu
                Console.Clear();
                Console.WriteLine("=== GAME MENU ===");
                Console.WriteLine("1. Police Chase");       // Hra na policajty
                Console.WriteLine("2. Jumping Platformer"); // Skákací plošinovka
                Console.WriteLine("3. Snake");              // Had
                Console.WriteLine("4. Bomb Dodger");        // Vyhýbání bombám
                Console.WriteLine("0. Exit");               // Ukončení
                Console.Write("Choose option: ");
                string input = Console.ReadLine();

                // Zpracování volby uživatele
                switch (input)
                {
                    case "1":
                        PoliceGame.Start(); // Spuštění hry policajti
                        break;
                    case "2":
                        JumpingGame.Start(); // Spuštění skákací hry
                        break;
                    case "3":
                        SnakeGame.Start(); // Spuštění hry had
                        break;
                    case "4":
                        BombDodger.Start(); // Spuštění bomb dodger
                        break;
                    case "0":
                        running = false; // Ukončení aplikace
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Press any key...");
                        Console.ReadKey();
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Hra "Police Chase" - hráč se snaží dostat k cíli a vyhnout se policii
    /// </summary>
    class PoliceGame
    {
        /// <summary>
        /// Spuštění hry Police Chase
        /// </summary>
        public static void Start()
        {
            // Nastavení konzole pro hru
            Console.CursorVisible = false;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Spuštění threadu pro sledování kláves
            Thread inputThread = new Thread(WatchKeys);
            inputThread.IsBackground = true;
            inputThread.Start();

            LoadLevel(currentLevel); // Načtení aktuálního levelu

            int interval = 100; // Interval aktualizace hry (ms)
            int tick = 0;       // Počítadlo ticků

            gameOver = false;
            // Hlavní herní smyčka
            while (!gameOver)
            {
                var start = DateTime.Now;
                DrawMap(); // Vykreslení mapy

                // Pohyb hráče pouze každý druhý tick (pomalejší pohyb)
                if (tick % 2 == 0)
                {
                    MovePlayer();
                }

                MovePolice(); // Pohyb policie

                // Kontrola kolize a ukončení hry
                if (CheckCollision()) break;

                tick++;

                // Čekání do konce intervalu
                int waitTime = interval - (int)(DateTime.Now - start).TotalMilliseconds;
                if (waitTime > 0) Thread.Sleep(waitTime);
            }

            // Zobrazení konce hry
            Console.SetCursorPosition(0, height + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Game over. Press any key...");
            Console.ReadKey();
        }

        // Definice levelů - každý level je pole řetězců reprezentujících mapu
        static string[][] levels = new string[][]
        {
            // Level 1
            new string[]
            {
                "##############################",
                "#S   #     #####      #     G#", // S = start, G = goal
                "### ### ###   ### ##  ### ####",
                "#         #   #   #       #  #",
                "# ### ### ##### ### ##### #  #",
                "#   #     #         #     ####",
                "##### ### # # ##### # ####### ",
                "#       #                    #",
                "##############################"
            },
            // Level 2
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

        // Herní proměnné
        static int currentLevel = 0;                                        // Aktuální level
        static char[,] map;                                                 // 2D mapa levelu
        static int width, height;                                           // Rozměry mapy
        static int playerX, playerY;                                        // Pozice hráče
        static int goalX, goalY;                                           // Pozice cíle
        static List<(int x, int y, int direction)> police = new List<(int x, int y, int direction)>(); // Seznam policistů
        static Random random = new Random();                               // Generátor náhodných čísel

        // Stav kláves (pro plynulý pohyb)
        static bool holdingW = false, holdingS = false, holdingA = false, holdingD = false;
        static bool gameOver = false; // Stav ukončení hry

        /// <summary>
        /// Načtení a inicializace levelu
        /// </summary>
        /// <param name="index">Index levelu k načtení</param>
        static void LoadLevel(int index)
        {
            var rows = levels[index];
            height = rows.Length;
            width = rows[0].Length;
            map = new char[height, width];
            police.Clear(); // Vymazání předchozích policistů

            // Procházení mapy a inicializace pozic
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char character = rows[y][x];
                    map[y, x] = character;

                    // Najití startovní pozice hráče
                    if (character == 'S')
                    {
                        playerX = x;
                        playerY = y;
                        map[y, x] = ' '; // Odstranění 'S' z mapy
                    }

                    // Najití pozice cíle
                    if (character == 'G')
                    {
                        goalX = x;
                        goalY = y;
                    }
                }
            }


            // Přidání policistů na pevné pozice
            police.Add((5, 2, 1));   // x, y, směr pohybu
            police.Add((10, 5, -1));
            police.Add((20, 3, 1));
        }

        /// <summary>
        /// Vykreslení herní mapy na obrazovku
        /// </summary>
        static void DrawMap()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Level {currentLevel + 1}");

            // Procházení každého bodu mapy
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Nastavení barvy podle typu objektu
                    if (x == playerX && y == playerY)
                        Console.ForegroundColor = ConsoleColor.Red;     // Hráč - červená
                    else if (x == goalX && y == goalY)
                        Console.ForegroundColor = ConsoleColor.Green;   // Cíl - zelená
                    else if (police.Exists(p => p.x == x && p.y == y))
                        Console.ForegroundColor = ConsoleColor.Cyan;    // Policie - tyrkysová
                    else
                        Console.ForegroundColor = ConsoleColor.White;   // Ostatní - bílá

                    // Vykreslení příslušného znaku
                    if (x == playerX && y == playerY)
                        Console.Write("Z");
                    else if (x == goalX && y == goalY)
                        Console.Write("G");
                    else if (police.Exists(p => p.x == x && p.y == y))
                        Console.Write("P");
                    else
                        Console.Write(map[y, x]);
                }
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Thread pro sledování stisknutých kláves
        /// </summary>
        static void WatchKeys()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;

                // Nastavení příslušných flag pro stisknuté klávesy
                if (key == ConsoleKey.W) holdingW = true;
                if (key == ConsoleKey.S) holdingS = true;
                if (key == ConsoleKey.A) holdingA = true;
                if (key == ConsoleKey.D) holdingD = true;
                if (key == ConsoleKey.Escape) gameOver = true; // Ukončení hry
            }
        }

        /// <summary>
        /// Pohyb hráče podle stisknutých kláves
        /// </summary>
        static void MovePlayer()
        {
            int newX = playerX, newY = playerY;

            // Určení nové pozice podle stisknuté klávesy
            if (holdingW) newY--;      // Nahoru
            else if (holdingS) newY++; // Dolů
            else if (holdingA) newX--; // Vlevo
            else if (holdingD) newX++; // Vpravo

            // Pohyb pouze pokud je pozice volná
            if (IsFree(newX, newY))
            {
                playerX = newX;
                playerY = newY;
            }

            // Reset všech flag kláves
            holdingW = holdingS = holdingA = holdingD = false;
        }

        /// <summary>
        /// Pohyb všech policistů podle jejich směru
        /// </summary>
        static void MovePolice()
        {
            for (int i = 0; i < police.Count; i++)
            {
                int x = police[i].x;
                int y = police[i].y;
                int direction = police[i].direction;
                int newX = x + direction; // Nová pozice podle směru

                // Pokud je nová pozice volná, pohni se
                if (IsFree(newX, y))
                {
                    police[i] = (newX, y, direction);
                }
                else
                {
                    // Pokud narazí na překážku, otoč směr
                    direction *= -1;
                    newX = x + direction;

                    if (IsFree(newX, y))
                        police[i] = (newX, y, direction);
                    else
                        police[i] = (x, y, direction); // Zůstat na místě
                }
            }
        }

        /// <summary>
        /// Kontrola, zda je daná pozice volná (není zeď)
        /// </summary>
        /// <param name="x">X souřadnice</param>
        /// <param name="y">Y souřadnice</param>
        /// <returns>True pokud je pozice volná</returns>
        static bool IsFree(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height && map[y, x] != '#';
        }

        /// <summary>
        /// Kontrola kolizí a vítězných podmínek
        /// </summary>
        /// <returns>True pokud hra skončila</returns>
        static bool CheckCollision()
        {
            // Kontrola kolize s policií
            foreach (var p in police)
                if (p.x == playerX && p.y == playerY)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\u274C Police caught you!");
                    return true;
                }

            // Kontrola dosažení cíle
            if (playerX == goalX && playerY == goalY)
            {
                currentLevel++;

                // Pokud jsou další levely, načti další
                if (currentLevel < levels.Length)
                {
                    LoadLevel(currentLevel);
                    return false; // Pokračovat ve hře
                }
                else
                {
                    // Všechny levely dokončeny
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\ud83c\udfc6 You won all levels!");
                    return true;
                }
            }
            return false; // Hra pokračuje
        }
    }

    /// <summary>
    /// Skákací plošinovka - hráč skáče po plošinách
    /// </summary>
    static class JumpingGame
    {
        // Herní konstanty
        static int targetFps = 50;                          // Cílové FPS
        static int frameTime = 1000 / targetFps;            // Čas jednoho snímku
        static int width, height;                           // Rozměry obrazovky
        static double gravity = 0.2;                        // Gravitace
        static double jumpVelocity = -4;                    // Rychlost skoku
        static double horizontalSpeed = 2;                  // Horizontální rychlost
        static int platformWidth = 8;                       // Šířka plošin
        static int platformCount = 14;                      // Počet plošin

        // Herní objekty
        static List<Platform> platforms;                    // Seznam plošin
        static Ball ball;                                   // Míček hráče
        static double scoreDistance;                        // Skóre (vzdálenost)
        static Random random = new Random();               // Generátor náhodných čísel
        static char[,] screenBuffer;                        // Buffer pro vykreslování

        // Import funkcí Windows API pro detekci kláves
        const int VK_LEFT = 0x25;
        const int VK_RIGHT = 0x27;
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        /// <summary>
        /// Spuštění skákací hry
        /// </summary>
        public static void Start()
        {
            // Inicializace konzole
            Console.CursorVisible = false;
            Console.Clear();
            width = Console.WindowWidth;
            height = Console.WindowHeight;
            screenBuffer = new char[height, width];
            scoreDistance = 0;

            Initialize(); // Inicializace herních objektů

            var stopwatch = new Stopwatch();

            // Hlavní herní smyčka
            while (true)
            {
                stopwatch.Restart();
                Update(); // Aktualizace herní logiky
                Render(); // Vykreslení

                // Čekání do konce frame time
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                if (elapsed < frameTime)
                    Thread.Sleep(frameTime - elapsed);
            }
        }

        /// <summary>
        /// Inicializace plošin a míčku
        /// </summary>
        static void Initialize()
        {
            platforms = new List<Platform>();

            // Vytvoření plošin ve vertikálních intervalech
            for (short i = 0; i < platformCount; i++)
            {
                int y = height - 1 - i * (height / platformCount);
                int x = random.Next(0, Math.Max(1, width - platformWidth));
                platforms.Add(new Platform(x, y));
            }

            // Umístění míčku nad první plošinu
            var platform0 = platforms[0];
            ball = new Ball(platform0.X + platformWidth / 2, platform0.Y - 1);
        }

        /// <summary>
        /// Aktualizace herní logiky
        /// </summary>
        static void Update()
        {
            ball.PreviousY = ball.Y; // Uložení předchozí pozice

            // Ovládání pomocí šipek
            if (GetAsyncKeyState(VK_LEFT) < 0) ball.X -= horizontalSpeed;   // Vlevo
            if (GetAsyncKeyState(VK_RIGHT) < 0) ball.X += horizontalSpeed;  // Vpravo

            // Aplikace gravitace
            ball.VY += gravity;
            ball.Y += ball.VY;

            // Detekce kolize s plošinami (pouze při pádu)
            if (ball.VY > 0)
            {
                foreach (var platform in platforms)
                {
                    // Kontrola, zda míček dopadl na plošinu
                    if (ball.PreviousY < platform.Y && ball.Y >= platform.Y &&
                        ball.X >= platform.X && ball.X <= platform.X + platformWidth)
                    {
                        ball.VY = jumpVelocity; // Skok
                        ball.Y = platform.Y - 1;
                        break;
                    }
                }
            }

            // Scrolling - pokud se míček dostane do horní třetiny, posuň kameru
            if (ball.Y < height / 3.0)
            {
                double shift = height / 3.0 - ball.Y;
                ball.Y = height / 3.0;
                scoreDistance += shift; // Zvýšení skóre

                // Posun všech plošin dolů
                for (short i = 0; i < platforms.Count; i++)
                {
                    platforms[i].Y += shift;

                    // Pokud plošina spadne ze spodku, vytvoř novou nahoře
                    if (platforms[i].Y > height - 1)
                    {
                        platforms[i].Y = 0;
                        platforms[i].X = random.Next(0, Math.Max(1, width - platformWidth));
                    }
                }
            }

            // Game Over - míček spadl ze spodku
            if (ball.Y > height - 1)
            {
                Console.Clear();
                Console.SetCursorPosition(Math.Max(0, width / 2 - 5), Math.Max(0, height / 2));
                Console.Write("Game Over!");
                Thread.Sleep(2000);
                return;
            }

            // Omezení míčku na šířku obrazovky
            ball.X = Math.Max(0, Math.Min(ball.X, width - 1));
        }

        /// <summary>
        /// Vykreslení hry do bufferu a na obrazovku
        /// </summary>
        static void Render()
        {
            // Vymazání bufferu
            for (short y = 0; y < height; y++)
                for (short x = 0; x < width; x++)
                    screenBuffer[y, x] = ' ';

            // Vykreslení plošin
            foreach (var platform in platforms)
            {
                int px = (int)Math.Round(platform.X);
                int py = (int)Math.Round(platform.Y);

                if (py >= 0 && py < height)
                    for (int i = 0; i < platformWidth && px + i < width; i++)
                        screenBuffer[py, px + i] = '=';
            }

            // Vykreslení míčku
            int ballX = (int)Math.Round(ball.X);
            int ballY = (int)Math.Round(ball.Y);
            if (ballX >= 0 && ballX < width && ballY >= 0 && ballY < height)
                screenBuffer[ballY, ballX] = 'O';

            // Vykreslení skóre
            var scoreText = $"Score: {(int)scoreDistance}";
            for (short i = 0; i < scoreText.Length && i < width; i++)
                screenBuffer[0, i] = scoreText[i];

            // Výpis bufferu na obrazovku
            for (short y = 0; y < height; y++)
            {
                Console.SetCursorPosition(0, y);
                for (short x = 0; x < width; x++)
                    Console.Write(screenBuffer[y, x]);
            }
        }
    }

    /// <summary>
    /// Klasická hra Snake s různými rychlostmi pohybu
    /// </summary>
    static class SnakeGame
    {
        // Rozměry herního pole
        static int width = 40;
        static int height = 20;

        // Pozice hada a jídla
        static int snakeX, snakeY;
        static int foodX, foodY;

        // Směr pohybu hada
        static int directionX = 1, directionY = 0;

        // Tělo hada jako seznam pozic
        static List<(int x, int y)> snakeBody = new List<(int x, int y)>();

        // Herní stav
        static int score = 0;
        static bool gameOver = false;
        static Random random = new Random();

        // Rychlosti pohybu (horizontální rychlejší než vertikální)
        static int horizontalSpeed = 120;  // Rychlejší pro vlevo/vpravo
        static int verticalSpeed = 170;    // Pomalejší pro nahoru/dolů
        static int currentSpeed = 100;     // Aktuální rychlost

        /// <summary>
        /// Spuštění Snake hry
        /// </summary>
        public static void Start()
        {
            // Inicializace konzole
            Console.CursorVisible = false;
            Console.Clear();

            // Inicializace hada na středu pole
            int initialLength = 5;
            snakeX = width / 2;
            snakeY = height / 2;
            snakeBody.Clear();

            // Vytvoří initialLength segmentů v horizontálním směru vlevo od středu
            for (int i = 0; i < initialLength; i++)
            {
                // každý nový segment se přidá o i políček vlevo od hlavy
                snakeBody.Add((snakeX - i, snakeY));
            }

            CreateFood(); // Vytvoření prvního jídla

            // Spuštění threadu pro sledování kláves
            Thread inputThread = new Thread(WatchKeys);
            inputThread.IsBackground = true;
            inputThread.Start();

            gameOver = false;

            // Hlavní herní smyčka
            while (!gameOver)
            {
                var start = DateTime.Now;
                DrawGame();      // Vykreslení
                MoveSnake();     // Pohyb hada
                CheckCollision(); // Kontrola kolizí

                // Čekání podle aktuální rychlosti
                int waitTime = currentSpeed - (int)(DateTime.Now - start).TotalMilliseconds;
                if (waitTime > 0) Thread.Sleep(waitTime);
            }

            // Zobrazení konce hry
            Console.SetCursorPosition(0, height + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Game over. Press any key...");
            Console.ReadKey();

            // Reset herních proměnných pro další hru
            score = 0;
            directionX = 1;
            directionY = 0;
            currentSpeed = horizontalSpeed;
        }

        /// <summary>
        /// Vytvoření nového jídla na náhodné pozici
        /// </summary>
        static void CreateFood()
        {
            do
            {
                // Generování náhodné pozice
                foodX = random.Next(1, width - 1);
                foodY = random.Next(1, height - 1);
            }
            // Opakovat dokud jídlo není na těle hada
            while (snakeBody.Exists(segment => segment.x == foodX && segment.y == foodY));
        }

        /// <summary>
        /// Vykreslení herního pole s hadem, jídlem a UI
        /// </summary>
        static void DrawGame()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;

            // Zobrazení skóre a rychlosti
            Console.WriteLine($"Snake - Score: {score}");

            // Horní ohraničení
            Console.Write("╔");
            for (int x = 0; x < width; x++) Console.Write("═");
            Console.WriteLine("╗");

            // Vykreslení herního pole
            for (byte y = 0; y < height; y++)
            {
                Console.Write("║"); // Levé ohraničení

                for (byte x = 0; x < width; x++)
                {
                    // Kontrola, zda je na pozici část hada
                    if (snakeBody.Exists(segment => segment.x == x && segment.y == y))
                    {
                        // Hlava hada je žlutá, tělo zelené
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
                    // Kontrola, zda je na pozici jídlo
                    else if (x == foodX && y == foodY)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("●");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        Console.Write(" "); // Prázdné místo
                    }
                }

                Console.WriteLine("║"); // Pravé ohraničení
            }

            // Dolní ohraničení
            Console.Write("╚");
            for (byte x = 0; x < width; x++) Console.Write("═");
            Console.WriteLine("╝");

            // Instrukce pro hráče
            Console.WriteLine("Controls: arrows or WASD, ESC = exit");
            Console.WriteLine("Horizontal movement is faster than vertical");
        }

        /// <summary>
        /// Thread pro sledování stisknutých kláves
        /// </summary>
        static void WatchKeys()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        // Pohyb nahoru (nelze jít opačným směrem)
                        if (directionY != 1)
                        {
                            directionX = 0; directionY = -1;
                            currentSpeed = verticalSpeed; // Pomalejší rychlost
                        }
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        // Pohyb dolů
                        if (directionY != -1)
                        {
                            directionX = 0; directionY = 1;
                            currentSpeed = verticalSpeed; // Pomalejší rychlost
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        // Pohyb vlevo
                        if (directionX != 1)
                        {
                            directionX = -1; directionY = 0;
                            currentSpeed = horizontalSpeed; // Rychlejší rychlost
                        }
                        break;

                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        // Pohyb vpravo
                        if (directionX != -1)
                        {
                            directionX = 1; directionY = 0;
                            currentSpeed = horizontalSpeed; // Rychlejší rychlost
                        }
                        break;

                    case ConsoleKey.Escape:
                        gameOver = true; // Ukončení hry
                        break;
                }
            }
        }

        /// <summary>
        /// Pohyb hada podle aktuálního směru
        /// </summary>
        static void MoveSnake()
        {
            // Výpočet nové pozice hlavy
            int newX = snakeBody[0].x + directionX;
            int newY = snakeBody[0].y + directionY;

            // Přidání nové hlavy na začátek seznamu
            snakeBody.Insert(0, (newX, newY));

            // Kontrola, zda had snědl jídlo
            if (newX == foodX && newY == foodY)
            {
                score++;           // Zvýšení skóre
                CreateFood();      // Vytvoření nového jídla
                // Had se prodlouží (neodstraňujeme ocas)
            }
            else
            {
                // Had se neprodlouží - odstranění ocasu
                snakeBody.RemoveAt(snakeBody.Count - 1);
            }
        }

        /// <summary>
        /// Kontrola kolizí se stěnami a vlastním tělem
        /// </summary>
        static void CheckCollision()
        {
            var head = snakeBody[0]; // Pozice hlavy

            // Kolize se stěnami
            if (head.x < 0 || head.x >= width || head.y < 0 || head.y >= height)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("💥 Hit the wall!");
                Console.WriteLine($"Your score: {score}");
                gameOver = true;
                return;
            }

            // Kolize s vlastním tělem
            for (byte i = 1; i < snakeBody.Count; i++)
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

    /// <summary>
    /// Hra Bomb Dodger - vyhýbání se padającím bombám a kamenům
    /// </summary>
    static class BombDodger
    {
        // Konstanty pro rozměry a znaky
        const int Width = 40;
        const int Height = 20;
        const char BorderChar = '#';      // Znak ohraničení
        const char PlayerChar = '@';      // Znak hráče
        const char BombChar = 'B';        // Znak bomby
        const char RockChar = 'K';        // Znak kamene
        const char ExplosionChar = '*';   // Znak exploze

        // Herní proměnné
        static short playerX = Width / 2;   // Pozice hráče X
        static short playerY = Height - 2;  // Pozice hráče Y
        static short score = 0;             // Skóre hráče
        static int timeAlive = 0;         // Čas přežití
        static bool gameOver = false;     // Stav ukončení hry

        // Seznam herních objektů
        static List<Entity> bombs = new List<Entity>();        // Seznam bomb
        static List<Entity> rocks = new List<Entity>();        // Seznam kamenů
        static List<Explosion> explosions = new List<Explosion>(); // Seznam explozí

        static Random random = new Random(); // Generátor náhodných čísel

        /// <summary>
        /// Spuštění Bomb Dodger hry
        /// </summary>
        public static void Start()
        {
            // Inicializace konzole
            Console.CursorVisible = false;
            Console.Clear();

            // Reset herních proměnných
            playerX = Width / 2;
            playerY = Height - 2;
            score = 0;
            timeAlive = 0;
            gameOver = false;
            bombs.Clear();
            rocks.Clear();
            explosions.Clear();

            // Spuštění threadu pro ovládání
            Thread inputThread = new Thread(HandleInput);
            inputThread.IsBackground = true;
            inputThread.Start();

            // Hlavní herní smyčka
            while (!gameOver)
            {
                var start = DateTime.Now;

                SpawnEntities();   // Generování nových objektů
                UpdateEntities();  // Aktualizace pozic objektů
                UpdateExplosions(); // Aktualizace explozí
                UpdateScore();     // Aktualizace skóre

                // Kontrola kolizí a ukončení hry
                if (CheckCollision())
                {
                    GameOver();
                    break;
                }

                Draw(); // Vykreslení

                // Čekání do konce frame time
                int elapsed = (int)(DateTime.Now - start).TotalMilliseconds;
                int sleepTime = 100 - elapsed;
                if (sleepTime > 0) Thread.Sleep(sleepTime);
            }

            // Zobrazení konce hry
            Console.SetCursorPosition(0, Height + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Game over. Press any key...");
            Console.ReadKey();
        }

        /// <summary>
        /// Thread pro zpracování vstupu od hráče
        /// </summary>
        static void HandleInput()
        {
            while (!gameOver)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    MovePlayer(key);
                }
                Thread.Sleep(50); // Krátká pauza pro thread
            }
        }

        /// <summary>
        /// Pohyb hráče podle stisknuté klávesy
        /// </summary>
        /// <param name="key">Stisknutá klávesa</param>
        static void MovePlayer(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    if (playerX > 1) playerX--; // Pohyb vlevo (s kontrolou hranic)
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    if (playerX < Width - 2) playerX++; // Pohyb vpravo
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    if (playerY > 1) playerY--; // Pohyb nahoru
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    if (playerY < Height - 2) playerY++; // Pohyb dolů
                    break;
                case ConsoleKey.Escape:
                    gameOver = true; // Ukončení hry
                    break;
            }
        }

        /// <summary>
        /// Generování nových bomb a kamenů
        /// </summary>
        static void SpawnEntities()
        {
            // Pravděpodobnost generování se zvyšuje s časem
            double bombChance = 0.18 + (timeAlive / 1000.0) * 0.18;  // Bomby
            double rockChance = 0.1 + (timeAlive / 1000.0) * 0.1; // Kameny

            // Generování bomby
            if (random.NextDouble() < bombChance)
            {
                int x = random.Next(1, Width - 1);     // Náhodná X pozice
                int dx = random.Next(-1, 2);           // Náhodný horizontální směr (-1, 0, 1)
                bombs.Add(new Entity(x, 1, dx, 1, BombChar));
            }

            // Generování kamene
            if (random.NextDouble() < rockChance)
            {
                int x = random.Next(1, Width - 1);
                int dx = random.Next(-1, 2);
                rocks.Add(new Entity(x, 1, dx, 1, RockChar));
            }
        }

        /// <summary>
        /// Aktualizace pozic všech objektů
        /// </summary>
        static void UpdateEntities()
        {
            List<Entity> toExplode = new List<Entity>(); // Seznam bomb k explozi

            // Aktualizace bomb
            foreach (var bomb in bombs.ToList())
            {
                bomb.Move(); // Pohyb bomby

                // Pokud bomba dosáhla spodku, přidej k explozi
                if (bomb.Y >= Height - 1)
                {
                    toExplode.Add(bomb);
                    bombs.Remove(bomb);
                }
            }

            // Aktualizace kamenů
            foreach (var rock in rocks.ToList())
            {
                rock.Move(); // Pohyb kamene

                // Odstranění kamene pokud opustil hranice
                if (rock.Y >= Height - 1 || rock.X <= 0 || rock.X >= Width - 1)
                    rocks.Remove(rock);
            }

            // Vytvoření explozí z bomb
            foreach (var bomb in toExplode)
            {
                explosions.Add(new Explosion(bomb.X, bomb.Y));
            }
        }

        /// <summary>
        /// Aktualizace explozí - odstranění starých
        /// </summary>
        static void UpdateExplosions()
        {
            // Odstranění explozí starších než 500ms
            explosions.RemoveAll(explosion => (DateTime.Now - explosion.StartTime).TotalMilliseconds > 500);
        }

        /// <summary>
        /// Aktualizace skóre podle času přežití
        /// </summary>
        static void UpdateScore()
        {
            timeAlive++;
            // Každých 10 ticků přidej bod
            if (timeAlive % 10 == 0) score++;
        }

        /// <summary>
        /// Kontrola kolizí hráče s objekty
        /// </summary>
        /// <returns>True pokud došlo ke kolizi</returns>
        static bool CheckCollision()
        {
            // Kolize s bombami
            foreach (var bomb in bombs)
                if (bomb.X == playerX && bomb.Y == playerY)
                    return true;

            // Kolize s kameny
            foreach (var rock in rocks)
                if (rock.X == playerX && rock.Y == playerY)
                    return true;

            // Kolize s explozemi
            foreach (var explosion in explosions)
                if (explosion.X == playerX && explosion.Y == playerY)
                    return true;

            return false; // Žádná kolize
        }

        /// <summary>
        /// Vykreslení celé herní obrazovky
        /// </summary>
        static void Draw()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;

            // Zobrazení skóre a času
            Console.WriteLine($"Bomb Dodger - Score: {score} | Time: {timeAlive / 10}s");

            // Vykreslení herního pole
            for (short y = 0; y < Height; y++)
            {
                for (short x = 0; x < Width; x++)
                {
                    // Vykreslení ohraničení
                    if (y == 0 || y == Height - 1 || x == 0 || x == Width - 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(BorderChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    // Vykreslení hráče
                    if (x == playerX && y == playerY)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(PlayerChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    // Vykreslení bomby
                    var bomb = bombs.FirstOrDefault(b => b.X == x && b.Y == y);
                    if (bomb != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(BombChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    // Vykreslení kamene
                    var rock = rocks.FirstOrDefault(r => r.X == x && r.Y == y);
                    if (rock != null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(RockChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    // Vykreslení exploze
                    var explosion = explosions.FirstOrDefault(e => e.X == x && e.Y == y);
                    if (explosion != null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(ExplosionChar);
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    Console.Write(' '); // Prázdné místo
                }
                Console.WriteLine();
            }

            // Instrukce pro hráče
            Console.WriteLine("Controls: arrows or WASD, ESC = exit");
            Console.WriteLine("Avoid bombs (B), rocks (K) and explosions (*)!");
        }

        /// <summary>
        /// Zobrazení Game Over obrazovky
        /// </summary>
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

        /// <summary>
        /// Třída pro herní entity (bomby a kameny)
        /// </summary>
        class Entity
        {
            public int X, Y;           // Pozice entity
            int deltaX, deltaY;        // Rychlost pohybu
            char symbol;               // Symbol pro vykreslení

            /// <summary>
            /// Konstruktor entity
            /// </summary>
            /// <param name="x">Počáteční X pozice</param>
            /// <param name="y">Počáteční Y pozice</param>
            /// <param name="deltaX">Rychlost pohybu X</param>
            /// <param name="deltaY">Rychlost pohybu Y</param>
            /// <param name="symbol">Symbol pro vykreslení</param>
            public Entity(int x, int y, int deltaX, int deltaY, char symbol)
            {
                X = x;
                Y = y;
                this.deltaX = deltaX;
                this.deltaY = deltaY;
                this.symbol = symbol;
            }

            /// <summary>
            /// Pohyb entity podle její rychlosti
            /// </summary>
            public void Move()
            {
                X += deltaX;
                Y += deltaY;
            }

            /// <summary>
            /// Vlastnost pro získání symbolu entity
            /// </summary>
            public char Symbol => symbol;
        }

        /// <summary>
        /// Třída pro exploze
        /// </summary>
        class Explosion
        {
            public int X, Y;                    // Pozice exploze
            public DateTime StartTime;          // Čas vzniku exploze

            /// <summary>
            /// Konstruktor exploze
            /// </summary>
            /// <param name="x">X pozice exploze</param>
            /// <param name="y">Y pozice exploze</param>
            public Explosion(int x, int y)
            {
                X = x;
                Y = y;
                StartTime = DateTime.Now; // Uložení času vzniku
            }
        }
    }

    /// <summary>
    /// Třída reprezentující plošinu ve skákací hře
    /// </summary>
    class Platform
    {
        public double X, Y; // Pozice plošiny

        /// <summary>
        /// Konstruktor plošiny
        /// </summary>
        /// <param name="x">X pozice</param>
        /// <param name="y">Y pozice</param>
        public Platform(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Třída reprezentující míček ve skákací hře
    /// </summary>
    class Ball
    {
        public double X, Y;        // Pozice míčku
        public double VY;          // Vertikální rychlost
        public double PreviousY;   // Předchozí Y pozice (pro detekci kolize)

        /// <summary>
        /// Konstruktor míčku
        /// </summary>
        /// <param name="x">Počáteční X pozice</param>
        /// <param name="y">Počáteční Y pozice</param>
        public Ball(double x, double y)
        {
            X = x;
            Y = y;
            VY = 0; // Žádná počáteční vertikální rychlost
        }
    }
}
