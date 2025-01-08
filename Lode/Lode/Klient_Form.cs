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
        Gameboard playerBoard = new Gameboard();
        Gameboard opponentBoard = new Gameboard();

        public Klient_Form()
        {
            InitializeComponent();
            InitializeGameComponents();
        }

        private void InitializeGameComponents()
        {
            this.Size = new Size(800, 600);

            // Response label
            labelResponse.Left = 20;
            labelResponse.Top = 20;
            labelResponse.Width = 300;
            Controls.Add(labelResponse);

            // Place Ship radio button
            rbPlaceShip.Text = "Place Ship";
            rbPlaceShip.Left = 20;
            rbPlaceShip.Top = 50;
            rbPlaceShip.Checked = true; // Default option
            Controls.Add(rbPlaceShip);

            // Attack radio button
            rbAttack.Text = "Attack";
            rbAttack.Left = 20;
            rbAttack.Top = 80;
            Controls.Add(rbAttack);

            // Initialize socket connection
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

            Timer responseTimer = new Timer();
            responseTimer.Interval = 100;
            responseTimer.Tick += CheckForResponse;
            responseTimer.Start();
        }

        private void Klient_Form_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Render player board
            playerBoard.RenderBoard(g, 20, 150);

            // Render opponent board
            opponentBoard.RenderBoard(g, 420, 150);
        }

        private void Klient_Form_MouseClick(object sender, MouseEventArgs e)
        {
            int boardX = e.X;
            int boardY = e.Y;
            int row, col;

            // Kliknutí na desku hráče
            if (boardX >= 20 && boardX < 20 + 10 * 30 && boardY >= 150 && boardY < 150 + 10 * 30)
            {
                row = (boardY - 150) / 30;
                col = (boardX - 20) / 30;

                if (rbPlaceShip.Checked)
                {
                    playerBoard.PlaceShip(row, col); // Správně
                    SendMessageToServer($"PlaceShip,{row},{col}");
                }
            }
            // Kliknutí na desku protihráče
            else if (boardX >= 420 && boardX < 420 + 10 * 30 && boardY >= 150 && boardY < 150 + 10 * 30)
            {
                row = (boardY - 150) / 30;
                col = (boardX - 420) / 30;

                if (rbAttack.Checked)
                {
                    SendMessageToServer($"Attack,{row},{col}"); // Správně odesílá útok
                }
            }

            this.Invalidate(); // Redraw the form
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
                        playerBoard.PlaceShip(row, col); // OK
                        break;

                    case "Attack":
                        bool hit = opponentBoard.CheckHit(row, col); // Zde je správné volání
                        string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                        SendMessageToServer(response); // Správně posílá výsledek
                        break; 

                    case "Hit":
                        opponentBoard.MarkHit(row, col); // Zde může být problém
                        break;

                    case "Miss":
                        opponentBoard.MarkMiss(row, col); // Zde může být problém
                        break;
                }
            }
            this.Invalidate(); // Redraw the boards
        }

        private void SendMessageToServer(string message)
        {
            byte[] data = Encoding.Default.GetBytes(message);
            clientSocket.Send(data);
        }
    }
}
