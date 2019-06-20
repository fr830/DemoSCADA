using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ControlLibrary
{
    public delegate void  GetRcvBuffer(DataForm dt);

   interface IConnect
    {
        string LocalIPAddress{ get;set;}
        int LocalPort{get;set;}
        string RemoteIPAddress{get;set;}
        int RemotePort{get;set;}
        event GetRcvBuffer GetRcvBufferEvent;

        bool OpenConnect();
        void CloseConnect();
        void RcvData();
        void SendData(byte[] buffer);
    }

    public class UDPConnect : IConnect
    {

        private Socket sckConnect;
        private EndPoint toPoint;
        private EndPoint fromPoint = new IPEndPoint(IPAddress.Any, 0);
        private byte[] rcvBuffer = new byte[1024];
        Thread tRcv;
        public event GetRcvBuffer GetRcvBufferEvent;
        private string _localIPAddress = "127.0.0.1";
        DataForm dt = new DataForm();
        public string LocalIPAddress
        {
            get { return _localIPAddress; }
            set
            {
                IPAddress temp;
                if (IPAddress.TryParse(value, out temp))
                {
                    _localIPAddress = value;
                }
                else
                {
                    _localIPAddress = "127.0.0.1";
                }
            }
        }

        private int _localPort = 8080;
        public int LocalPort
        {
            get { return _localPort; }
            set
            {
                if (value > 0 && value < 65535)
                {
                    _localPort = value;
                }
                else
                {
                    _localPort = 8080;
                }
            }
        }

        private string _remoteIPAddress = "127.0.0.1";
        public string RemoteIPAddress
        {
            get { return _remoteIPAddress; }
            set
            {
                if (IPCheck(value))
                {
                    _remoteIPAddress =  value.Trim();
                }
                else
                {
                    _remoteIPAddress = "127.0.0.1";
                }
            }
        }

        private int _remotePort = 8081;
        public int RemotePort
        {
            get { return _remotePort; }
            set
            {
                if (value > 0 && value < 65535)
                {
                    _remotePort = value;
                }
                else
                {
                    _remotePort = 8081;
                }
            }
        }

        public bool IPCheck(string IP)
        {
            return Regex.IsMatch(IP.Trim(), @"^(\d{1,3}.){3}(\d{1,3})$");
        }

        public UDPConnect(string localIP,int localPort,string remoteIP,int remotePort)
        {
            LocalIPAddress = localIP;
            LocalPort = localPort;
            RemoteIPAddress = remoteIP;
            RemotePort = remotePort;
        }

        public UDPConnect(string localIP, int localPort)
        {
            LocalIPAddress = localIP;
            LocalPort = localPort;
        }

        public void CloseConnect()
        {
            if (tRcv.IsAlive)
            {
                tRcv.Abort();
                tRcv = null;
            }

            if(sckConnect != null)
            {
                sckConnect.Close();
                sckConnect = null;
            }
        }

        public bool OpenConnect()
        {
            try
            {
                sckConnect = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sckConnect.Bind(new IPEndPoint(IPAddress.Parse(LocalIPAddress), LocalPort));
                tRcv = new Thread(RcvData) { Name="Thread_Rcv",IsBackground=true};
                tRcv.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);

                return false;
            }

            return true;
        }

        public void RcvData()
        {
            while (true)
            {
                int length = sckConnect.ReceiveFrom(rcvBuffer, ref fromPoint);

                if((GetRcvBufferEvent != null) && (length > 0))
                {
                    dt.SetValue(false, Encoding.UTF8.GetString(rcvBuffer, 0, length), fromPoint.ToString(), length);
                    GetRcvBufferEvent(dt);
                }
            }
        }

        public void SendData(byte[] buffer)
        {
            if (toPoint == null)
            {
                toPoint = new IPEndPoint(IPAddress.Parse(RemoteIPAddress), RemotePort);
            }

            try
            {
                sckConnect.SendTo(buffer, toPoint);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
