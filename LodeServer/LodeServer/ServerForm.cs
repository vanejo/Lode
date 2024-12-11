using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System;

namespace LodeServer
{
    public partial class ServerForm : Form
    {
        static byte[] data;
        static Socket serverSocket;
        static Socket clientSocket;
        Label labelStatus = new Label();
        Label labelData = new Label();
        Timer casovac = new Timer();
        TextBox txtZprava = new TextBox();
        Button btnOdeslat = new Button();
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

            labelStatus.Top = 20;
            labelStatus.Left = 20;
            Controls.Add(labelStatus);

            labelData.Top = 50;
            labelData.Left = 20;
            Controls.Add(labelData);

            txtZprava.Top = 80;
            txtZprava.Left = 20;
            Controls.Add(txtZprava);

            btnOdeslat.Text = "Send";
            btnOdeslat.Top = 110;
            btnOdeslat.Click += new EventHandler(BtnOdeslat_Click);
            Controls.Add(btnOdeslat);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            serverSocket.Listen(1);
            labelStatus.Text = "Čekání na spojení";
            clientSocket = serverSocket.Accept();
            labelStatus.Text = "Klient připojen";

            casovac.Interval = 100;
            casovac.Tick += ZkontrolovatData;
            casovac.Start();

            this.Paint += new PaintEventHandler(ServerForm_Paint);
        }

        private void ServerForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            
            playerBoard.RenderBoard(g, 20, 150);
            opponentBoard.RenderBoard(g, 420, 150);
        }

        private void ZkontrolovatData(object sender, EventArgs e)
        {
            if (clientSocket.Available > 0)
            {
                byte[] data = new byte[clientSocket.ReceiveBufferSize];
                int bytesRead = clientSocket.Receive(data);
                if (bytesRead > 0)
                {
                    string message = Encoding.Default.GetString(data, 0, bytesRead);
                    labelData.Text = message;
                    
                    ProcessGameData(message);
                }
            }
        }

        private void BtnOdeslat_Click(object sender, EventArgs e)
        {
            string message = txtZprava.Text;
            byte[] data = Encoding.Default.GetBytes(message);
            clientSocket.Send(data);
        }

        private void ProcessGameData(string message)
        {
            string[] casti = message.Split(',');
            if (casti.Length == 3)
            {
                string command = casti[0];
                int row = int.Parse(casti[1]);
                int col = int.Parse(casti[2]);

                switch (command)
                {
                    case "PlaceShip":
                        playerBoard.PlaceShip(row, col);
                        break;
                    case "Attack":
                        bool hit = playerBoard.CheckHit(row, col);
                        string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                        byte[] responseBytes = Encoding.Default.GetBytes(response);
                        clientSocket.Send(responseBytes);
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
    }
}
