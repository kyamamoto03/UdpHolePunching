using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerLoop serverLoop = new ServerLoop();

            Task.Run(async () => await serverLoop.StartAsync()).Wait();
        }
    }
}
