using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace ChatDemo
{
    public partial class MainFrm : Form
    {
        List<Socket> ClientProxSocketList = new List<Socket>();
        public MainFrm()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            //第一步：创建socket对象
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //第二步：绑定端口IP
            socket.Bind(new IPEndPoint(IPAddress.Parse(txtIP.Text),int.Parse(txtPort.Text)));

            //第三步：开启端口监听
            socket.Listen(10);//队列中等待连接的最大数量，其他链接会返回连接错误

            //第四步：开始接受客户端的连接
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.AcceptClientConnect), socket);
        

        }
        public void AcceptClientConnect(object socket)
        {
            var serverSocket = socket as Socket;

            this.AppendTextToTxtLog("服务器开始接收客户端的链接");
            while (true)
            {
                var proxSocket = serverSocket.Accept();
                ClientProxSocketList.Add(proxSocket);
                this.AppendTextToTxtLog(string.Format("客户端：{0} 连接上了",proxSocket.RemoteEndPoint.ToString()));
                ThreadPool.QueueUserWorkItem(new WaitCallback(RecevieData), proxSocket);//委托
            }
        }
        /// <summary>
        /// 接受客户端的消息
        /// </summary>
        /// <param name="socket"></param>
        public void RecevieData(object socket)
        {
            var proxSocket = socket as Socket;
            //缓冲区
            byte[] data = new byte[1024 * 1024];
            while (true)
            {
                int len = 0;
                try
                {
                    len = proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
                }
                catch
                {
                    //异常退出
                    AppendTextToTxtLog(string.Format("客户端：{0}异常退出", proxSocket.RemoteEndPoint.ToString()));
                    ClientProxSocketList.Remove(proxSocket);
                    return;//线程结束
                }
                
                //正常退出
                if (len <= 0)
                {
                    AppendTextToTxtLog(string.Format("客户端：{0}正常退出", proxSocket.RemoteEndPoint.ToString()));
                    ClientProxSocketList.Remove(proxSocket);
                    return;//线程结束
                }

                string str = Encoding.Default.GetString(data, 0, len);
                AppendTextToTxtLog(string.Format("接收到客户端：{0}的消息：{1}",proxSocket.RemoteEndPoint.ToString(),str));
            }
        }

        public void AppendTextToTxtLog(string txt)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(s => {
                    txtLog.Text = String.Format("{0}\r\n{1}", s, txtLog.Text);
                }), txt);
            }
            else
            {
               //不考虑跨线程
                txtLog.Text = String.Format("{0}\r\n{1}", txt, txtLog.Text);
            }
            
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            foreach(var proxSocket in ClientProxSocketList)
            {
                //判断是否是连接状态
                if (proxSocket.Connected)
                {
                    byte[] data = Encoding.Default.GetBytes(txtMsg.Text);
                    proxSocket.Send(data, 0, data.Length, SocketFlags.None);

                }
            }
        }
    }
}
