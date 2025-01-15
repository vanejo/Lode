using System;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Lode;

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
        Gameboard playerBoard = new Gameboard();   // Server's own board
        Gameboard opponentBoard = new Gameboard(); // Displays hits/misses on client side

        public ServerForm()
        {
            InitializeComponent();
            InitializeServerComponents();
        }

        private void InitializeServerComponents()
        {
            this.Size = new Size(800, 600);

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
            rbPlaceShip.Checked = true; // Default option
            Controls.Add(rbPlaceShip);

            // Attack radio button
            rbAttack.Text = "Attack";
            rbAttack.Top = 110;
            rbAttack.Left = 20;
            Controls.Add(rbAttack);

            // Socket setup
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            serverSocket.Listen(1);
            labelStatus.Text = "Waiting for connection...";

            // Accept a single client connection
            clientSocket = serverSocket.Accept();
            labelStatus.Text = "Client connected";

            // Start receiving data in a separate thread
            Thread receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            this.Paint += new PaintEventHandler(ServerForm_Paint);
            this.MouseClick += new MouseEventHandler(ServerForm_MouseClick);
        }

        private void ServerForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Render server's (player) board on the left
            playerBoard.RenderBoard(g, 20, 150);

            // Render opponent's board on the right
            opponentBoard.RenderBoard(g, 420, 150);
        }

        // Process data received from the client
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
                        SendMessageToClient(response);
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

        private void ServerForm_MouseClick(object sender, MouseEventArgs e)
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
                }
            }
            else if (boardX >= 420 && boardX < 420 + 10 * 30 && boardY >= 150 && boardY < 150 + 10 * 30)
            {
                row = (boardY - 150) / 30;
                col = (boardX - 420) / 30;

                if (rbAttack.Checked)
                {
                    SendMessageToClient($"Attack,{row},{col}");
                }
            }

            this.Invalidate(); 
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

        private void SendMessageToClient(string message)
        {
            byte[] responseBytes = Encoding.Default.GetBytes(message);
            clientSocket.Send(responseBytes);
        }
    }
}