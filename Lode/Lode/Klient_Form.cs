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
        // Client's own board (holds client's ships)
        Gameboard playerBoard = new Gameboard();
        // Displays results of client's attacks
        Gameboard opponentBoard = new Gameboard();

        private Image waterImage;
        private Image shipImage;
        private Image hitImage;
        private Image missImage;

        private NumericUpDown nudShipSize = new NumericUpDown();
        private ComboBox cbOrientation = new ComboBox();

        // Constants for board drawing
        const int cellSize = 30;
        const int playerBoardOffsetX = 20;
        const int playerBoardOffsetY = 200;
        const int opponentBoardOffsetX = 420;
        const int opponentBoardOffsetY = 200;

        public Klient_Form()
        {
            InitializeComponent();
            InitializeGameComponents();
        }

        private void InitializeGameComponents()
        {
            this.Size = new Size(800, 600);

            // Load images
            waterImage = Image.FromFile(@"..\..\water.png");
            shipImage = Image.FromFile(@"..\..\ship.png");
            hitImage = Image.FromFile(@"..\..\hit.png");
            missImage = Image.FromFile(@"..\..\miss.png");

            // Response label
            labelResponse.Left = 20;
            labelResponse.Top = 20;
            labelResponse.Width = 300;
            Controls.Add(labelResponse);

            // Place Ship radio button
            rbPlaceShip.Text = "Place Ship";
            rbPlaceShip.Left = 20;
            rbPlaceShip.Top = 50;
            rbPlaceShip.Checked = true;
            Controls.Add(rbPlaceShip);

            // Attack radio button
            rbAttack.Text = "Attack";
            rbAttack.Left = 20;
            rbAttack.Top = 80;
            Controls.Add(rbAttack);

            // Ship size numeric up-down
            nudShipSize.Minimum = 1;
            nudShipSize.Maximum = 5;
            nudShipSize.Value = 3;
            nudShipSize.Top = 110;
            nudShipSize.Left = 20;
            Controls.Add(nudShipSize);

            // Orientation combobox
            cbOrientation.Items.AddRange(new string[] { "Horizontal", "Vertical" });
            cbOrientation.SelectedIndex = 0;
            cbOrientation.Top = 140;
            cbOrientation.Left = 20;
            Controls.Add(cbOrientation);

            // Player board label
            labelPlayerBoard.Text = "Your Board";
            labelPlayerBoard.Left = 20;
            labelPlayerBoard.Top = 170;
            Controls.Add(labelPlayerBoard);

            // Opponent board label
            labelOpponentBoard.Text = "Opponent's Board";
            labelOpponentBoard.Left = 420;
            labelOpponentBoard.Top = 170;
            Controls.Add(labelOpponentBoard);

            // Socket setup
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect("127.0.0.1", 5555);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
                return;
            }

            this.Paint += new PaintEventHandler(Klient_Form_Paint);
            this.MouseClick += new MouseEventHandler(Klient_Form_MouseClick);

            // Timer to check for server responses
            Timer responseTimer = new Timer();
            responseTimer.Interval = 100;
            responseTimer.Tick += CheckForResponse;
            responseTimer.Start();
        }

        private void Klient_Form_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Render client's board on the left
            playerBoard.RenderBoard(g, playerBoardOffsetX, playerBoardOffsetY, waterImage, shipImage, hitImage, missImage);

            // Render opponent's board on the right
            opponentBoard.RenderBoard(g, opponentBoardOffsetX, opponentBoardOffsetY, waterImage, shipImage, hitImage, missImage);
        }

        private void Klient_Form_MouseClick(object sender, MouseEventArgs e)
        {
            int boardX = e.X;
            int boardY = e.Y;
            int row, col;

            // Left side (player board) is used for placing ships
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
                    // For multiple cells update each cell
                    for (int i = 0; i < shipSize; i++)
                    {
                        int cellCol = isHorizontal ? col + i : col;
                        int cellRow = isHorizontal ? row : row + i;
                        InvalidateCell(cellRow, cellCol, playerBoardOffsetX, playerBoardOffsetY);
                    }
                    SendMessageToServer($"PlaceShip,{row},{col},{shipSize},{isHorizontal}");
                }
            }
            // Right side (opponent board) is used for attacking
            else if (boardX >= opponentBoardOffsetX && boardX < opponentBoardOffsetX + 10 * cellSize &&
                     boardY >= opponentBoardOffsetY && boardY < opponentBoardOffsetY + 10 * cellSize)
            {
                row = (boardY - opponentBoardOffsetY) / cellSize;
                col = (boardX - opponentBoardOffsetX) / cellSize;

                if (rbAttack.Checked)
                {
                    SendMessageToServer($"Attack,{row},{col}");
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
                        // No client processing needed for now.
                        break;

                    case "Attack":
                        // Incoming attack on the player's board.
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
                        // Send a response to the server
                        string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                        SendMessageToServer(response);
                        break;

                    case "Hit":
                        // Response for an attack sent by the client; update opponent board.
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

        private void CheckForResponse(object sender, EventArgs e)
        {
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

        private void SendMessageToServer(string message)
        {
            byte[] data = Encoding.Default.GetBytes(message);
            clientSocket.Send(data);
        }

        // Helper to invalidate a single cell in a given board region.
        private void InvalidateCell(int row, int col, int offsetX, int offsetY)
        {
            Rectangle cellRect = new Rectangle(offsetX + col * cellSize, offsetY + row * cellSize, cellSize, cellSize);
            this.Invalidate(cellRect);
        }
    }
}