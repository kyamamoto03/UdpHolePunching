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
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private Task _executingTask;

        public readonly int PORT_NUMBER = 11000;
        private Socket PunchingSocket;
        private IPEndPoint PunchingPoint;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Start");

            //受け付けるアドレスを設定
            var WaitingServerAddress = IPAddresses[0];
            IPEndPoint groupEP = new IPEndPoint(WaitingServerAddress, PORT_NUMBER);

            Console.WriteLine($"Waiting Address:{WaitingServerAddress}");

            string TargetAddress;
            //クライアントからのメッセージ(UDPホールパンチング）を待つ
            //groupEPにNATが変換したアドレス＋ポート番号は入ってくる
            using (var udpClient = new UdpClient(PORT_NUMBER))
            {
                //Udp Hole Puchingをするために何かしらのデータを受信する(ここではクライアントが指定したサーバのアドレス)
                TargetAddress = Encoding.UTF8.GetString(udpClient.Receive(ref groupEP));
            }

            //NATで変換されたIPアドレスおよびポート番号
            var ip = groupEP.Address.ToString();
            var port = groupEP.Port;

            PunchingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //ソースアドレスを設定する(NATが変換できるように、クライアントが指定した宛先を設定)
            PunchingSocket.Bind(new IPEndPoint(WaitingServerAddress, PORT_NUMBER));

            PunchingPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            _executingTask = Task.Run(async () => await MainLoop());

            return Task.CompletedTask;
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
                    }else if(adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
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
        private async Task MainLoop()
        {
            while (!_stoppingCts.IsCancellationRequested)
            {
                //サーバが送信する文字列を作成
                string echo_str = $"ServerSent: {DateTime.Now.ToString()}";
                //Byte配列に変換
                byte[] data = Encoding.UTF8.GetBytes(echo_str);
                //サーバからクライアントへ送信
                PunchingSocket.SendTo(data, SocketFlags.None, PunchingPoint);

                await Task.Delay(1000, _stoppingCts.Token);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite,
                                                              cancellationToken));
            }
        }
    }
}
