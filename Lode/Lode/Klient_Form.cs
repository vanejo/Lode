using Lode;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System;

namespace LodeClient
{
    public partial class Klient_Form : Form
    {
        static Socket clientSocket;
        Gameboard gameBoard = new Gameboard();
        TextBox txtZprava = new TextBox();
        Button btnOdeslat = new Button();
        Label labelResponse = new Label();
        Timer responseTimer = new Timer();

        public Klient_Form()
        {
            InitializeComponent();
            InitializeGameComponents();
        }

        private void InitializeGameComponents()
        {
            // Nastavení velikosti formuláře
            this.Size = new Size(800, 600);

            // Přidání TextBoxu, tlačítka a labelu
            txtZprava.Left = 20;
            txtZprava.Top = 20;
            txtZprava.Width = 200;
            Controls.Add(txtZprava);

            btnOdeslat.Left = 240;
            btnOdeslat.Top = 20;
            btnOdeslat.Text = "Odeslat";
            btnOdeslat.Click += new EventHandler(BtnOdeslat_Click);
            Controls.Add(btnOdeslat);

            labelResponse.Left = 20;
            labelResponse.Top = 60;
            labelResponse.Width = 300;
            Controls.Add(labelResponse);

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(IPAddress.Parse("127.0.0.1"), 5555);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Spojení se nezdařilo: " + ex.Message);
                return;
            }

            responseTimer.Interval = 100;
            responseTimer.Tick += CheckForResponse;
            responseTimer.Start();

            this.Paint += new PaintEventHandler(Klient_Form_Paint);
        }

        private void Klient_Form_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            gameBoard.RenderBoard(g, 20, 100);
        }

        private void BtnOdeslat_Click(object sender, EventArgs e)
        {
            string zprava = txtZprava.Text;
            byte[] data = Encoding.Default.GetBytes(zprava);

            clientSocket.Send(data);
            
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

        private void ProcessGameData(string data)
        {
            string[] casti = data.Split(',');
            if (casti.Length == 3)
            {
                string command = casti[0];
                int row = int.Parse(casti[1]);
                int col = int.Parse(casti[2]);

                if (command == "Hit")
                {
                    gameBoard.MarkHit(row, col);
                }
                
                else if (command == "Miss")
                {
                    gameBoard.MarkMiss(row, col);
                }
                else if (command == "PlaceShip")
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

            }
            this.Invalidate();
        }
    }
}
