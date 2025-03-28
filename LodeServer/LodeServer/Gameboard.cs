using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lode
{
    internal class Gameboard
    {
        private const int GridSize = 50;
        private int[,] board;
        private List<Ship> ships;

        public Gameboard()
        {
            board = new int[GridSize, GridSize];
            ships = new List<Ship>();
        }

        public int ShipCount => ships.Count;

        public void PlaceShip(int row, int col, int shipSize, bool isHorizontal)
        {
            if (ShipCount >= 10 || ShipCount < 1)
            {
                return;
            }

            List<Point> shipCells = new List<Point>();

            for (int i = 0; i < shipSize; i++)
            {
                int r = isHorizontal ? row : row + i;
                int c = isHorizontal ? col + i : col;

                if (r < GridSize && c < GridSize && board[r, c] == 0)
                {
                    shipCells.Add(new Point(r, c));
                }
                else
                {
                    return;
                }
            }

            foreach (Point p in shipCells)
            {
                board[p.X, p.Y] = 1;
            }

            ships.Add(new Ship(shipCells));
        }

        public int GetSunkShipsCount()
        {
            int sunkCount = 0;
            foreach (Ship ship in ships)
            {
                if (ship.IsSunk()) sunkCount++;
            }
            return sunkCount;
        }

        public bool CheckHit(int row, int col)
        {
            return (board[row, col] == 1);
        }

        public int ProcessAttack(int row, int col)
        {
            if (board[row, col] == 2 || board[row, col] == 3)
            {
                return board[row, col] == 2 ? 1 : 0;
            }

            if (board[row, col] == 1)
            {
                board[row, col] = 2;

                foreach (Ship ship in ships)
                {
                    if (ship.ContainsCell(row, col))
                    {
                        ship.RegisterHit(row, col);
                        if (ship.IsSunk())
                        {
                            return 2;
                        }
                        return 1;
                    }
                }
            }
            else
            {
                board[row, col] = 3;
                return 0;
            }

            return 0;
        }

        public void MarkHit(int row, int col)
        {
            board[row, col] = 2;
        }

        public void MarkMiss(int row, int col)
        {
            board[row, col] = 3;
        }

        public void RenderBoard(
            System.Drawing.Graphics g,
            int offsetX,
            int offsetY,
            int cellSize)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Color cellColor = Color.Blue;  // default water
                    if (board[row, col] == 1)
                    {
                        cellColor = Color.Green;    // ship
                    }
                    else if (board[row, col] == 2)
                    {
                        cellColor = Color.Red;      // hit
                    }
                    else if (board[row, col] == 3)
                    {
                        cellColor = Color.Gray;     // miss
                    }

                    using (SolidBrush brush = new SolidBrush(cellColor))
                    {
                        g.FillRectangle(
                            brush,
                            offsetX + col * cellSize,
                            offsetY + row * cellSize,
                            cellSize,
                            cellSize
                        );
                    }

                    g.DrawRectangle(
                        Pens.Black,
                        offsetX + col * cellSize,
                        offsetY + row * cellSize,
                        cellSize,
                        cellSize
                    );
                }
            }
        }

        public void ResetBoard()
        {
            board = new int[GridSize, GridSize];
            ships.Clear();
        }

        private class Ship
        {
            private List<Point> cells;
            private HashSet<Point> hits;

            public Ship(List<Point> cells)
            {
                this.cells = new List<Point>(cells);
                hits = new HashSet<Point>();
            }

            public bool ContainsCell(int row, int col)
            {
                foreach (Point p in cells)
                {
                    if (p.X == row && p.Y == col)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void RegisterHit(int row, int col)
            {
                if (ContainsCell(row, col))
                {
                    hits.Add(new Point(row, col));
                }
            }

            public bool IsSunk()
            {
                return hits.Count == cells.Count;
            }
        }
    }
}