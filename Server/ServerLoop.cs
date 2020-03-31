using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

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

            var WaitingServerAddress = IPAddresses[0];
            var udpClient = new UdpClient(PORT_NUMBER);
            IPEndPoint groupEP = new IPEndPoint(WaitingServerAddress, PORT_NUMBER);

            Console.WriteLine($"Waiting Address:{WaitingServerAddress}");
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
            PunchingSocket.Bind(new IPEndPoint(WaitingServerAddress, PORT_NUMBER));

            PunchingPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            await MainLoop(cancellationToken);
        }

        /// <summary>
        /// IPアドレスを取得(IPV4のみ)
        /// </summary>
        internal List<IPAddress> IPAddresses
        {
            get
            {
                List<IPAddress> rets = new List<IPAddress>();
                // 物理インターフェース情報をすべて取得
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                // 各インターフェースごとの情報を調べる
                foreach (var adapter in interfaces)
                {
                    // 有効なインターフェースのみを対象とする
                    if (adapter.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    // インターフェースに設定されたIPアドレス情報を取得
                    var properties = adapter.GetIPProperties();

                    // 設定されているすべてのユニキャストアドレスについて
                    foreach (var unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            // IPv4アドレス
                            rets.Add(unicast.Address);
                        }
                    }
                }

                return rets;
            }
        }
        
        /// <summary>
        /// メインループ
        /// 1秒ごとに時間を送る
        /// </summary>
        /// <returns></returns>
        private async Task MainLoop(CancellationToken stoppingToken)
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

                await Task.Delay(1000, stoppingToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
