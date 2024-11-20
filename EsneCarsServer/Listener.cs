using System.Net;
using System.Net.Sockets;

namespace EsneCarsServer
{
    internal class Listener
    {
        public TcpListener TcpListener { get; private set; }

        public Listener(Pool pool, int port)
        {
            IPAddress localAddress = IPAddress.Parse("127.0.0.1");
            TcpListener = new TcpListener(localAddress, port);
            TcpListener.Start();

            while (true) 
            {
                TcpClient client = TcpListener.AcceptTcpClient();

                client.Client.Blocking = false;

                if (!pool.AddConnection(client)) client.Close();
            }
        }
    }
}
