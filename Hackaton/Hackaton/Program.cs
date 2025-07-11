using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace HerniProjekt
{
    class HerniMenu
    {
        static void Main(string[] args)
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine("=== HERNÍ MENU ===");
                Console.WriteLine("1. Policajti a cíl");
                Console.WriteLine("2. Skákací plošinovka");
                Console.WriteLine("3. Snake");
                Console.WriteLine("0. Konec");
                Console.Write("Zadej volbu: ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        HraPolicajti.Start();
                        break;
                    case "2":
                        SkakaciHra.Start();
                        break;
                    case "3":
                        SnakeGame.Start();
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Neplatná volba. Stiskni libovolnou klávesu...");
                        Console.ReadKey();
                        break;
                }
            }
        }
    }

    class HraPolicajti
    {
        public static void Start()
        {
            Console.CursorVisible = false;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Thread vstupThread = new Thread(SledujKlavesy);
            vstupThread.IsBackground = true;
            vstupThread.Start();

            NactiLevel(aktualniLevel);

            int interval = 120;
            int tick = 0;

            konec = false;
            while (!konec)
            {
                var start = DateTime.Now;
                VykresliMapu();
                if (tick % 2 == 0)
                {
                    PohniHrace();
                }
                PohniPolicisty();
                if (ZkontrolujKolize()) break;
                tick++;
                int cekani = interval - (int)(DateTime.Now - start).TotalMilliseconds;
                if (cekani > 0) Thread.Sleep(cekani);
            }

            Console.SetCursorPosition(0, vyska + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Konec hry. Stiskni libovolnou klávesu...");
            Console.ReadKey();
        }

        static string[][] levely = new string[][]
        {
            new string[]
            {
                "##############################",
                "#Z   #     #####      #     X#",
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
                "#Z    #     ###     ###     X#",
                "# ### # ### ### ### ### #####",
                "# #     #   #   #   #       #",
                "# # ### # ##### ##### ##### #",
                "#     #       ##     #   #",
                "### ####### # #####   # # ###",
                "#         # #         #     #",
                "##############################"
            }
        };

        static int aktualniLevel = 0;
        static char[,] mapa;
        static int sirka, vyska;
        static int hracX, hracY;
        static int cilX, cilY;
        static List<(int x, int y, int smer)> policajti = new List<(int x, int y, int smer)>();
        static Random rnd = new Random();

        static bool drzimW = false, drzimS = false, drzimA = false, drzimD = false;
        static bool konec = false;

        static void NactiLevel(int index)
        {
            var radky = levely[index];
            vyska = radky.Length;
            sirka = radky[0].Length;
            mapa = new char[vyska, sirka];
            policajti.Clear();

            for (int y = 0; y < vyska; y++)
            {
                for (int x = 0; x < sirka; x++)
                {
                    char znak = radky[y][x];
                    mapa[y, x] = znak;
                    if (znak == 'Z') { hracX = x; hracY = y; mapa[y, x] = ' '; }
                    if (znak == 'X') { cilX = x; cilY = y; }
                }
            }

            policajti.Add((5, 2, 1));
            policajti.Add((10, 5, -1));
            policajti.Add((20, 3, 1));
        }

        static void VykresliMapu()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Level {aktualniLevel + 1}");

            for (int y = 0; y < vyska; y++)
            {
                for (int x = 0; x < sirka; x++)
                {
                    if (x == hracX && y == hracY)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (x == cilX && y == cilY)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else if (policajti.Exists(p => p.x == x && p.y == y))
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    else
                        Console.ForegroundColor = ConsoleColor.White;

                    if (x == hracX && y == hracY) Console.Write("Z");
                    else if (x == cilX && y == cilY) Console.Write("X");
                    else if (policajti.Exists(p => p.x == x && p.y == y)) Console.Write("P");
                    else Console.Write(mapa[y, x]);
                }
                Console.WriteLine();
            }
        }

        static void SledujKlavesy()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.W) drzimW = true;
                if (key == ConsoleKey.S) drzimS = true;
                if (key == ConsoleKey.A) drzimA = true;
                if (key == ConsoleKey.D) drzimD = true;
                if (key == ConsoleKey.Escape) konec = true;
            }
        }

        static void PohniHrace()
        {
            int novaX = hracX, novaY = hracY;
            if (drzimW) novaY--;
            else if (drzimS) novaY++;
            else if (drzimA) novaX--;
            else if (drzimD) novaX++;

            if (JeVolne(novaX, novaY))
            {
                hracX = novaX;
                hracY = novaY;
            }

            drzimW = drzimS = drzimA = drzimD = false;
        }

        static void PohniPolicisty()
        {
            for (int i = 0; i < policajti.Count; i++)
            {
                int x = policajti[i].x;
                int y = policajti[i].y;
                int smer = policajti[i].smer;
                int novaX = x + smer;

                if (JeVolne(novaX, y))
                {
                    policajti[i] = (novaX, y, smer);
                }
                else
                {
                    smer *= -1;
                    novaX = x + smer;
                    if (JeVolne(novaX, y)) policajti[i] = (novaX, y, smer);
                    else policajti[i] = (x, y, smer);
                }
            }
        }

        static bool JeVolne(int x, int y)
        {
            return x >= 0 && x < sirka && y >= 0 && y < vyska && mapa[y, x] != '#';
        }

        static bool ZkontrolujKolize()
        {
            foreach (var p in policajti)
                if (p.x == hracX && p.y == hracY)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\u274C Policie tě chytila!");
                    return true;
                }

            if (hracX == cilX && hracY == cilY)
            {
                aktualniLevel++;
                if (aktualniLevel < levely.Length)
                {
                    NactiLevel(aktualniLevel);
                    return false;
                }
                else
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\ud83c\udfc6 Vyhrál jsi všechny levely!");
                    return true;
                }
            }
            return false;
        }
    }

    static class SkakaciHra
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
        static Random rand = new Random();
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

            var sw = new Stopwatch();
            while (true)
            {
                sw.Restart();
                Update();
                Render();
                int elapsed = (int)sw.ElapsedMilliseconds;
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
                int x = rand.Next(0, Math.Max(1, width - platformWidth));
                platforms.Add(new Platform(x, y));
            }
            var p0 = platforms[0];
            ball = new Ball(p0.X + platformWidth / 2, p0.Y - 1);
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
                foreach (var p in platforms)
                {
                    if (ball.PreviousY < p.Y && ball.Y >= p.Y && ball.X >= p.X && ball.X <= p.X + platformWidth)
                    {
                        ball.VY = jumpVelocity;
                        ball.Y = p.Y - 1;
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
                        platforms[i].X = rand.Next(0, Math.Max(1, width - platformWidth));
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

            foreach (var p in platforms)
            {
                int px = (int)Math.Round(p.X);
                int py = (int)Math.Round(p.Y);
                if (py >= 0 && py < height)
                    for (int i = 0; i < platformWidth && px + i < width; i++)
                        screenBuffer[py, px + i] = '=';
            }

            int bx = (int)Math.Round(ball.X);
            int by = (int)Math.Round(ball.Y);
            if (bx >= 0 && bx < width && by >= 0 && by < height)
                screenBuffer[by, bx] = 'O';

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

    // ===== NOVÁ TŘÍDA - SNAKE HRA =====
    static class SnakeGame
    {
        static int sirka = 40;
        static int vyska = 20;
        static int hadX, hadY;
        static int jidloX, jidloY;
        static int smerX = 1, smerY = 0;
        static List<(int x, int y)> hadTelo = new List<(int x, int y)>();
        static int skore = 0;
        static bool konec = false;
        static Random rnd = new Random();

        // Rychlosti pro různé směry
        static int horizontalniRychlost = 100;  // Rychlejší pro vlevo/vpravo
        static int vertikalniRychlost = 200;    // Pomalejší pro nahoru/dolů
        static int aktualniRychlost = 100;

        public static void Start()
        {
            Console.CursorVisible = false;
            Console.Clear();

            // Inicializace hada
            hadX = sirka / 2;
            hadY = vyska / 2;
            hadTelo.Clear();
            hadTelo.Add((hadX, hadY));

            // Vytvoření prvního jídla
            VytvorJidlo();

            // Hlavní herní smyčka
            Thread vstupThread = new Thread(SledujKlavesy);
            vstupThread.IsBackground = true;
            vstupThread.Start();

            konec = false;
            while (!konec)
            {
                var start = DateTime.Now;
                VykresliHru();
                PohniHada();
                ZkontrolujKolize();

                // Čekání podle aktuální rychlosti
                int cekani = aktualniRychlost - (int)(DateTime.Now - start).TotalMilliseconds;
                if (cekani > 0) Thread.Sleep(cekani);
            }

            Console.SetCursorPosition(0, vyska + 3);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Konec hry. Stiskni libovolnou klávesu...");
            Console.ReadKey();

            // Reset pro další hru
            skore = 0;
            smerX = 1;
            smerY = 0;
            aktualniRychlost = horizontalniRychlost;
        }

        static void VytvorJidlo()
        {
            do
            {
                jidloX = rnd.Next(1, sirka - 1);
                jidloY = rnd.Next(1, vyska - 1);
            } while (hadTelo.Exists(segment => segment.x == jidloX && segment.y == jidloY));
        }

        static void VykresliHru()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Snake - Skóre: {skore} | Rychlost: {(aktualniRychlost == horizontalniRychlost ? "Rychlá" : "Pomalá")}");

            // Horní ohraničení
            Console.Write("╔");
            for (int x = 0; x < sirka; x++) Console.Write("═");
            Console.WriteLine("╗");

            // Herní pole
            for (int y = 0; y < vyska; y++)
            {
                Console.Write("║");
                for (int x = 0; x < sirka; x++)
                {
                    if (hadTelo.Exists(segment => segment.x == x && segment.y == y))
                    {
                        // Hlava hada je žlutá, tělo zelené
                        if (hadTelo[0].x == x && hadTelo[0].y == y)
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
                    else if (x == jidloX && y == jidloY)
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

            // Dolní ohraničení
            Console.Write("╚");
            for (int x = 0; x < sirka; x++) Console.Write("═");
            Console.WriteLine("╝");

            Console.WriteLine("Ovládání: šipky nebo WASD, ESC = konec");
            Console.WriteLine("Horizontální pohyb je rychlejší než vertikální");
        }

        static void SledujKlavesy()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        if (smerY != 1) // Nemůže jít opačným směrem
                        {
                            smerX = 0; smerY = -1;
                            aktualniRychlost = vertikalniRychlost;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        if (smerY != -1)
                        {
                            smerX = 0; smerY = 1;
                            aktualniRychlost = vertikalniRychlost;
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        if (smerX != 1)
                        {
                            smerX = -1; smerY = 0;
                            aktualniRychlost = horizontalniRychlost;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        if (smerX != -1)
                        {
                            smerX = 1; smerY = 0;
                            aktualniRychlost = horizontalniRychlost;
                        }
                        break;
                    case ConsoleKey.Escape:
                        konec = true;
                        break;
                }
            }
        }

        static void PohniHada()
        {
            // Nová pozice hlavy
            int novaX = hadTelo[0].x + smerX;
            int novaY = hadTelo[0].y + smerY;

            // Přidání nové hlavy
            hadTelo.Insert(0, (novaX, novaY));

            // Kontrola jídla
            if (novaX == jidloX && novaY == jidloY)
            {
                skore++;
                VytvorJidlo();
            }
            else
            {
                // Odstranění ocasu (had se neprodlouží)
                hadTelo.RemoveAt(hadTelo.Count - 1);
            }
        }

        static void ZkontrolujKolize()
        {
            var hlava = hadTelo[0];

            // Kolize se stěnami
            if (hlava.x < 0 || hlava.x >= sirka || hlava.y < 0 || hlava.y >= vyska)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("💥 Narazil jsi do stěny!");
                Console.WriteLine($"Tvé skóre: {skore}");
                konec = true;
                return;
            }

            // Kolize se sebou samým
            for (int i = 1; i < hadTelo.Count; i++)
            {
                if (hadTelo[i].x == hlava.x && hadTelo[i].y == hlava.y)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("💥 Had se kousl do ocasu!");
                    Console.WriteLine($"Tvé skóre: {skore}");
                    konec = true;
                    return;
                }
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
