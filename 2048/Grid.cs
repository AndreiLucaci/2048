﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace _2048
{
    [Serializable]
    public partial class Grid : UserControl
    {
        public Tile[,] Tiles { get; private set; }
        public event EventHandler UpdateScore;
        private Random rnd = new Random(DateTime.UtcNow.Millisecond);
        private bool modified = false;
        private int _score;
        private System.Windows.Forms.Timer _timer;
        private bool endGame = false;
        public int Score
        {
            get { return _score; }
            set
            {
                _score = value;
                modified = true;
                if (UpdateScore != null) UpdateScore(this, EventArgs.Empty);
            }
        }

        public int StartingTiles { get; set; }
        public Grid()
        {
            InitializeComponent();
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 100;
            _timer.Tick += _timer_Tick;
            this.DoubleBuffered = true;
            Tiles = new Tile[4, 4]
            {
                {tile1, tile2, tile3, tile4},
                {tile5, tile6, tile7, tile8},
                {tile9, tile10, tile11, tile12},
                {tile13, tile14, tile15, tile16}
            };

            StartingTiles = 2;
            //tile10.Type = TileNumbers.Tile2;
            //tile2.Type = TileNumbers.Tile2;
            //tile6.Type = TileNumbers.Tile2;
            //tile12.Type = TileNumbers.Tile2;
            //tile13.Type = TileNumbers.Tile2;
            //tile14.Type = TileNumbers.Tile1024;
            //tile15.Type = TileNumbers.Tile1024;
            //tile16.Type = TileNumbers.Tile4;

            GenerateStartingTiles();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            if (Reached2048 != null) Reached2048(this, EventArgs.Empty);
        }

        public void ResetGame()
        {
            foreach (var i in Tiles)
            {
                i.Type = TileNumbers.TileEmpty;
            }

            GenerateStartingTiles();
            Score = 0;
            endGame = false;
        }

        private void GenerateStartingTiles()
        {
            for (int i = 0; i < StartingTiles; i++)
            {
                GenerateTile();
            }
        }

        public void GenerateTile()
        {
            NormalizeTiles();
            int val = (rnd.NextDouble() < 0.9) ? 2 : 4;
            var pos = GetFreePositions();
            if (pos.Count == 0) return;
            var tilePos = GetRandomTile(pos);
            var tile = Tiles[tilePos[0], tilePos[1]];
            tile.Type = Helper.ValueToTile(val.ToString());
            tile.OuterColor = Color.Black;
        }
        public void NormalizeTiles()
        {
            foreach (var i in Tiles)
                i.Type = i.Type;
        }

        public List<Tuple<int, int>> GetFreePositions()
        {
            List<Tuple<int, int>> pos = new List<Tuple<int,int>>();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (Tiles[i, j].Type == TileNumbers.TileEmpty)
                    {
                        pos.Add(new Tuple<int, int>(i, j));
                    }
                }
            }
            return pos;
        }

        private int[] GetRandomTile(List<Tuple<int, int>> pos)
        {
            int[] res = new int[2];
            var rpos = pos[rnd.Next(0, pos.Count - 1)];
            res[0] = rpos.Item1;
            res[1] = rpos.Item2;
            return res;
        }

        public void KeyMove(Moves move)
        {
            try
            {
                //Thread.Sleep(100);
                switch (move)
                {
                    case Moves.Up:
                        MoveUp();
                        break;
                    case Moves.Down:
                        MoveDown();
                        break;
                    case Moves.Left:
                        MoveLeft();
                        break;
                    case Moves.Right:
                        MoveRight();
                        break;
                }

                if (modified)
                {
                    GenerateTile();
                    modified = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }



        private void MoveUp()
        {
            try
            {
                for (int j = 0; j < 4; j++)
                {
                    ColapseUp(j);
                    var cols = GetTilesByCol(j);
                    for (int i = 0; i < cols; i++)
                    {
                        if (CanMerge(i, j, Moves.Up))
                        {
                            Merge(Tiles[i, j], Tiles[i + 1, j]);
                            ColapseUp(j);
                        }
                        ColapseUp(j);
                    }
                    ColapseAllUp();
                }
            }
            catch (Exception) { }
        }

        public event EventHandler Reached2048;

        private void Merge(Tile first, Tile second)
        {
            var val = Int(first.Value) + Int(second.Value);
            if (val == 2048 && !endGame)
            {
                endGame = true;
                _timer.Start();
            }
            Score += val;
            first.Type = Helper.ValueToTile(val.ToString());
            second.Type = TileNumbers.TileEmpty;

        }

        private void MoveLeft()
        {
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    ColapseLeft(i);
                    var cols = GetTilesByRow(i);
                    for (int j = 0; j < cols - 1; j++)
                    {
                        if (CanMerge(i, j, Moves.Left))
                        {
                            Merge(Tiles[i, j], Tiles[i, j + 1]);
                            ColapseLeft(i);
                        }
                        ColapseLeft(i);
                    }
                    ColapseAllLeft();
                }
            }
            catch (Exception) { }
        }
        private void MoveRight()
        {
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    ColapseRight(i);
                    var cols = GetTilesByRow(i);
                    for (int j = 3; j != 3 - cols; j--)
                    {
                        if (CanMerge(i, j, Moves.Right))
                        {
                            Merge(Tiles[i, j], Tiles[i, j - 1]);
                            ColapseRight(j);
                        }
                        ColapseRight(j);
                    }
                    ColapseAllRight();
                }
            }
            catch (Exception) { }
        }


        private void MoveDown()
        {
            try
            {
                for (int j = 0; j < 4; j++)
                {
                    ColapseDown(j);
                    var cols = GetTilesByCol(j);
                    for (int i = 3; i != 3 - cols; i--)
                    {
                        if (i != 0 && CanMerge(i, j, Moves.Down))
                        {
                            Merge(Tiles[i, j], Tiles[i - 1, j]);
                            ColapseDown(j);
                        }
                        ColapseDown(j);
                    }
                    ColapseAllDown();
                }
            }
            catch (Exception) { }
        }

        private int GetTilesByRow(int i)
        {
            int count = 0;
            for (int j = 0; j < 4; j++)
            {
                if (Tiles[i, j].Type != TileNumbers.TileEmpty) count++;
            }
            return count;
        }

        private int GetTilesByCol(int j)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (Tiles[i, j].Type != TileNumbers.TileEmpty) count++;
            }
            return count;
        }

        private int ColapseRight(int i)
        {
            int cols = 0, skip = 0;
            try
            {
                for (int j = 3; j != -1; j--)
                {
                    if (Tiles[i, j].Type != TileNumbers.TileEmpty)
                    {
                        skip++;
                        continue;
                    }
                    int next = GetNextNotEmptyTile(i, j, Moves.Right);
                    Tiles[i, j].Type = Tiles[i, next].Type;
                    Tiles[i, next].Type = TileNumbers.TileEmpty;
                    modified = true;
                    cols++;
                }
            }
            catch (Exception) { }

            return (cols == 0 && skip != 0) ? skip : cols;
        }

        private int ColapseUp(int j)
        {
            int cols = 0, skip = 0;
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Tiles[i, j].Type != TileNumbers.TileEmpty)
                    {
                        skip++;
                        continue;
                    }
                    int next = GetNextNotEmptyTile(i, j, Moves.Up);
                    Tiles[i, j].Type = Tiles[next, j].Type;
                    Tiles[next, j].Type = TileNumbers.TileEmpty;
                    modified = true;
                    cols++;
                }
            }
            catch (Exception) { }

            return (cols == 0 && skip != 0) ? skip : cols;
        }

        private int ColapseLeft(int i)
        {
            int cols = 0, skip = 0;
            try
            {
                for (int j = 0; j < 4; j++)
                {
                    if (Tiles[i, j].Type != TileNumbers.TileEmpty)
                    {
                        skip++;
                        continue;
                    }
                    int next = GetNextNotEmptyTile(i, j, Moves.Left);
                    Tiles[i, j].Type = Tiles[i, next].Type;
                    Tiles[i, next].Type = TileNumbers.TileEmpty;
                    modified = true;
                    cols++;
                }
            }
            catch (Exception) { }

            return (cols == 0 && skip != 0) ? skip : cols;
        }

        private int ColapseDown(int j)
        {
            int cols = 0, skip = 0;
            try
            {
                for (int i = 3; i != -1; i--)
                {
                    if (Tiles[i, j].Type != TileNumbers.TileEmpty)
                    {
                        skip++;
                        continue;
                    }
                    int next = GetNextNotEmptyTile(i, j, Moves.Down);
                    Tiles[i, j].Type = Tiles[next, j].Type;
                    Tiles[next, j].Type = TileNumbers.TileEmpty;
                    modified = true;
                    cols++;
                }
            }
            catch (Exception) { }

            return (cols == 0 && skip != 0) ? skip : cols;
        }

        private int Int(string str)
        {
            return Convert.ToInt32(str);
        }

        private void EmptyColumn(int start, int end, int j)
        {
            for (; start < end; start++)
                Tiles[start, j].Type = TileNumbers.TileEmpty;
        }

        private int GetNextNotEmptyTile(int i, int j, Moves move)
        {
            switch (move)
            {
                case Moves.Up:
                    return GetNextUpTile(i, j);
                case Moves.Down:
                    return GetNextDownTile(i, j);
                case Moves.Left:
                    return GetNextLeftTile(i, j);
                case Moves.Right:
                    return GetNextRightTile(i, j);
            }
            return -1;
        }

        private void ColapseAllRight()
        {
            for (int j = 0; j < 4; j++)
            {
                ColapseRight(j);
            }
        }
        private void ColapseAllDown()
        {
            for (int i = 0; i < 4; i++)
                ColapseDown(i);
        }
        private void ColapseAllUp()
        {
            for (int j = 0; j < 4; j++)
            {
                ColapseUp(j);
            }
        }

        private void ColapseAllLeft()
        {
            for (int i = 0; i < 4; i++)
                ColapseLeft(i);
        }

        private bool CanMerge(int i, int j, Moves move)
        {
            switch (move)
            {
                case Moves.Up:
                    return CanMergeUpTile(i, j);
                case Moves.Down:
                    return CanMergeDownTile(i, j);
                case Moves.Left:
                    return CanMergeLeftTile(i, j);
                case Moves.Right:
                    return CanMergeRightTile(i, j);
            }
            return false;
        }

        private bool CanMergeRightTile(int i, int j)
        {
            try { return Tiles[i, j].Type != TileNumbers.TileEmpty && Tiles[i, j].Type == Tiles[i, j - 1].Type; }
            catch (Exception) { return false; }
        }

        private bool CanMergeLeftTile(int i, int j)
        {
            try { return Tiles[i, j].Type != TileNumbers.TileEmpty && Tiles[i, j].Type == Tiles[i, j + 1].Type; }
            catch (Exception) { return false; }
        }

        private bool CanMergeDownTile(int i, int j)
        {
            try { return Tiles[i, j].Type != TileNumbers.TileEmpty && Tiles[i, j].Type == Tiles[i - 1, j].Type; }
            catch (Exception) { return false; }
        }

        private bool CanMergeUpTile(int i, int j)
        {
            try { return Tiles[i, j].Type != TileNumbers.TileEmpty && Tiles[i, j].Type == Tiles[i + 1, j].Type; }
            catch (Exception) { return false; }
        }

        private int GetNextRightTile(int i, int j)
        {
            j--;
            for (; i < 4; i++)
            {
                for (; j != -1; j--)
                {
                    if (Tiles[i, j].Type == TileNumbers.TileEmpty) continue;
                    return j;
                }
            }
            return -1;
        }

        private int GetNextLeftTile(int i, int j)
        {
            j++;
            for (; i < 4; i++)
            {
                for (; j < 4; j++)
                {
                    if (Tiles[i, j].Type == TileNumbers.TileEmpty) continue;
                    return j;
                }
            }
            return -1;
        }

        private int GetNextDownTile(int i, int j)
        {
            i--;
            for (; j < 4; j++)
            {
                for (; i != -1; i--)
                {
                    if (Tiles[i, j].Type == TileNumbers.TileEmpty) continue;
                    return i;
                }
            }
            return -1;
        }

        private int GetNextUpTile(int i, int j)
        {
            i++;
            for (; j < 4; j++)
            {
                for (; i < 4; i++)
                {
                    if (Tiles[i, j].Type == TileNumbers.TileEmpty) continue;
                    return i;
                }
            }
            return -1;
        }


        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left)
                return true;
            else return base.IsInputKey(keyData);
        }

        private void grid_layout_Paint(object sender, PaintEventArgs e)
        {

        }

        private SaveGame currentSaveGame;

        public SaveGame CurrentSaveGame
        {
            get { return currentSaveGame; }
            set { currentSaveGame = value; }
        }

        public void SaveGame(string filename)
        {
            CurrentSaveGame = new SaveGame(this.Tiles, this.Score);
            CurrentSaveGame.SaveGameToFile(filename);
        }

        public void LoadGame(string filename)
        {
            var LoadNewGame = new SaveGame();
            LoadNewGame.LoadGameFromFile(filename);
            this.Score = LoadNewGame.Score;
            var tiles = LoadNewGame.GetTiles();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    this.Tiles[i, j].Type = tiles[i, j].Type;
                }
            }
            Application.DoEvents();
        }
    }
}
