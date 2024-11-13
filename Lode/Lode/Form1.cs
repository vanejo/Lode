using System;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Drawing;

namespace Lode
{
    public partial class Form1 : Form
    {
        static byte[] data;
        TextBox txtZprava = new TextBox();
        Button btnOdeslat = new Button();
        Label labelResponse = new Label();
        Timer responseTimer = new Timer();
        static Socket clientSocket;

        public Form1()
        {
            InitializeComponent();

            txtZprava.Left = 20;
            txtZprava.Top = 20;
            txtZprava.Width = 200;
            txtZprava.Font = new Font("Calibri", 12);
            Controls.Add(txtZprava);

            btnOdeslat.Left = 240;
            btnOdeslat.Top = 20;
            btnOdeslat.Text = "Odeslat";
            btnOdeslat.Font = new Font("Calibri", 12);
            btnOdeslat.Click += new EventHandler(BtnOdeslat_Click);
            Controls.Add(btnOdeslat);

            labelResponse.Left = 20;
            labelResponse.Top = 60;
            labelResponse.Width = 300;
            labelResponse.Font = new Font("Calibri", 12);
            Controls.Add(labelResponse);

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                      ProtocolType.Tcp);
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
            responseTimer.Tick += ZkontrolovatOdpoved;
            responseTimer.Start();
        }

        private void BtnOdeslat_Click(object sender, EventArgs e)
        {
            string zprava = txtZprava.Text;

            byte[] sendData = Encoding.Default.GetBytes(zprava);
            clientSocket.Send(sendData);
        }

        private void ZkontrolovatOdpoved(object sender, EventArgs e)
        {
            if (clientSocket != null && clientSocket.Connected)
            {
                if (clientSocket.Available > 0)
                {
                    data = new byte[clientSocket.ReceiveBufferSize];
                    int bytesRead = clientSocket.Receive(data);
                    if (bytesRead > 0)
                    {
                        string ziskanyText = Encoding.Default.GetString(data, 0, bytesRead);
                        labelResponse.Text = "Odpověď: " + ziskanyText;
                    }
                }
            }
        }
    }
}
