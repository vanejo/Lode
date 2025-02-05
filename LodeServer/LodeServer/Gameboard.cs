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

        public void PlaceShip(int row, int col, int shipSize, bool isHorizontal)
        {
            if (isHorizontal)
            {
                for (int i = 0; i < shipSize; i++)
                {
                    if (col + i < GridSize && board[row, col + i] == 0)
                    {
                        board[row, col + i] = 1;
                    }
                }
            }
            else
            {
                for (int i = 0; i < shipSize; i++)
                {
                    if (row + i < GridSize && board[row + i, col] == 0)
                    {
                        board[row + i, col] = 1;
                    }
                }
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

        public void RenderBoard(Graphics g, int offsetX, int offsetY, Image waterImage, Image shipImage, Image hitImage, Image missImage)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Image cellImage = waterImage; // Default (water)
                    if (board[row, col] == 1) cellImage = shipImage;   // Ship
                    else if (board[row, col] == 2) cellImage = hitImage;   // Hit
                    else if (board[row, col] == 3) cellImage = missImage; // Miss

                    g.DrawImage(cellImage, offsetX + col * 30, offsetY + row * 30, 30, 30);
                    g.DrawRectangle(Pens.Black, offsetX + col * 30, offsetY + row * 30, 30, 30);
                }
            }
        }
    }
}