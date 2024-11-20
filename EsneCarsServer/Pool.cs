using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EsneCarsServer
{
    internal class Pool
    {
        private int maxConnections;
        private List<Connection> connections = new List<Connection>();

        public Pool(int maxConnections)
        {
            this.maxConnections = maxConnections;
        }

        public bool AddConnection(TcpClient newClient)
        {
            if (connections.Count >= maxConnections) return false;

            Connection connection = new Connection(newClient);
            connection.SendToAllDelegate = SendToAll;
            connection.SendToAllExceptThisDelegate = SendToAllExcept;

            lock (connections)
            {
                connections.Add(connection);

                // TODO: Avisar al resto de clientes.

                Console.WriteLine("New connection added!: {0}", connection);
                return true;
            }
        }

        public void RemoveConnection(Connection connection)
        {
            lock (connections)
            {
                CloseConnectionSocket(connection);
                Console.WriteLine("Removing connection: {0}", connection);
                connections.Remove(connection);
            }
        }

        public void Process()
        {
            List<Connection> connectionsToClose = new List<Connection>();

            lock (connections)
            {
                // Guarda en una lista previa las conexiones pendientes de cerrar.
                foreach (Connection connection in connections)
                {
                    if (!connection.Process()) connectionsToClose.Add(connection);
                }
            }

            // Cierra las conexiones que estaban pendientes de cerrar.
            foreach (Connection connection in connectionsToClose)
            {
                RemoveConnection(connection);
            }
        }

        private void CloseConnectionSocket(Connection connection) => connection.Socket.Close();

        // Envía el mensaje a todos.
        private void SendToAll(string str)
        {
            lock (connections)
            {
                foreach (Connection connection in connections)
                {
                    connection.Send(str);
                }
            }
        }

        // Envía el mensaje a todos menos a connection.
        private void SendToAllExcept(string str, Connection sender)
        {
            lock (connections)
            {
                foreach (Connection connection in connections)
                {
                    if (connection != sender) connection.Send(str);
                }
            }
        }
    }
}
