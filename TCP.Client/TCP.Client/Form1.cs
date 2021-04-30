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
// 因為需要做到收與發因此要引入
using System.Threading;

namespace TCP.Client
{
    public partial class Form1 : Form
    {
        Socket socket;
        string userName;
        Thread th; // 網路監聽執行緒

        public Form1()
        {
            InitializeComponent();
            this.button1.Click += LoginServer;
            this.button3.Click += SendAll;
            this.button2.Click += SendTo;
            this.FormClosing += FormClose;
            this.button2.Enabled = false; // 等連上伺服器後才可啟用
            this.button3.Enabled = false;
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
            CheckForIllegalCrossThreadCalls = false; // 忽略跨執行緒錯誤

            IPEndPoint ipEP = 
                new IPEndPoint(IPAddress.Parse(this.textBox1.Text), int.Parse(this.textBox2.Text));

            // 使用指定的通訊協定家族 (Family)、通訊端類型和通訊協定，初始化 Socket 類別的新執行個體。
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.userName = this.textBox3.Text;
            try 
            {
                this.socket.Connect(ipEP);
                this.th = new Thread(new ThreadStart(Listen)); // 建立監聽執行緒
                this.th.IsBackground = true; // 設為背景執行緒
                this.th.Start(); // 開始監聽
                this.textBox4.Text = "已連線伺服器\n";
                this.Send("0" + this.userName);
            }
            catch(Exception ex) 
            {
                this.textBox4.Text = "無法連上伺服器\n";
                return;
            }

            this.button1.Enabled = false;
            this.button2.Enabled = true;
            this.button3.Enabled = true;
        }

        /// <summary>
        /// 監聽執行緒方法
        /// </summary>
        private void Listen()
        {
            EndPoint serverEP = this.socket.RemoteEndPoint; // Server 的 EndPoint
            byte[] receiveBytes = new byte[1023]; // 接受用 byte 陣列
            int inLen = 0; // 接收的位元組數目
            string msg = string.Empty; // 接收到的完整訊息
            string code = string.Empty; // 命令碼
            string str = string.Empty; // 訊息內容(不含命令碼)

            while (true) // 持續監聽 Server 傳來的訊息 
            {
                try 
                {
                    inLen = this.socket.ReceiveFrom(receiveBytes, 0, receiveBytes.Length, SocketFlags.None, ref serverEP);
                }
                catch(Exception ex) 
                {
                    this.socket.Close();
                    this.listBox1.Items.Clear();
                    MessageBox.Show("伺服器斷線了");
                    this.button1.Enabled = true;
                    this.button2.Enabled = false;
                    this.button3.Enabled = false;
                    this.th.Abort();
                    return;
                }
                msg = Encoding.Default.GetString(receiveBytes, 0, inLen); // 解讀完整訊息
                code = msg.Substring(0, 1); // 取得命令碼
                str = msg.Substring(1); // 取得命令碼之後的訊息

                if(code == "L") // 接收線上名單
                {
                    this.listBox1.Items.Clear(); // 清除名單
                    string[] strArray = str.Split(","); // 拆解成名單陣列
                    foreach(string val in strArray)
                    {
                        this.listBox1.Items.Add(val); // 逐一加入名單
                    }
                }

                if(code == "1") // 接收廣播訊息
                {
                    this.textBox4.Text += "(公開)" + str + "\n";
                }

                if(code == "2") // 接收私密訊息
                {
                    this.textBox4.Text += "(私密)" + str + "\n";
                }

            }
        }

        /// <summary>
        /// 廣播按鈕事件
        /// </summary>
        private void SendAll(object sender,EventArgs e) 
        {
            if (string.IsNullOrEmpty(this.textBox5.Text)) return;

            this.listBox1.ClearSelected(); // 清除選取項目，不選特定人即為廣播

            this.Send("1" + this.userName + "公告:" + this.textBox5.Text);

            this.textBox5.Text = ""; // 清除發言框
        }

        /// <summary>
        /// 發話事件
        /// </summary>
        private void SendTo(object sender,EventArgs e) 
        {
            if (string.IsNullOrEmpty(this.textBox5.Text)) return;

            // 無選取對象 = 廣播
            if (this.listBox1.SelectedItem == null)
            {
                this.SendAll(sender, e);
                return;
            }

            // 私密發話
            this.Send("2" + "來自" + this.userName + ":" + this.textBox5.Text + "|" + this.listBox1.SelectedItem);

            this.textBox4.Text += "告訴" + this.listBox1.SelectedItem + ":" + this.textBox5.Text + "\n";

            this.textBox5.Text = ""; // 清除發言框
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
            Application.ExitThread();
        }
    }
}
