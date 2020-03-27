using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class ClientLoop
    {
        public readonly string ServerAddress = "127.0.0.1";
        public readonly int PortNumber = 11000;

        public void Run()
        {
            var sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress send_to_address = IPAddress.Parse(ServerAddress);
            var sending_end_point = new IPEndPoint(send_to_address, PortNumber);

            sending_socket.SendTo(Encoding.UTF8.GetBytes(ServerAddress), sending_end_point);

            var Buffer = new byte[128];
            while (true)
            {
                int cnt = sending_socket.Receive(Buffer);
                byte[] ServerResponse = new byte[cnt];
                Array.Copy(Buffer, ServerResponse, cnt);

                var str =Encoding.UTF8.GetString(ServerResponse);
                Console.WriteLine(str);
            }

        }
    }

}


