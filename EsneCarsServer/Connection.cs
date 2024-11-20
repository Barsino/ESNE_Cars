using System;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace EsneCarsServer
{
    internal class Connection
    {
        static byte[] inBuffer = new byte[2048];
        static byte[] outBuffer = new byte[2048];

        private TcpClient client;
        public Socket Socket { get; private set; }

        public Action<string> SendToAllDelegate;
        public Action<string, Connection> SendToAllExceptThisDelegate;

        public Connection(TcpClient inClient)
        {
            client = inClient;
            Socket = client.Client;
        }

        public override string ToString()
        {
            if (client.Client.Connected)
            {
                return ((IPEndPoint)Socket.LocalEndPoint).Address.ToString() + ":" + ((IPEndPoint)Socket.LocalEndPoint).Port.ToString();
            }
            else return "Client disconnected";
        }

        public bool Process()
        {
            // Si no hay datos...
            if (Socket.Poll(1, SelectMode.SelectRead) && Socket.Available == 0)
            {
                Console.WriteLine("Client {0} disconnected.", this);
                SendToAllExceptThisDelegate.Invoke("User disconnected", this);

                return false;
            }

            // Si hay datos...
            if (Socket.Available > 0)
            {
                int bytesReceived = Socket.Available;
                byte[] strBuffer = new byte[bytesReceived];

                Socket.Receive(inBuffer, bytesReceived, SocketFlags.None);
                Buffer.BlockCopy(inBuffer, 0, strBuffer, 0, bytesReceived);

                string str = Encoding.ASCII.GetString(strBuffer);

                Console.WriteLine(str);

                SendToAllExceptThisDelegate.Invoke(str, this);
            }

            return true;
        }

        public void Send(string str)
        {
            byte[] strBuffer = Encoding.ASCII.GetBytes(str);
            strBuffer.CopyTo(outBuffer, 0);
            Socket.Send(outBuffer, strBuffer.Length, SocketFlags.None);
        }
    }
}
