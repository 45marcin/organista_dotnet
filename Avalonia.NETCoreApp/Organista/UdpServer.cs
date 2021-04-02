using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Organista
{
    public class UdpServer
    {
        public UdpServer()
        {
            Thread x = new Thread(run);
            x.Start();
        }

        void run()
        {
            byte[] data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 2708);
            UdpClient newsock = new UdpClient(ipep);
            while (true)
            {
                Console.WriteLine("Waiting for a client...");

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                data = newsock.Receive(ref sender);
                Console.WriteLine("Message received from {0}:", sender.ToString());
                string message = Encoding.ASCII.GetString(data, 0, data.Length);
                Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
                if (message.Equals("Where are you my play box?"))
                {
                    data = Encoding.ASCII.GetBytes("I'm here my love");
                    newsock.Send(data, data.Length, sender);
                }
            }


        /*
        while(true)
        {
            data = newsock.Receive(ref sender);

            Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
            newsock.Send(data, data.Length, sender);
        }
        */
        }
    }
}