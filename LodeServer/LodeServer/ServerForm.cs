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
        // Server's own board (holds server's ships)
        Gameboard playerBoard = new Gameboard();
        // Displays results of server's attacks
        Gameboard opponentBoard = new Gameboard();

        private Image waterImage;
        private Image shipImage;
        private Image hitImage;
        private Image missImage;

        private NumericUpDown nudShipSize = new NumericUpDown();
        private ComboBox cbOrientation = new ComboBox();

        const int cellSize = 30;
        const int playerBoardOffsetX = 20;
        const int playerBoardOffsetY = 220;
        const int opponentBoardOffsetX = 420;
        const int opponentBoardOffsetY = 220;

        public ServerForm()
        {
            InitializeComponent();
            InitializeServerComponents();
        }

        private void InitializeServerComponents()
        {
            this.Size = new Size(800, 600);

            // Load images
            waterImage = Image.FromFile(@"..\..\water.png");
            shipImage = Image.FromFile(@"..\..\ship.png");
            hitImage = Image.FromFile(@"..\..\hit.png");
            missImage = Image.FromFile(@"..\..\miss.png");

            // Status label
            labelStatus.Top = 20;
            labelStatus.Left = 20;
            Controls.Add(labelStatus);

            // Data label
            labelData.Top = 50;
            labelData.Left = 20;
            Controls.Add(labelData);

            // Place Ship radio button
            rbPlaceShip.Text = "Place Ship";
            rbPlaceShip.Top = 80;
            rbPlaceShip.Left = 20;
            rbPlaceShip.Checked = true;
            Controls.Add(rbPlaceShip);

            // Attack radio button
            rbAttack.Text = "Attack";
            rbAttack.Top = 110;
            rbAttack.Left = 20;
            Controls.Add(rbAttack);

            // Ship size numeric up-down
            nudShipSize.Minimum = 1;
            nudShipSize.Maximum = 5;
            nudShipSize.Value = 3;
            nudShipSize.Top = 140;
            nudShipSize.Left = 20;
            Controls.Add(nudShipSize);

            // Orientation combobox
            cbOrientation.Items.AddRange(new string[] { "Horizontal", "Vertical" });
            cbOrientation.SelectedIndex = 0;
            cbOrientation.Top = 170;
            cbOrientation.Left = 20;
            Controls.Add(cbOrientation);

            // Player board label
            labelPlayerBoard.Text = "Your Board";
            labelPlayerBoard.Left = 20;
            labelPlayerBoard.Top = 200;
            Controls.Add(labelPlayerBoard);

            // Opponent board label
            labelOpponentBoard.Text = "Opponent's Board";
            labelOpponentBoard.Left = 420;
            labelOpponentBoard.Top = 200;
            Controls.Add(labelOpponentBoard);

            // Socket setup
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

            // Render server's board on the left
            playerBoard.RenderBoard(g, playerBoardOffsetX, playerBoardOffsetY, waterImage, shipImage, hitImage, missImage);

            // Render opponent's board on the right
            opponentBoard.RenderBoard(g, opponentBoardOffsetX, opponentBoardOffsetY, waterImage, shipImage, hitImage, missImage);
        }

        private void ServerForm_MouseClick(object sender, MouseEventArgs e)
        {
            int boardX = e.X;
            int boardY = e.Y;
            int row, col;

            // Left side (server's board) for placing ships
            if (boardX >= playerBoardOffsetX && boardX < playerBoardOffsetX + 10 * cellSize &&
                boardY >= playerBoardOffsetY && boardY < playerBoardOffsetY + 10 * cellSize)
            {
                row = (boardY - playerBoardOffsetY) / cellSize;
                col = (boardX - playerBoardOffsetX) / cellSize;
                if (rbPlaceShip.Checked)
                {
                    int shipSize = (int)nudShipSize.Value;
                    bool isHorizontal = cbOrientation.SelectedItem.ToString() == "Horizontal";
                    playerBoard.PlaceShip(row, col, shipSize, isHorizontal);
                    for (int i = 0; i < shipSize; i++)
                    {
                        int cellCol = isHorizontal ? col + i : col;
                        int cellRow = isHorizontal ? row : row + i;
                        InvalidateCell(cellRow, cellCol, playerBoardOffsetX, playerBoardOffsetY);
                    }
                }
            }
            // Right side (opponent board) for attacking
            else if (boardX >= opponentBoardOffsetX && boardX < opponentBoardOffsetX + 10 * cellSize &&
                     boardY >= opponentBoardOffsetY && boardY < opponentBoardOffsetY + 10 * cellSize)
            {
                row = (boardY - opponentBoardOffsetY) / cellSize;
                col = (boardX - opponentBoardOffsetX) / cellSize;
                if (rbAttack.Checked)
                {
                    SendMessageToClient($"Attack,{row},{col}");
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
            if (parts.Length == 3)
            {
                string command = parts[0];
                int row = int.Parse(parts[1]);
                int col = int.Parse(parts[2]);

                switch (command)
                {
                    case "PlaceShip":
                        // No processing needed here.
                        break;

                    case "Attack":
                        // Incoming attack on server's board.
                        bool hit = playerBoard.CheckHit(row, col);
                        if (hit)
                        {
                            playerBoard.MarkHit(row, col);
                        }
                        else
                        {
                            playerBoard.MarkMiss(row, col);
                        }
                        InvalidateCell(row, col, playerBoardOffsetX, playerBoardOffsetY);
                        string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                        SendMessageToClient(response);
                        break;

                    case "Hit":
                        // Response for server's own attack; update opponent board.
                        opponentBoard.MarkHit(row, col);
                        InvalidateCell(row, col, opponentBoardOffsetX, opponentBoardOffsetY);
                        break;
                    case "Miss":
                        opponentBoard.MarkMiss(row, col);
                        InvalidateCell(row, col, opponentBoardOffsetX, opponentBoardOffsetY);
                        break;
                }
            }
        }

        private void SendMessageToClient(string message)
        {
            byte[] responseBytes = Encoding.Default.GetBytes(message);
            clientSocket.Send(responseBytes);
        }

        // Helper to invalidate a single cell in a given board region.
        private void InvalidateCell(int row, int col, int offsetX, int offsetY)
        {
            Rectangle cellRect = new Rectangle(offsetX + col * cellSize, offsetY + row * cellSize, cellSize, cellSize);
            this.Invalidate(cellRect);
        }
    }
}