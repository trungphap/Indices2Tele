using Analyse;
using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            TestFluxChannel();
            Console.ReadLine();
        }

        static void TestFluxChannel()
        {
            var fluxChannel = new FluxChannel(0,0.005m,0,0);
            fluxChannel.AnalysePrice("wss://stream.binance.com:9443/ws/dogeusdt@trade", "-1001468772498", "1794207254:AAGW_paSPms-Et35OdGmJLfIi6TbVVkJhBg");
        }
    }
}
