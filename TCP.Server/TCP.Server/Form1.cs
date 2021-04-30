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
                    string str = msg.Substring(1); // 取得使用者名稱

                    if (code == "0") // 使用者登入
                    {
                        this.ht.Add(str, sck);
                        this.listBox1.Items.Add(str);
                        this.SendAll(this.GetOnlineList());
                    }

                    if (code == "9") // 使用者離開
                    {
                        this.ht.Remove(str);
                        this.listBox1.Items.Remove(str);
                        this.SendAll(this.GetOnlineList());
                        th.Abort();
                        sck.Close();
                    }

                    if(code == "1") // 傳遞訊息給所有人
                    {
                        // 完整格式應寫成："1+訊息"。
                        this.SendAll(msg); // 廣播所有人
                    }

                    if(code == "2") // 使用者傳送私密訊息 
                    {
                        // 完整訊息應該寫成："2 + 訊息 + "|" + 目標用戶
                        string[] strArray = str.Split("|"); // 切開訊息與收件者

                        this.SendTo(code + strArray[0], strArray[1]); // strArray[0] 是訊息， strArray[1] 是收件者
                    }
                }
            }
            catch(Exception ex)
            {
                // 通常為使用者無預警關閉程式
            }
        }

        /// <summary>
        /// 傳遞訊息給指定對象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="userName"></param>
        private void SendTo(string str,string userName) 
        {
            // 將訊息轉為 byte 陣列
            byte[] sendBytes = Encoding.Default.GetBytes(str);
            // 取得對象 Socket
            Socket target = this.ht[userName] as Socket;
            // 發送訊息
            target.Send(sendBytes, 0, sendBytes.Length, SocketFlags.None);
        }

        /// <summary>
        /// 對所有人進行廣播
        /// </summary>
        private void SendAll(string str) 
        {
            // 將訊息轉為 byte 陣列
            byte[] sendBytes = Encoding.Default.GetBytes(str);

            // 取得雜湊表內所有的 Socket 資料
            foreach(Socket target in this.ht.Values) 
            {
                target.Send(sendBytes, 0, sendBytes.Length, SocketFlags.None);
            }
        }

        /// <summary>
        /// 建立線上人員名單，供客戶端更新
        /// </summary>
        /// <returns></returns>
        private string GetOnlineList()
        {
            var res = "L";

            for(int i = 0; i< this.listBox1.Items.Count; i += 1)
            {
                res += this.listBox1.Items[i];
                // 非最後一位都要加上 , 區隔
                if (i < this.listBox1.Items.Count - 1) res += ",";
            }

            return res;
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
