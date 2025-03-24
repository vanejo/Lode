using Lode;
using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace LodeClient
{
    public partial class Klient_Form : Form
    {
        static Socket clientSocket;
        RadioButton rbPlaceShip = new RadioButton();
        RadioButton rbAttack = new RadioButton();
        Label labelResponse = new Label();
        Label labelPlayerBoard = new Label();
        Label labelOpponentBoard = new Label();
        Gameboard playerBoard = new Gameboard();
        Gameboard opponentBoard = new Gameboard();

        private Image waterImage;
        private Image shipImage;
        private Image hitImage;
        private Image missImage;

        private TextBox tbIPAddress = new TextBox();
        private TextBox tbPort = new TextBox();
        private Button btnConnect = new Button();
        private Label labelIPAddress = new Label();
        private Label labelPort = new Label();

        private NumericUpDown nudShipSize = new NumericUpDown();
        private ComboBox cbOrientation = new ComboBox();

        // We'll keep the cell size as before (for example 15),
        // but adjust the board offsets for more space
        const int cellSize = 15;

        // Since we have a 50x50 board now, let's make the form bigger and
        // give more space between player and opponent boards
        const int playerBoardOffsetX = 20;
        const int playerBoardOffsetY = 200;
        // Increase distance between boards
        const int opponentBoardOffsetX = 900;
        const int opponentBoardOffsetY = 200;

        private bool isMyTurn = false;

        public Klient_Form()
        {
            InitializeComponent();
            InitializeGameComponents();
        }

        private void InitializeGameComponents()
        {
            // Make the form bigger
            this.Size = new Size(1700, 1300);

            // Load images (adjust paths as needed)
            waterImage = Image.FromFile(@"..\..\water.png");
            shipImage = Image.FromFile(@"..\..\ship.png");
            hitImage = Image.FromFile(@"..\..\hit.png");
            missImage = Image.FromFile(@"..\..\miss.png");

            labelResponse.Left = 20;
            labelResponse.Top = 20;
            labelResponse.Width = 300;
            Controls.Add(labelResponse);

            rbPlaceShip.Text = "Place Ship";
            rbPlaceShip.Left = 20;
            rbPlaceShip.Top = 50;
            rbPlaceShip.Checked = true;
            Controls.Add(rbPlaceShip);

            rbAttack.Text = "Attack";
            rbAttack.Left = 20;
            rbAttack.Top = 80;
            Controls.Add(rbAttack);

            nudShipSize.Minimum = 1;
            nudShipSize.Maximum = 5;
            nudShipSize.Value = 3;
            nudShipSize.Top = 110;
            nudShipSize.Left = 20;
            Controls.Add(nudShipSize);

            cbOrientation.Items.AddRange(new string[] { "Horizontal", "Vertical" });
            cbOrientation.SelectedIndex = 0;
            cbOrientation.Top = 140;
            cbOrientation.Left = 20;
            Controls.Add(cbOrientation);

            labelPlayerBoard.Text = "Your Board";
            labelPlayerBoard.Left = 20;
            labelPlayerBoard.Top = 170;
            Controls.Add(labelPlayerBoard);

            labelOpponentBoard.Text = "Opponent's Board";
            labelOpponentBoard.Left = opponentBoardOffsetX;
            labelOpponentBoard.Top = 170;
            Controls.Add(labelOpponentBoard);

            labelIPAddress.Text = "IP Address:";
            labelIPAddress.Left = 150;
            labelIPAddress.Top = 50;
            Controls.Add(labelIPAddress);

            tbIPAddress.Left = 150;
            tbIPAddress.Top = 70;
            tbIPAddress.Width = 150;
            Controls.Add(tbIPAddress);

            labelPort.Text = "Port:";
            labelPort.Left = 150;
            labelPort.Top = 100;
            Controls.Add(labelPort);

            tbPort.Left = 150;
            tbPort.Top = 120;
            tbPort.Width = 60;
            Controls.Add(tbPort);

            btnConnect.Text = "Connect";
            btnConnect.Left = 150;
            btnConnect.Top = 150;
            btnConnect.Click += BtnConnect_Click;
            Controls.Add(btnConnect);

            this.Paint += new PaintEventHandler(Klient_Form_Paint);
            this.MouseClick += new MouseEventHandler(Klient_Form_MouseClick);

            System.Windows.Forms.Timer responseTimer = new System.Windows.Forms.Timer();
            responseTimer.Interval = 100;
            responseTimer.Tick += CheckForResponse;
            responseTimer.Start();
        }

        private void Klient_Form_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Render the player's board
            playerBoard.RenderBoard(
                g,
                playerBoardOffsetX,
                playerBoardOffsetY,
                waterImage,
                shipImage,
                hitImage,
                missImage,
                cellSize
            );

            // Render the opponent's board (now placed further away)
            opponentBoard.RenderBoard(
                g,
                opponentBoardOffsetX,
                opponentBoardOffsetY,
                waterImage,
                shipImage,
                hitImage,
                missImage,
                cellSize
            );
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            string ipAddress = tbIPAddress.Text;
            if (!int.TryParse(tbPort.Text, out int port))
            {
                MessageBox.Show("Invalid port number");
                return;
            }

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(ipAddress, port);
                MessageBox.Show("Connected successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
            }
        }

        private void Klient_Form_MouseClick(object sender, MouseEventArgs e)
        {
            int boardX = e.X;
            int boardY = e.Y;
            int row, col;

            // 50x50 board, each cell is cellSize
            if (boardX >= playerBoardOffsetX && boardX < playerBoardOffsetX + 50 * cellSize &&
                boardY >= playerBoardOffsetY && boardY < playerBoardOffsetY + 50 * cellSize)
            {
                row = (boardY - playerBoardOffsetY) / cellSize;
                col = (boardX - playerBoardOffsetX) / cellSize;

                if (rbPlaceShip.Checked)
                {
                    int shipSize = (int)nudShipSize.Value;
                    bool isHorizontal = (cbOrientation.SelectedItem?.ToString() == "Horizontal");
                    playerBoard.PlaceShip(row, col, shipSize, isHorizontal);

                    for (int i = 0; i < shipSize; i++)
                    {
                        int cellCol = isHorizontal ? col + i : col;
                        int cellRow = isHorizontal ? row : row + i;
                        InvalidateCell(cellRow, cellCol, playerBoardOffsetX, playerBoardOffsetY);
                    }
                    SendMessageToServer($"PlaceShip,{row},{col},{shipSize},{isHorizontal}");
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
                    SendMessageToServer($"Attack,{row},{col}");
                    isMyTurn = false;
                }
            }
        }

        private void CheckForResponse(object sender, EventArgs e)
        {
            if (clientSocket == null || !clientSocket.Connected)
                return;

            if (clientSocket.Available > 0)
            {
                byte[] data = new byte[clientSocket.ReceiveBufferSize];
                int bytesRead = clientSocket.Receive(data);
                if (bytesRead > 0)
                {
                    string response = Encoding.Default.GetString(data, 0, bytesRead);
                    labelResponse.Text = response;
                    ProcessGameData(response);
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
                            SendMessageToServer($"ShipDestroyed,{row},{col}");
                            MessageBox.Show($"One of your ships has been destroyed at ({row},{col})!");
                        }
                        else if (result == 1)
                        {
                            SendMessageToServer($"Hit,{row},{col}");
                        }
                        else
                        {
                            SendMessageToServer($"Miss,{row},{col}");
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
                        MessageBox.Show($"You destroyed an enemy ship at ({row},{col})!");
                        break;
                }
            }
        }

        private void SendMessageToServer(string message)
        {
            byte[] data = Encoding.Default.GetBytes(message);
            clientSocket.Send(data);
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