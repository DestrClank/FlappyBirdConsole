using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FlappyBirdNET
{
    public partial class Form1 : Form
    {
        const int WidthPx = 400;
        const int HeightPx = 400;
        const int BirdX = 60;
        const int BirdWidth = 28;
        const int BirdHeight = 20;
        const int PipeWidth = 32;
        const string HighScoreFile = "highscores.txt";
        const int LeaderboardSize = 5;

        // Paramètres dynamiques selon la difficulté
        int pipeGap = 80;
        int pipeInterval = 600;
        int gravity = 1;
        int jump = -8;
        int pipeSpeed = 4;

        enum Difficulty { Easy, Normal, Hard }
        Difficulty currentDifficulty = Difficulty.Normal;

        Timer timer = new Timer();
        int birdY, velocity, score;
        List<(int x, int gapY)> pipes = new List<(int, int)>();
        int pipeCounter = 0;
        bool alive = false;
        bool inGame = false;
        bool showGameOver = false;
        string lastPlayerName = "Player";
        List<(string name, int score)> leaderboard = new List<(string, int)>();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.Width = WidthPx;
            this.Height = HeightPx + 40;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Flappy Bird .NET";
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            timer.Interval = 20;
            timer.Tick += Timer_Tick;
            leaderboard = LoadLeaderboard();
            SetDifficulty(Difficulty.Normal);
            ShowStartScreen();
        }

        void SetDifficulty(Difficulty diff)
        {
            currentDifficulty = diff;
            switch (diff)
            {
                case Difficulty.Easy:
                    pipeGap = 120;
                    pipeInterval = 800;
                    gravity = 1;
                    jump = -10;
                    pipeSpeed = 3;
                    break;
                case Difficulty.Normal:
                    pipeGap = 80;
                    pipeInterval = 600;
                    gravity = 1;
                    jump = -8;
                    pipeSpeed = 4;
                    break;
                case Difficulty.Hard:
                    pipeGap = 60;
                    pipeInterval = 400;
                    gravity = 2;
                    jump = -7;
                    pipeSpeed = 5;
                    break;
            }
        }

        void ShowStartScreen()
        {
            inGame = false;
            alive = false;
            showGameOver = false;
            pipes.Clear();
            score = 0;
            this.Invalidate();
        }

        void StartGame()
        {
            inGame = true;
            alive = true;
            showGameOver = false;
            pipes.Clear();
            score = 0;
            birdY = HeightPx / 2;
            velocity = -4;
            pipeCounter = 0;
            timer.Start();
        }

        void EndGame()
        {
            alive = false;
            timer.Stop();
            showGameOver = true;
            this.Invalidate();
            // Leaderboard update
            leaderboard = LoadLeaderboard();
            if (IsHighScore(score, leaderboard))
            {
                string name = PromptForName($"New High Score! Enter your name (max 10 chars):", lastPlayerName);
                if (string.IsNullOrWhiteSpace(name)) name = lastPlayerName;
                if (name.Length > 10) name = name.Substring(0, 10);
                lastPlayerName = name;
                int existingIndex = leaderboard.FindIndex(e => e.name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    if (score > leaderboard[existingIndex].score)
                        leaderboard[existingIndex] = (name, score);
                }
                else
                {
                    leaderboard.Add((name, score));
                }
                leaderboard = leaderboard.OrderByDescending(s => s.score).Take(LeaderboardSize).ToList();
                SaveLeaderboard(leaderboard);
            }
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            // Gravity
            velocity += gravity;
            birdY += velocity;
            if (birdY < 0) birdY = 0;
            if (birdY + BirdHeight > HeightPx) alive = false;

            // Pipe generation
            pipeCounter += timer.Interval;
            if (pipeCounter >= pipeInterval)
            {
                pipeCounter = 0;
                int gapY = new Random().Next(40, HeightPx - pipeGap - 40);
                pipes.Add((WidthPx, gapY));
            }

            // Move pipes
            for (int i = 0; i < pipes.Count; i++)
                pipes[i] = (pipes[i].x - pipeSpeed, pipes[i].gapY);

            // Remove pipes off screen
            if (pipes.Count > 0 && pipes[0].x + PipeWidth < 0)
            {
                pipes.RemoveAt(0);
                score++;
            }

            // Collision detection
            Rectangle birdRect = new Rectangle(BirdX, birdY, BirdWidth, BirdHeight);
            foreach (var p in pipes)
            {
                Rectangle topPipe = new Rectangle(p.x, 0, PipeWidth, p.gapY);
                Rectangle bottomPipe = new Rectangle(p.x, p.gapY + pipeGap, PipeWidth, HeightPx - (p.gapY + pipeGap));
                if (birdRect.IntersectsWith(topPipe) || birdRect.IntersectsWith(bottomPipe))
                {
                    alive = false;
                }
            }

            if (!alive)
            {
                EndGame();
            }
            this.Invalidate();
        }

        void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!inGame)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    StartGame();
                }
                else if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
                {
                    SetDifficulty(Difficulty.Easy);
                    this.Invalidate();
                }
                else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
                {
                    SetDifficulty(Difficulty.Normal);
                    this.Invalidate();
                }
                else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
                {
                    SetDifficulty(Difficulty.Hard);
                    this.Invalidate();
                }
                else if (e.KeyCode == Keys.D)
                {
                    if (File.Exists(HighScoreFile))
                        File.Delete(HighScoreFile);
                    leaderboard = new List<(string, int)>();
                    MessageBox.Show("Leaderboard has been reset!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Invalidate();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            }
            else if (alive)
            {
                if (e.KeyCode == Keys.Space)
                {
                    velocity = jump;
                }
            }
            else if (showGameOver)
            {
                if (e.KeyCode == Keys.R)
                {
                    StartGame();
                }
                else
                {
                    ShowStartScreen();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.SkyBlue);

            if (!inGame)
            {
                // Start screen
                using (var font = new Font("Arial", 20, FontStyle.Bold))
                    g.DrawString("FLAPPY BIRD .NET", font, Brushes.DarkBlue, 40, 40);
                using (var font = new Font("Arial", 10))
                {
                    g.DrawString("Controls:", font, Brushes.Black, 40, 90);
                    g.DrawString("- SPACE to jump", font, Brushes.Black, 40, 110);
                    g.DrawString("- ENTER or SPACE to play", font, Brushes.Black, 40, 130);
                    g.DrawString("- 1: Easy   2: Normal   3: Hard", font, Brushes.Black, 40, 150);
                    g.DrawString("- D to reset leaderboard", font, Brushes.Black, 40, 170);
                    g.DrawString("- ESC to quit", font, Brushes.Black, 40, 190);
                }
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                {
                    string diffText = "Difficulty: ";
                    switch (currentDifficulty)
                    {
                        case Difficulty.Easy: diffText += "Easy"; break;
                        case Difficulty.Normal: diffText += "Normal"; break;
                        case Difficulty.Hard: diffText += "Hard"; break;
                    }
                    g.DrawString(diffText, font, Brushes.DarkRed, 40, 210);
                }
                // Leaderboard
                using (var font = new Font("Consolas", 11, FontStyle.Bold))
                {
                    g.DrawString("Leaderboard:", font, Brushes.Black, 40, 240);
                    if (leaderboard.Count == 0)
                    {
                        g.DrawString("  No high scores yet!", font, Brushes.Gray, 40, 260);
                    }
                    else
                    {
                        int y = 260;
                        int rank = 1;
                        foreach (var entry in leaderboard)
                        {
                            g.DrawString($"  {rank}. {entry.name.PadRight(10)} {entry.score}", font, Brushes.DarkGreen, 40, y);
                            y += 20;
                            rank++;
                        }
                    }
                }
                return;
            }

            // Draw pipes
            foreach (var p in pipes)
            {
                // Top pipe
                g.FillRectangle(Brushes.Green, p.x, 0, PipeWidth, p.gapY);
                g.FillRectangle(Brushes.DarkGreen, p.x, p.gapY - 10, PipeWidth, 10); // pipe head
                // Bottom pipe
                g.FillRectangle(Brushes.Green, p.x, p.gapY + pipeGap, PipeWidth, HeightPx - (p.gapY + pipeGap));
                g.FillRectangle(Brushes.DarkGreen, p.x, p.gapY + pipeGap, PipeWidth, 10); // pipe head
            }

            // Draw bird
            var birdRect = new Rectangle(BirdX, birdY, BirdWidth, BirdHeight);
            g.FillEllipse(Brushes.Gold, birdRect);
            using (var font = new Font("Consolas", 14, FontStyle.Bold))
                g.DrawString(">", font, Brushes.Orange, BirdX + 16, birdY + 2);

            // Draw score
            using (var font = new Font("Arial", 16, FontStyle.Bold))
                g.DrawString($"Score: {score}", font, Brushes.Black, 10, 10);

            if (!alive && showGameOver)
            {
                using (var font = new Font("Arial", 18, FontStyle.Bold))
                    g.DrawString("GAME OVER!", font, Brushes.Red, 100, 120);
                using (var font = new Font("Arial", 12))
                {
                    g.DrawString("Press R to retry or any other key for menu.", font, Brushes.Black, 60, 160);
                    // Show leaderboard after game over
                    int y = 200;
                    g.DrawString("Leaderboard:", font, Brushes.Black, 40, y);
                    y += 20;
                    if (leaderboard.Count == 0)
                    {
                        g.DrawString("  No high scores yet!", font, Brushes.Gray, 40, y);
                    }
                    else
                    {
                        int rank = 1;
                        foreach (var entry in leaderboard)
                        {
                            g.DrawString($"  {rank}. {entry.name.PadRight(10)} {entry.score}", font, Brushes.DarkGreen, 40, y);
                            y += 20;
                            rank++;
                        }
                    }
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

        // Simple input box for name
        static string PromptForName(string prompt, string defaultName)
        {
            using (Form inputForm = new Form())
            {
                inputForm.Width = 350;
                inputForm.Height = 140;
                inputForm.Text = "High Score";
                Label lbl = new Label() { Left = 10, Top = 10, Text = prompt, Width = 320 };
                TextBox txt = new TextBox() { Left = 10, Top = 40, Width = 300, Text = defaultName };
                Button ok = new Button() { Text = "OK", Left = 220, Width = 90, Top = 70, DialogResult = DialogResult.OK };
                inputForm.Controls.Add(lbl);
                inputForm.Controls.Add(txt);
                inputForm.Controls.Add(ok);
                inputForm.AcceptButton = ok;
                if (inputForm.ShowDialog() == DialogResult.OK)
                    return txt.Text;
                return defaultName;
            }
        }
    }
}