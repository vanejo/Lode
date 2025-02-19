using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lode
{
    internal class Gameboard
    {
        private const int GridSize = 10;
        private int[,] board;
        private List<Ship> ships;

        public Gameboard()
        {
            board = new int[GridSize, GridSize];
            ships = new List<Ship>();
        }

        public void PlaceShip(int row, int col, int shipSize, bool isHorizontal)
        {
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

        public bool CheckHit(int row, int col)
        {
            if (board[row, col] == 1)
            {
                return true;
            }
            return false;
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

        public void RenderBoard(Graphics g, int offsetX, int offsetY, Image waterImage, Image shipImage, Image hitImage, Image missImage)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Image cellImage = waterImage; 
                    if (board[row, col] == 1)
                    {
                        cellImage = shipImage;
                    }
                    else if (board[row, col] == 2)
                    {
                        cellImage = hitImage;
                    }
                    else if (board[row, col] == 3)
                    {
                        cellImage = missImage;
                    }

                    g.DrawImage(cellImage, offsetX + col * 30, offsetY + row * 30, 30, 30);
                    g.DrawRectangle(Pens.Black, offsetX + col * 30, offsetY + row * 30, 30, 30);
                }
            }
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
                Point hitPoint = new Point(row, col);
                if (ContainsCell(row, col))
                {
                    hits.Add(hitPoint);
                }
            }

            public bool IsSunk()
            {
                return hits.Count == cells.Count;
            }
        }
    }
}