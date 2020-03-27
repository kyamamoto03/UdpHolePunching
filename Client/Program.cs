using System;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            ClientLoop loop = new ClientLoop();
            loop.Run();
        }
    }
}
