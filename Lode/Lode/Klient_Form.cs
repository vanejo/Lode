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
        Gameboard playerBoard = new Gameboard();   // Client's own board
        Gameboard opponentBoard = new Gameboard(); // Displays hits/misses from the server

        public Klient_Form()
        {
            InitializeComponent();
            InitializeGameComponents();
        }

        private void InitializeGameComponents()
        {
            this.Size = new Size(800, 600);

           
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

            
            playerBoard.RenderBoard(g, 20, 150);

            opponentBoard.RenderBoard(g, 420, 150);
        }

        private void Klient_Form_MouseClick(object sender, MouseEventArgs e)
        {
            int boardX = e.X;
            int boardY = e.Y;
            int row, col;

            if (boardX >= 20 && boardX < 20 + 10 * 30 && boardY >= 150 && boardY < 150 + 10 * 30)
            {
                row = (boardY - 150) / 30;
                col = (boardX - 20) / 30;

                if (rbPlaceShip.Checked)
                {
                    playerBoard.PlaceShip(row, col);
                    SendMessageToServer($"PlaceShip,{row},{col}");
                }
            }
            else if (boardX >= 420 && boardX < 420 + 10 * 30 && boardY >= 150 && boardY < 150 + 10 * 30)
            {
                row = (boardY - 150) / 30;
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
            if (parts.Length == 3)
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