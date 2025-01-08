using System;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
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
        Gameboard playerBoard = new Gameboard();
        Gameboard opponentBoard = new Gameboard();

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

            // Render player board
            playerBoard.RenderBoard(g, 20, 150);

            // Render opponent board
            opponentBoard.RenderBoard(g, 420, 150);
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
                        playerBoard.PlaceShip(row, col); // Ukládání lodí na hráčovu desku
                        break;

                    case "Attack":
                        bool hit = opponentBoard.CheckHit(row, col); // Zásah na soupeřovu desku
                        string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                        SendMessageToClient(response); // Odeslání výsledku útoku klientovi
                        break;

                    case "Hit":
                        opponentBoard.MarkHit(row, col); // Zaznamenání zásahu na hráčovu desku
                        break;

                    case "Miss":
                        opponentBoard.MarkMiss(row, col); // Zaznamenání zásahu mimo hráčovu desku
                        break;
                }
            }
            this.Invalidate(); // Redraw the boards
        }

        private void ServerForm_MouseClick(object sender, MouseEventArgs e)
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
                }
            }
            // Kliknutí na desku protihráče
            else if (boardX >= 420 && boardX < 420 + 10 * 30 && boardY >= 150 && boardY < 150 + 10 * 30)
            {
                row = (boardY - 150) / 30;
                col = (boardX - 420) / 30;

                if (rbAttack.Checked)
                {
                    bool hit = opponentBoard.CheckHit(row, col); // Zde je chyba
                    string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                    SendMessageToClient(response);
                }
            }

            this.Invalidate(); // Redraw the form
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
                            Invoke(new Action(() => ProcessGameData(message))); // Zpracování na hlavním vlákně
                        }
                    }
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