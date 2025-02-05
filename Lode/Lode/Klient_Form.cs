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
        Gameboard playerBoard = new Gameboard();   // Client's own board
        Gameboard opponentBoard = new Gameboard(); // Displays hits/misses from the server

        private Image waterImage;
        private Image shipImage;
        private Image hitImage;
        private Image missImage;

        private NumericUpDown nudShipSize = new NumericUpDown();
        private ComboBox cbOrientation = new ComboBox();

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

            // Render client's (player) board on the left
            playerBoard.RenderBoard(g, 20, 200, waterImage, shipImage, hitImage, missImage);

            // Render opponent's board on the right
            opponentBoard.RenderBoard(g, 420, 200, waterImage, shipImage, hitImage, missImage);
        }

        private void Klient_Form_MouseClick(object sender, MouseEventArgs e)
        {
            int boardX = e.X;
            int boardY = e.Y;
            int row, col;

            if (boardX >= 20 && boardX < 20 + 10 * 30 && boardY >= 200 && boardY < 200 + 10 * 30)
            {
                row = (boardY - 200) / 30;
                col = (boardX - 20) / 30;

                if (rbPlaceShip.Checked)
                {
                    int shipSize = (int)nudShipSize.Value;
                    bool isHorizontal = cbOrientation.SelectedItem.ToString() == "Horizontal";
                    playerBoard.PlaceShip(row, col, shipSize, isHorizontal); // Place a ship of variable size
                    SendMessageToServer($"PlaceShip,{row},{col},{shipSize},{isHorizontal}");
                }
            }
            else if (boardX >= 420 && boardX < 420 + 10 * 30 && boardY >= 200 && boardY < 200 + 10 * 30)
            {
                row = (boardY - 200) / 30;
                col = (boardX - 420) / 30;

                if (rbAttack.Checked)
                {
                    SendMessageToServer($"Attack,{row},{col}");
                }
            }

            this.Invalidate();
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
                        bool hit = playerBoard.CheckHit(row, col);
                        string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                        SendMessageToServer(response);
                        break;

                    case "Hit":
                        opponentBoard.MarkHit(row, col);
                        break;
                    case "Miss":
                        opponentBoard.MarkMiss(row, col);
                        break;
                }
            }
            this.Invalidate();
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
    }
}