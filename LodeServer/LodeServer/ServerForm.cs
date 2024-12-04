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
        Gameboard gameBoard = new Gameboard();
        public ServerForm()
        {
            InitializeComponent();
            InitializeServerComponents();
        }

        private void InitializeServerComponents()
        {
            this.Size = new Size(800, 600);

            // Přidání ovládacích prvků
            labelStatus.Top = 20;
            Controls.Add(labelStatus);

            labelData.Top = 50;
            Controls.Add(labelData);

            txtZprava.Top = 80;
            Controls.Add(txtZprava);

            btnOdeslat.Text = "Send";
            btnOdeslat.Top = 110;
            btnOdeslat.Click += new EventHandler(BtnOdeslat_Click);
            Controls.Add(btnOdeslat);

            // Spuštění serveru
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            serverSocket.Listen(1);
            labelStatus.Text = "Čekání na spojení";
            clientSocket = serverSocket.Accept();
            labelStatus.Text = "Klient připojen";

            // Časovač pro příjem dat
            casovac.Interval = 100;
            casovac.Tick += ZkontrolovatData;
            casovac.Start();

            // Povolit přepisování plochy (malování herní plochy)
            this.Paint += new PaintEventHandler(ServerForm_Paint);
        }

        private void ServerForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // Kreslení herní plochy
            gameBoard.RenderBoard(g, 20, 150);
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
                if (command == "PlaceShip")
                {
                    gameBoard.PlaceShip(row, col);
                    
                }

                else if (command == "Attack")
                {
                    bool hit = gameBoard.CheckHit(row, col);
                    string response = hit ? $"Hit,{row},{col}" : $"Miss,{row},{col}";
                    byte[] responseBytes = Encoding.Default.GetBytes(response);
                    clientSocket.Send(responseBytes);
                }
                else if (command == "Hit")
                {
                    gameBoard.MarkHit(row, col);
                }
                else if (command == "Miss")
                {
                    gameBoard.MarkMiss(row, col);
                }
                
            }
            this.Invalidate();
        }
    }
}
