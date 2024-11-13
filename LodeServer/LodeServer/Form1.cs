using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace LodeServer
{
    public partial class Form1 : Form
    {
        static byte[] data;
        static Socket socket_server;
        static Socket clientSocket;
        Label labelStatus = new Label();
        Label labelData = new Label();
        Timer casovac = new Timer();
        TextBox txtZprava = new TextBox();
        Button btnOdeslat = new Button();

        public Form1()
        {
            InitializeComponent();

            labelStatus.Left = 20;
            labelStatus.Top = 20;
            labelStatus.Text = "Server není spuštěn.";
            labelStatus.Width = 300;
            labelStatus.Font = new Font("Calibri", 12);
            Controls.Add(labelStatus);

            labelData.Left = 20;
            labelData.Top = 50;
            labelData.Text = "Data nejsou";
            labelData.Width = 300;
            labelData.Font = new Font("Calibri", 12);
            Controls.Add(labelData);

            Controls.Add(txtZprava);
            
            btnOdeslat.Left = 240; 
            btnOdeslat.Top = 80; 
            btnOdeslat.Text = "Odeslat"; 
            btnOdeslat.Font = new Font("Calibri", 12); 
            btnOdeslat.Click += new EventHandler(BtnOdeslat_Click);
            Controls.Add(btnOdeslat);

            txtZprava.Left = 20; 
            txtZprava.Top = 80; 
            txtZprava.Width = 200; 
            txtZprava.Font = new Font("Calibri", 12); 
            Controls.Add(txtZprava);

            labelStatus.Text = "Spuštění serveru";
            socket_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                       ProtocolType.Tcp);
            socket_server.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555));
            socket_server.Listen(1);
            labelStatus.Text = "Server spuštěn.";

            clientSocket = socket_server.Accept();

            casovac.Interval = 100;
            casovac.Tick += ZkontrolovatData;
            casovac.Start();
        }

        private void ZkontrolovatData(object sender, EventArgs e)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                if (clientSocket.Available > 0)
                {
                    data = new byte[clientSocket.SendBufferSize];
                    int bytesRead = clientSocket.Receive(data);

                    if (bytesRead > 0)
                    {
                        string ziskanyText =
                            Encoding.Default.GetString(data, 0, bytesRead);
                        labelData.Text = ziskanyText;

                        string odpoved = "Data přijata: " + ziskanyText;
                        byte[] odpoBytes = Encoding.Default.GetBytes(odpoved);
                        clientSocket.Send(odpoBytes);
                    }
                }
            }
        }
        private void BtnOdeslat_Click(object sender, EventArgs e) 
        { 
            if (clientSocket != null && clientSocket.Connected) 
            { 
                string zprava = txtZprava.Text;
                byte[] sendData = Encoding.Default.GetBytes(zprava);
                clientSocket.Send(sendData);
            } 
        }
    }
}
