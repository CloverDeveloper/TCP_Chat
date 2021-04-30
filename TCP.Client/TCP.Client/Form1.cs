using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// 引用名稱空間
using System.Net;
using System.Net.Sockets;

namespace TCP.Client
{
    public partial class Form1 : Form
    {
        Socket socket;
        string userName;

        public Form1()
        {
            InitializeComponent();
            this.button1.Click += LoginServer;
            this.FormClosing += FormClose;
            this.textBox1.Text = "192.168.0.192";
            this.textBox2.Text = "222";
        }

        /// <summary>
        /// 登入伺服器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginServer(object sender,EventArgs e) 
        {
            IPEndPoint ipEP = 
                new IPEndPoint(IPAddress.Parse(this.textBox1.Text), int.Parse(this.textBox2.Text));

            // 使用指定的通訊協定家族 (Family)、通訊端類型和通訊協定，初始化 Socket 類別的新執行個體。
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.userName = this.textBox3.Text;
            try 
            {
                this.socket.Connect(ipEP);
                this.Send("0" + this.userName);
            }
            catch(Exception ex) 
            {
                MessageBox.Show("無法連線");
                return;
            }

            this.button1.Enabled = false;
        }

        /// <summary>
        /// 傳遞訊息到 Server
        /// </summary>
        /// <param name="str"></param>
        private void Send(string str) 
        {
            var sendBytes = Encoding.Default.GetBytes(str);
            this.socket.Send(sendBytes,SocketFlags.None);
        }

        /// <summary>
        /// 表單關閉事件
        /// </summary>
        private void FormClose(object sender,FormClosingEventArgs e) 
        {
            if (this.button1.Enabled) return;

            this.Send("9" + this.userName);
            this.socket.Close();
        }
    }
}
