using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ControlLibrary
{
    interface IConnect
    {
        string LocalIPAddress{ get;set;}
        int LocalPort{get;set;}
        string RemoteIPAddress{get;set;}
        int RemotePort{get;set;}

        bool OpenConnect();
        bool CloseConnect();
        void RcvData();
        bool SendData();
    }

    public class UDPConnect : IConnect
    {

        private Socket sckConnect;
        private string _localIPAddress = "127.0.0.1";
        public string LocalIPAddress
        {
            get { return _localIPAddress; }
            set
            {
                _localIPAddress = value;
            }
        }

        private int _localPort = 8080;
        public int LocalPort
        {
            get { return _localPort; }
            set { _localPort = value; }
        }

        private string _remoteIPAddress = "127.0.01";
        public string RemoteIPAddress
        {
            get { return _remoteIPAddress; }
            set { _remoteIPAddress = value; }
        }

        private int _remotePort = 8080;
        public int RemotePort
        {
            get { return _remotePort; }
            set { _remotePort = value; }
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

        public bool CloseConnect()
        {
            throw new NotImplementedException();
        }

        public bool OpenConnect()
        {
            sckConnect = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
            sckConnect.Bind(new IPEndPoint(IPAddress.Parse(LocalIPAddress),LocalPort));
            return true;
        }

        public void RcvData()
        {
            throw new NotImplementedException();
        }

        public bool SendData()
        {
            throw new NotImplementedException();
        }
    }
}
