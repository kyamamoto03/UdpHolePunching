using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class ServerLoop : IHostedService
    {
        public readonly int PORT_NUMBER = 11000;
        private Socket PunchingSocket;
        private IPEndPoint PunchingPoint;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Start");

            var udpClient = new UdpClient(PORT_NUMBER);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, PORT_NUMBER);

            //クライアントからのメッセージ(UDPホールパンチング）を待つ
            //groupEPにNATが変換したアドレス＋ポート番号は入ってくる
            var TargetAddress = Encoding.UTF8.GetString(udpClient.Receive(ref groupEP));
            udpClient.Dispose();

            Console.WriteLine($"UDP HolePunching({TargetAddress})!");
            Console.WriteLine($"groupEP {groupEP.Address}");

            //NATで変換されたIPアドレスおよびポート番号
            var ip = groupEP.Address.ToString();
            var port = groupEP.Port;

            PunchingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //ソースアドレスを設定する(NATが変換できるように、クライアントが指定した宛先を設定)
            PunchingSocket.Bind(new IPEndPoint(groupEP.Address, PORT_NUMBER));
            
            PunchingPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            await MainLoop();
        }

        private async Task MainLoop()
        {
            var Buffer = new byte[128];

            while (true)
            {
                //サーバが送信する文字列を作成
                string echo_str = $"ServerSent: {DateTime.Now.ToString()}";
                //Byte配列に変換
                byte[] data = Encoding.UTF8.GetBytes(echo_str);
                //サーバからクライアントへ送信
                PunchingSocket.SendTo(data, SocketFlags.None, PunchingPoint);

                await Task.Delay(1000);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
