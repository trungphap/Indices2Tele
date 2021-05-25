using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using Telegram.Bot;
using System.Globalization;
using System.Collections.Generic;

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
        int Iteration ;
        List<decimal> _firstDerives;
        List<decimal> _secondDerives;
        public FluxChannel(decimal startPrice, decimal priceCap, decimal priceMin, decimal priceMax)
        {
            StartPrice = startPrice;
            PriceCap = priceCap;
            PriceMin = priceMin;
            PriceMax = priceMax;
            _firstDerives = new List<decimal>();
            _secondDerives = new List<decimal>();
            Iteration =0;
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
            var negatifFirstDerive = (PriceMin- StartPrice ) / StartPrice;
            var positifFirstDerive = (PriceMax - StartPrice ) / StartPrice;
            if (-negatifFirstDerive > PriceCap)
            {
                AddFirstDerive(negatifFirstDerive);
                string message = $"Gia giam > {PriceCap} , hinh {GetSecondDeriveType()}, DogeUsdt price = {price} Datetime= {GetDateTime(tradeResponse.T)}";              
                Task.Run(async () => await SendTelegramMessage(message,TelegramChannelId,Token));
     
                PriceMin = PriceMax = StartPrice = price;
            }
            else if (positifFirstDerive > PriceCap)
            {
                AddFirstDerive(positifFirstDerive);
                string message = $"Gia len > {PriceCap} , hinh {GetSecondDeriveType()}, DogeUsdt price = {price} Datetime= {GetDateTime(tradeResponse.T)}";             
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
        void AddFirstDerive(decimal firstDerive)
        {
            _firstDerives.Add(firstDerive);
            if (_firstDerives.Count > 1000) _firstDerives.RemoveAt(0);
        }
        string GetSecondDeriveType()
        {
            if (_firstDerives.Count < 2)
                return string.Empty;
            var lastFirstDerive = _firstDerives[_firstDerives.Count-1];
            var secondLastFirstDerive = _firstDerives[_firstDerives.Count - 2];
            var secondDerive = (lastFirstDerive - secondLastFirstDerive) / secondLastFirstDerive;
            _secondDerives.Add(secondDerive);
            if ((lastFirstDerive > 0 && secondDerive>0)  ) return "tang lom";
            if ((lastFirstDerive > 0 && secondDerive<0)  ) return "tang loi";
            if ((lastFirstDerive < 0 && secondDerive<0)  ) return "giam lom";
            if ((lastFirstDerive < 0 && secondDerive >0)  ) return "giam loi";

            return string.Empty;
        }
    }
}
