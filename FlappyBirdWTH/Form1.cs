using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FlappyBirdWTH
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
        Button bird;    
        List<(ProgressBar top, ProgressBar bottom)> pipes = new List<(ProgressBar, ProgressBar)>();
        int birdY, velocity, score;
        int pipeCounter = 0;
        bool alive = false;
        bool inGame = false;
        bool showGameOver = false;
        string lastPlayerName = "Player";
        List<(string name, int score)> leaderboard = new List<(string, int)>();
        Random rnd = new Random();

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

            // Bird as a button
            bird = new Button
            {
                Width = BirdWidth,
                Height = BirdHeight,
                Left = BirdX,
                Top = HeightPx / 2,
                Image = ResizeImage(Properties.Resources.bird, BirdWidth-10, BirdHeight-10), // Assurez-vous que l'image existe dans le répertoire de l'application
                ImageAlign = ContentAlignment.MiddleCenter,
                Text = "",
            };
            bird.FlatAppearance.BorderSize = 0;
            bird.Click += (s, e) => { if (inGame && alive) velocity = jump; };
            bird.TabStop = false;
            this.Controls.Add(bird);

            ShowStartScreen();
        }

        private static Image ResizeImage(Image img, int width, int height)
{
    Bitmap bmp = new Bitmap(width, height);
    using (Graphics g = Graphics.FromImage(bmp))
    {
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(img, 0, 0, width, height);
    }
    return bmp;
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
            RemoveAllPipes();
            score = 0;
            birdY = HeightPx / 2;
            velocity = -4;
            bird.Top = birdY;
            bird.Visible = false; // Cacher le bouton sur l'écran de démarrage
            this.Invalidate();
        }

        void StartGame()
        {
            inGame = true;
            alive = true;
            showGameOver = false;
            RemoveAllPipes();
            score = 0;
            birdY = HeightPx / 2;
            velocity = -4;
            pipeCounter = 0;
            bird.Top = birdY;
            bird.Visible = true; // Afficher le bouton pendant la partie
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

        void RemoveAllPipes()
        {
            foreach (var (top, bottom) in pipes)
            {
                this.Controls.Remove(top);
                this.Controls.Remove(bottom);
            }
            pipes.Clear();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            // Gravity
            velocity += gravity;
            birdY += velocity;
            bird.Top = birdY;
            if (birdY < 0) birdY = 0;
            if (birdY + BirdHeight > HeightPx) alive = false;

            // Pipe generation
            pipeCounter += timer.Interval;
            if (pipeCounter >= pipeInterval)
            {
                pipeCounter = 0;
                int gapY = rnd.Next(40, HeightPx - pipeGap - 40);

                // Top pipe
                var top = new ProgressBar
                {
                    Width = PipeWidth,
                    Height = gapY,
                    Left = WidthPx,
                    Top = 0,
                    Value = 99,
                    Maximum = 100,
                    Minimum = 0,
                    ForeColor = Color.Green,
                    Style = ProgressBarStyle.Continuous
                };
                // Bottom pipe
                var bottom = new ProgressBar
                {
                    Width = PipeWidth,
                    Height = HeightPx - (gapY + pipeGap),
                    Left = WidthPx,
                    Top = gapY + pipeGap,
                    Value = 99,
                    Maximum = 100,
                    Minimum = 0,
                    ForeColor = Color.Green,
                    Style = ProgressBarStyle.Continuous
                };
                this.Controls.Add(top);
                this.Controls.Add(bottom);
                top.BringToFront();
                bottom.BringToFront();
                bird.BringToFront();
                pipes.Add((top, bottom));
            }

            // Move pipes
            for (int i = pipes.Count - 1; i >= 0; i--)
            {
                var (top, bottom) = pipes[i];
                top.Left -= pipeSpeed;
                bottom.Left -= pipeSpeed;

                // Remove pipes off screen
                if (top.Left + top.Width < 0)
                {
                    this.Controls.Remove(top);
                    this.Controls.Remove(bottom);
                    pipes.RemoveAt(i);
                    score++;
                }
                else
                {
                    // Collision detection
                    if (bird.Bounds.IntersectsWith(top.Bounds) || bird.Bounds.IntersectsWith(bottom.Bounds))
                    {
                        alive = false;
                    }
                }
            }

            // Bird out of bounds
            if (bird.Top < 0 || bird.Bottom > HeightPx)
            {
                alive = false;
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
                    e.Handled = true; // Empêche le double saut
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
                    e.Handled = true; // Empêche le double saut
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
            // Affichage du score toujours au-dessus des tuyaux
            using (var font = new Font("Arial", 16, FontStyle.Bold))
                e.Graphics.DrawString($"Score: {score}", font, Brushes.Black, 10, 10);

            if (!inGame)
            {
                Graphics g = e.Graphics;
                g.Clear(Color.SkyBlue);
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

            if (!alive && showGameOver)
            {
                using (var font = new Font("Arial", 18, FontStyle.Bold))
                    e.Graphics.DrawString("GAME OVER!", font, Brushes.Red, 100, 120);
                using (var font = new Font("Arial", 12))
                {
                    e.Graphics.DrawString("Press R to retry or any other key for menu.", font, Brushes.Black, 60, 160);
                    // Show leaderboard after game over
                    int y = 200;
                    e.Graphics.DrawString("Leaderboard:", font, Brushes.Black, 40, y);
                    y += 20;
                    if (leaderboard.Count == 0)
                    {
                        e.Graphics.DrawString("  No high scores yet!", font, Brushes.Gray, 40, y);
                    }
                    else
                    {
                        int rank = 1;
                        foreach (var entry in leaderboard)
                        {
                            e.Graphics.DrawString($"  {rank}. {entry.name.PadRight(10)} {entry.score}", font, Brushes.DarkGreen, 40, y);
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