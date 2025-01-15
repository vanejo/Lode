using System;
using System.Drawing;

namespace Lode
{
    internal class Gameboard
    {
        private const int GridSize = 10;
        private int[,] board;

        // 0 = Empty
        // 1 = Ship
        // 2 = Hit
        // 3 = Miss

        public Gameboard()
        {
            board = new int[GridSize, GridSize];
        }

        public void PlaceShip(int row, int col)
        {
            if (board[row, col] == 0)
            {
                board[row, col] = 1;
            }
        }

        public bool CheckHit(int row, int col)
        {
            if (board[row, col] == 1)
            {
                board[row, col] = 2; // Mark as hit
                return true;
            }
            return false;
        }

        public void MarkHit(int row, int col)
        {
            board[row, col] = 2;
        }

        public void MarkMiss(int row, int col)
        {
            board[row, col] = 3;
        }

        public void RenderBoard(Graphics g, int offsetX, int offsetY)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Brush brush = Brushes.Blue; // Default (water)
                    if (board[row, col] == 1) brush = Brushes.Gray;   // Ship
                    else if (board[row, col] == 2) brush = Brushes.Red;   // Hit
                    else if (board[row, col] == 3) brush = Brushes.White; // Miss

                    g.FillRectangle(brush, offsetX + col * 30, offsetY + row * 30, 30, 30);
                    g.DrawRectangle(Pens.Black, offsetX + col * 30, offsetY + row * 30, 30, 30);
                }
            }
        }
    }
}