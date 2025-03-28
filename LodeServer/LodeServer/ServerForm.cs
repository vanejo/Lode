using Lode;
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace LodeServer
{
    public partial class ServerForm : Form
    {
        static Socket serverSocket;
        static Socket clientSocket;
        RadioButton rbPlaceShip = new RadioButton();
        RadioButton rbAttack = new RadioButton();
        Label labelStatus = new Label();
        Label labelData = new Label();
        Label labelPlayerBoard = new Label();
        Label labelOpponentBoard = new Label();
        Gameboard playerBoard = new Gameboard();
        Gameboard opponentBoard = new Gameboard();

        private NumericUpDown nudShipSize = new NumericUpDown();
        private ComboBox cbOrientation = new ComboBox();

        const int cellSize = 15;

        const int playerBoardOffsetX = 20;
        const int playerBoardOffsetY = 220;

        const int opponentBoardOffsetX = 900;
        const int opponentBoardOffsetY = 220;

        private bool isMyTurn = true;

        public ServerForm()
        {
            InitializeComponent();
            InitializeServerComponents();
        }

        private void InitializeServerComponents()
        {
            this.Size = new Size(1700, 1300);

            labelStatus.Top = 20;
            labelStatus.Left = 20;
            Controls.Add(labelStatus);

            labelData.Top = 50;
            labelData.Left = 20;
            Controls.Add(labelData);

            rbPlaceShip.Text = "Place Ship";
            rbPlaceShip.Top = 80;
            rbPlaceShip.Left = 20;
            rbPlaceShip.Checked = true;
            Controls.Add(rbPlaceShip);

            rbAttack.Text = "Attack";
            rbAttack.Top = 110;
            rbAttack.Left = 20;
            Controls.Add(rbAttack);

            nudShipSize.Minimum = 1;
            nudShipSize.Maximum = 5;
            nudShipSize.Value = 3;
            nudShipSize.Top = 140;
            nudShipSize.Left = 20;
            Controls.Add(nudShipSize);

            cbOrientation.Items.AddRange(new string[] { "Horizontal", "Vertical" });
            cbOrientation.SelectedIndex = 0;
            cbOrientation.Top = 170;
            cbOrientation.Left = 20;
            Controls.Add(cbOrientation);

            labelPlayerBoard.Text = "Your Board";
            labelPlayerBoard.Left = 20;
            labelPlayerBoard.Top = 195;
            Controls.Add(labelPlayerBoard);

            labelOpponentBoard.Text = "Opponent's Board";
            labelOpponentBoard.Left = opponentBoardOffsetX;
            labelOpponentBoard.Top = 195;
            Controls.Add(labelOpponentBoard);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            serverSocket.Listen(1);
            labelStatus.Text = "Waiting for connection...";

            clientSocket = serverSocket.Accept();
            labelStatus.Text = "Client connected";

            Thread receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            this.Paint += new PaintEventHandler(ServerForm_Paint);
            this.MouseClick += new MouseEventHandler(ServerForm_MouseClick);
        }

        private void ServerForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            playerBoard.RenderBoard(
                g,
                playerBoardOffsetX,
                playerBoardOffsetY,
                cellSize
            );
            opponentBoard.RenderBoard(
                g,
                opponentBoardOffsetX,
                opponentBoardOffsetY,
                cellSize
            );
        }

        private void ServerForm_MouseClick(object sender, MouseEventArgs e)
        {
            int boardX = e.X;
            int boardY = e.Y;
            int row, col;

            if (boardX >= playerBoardOffsetX && boardX < playerBoardOffsetX + 50 * cellSize &&
                boardY >= playerBoardOffsetY && boardY < playerBoardOffsetY + 50 * cellSize)
            {
                row = (boardY - playerBoardOffsetY) / cellSize;
                col = (boardX - playerBoardOffsetX) / cellSize;
                if (rbPlaceShip.Checked)
                {
                    if (playerBoard.ShipCount >= 10)
                    {
                        MessageBox.Show("Please place only between 1 and 10 ships.");
                        return;
                    }

                    int shipSize = (int)nudShipSize.Value;
                    bool isHorizontal = cbOrientation.SelectedItem?.ToString() == "Horizontal";
                    playerBoard.PlaceShip(row, col, shipSize, isHorizontal);

                    for (int i = 0; i < shipSize; i++)
                    {
                        int cellCol = isHorizontal ? col + i : col;
                        int cellRow = isHorizontal ? row : row + i;
                        InvalidateCell(cellRow, cellCol, playerBoardOffsetX, playerBoardOffsetY);
                    }
                }
            }
            else if (boardX >= opponentBoardOffsetX && boardX < opponentBoardOffsetX + 50 * cellSize &&
                     boardY >= opponentBoardOffsetY && boardY < opponentBoardOffsetY + 50 * cellSize)
            {
                if (!isMyTurn)
                {
                    MessageBox.Show("It is not your turn!");
                    return;
                }
                row = (boardY - opponentBoardOffsetY) / cellSize;
                col = (boardX - opponentBoardOffsetX) / cellSize;
                if (rbAttack.Checked)
                {
                    SendMessageToClient($"Attack,{row},{col}");
                    isMyTurn = false;
                }
            }
        }

        private void ReceiveData()
        {
            while (true)
            {
                try
                {
                    if (clientSocket.Available > 0)
                    {
                        byte[] buffer = new byte[clientSocket.ReceiveBufferSize];
                        int bytesRead = clientSocket.Receive(buffer);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.Default.GetString(buffer, 0, bytesRead);
                            Invoke(new Action(() => ProcessGameData(message)));
                        }
                    }
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error receiving data: " + ex.Message);
                    break;
                }
            }
        }

        private void ProcessGameData(string message)
        {
            string[] parts = message.Split(',');
            if (parts.Length >= 3)
            {
                string command = parts[0];
                int row = int.Parse(parts[1]);
                int col = int.Parse(parts[2]);

                switch (command)
                {
                    case "PlaceShip":
                        break;

                    case "Attack":
                        int result = playerBoard.ProcessAttack(row, col);
                        if (result == 2)
                        {
                            SendMessageToClient($"ShipDestroyed,{row},{col}");
                            MessageBox.Show($"One of your ships has been destroyed at ({row + 1},{col + 1})!");
                            CheckWinOrLose();
                        }
                        else if (result == 1)
                        {
                            SendMessageToClient($"Hit,{row},{col}");
                        }
                        else
                        {
                            SendMessageToClient($"Miss,{row},{col}");
                        }
                        isMyTurn = true;
                        InvalidateCell(row, col, playerBoardOffsetX, playerBoardOffsetY);
                        break;

                    case "Hit":
                        opponentBoard.MarkHit(row, col);
                        InvalidateCell(row, col, opponentBoardOffsetX, opponentBoardOffsetY);
                        break;

                    case "Miss":
                        opponentBoard.MarkMiss(row, col);
                        InvalidateCell(row, col, opponentBoardOffsetX, opponentBoardOffsetY);
                        break;

                    case "ShipDestroyed":
                        opponentBoard.MarkHit(row, col);
                        InvalidateCell(row, col, opponentBoardOffsetX, opponentBoardOffsetY);
                        MessageBox.Show($"You destroyed an enemy ship at ({row + 1},{col + 1})!");
                        CheckWinOrLose();
                        break;
                }
            }
        }

       
        private void CheckWinOrLose()
        {
            int mySunkCount = playerBoard.GetSunkShipsCount();
            int opponentSunkCount = opponentBoard.GetSunkShipsCount();
         
            if (mySunkCount == 10)
            {
                MessageBox.Show("You Lost! All your ships are destroyed.");
                ResetBoards();
                return;
            }
          
            if (opponentSunkCount == 10)
            {
                MessageBox.Show("You Won! You destroyed all opponent ships.");
                ResetBoards();
                return;
            }
        }

        private void ResetBoards()
        {
            playerBoard.ResetBoard();
            opponentBoard.ResetBoard();
            this.Invalidate();
        }

        private void SendMessageToClient(string message)
        {
            byte[] responseBytes = Encoding.Default.GetBytes(message);
            clientSocket.Send(responseBytes);
        }

        private void InvalidateCell(int row, int col, int offsetX, int offsetY)
        {
            Rectangle cellRect = new Rectangle(
                offsetX + col * cellSize,
                offsetY + row * cellSize,
                cellSize,
                cellSize
            );
            this.Invalidate(cellRect);
        }
    }
}