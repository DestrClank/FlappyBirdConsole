using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FlappyBirdConsole
{
    internal class Program
    {
        const int Width = 40;
        const int Height = 20;
        const int BirdX = 10;
        const string BirdStr = "O>";
        const char EmptyChar = ' ';
        const int PipeInterval = 20;
        const int PipeHeight = 6;
        const int Gravity = 1;
        const int Jump = -3;
        const string HighScoreFile = "highscores.txt";
        const int LeaderboardSize = 5;

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            bool quit = false;
            string lastPlayerName = "Player";
            while (!quit)
            {
                var leaderboard = LoadLeaderboard();
                quit = !ShowStartScreen(leaderboard);
                if (quit)
                    break;

                int birdY = Height / 2;
                int velocity = -2;
                int score = 0;
                bool alive = true;
                Random rnd = new Random();

                List<(int x, int gapY)> pipes = new List<(int, int)>();
                int pipeCounter = 0;

                // Small pause before the game starts to let the player react
                Console.SetCursorPosition(BirdX, birdY);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(BirdStr);
                Console.ResetColor();
                Thread.Sleep(500);

                while (alive)
                {
                    // Input handling
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Spacebar)
                            velocity = Jump;
                    }

                    // Gravity
                    velocity += Gravity;
                    birdY += velocity;
                    if (birdY < 0) birdY = 0;
                    if (birdY >= Height) alive = false;

                    // Pipe generation
                    pipeCounter++;
                    if (pipeCounter >= PipeInterval)
                    {
                        pipeCounter = 0;
                        int gapY = rnd.Next(2, Height - PipeHeight - 2);
                        pipes.Add((Width - 1, gapY));
                    }

                    // Move pipes
                    for (int i = 0; i < pipes.Count; i++)
                        pipes[i] = (pipes[i].x - 1, pipes[i].gapY);

                    // Remove pipes off screen
                    if (pipes.Count > 0 && pipes[0].x < 0)
                    {
                        pipes.RemoveAt(0);
                        score++;
                    }

                    // Collision detection (bird is 2 chars wide)
                    foreach (var p in pipes)
                    {
                        if ((p.x == BirdX || p.x == BirdX + 1))
                        {
                            if (birdY < p.gapY || birdY > p.gapY + PipeHeight)
                                alive = false;
                        }
                    }

                    // Drawing
                    Console.SetCursorPosition(0, 0);
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            // Draw bird (2 chars wide)
                            if (x == BirdX && y == birdY)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(BirdStr);
                                Console.ResetColor();
                                x++; // Skip next cell (bird is 2 chars)
                                continue;
                            }

                            // Draw pipes (ASCII art)
                            bool isPipe = false;
                            foreach (var p in pipes)
                            {
                                if (x == p.x)
                                {
                                    if (y == p.gapY - 1)
                                    {
                                        // Top of lower pipe
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.Write("╦");
                                        Console.ResetColor();
                                        isPipe = true;
                                        break;
                                    }
                                    else if (y == p.gapY + PipeHeight + 1)
                                    {
                                        // Bottom of upper pipe
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.Write("╩");
                                        Console.ResetColor();
                                        isPipe = true;
                                        break;
                                    }
                                    else if (y < p.gapY || y > p.gapY + PipeHeight)
                                    {
                                        // Pipe body
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.Write("║");
                                        Console.ResetColor();
                                        isPipe = true;
                                        break;
                                    }
                                }
                            }
                            if (!isPipe)
                                Console.Write(EmptyChar);
                        }
                        Console.WriteLine();
                    }
                    Console.WriteLine($"Score : {score}");

                    Thread.Sleep(50);
                }

                // Leaderboard update
                leaderboard = LoadLeaderboard();
                if (IsHighScore(score, leaderboard))
                {
                    Console.SetCursorPosition(0, Height + 3);
                    Console.Write($"New High Score! Enter your name (max 10 chars) [{lastPlayerName}]: ");
                    string name = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(name)) name = lastPlayerName;
                    if (name.Length > 10) name = name.Substring(0, 10);
                    lastPlayerName = name;

                    // Update or add the score for this name
                    int existingIndex = leaderboard.FindIndex(e => e.name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (existingIndex >= 0)
                    {
                        if (score > leaderboard[existingIndex].score)
                            leaderboard[existingIndex] = (name, score); // Update only if new score is better
                    }
                    else
                    {
                        leaderboard.Add((name, score));
                    }
                    leaderboard = leaderboard.OrderByDescending(s => s.score).Take(LeaderboardSize).ToList();
                    SaveLeaderboard(leaderboard);
                }

                ShowLeaderboard(leaderboard, Height + 4);

                Console.SetCursorPosition(0, Height + 10);
                Console.WriteLine("Game Over! Press R to retry.");
                var keyPress = Console.ReadKey(true);
                Console.Clear();
            }
        }

        static bool ShowStartScreen(List<(string name, int score)> leaderboard)
        {
            while (true)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("=== FLAPPY BIRD CONSOLE ===\n");
                Console.WriteLine("Controls:");
                Console.WriteLine("  - Press SPACE to make the bird jump.");
                Console.WriteLine("  - Avoid the pipes!");
                Console.WriteLine("\nLeaderboard:");
                ShowLeaderboard(leaderboard, Console.CursorTop);
                Console.WriteLine("\nPress ENTER or SPACE to play, D to reset leaderboard, or ESC to quit...");

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
                {
                    Console.Clear();
                    return true;
                }
                if (key.Key == ConsoleKey.Escape)
                {
                    return false;
                }
                if (key.Key == ConsoleKey.D)
                {
                    // Reset leaderboard
                    if (File.Exists(HighScoreFile))
                        File.Delete(HighScoreFile);
                    leaderboard = new List<(string, int)>();
                    Console.Clear();
                    Console.WriteLine("Leaderboard has been reset!");
                    Thread.Sleep(1000);
                }
            }
        }

        static void ShowLeaderboard(List<(string name, int score)> leaderboard, int top)
        {
            Console.SetCursorPosition(0, top);
            if (leaderboard.Count == 0)
            {
                Console.WriteLine("  No high scores yet!");
            }
            else
            {
                int rank = 1;
                foreach (var entry in leaderboard)
                {
                    Console.WriteLine($"  {rank}. {entry.name.PadRight(10)} {entry.score}");
                    rank++;
                }
            }
        }

        static bool IsHighScore(int score, List<(string name, int score)> leaderboard)
        {
            if (score <= 0) return false;
            if (leaderboard.Count < LeaderboardSize) return true;
            return score > leaderboard.Min(s => s.score);
        }

        static List<(string name, int score)> LoadLeaderboard()
        {
            var list = new List<(string, int)>();
            if (!File.Exists(HighScoreFile))
                return list;
            foreach (var line in File.ReadAllLines(HighScoreFile))
            {
                var parts = line.Split(';');
                if (parts.Length == 2 && int.TryParse(parts[1], out int s))
                    list.Add((parts[0], s));
            }
            return list.OrderByDescending(s => s.Item2).Take(LeaderboardSize).ToList();
        }

        static void SaveLeaderboard(List<(string name, int score)> leaderboard)
        {
            File.WriteAllLines(HighScoreFile, leaderboard.Select(e => $"{e.name};{e.score}"));
        }
    }
}