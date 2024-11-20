using System;
using System.Threading.Tasks;

namespace EsneCarsServer
{
    internal class Program
    {
        static Pool connectionsPool = new Pool(32);

        static void Main(string[] args)
        {
            ShowWelcomeMessage();

            Console.ReadLine();

            Task task = new Task(() => { new Listener(connectionsPool, 666); });
            task.Start();

            while (true)
            {
                connectionsPool.Process();
            }

            Console.WriteLine("Exit");
            Console.ReadLine(); 
        }

        static void ShowWelcomeMessage()
        {
            Console.WriteLine("Welcome to ESNE Server 2023!");
            Console.WriteLine("Listening for new connections...");
        }
    }
}
