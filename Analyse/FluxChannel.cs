using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using Telegram.Bot;
using System.Globalization;

namespace Analyse
{
    public class FluxChannel
    {
        decimal StartPrice  = 0;
        decimal PriceCap = 0.001m;
        decimal PriceMin ;
        decimal PriceMax  = 0;
        string TelegramChannelId;
        string Token;
        int Iteration { get; set; } = 0;
        public FluxChannel(decimal startPrice, decimal priceCap, decimal priceMin, decimal priceMax)
        {
            StartPrice = startPrice;
            PriceCap = priceCap;
            PriceMin = priceMin;
            PriceMax = priceMax;
        }    
        public void AnalysePrice(string uri, string telegramChannelId,string token)
        {
            TelegramChannelId = telegramChannelId;
            Token = token;
            WebSocket client = new WebSocket(uri);
            client.Connect();
            client.OnMessage += Ws_OnMessage;
        }
        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            TradeResponse tradeResponse = JsonConvert.DeserializeObject<TradeResponse>(e.Data);        
           
            //Console.WriteLine("data : " + e.Data);           

            decimal.TryParse(tradeResponse.p, out decimal price);
            if (Iteration == 0)
            {
                StartPrice = price;
                PriceMin = price;
                PriceMax = price;
            }
            Iteration++;
            if (price > PriceMax) PriceMax = price;
            if (price < PriceMin) PriceMin = price;           
            if ((StartPrice - PriceMin) / PriceMin > PriceCap)
            {
                string message = $"Price baisse > {PriceCap} DogecoinUsdt price = {price} Datetime= {GetDateTime(tradeResponse.T)}";              
                Task.Run(async () => await SendTelegramMessage(message,TelegramChannelId,Token));

                PriceMin = PriceMax = StartPrice = price;
            }
            else if ((PriceMax - StartPrice) / PriceMin > PriceCap)
            {
                string message = $"Price monte > {PriceCap} DogecoinUsdt price = {price} Datetime= {GetDateTime(tradeResponse.T)}";             
                Task.Run(async () => await SendTelegramMessage(message,TelegramChannelId, Token));
                               PriceMin = PriceMax = StartPrice = price;
            }

            async Task SendTelegramMessage(string message, string telegramChannelId, string token)
            {             
              
                var client = new TelegramBotClient(token);
                await client.SendTextMessageAsync(telegramChannelId, message);
            }
            //var WsClient = (WebSocket)sender;
            //WsClient.Close();
        }
        DateTime GetDateTime(long longTypeDateTime)
        {
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime date = start.AddMilliseconds(longTypeDateTime).ToLocalTime();
            return date;
        }
    }
}
