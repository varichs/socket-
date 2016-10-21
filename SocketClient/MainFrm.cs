using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketClient
{
    public partial class MainFrm : Form
    {
        public Socket ClientSocket { set; get; }
        public MainFrm()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            //客户端连接服务器
            //第一步：创建socket对象
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ClientSocket = socket;
            //第二步：连接服务器
            try
            {
                socket.Connect(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
                return;
            }
            //第三步：接收消息
            Thread thread = new Thread(new ParameterizedThreadStart(RecevieData));
            thread.IsBackground = true;
            thread.Start(ClientSocket);
            
        }
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
                    AppendTextToTxtLog(string.Format("服务器：{0}异常退出", proxSocket.RemoteEndPoint.ToString()));
                    StopConnect();
                    return;//线程结束
                }

                //正常退出
                if (len <= 0)
                {
                    AppendTextToTxtLog(string.Format("服务器端：{0}正常退出", proxSocket.RemoteEndPoint.ToString()));
                    StopConnect();
                    return;//线程结束
                }

                string str = Encoding.Default.GetString(data, 0, len);
                AppendTextToTxtLog(string.Format("接收到客户端：{0}的消息：{1}", proxSocket.RemoteEndPoint.ToString(), str));
            }
        }

        private void StopConnect()
        {
            try
            {
                if (ClientSocket.Connected)
                {
                    ClientSocket.Shutdown(SocketShutdown.Both);
                    ClientSocket.Close(100);
                }
            }
            catch (Exception)
            {

                
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
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            if (ClientSocket.Connected)
            {
                byte[] data = Encoding.Default.GetBytes(txtMsg.Text);
                ClientSocket.Send(data, 0, data.Length, SocketFlags.None);
            }
        }
    }
}
