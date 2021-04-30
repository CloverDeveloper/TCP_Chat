using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// 1.引用名稱空間
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace TCP.Server
{
    public partial class Form1 : Form
    {
        TcpListener server;
        Socket client;
        Thread th_Server;
        Thread th_Client;
        Hashtable ht;

        public Form1()
        {
            InitializeComponent();
            this.ht = new Hashtable();
            this.button1.Click += ActivationServer;
            this.FormClosing += FormClose;
            this.textBox1.Text = this.GetIP();
        }

        /// <summary>
        /// 取得本機 IP
        /// </summary>
        /// <returns></returns>
        private string GetIP() 
        {
            // 取得本機 IP 陣列
            IPAddress[] ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

            foreach(IPAddress ip in ips) 
            {
                // 如果是 IP V4 回傳此 IP 字串
                if (ip.AddressFamily == AddressFamily.InterNetwork) 
                    return ip.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// 啟動伺服器
        /// </summary>
        private void ActivationServer(object sender,EventArgs e) 
        {
            CheckForIllegalCrossThreadCalls = false;
            this.th_Server = new Thread(new ThreadStart(ServerSub));
            this.th_Server.IsBackground = true;
            this.th_Server.Start();
            this.button1.Enabled = false;
        }

        /// <summary>
        /// 伺服器建立方法
        /// </summary>
        private void ServerSub()
        {
            // 建立本機端點
            IPEndPoint ipEP =
                new IPEndPoint(IPAddress.Parse(this.textBox1.Text), int.Parse(this.textBox2.Text));

            // 使用本機端點初始化類別
            this.server = new TcpListener(ipEP);
            // 允許最多的連線數
            this.server.Start(5);

            // 監聽等待客戶連線
            while (true)
            {
                this.client = this.server.AcceptSocket();
                this.th_Client = new Thread(new ThreadStart(Listen));
                this.th_Client.IsBackground = true;
                this.th_Client.Start();
            }
        }

        /// <summary>
        /// 監聽使用者傳遞來的資訊
        /// </summary>
        private void Listen()
        {
            Socket sck = this.client;
            Thread th = this.th_Client;

            try 
            {
                while (true)
                {
                    // 接收客戶端傳來的資料
                    byte[] receiveByte = new byte[1023];
                    int inLen = sck.Receive(receiveByte);
                    string msg = Encoding.Default.GetString(receiveByte, 0, inLen);
                    string code = msg.Substring(0, 1); // 取得指令代碼
                    string userName = msg.Substring(1); // 取得使用者名稱

                    if (code == "0") // 使用者登入
                    {
                        this.ht.Add(userName, sck);
                        this.listBox1.Items.Add(userName);
                    }

                    if (code == "9") // 使用者離開
                    {
                        this.ht.Remove(userName);
                        this.listBox1.Items.Remove(userName);
                        th.Abort();
                        sck.Close();
                    }
                }
            }
            catch(Exception ex)
            {
                // 通常為使用者無預警關閉程式
            }
        }

        /// <summary>
        /// 表單關閉事件
        /// </summary>
        private void FormClose(object sender,FormClosingEventArgs e) 
        {
            Application.ExitThread();
        }
    }
}
