using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;
using BinanceExchange.API;
using BinanceExchange.API.Client;
using BinanceExchange.API.Client.Interfaces;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Market;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Models.Response.Abstract;
using BinanceExchange.API.Models.Response.Error;
using BinanceExchange.API.Models.WebSocket;
using BinanceExchange.API.Utility;
using BinanceExchange.API.Websockets;
using log4net;
using Newtonsoft.Json;



namespace BinanceBot
{
    internal class Program
    {
        
        /*
         * First attempt at writing a trading bot.
         * Currently only able to trade altcoins vs BTC.
         * Current settings are buying/selling at 11% up/down but is easily changed in checkPriveMovement().
         * Uses the BinanceDotNet API wrapper by glitch100 @ github.
         * 
         */
        
        
        
        
        private static String apiKey = "APIkey";
        private static String secretKey = "Secret key";
        private double lowestPrice = 10000;
        private double highestPrice;
        private double soldAt = 6603;
        private double boughtAt = 6630;
        private double price;
        private Boolean sold = false;
        private double myCoin = 0;
        private double myDollar = 1;
        private double myBTC = 0;
        private DateTime lastTrade;
        private BinanceClient client;

        double totalBids = 0;
        double totalAsks = 0;
        private double MFTAmount;
        private int counter = 0;
        private string tradeCoin = "NANO";
        private int coinNumber;
        private int number = 0;
        private int num = 0;
        private String binanceUrl = "https://api.binance.com/api/v3/ticker/price";


        public Program()
        {
            this.client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = apiKey,
                SecretKey = secretKey,
            });
        }


        public void continousFetching()
        {
            while (true)
            {
                fetchPrice();
                checkPriceMovement();
                Thread.Sleep(300);
            }
        }


        public void checkCoinNumber()
        {
            using (WebClient w = new WebClient())
            {
                try
                {
                    var json = w.DownloadString("https://api.binance.com/api/v3/ticker/price");
                    var coins = JsonConvert.DeserializeObject<Coin[]>(json);
                    string symbol = tradeCoin + "BTC";
                    bool found = false;
                    Console.WriteLine("här " + symbol);

                    for (int i = 0; i < coins.Length; i++)
                    {
                        if (!found)
                        {
                            if (coins[i].symbol.Equals(symbol))
                            {
                                coinNumber = i;
                                Console.WriteLine("coinnumber: " + coinNumber);
                                Console.WriteLine(coins[i].symbol);
                                found = true;
                            }
                        }
                    }
                }
                catch (JsonReaderException e)
                {
                }
            }
        }


        public void checkPriceMovement()
        {
            if (sold)
            {
                //Set percentage here
                if (price * 0.989 > lowestPrice)
                {
                    var time = DateTime.Now;
                    double seconds = (time - lastTrade).TotalSeconds;
                    if (seconds > 20)
                    {
                        buy();
                        lowestPrice = price;
                        highestPrice = price;
                    }
                }
            }
            else
            {
                //Set percentage here
                if (price * 1.011 < highestPrice)
                {
                    var time = DateTime.Now;
                    double seconds = (time - lastTrade).TotalSeconds;
                    if (seconds > 20)
                    {
                        sell();
                        lowestPrice = price;
                        highestPrice = price;
                    }
                }
            }
        }

        public void buy()
        {
            Console.Write("my btc:" + myBTC);
            Console.WriteLine("my coin " + myCoin);
            fetchOrderBook();
            if (totalBids * 4 < totalAsks)
            {
                Console.WriteLine("not gonna buy because order book");
                return;
            }
            else
            {
                makeBuy();
                checkAmountCoin();
                if (!sold && myCoin < 5)
                {
                    makeBuy();
                }

                lastTrade = DateTime.Now;
                sold = false;
                Console.WriteLine("highest was: " + highestPrice + " lowest was " + lowestPrice + " price = " + price);
                Console.WriteLine("Bought at " + price + ". Coin value currently: " + myCoin);
            }

            checkAmountBtc();
            checkAmountCoin();
        }

        public void sell()
        {
            fetchOrderBook();

            if (totalAsks * 4 < totalBids)
            {
                Console.WriteLine("not gonna sell because order book");
                return;
            }
            else
            {
                makeSell();

                lastTrade = DateTime.Now;

                sold = true;
                Console.WriteLine("highest was: " + highestPrice + " lowest was " + lowestPrice + " price = " + price);
                Console.WriteLine("Sold at " + price + ". Current btc value: " + myBTC);
            }

            checkAmountBtc();
            checkAmountCoin();
        }

        public async void makeBuy()
        {
            try
            {
                double amount = myBTC / price;
                decimal amountToBuy = (decimal) amount - 1;
                Console.WriteLine("amount to buy: " + amountToBuy);
                int a = (int) amount;


                BaseCreateOrderResponse response = await client.CreateOrder(new CreateOrderRequest()
                {
                    Quantity = a,
                    Side = OrderSide.Buy,
                    Symbol = tradeCoin + "BTC",
                    Type = OrderType.Market,
                });
            }
            catch (BinanceBadRequestException e)
            {
                Console.WriteLine("bad request in buy");

                makeBuy();
            }

            Console.WriteLine("sold: " + sold);
        }

        public async void makeSell()
        {
            try
            {
                int amountToSell = (int) myCoin;
                decimal a = (decimal) myCoin;


                BaseCreateOrderResponse response = await client.CreateOrder(new CreateOrderRequest()
                {
                    //IcebergQuantity = amountToSell,
                    // Price = d,
                    //Quantity = amount,
                    Quantity = amountToSell,
                    Side = OrderSide.Sell,
                    Symbol = tradeCoin + "BTC",
                    Type = OrderType.Market,
                });
            }
            catch (BinanceBadRequestException e)
            {
                Console.WriteLine("bad request i sell");


                makeSell();
            }

            Console.WriteLine("sold: " + sold);
        }


        public async void checkAmountCoin()
        {
            try
            {
                var info = await client.GetAccountInformation();

                bool found = false;
                foreach (var balance in info.Balances)
                {
                    if (!found)
                    {
                        var serializedBalanceObject = JsonConvert.SerializeObject(balance);

                        if (serializedBalanceObject.Contains(tradeCoin))
                        {
                            Console.WriteLine(serializedBalanceObject.ToString());
                            string firstString = (String) serializedBalanceObject;
                            string sub = firstString.Substring(22, 25);
                            string finalString = sub.Substring(0, 10);
                            sub = sub.Replace(",", ".");
                            string final = "";
                            for (int i = 0; i < finalString.Length; i++)
                            {
                                if (finalString[i].Equals('.'))
                                {
                                    final += ",";
                                }
                                else if (!finalString[i].Equals(':'))
                                {
                                    final += finalString[i];
                                }
                            }

                            double totalAmount = double.Parse(final);
                            myCoin = double.Parse(final);
                            Console.WriteLine("i check amount coin" + totalAmount);


                            myCoin = totalAmount;
                        }
                    }
                }
            }
            catch (BinanceBadRequestException e)
            {
                Console.WriteLine("bad request check coin");
                Console.Write(e);
                checkAmountCoin();
            }
        }

        public async void checkAmountBtc()
        {
            try
            {
                var info = await client.GetAccountInformation();
                bool found = false;


                foreach (var balance in info.Balances)
                {
                    if (!found)
                    {
                        var serializedBalanceObject = JsonConvert.SerializeObject(balance);
                        if (serializedBalanceObject.Contains("BTC") && !serializedBalanceObject.Contains("SBTC"))
                        {
                            string firstString = (String) serializedBalanceObject;
                            string sub = firstString.Substring(22, 25);
                            string finalString = sub.Substring(0, 10);
                            sub = sub.Replace(",", ".");
                            string final = "";
                            for (int i = 0; i < finalString.Length; i++)
                            {
                                if (finalString[i].Equals('.'))
                                {
                                    final += ",";
                                }
                                else if (!finalString[i].Equals(':'))
                                {
                                    final += finalString[i];
                                }
                            }

                            double totalAmount = double.Parse(final);
                            myBTC = totalAmount;
                        }
                    }
                }
            }
            catch (BinanceBadRequestException e)
            {
                checkAmountBtc();
            }
        }

        public void fetchPrice()
        {
            using (WebClient w = new WebClient())
            {
                try
                {
                    var json = w.DownloadString("https://www.binance.com/api/v3/ticker/price");
                    var coins = JsonConvert.DeserializeObject<Coin[]>(json);

                    var coinPrice = coins[coinNumber].price;
                    string symbol = tradeCoin + "BTC";

                    if (coins[coinNumber].symbol.Equals(symbol))
                    {
                        price = coinPrice;
                        if (coinPrice > highestPrice)
                        {
                            highestPrice = coinPrice;
                        }

                        if (coinPrice < lowestPrice)
                        {
                            lowestPrice = coinPrice;
                        }
                    }
                }
                catch (JsonReaderException e)
                {
                }
                catch (WebException e)
                {
                    Console.WriteLine("too many requests" + number);
                    Thread.Sleep(1000);
                    number++;
                }
            }
        }

        public void fetchOrderBook()
        {
            using (WebClient w = new WebClient())
            {
                var depth = w.DownloadString(
                    "https://api.binance.com/api/v1/depth?symbol=" + tradeCoin + "BTC&limit=20");
                var book = JsonConvert.DeserializeObject<Bids>(depth);
                foreach (var ask in book.asks)
                {
                    string allAsks = JsonConvert.SerializeObject(ask[1]);

                    string s = allAsks.Replace(".", ",");
                    string k = s.Replace("\"", "");
                    double d = double.Parse(k);
                    totalAsks = totalAsks + d;
                }

                foreach (var bids in book.bids)
                {
                    string allBids = JsonConvert.SerializeObject(bids[1]);
                    string priceString = JsonConvert.SerializeObject(bids.First());
                    string s = allBids.Replace(".", ",");
                    string k = s.Replace("\"", "");

                    double d = double.Parse(k);
                    totalBids = totalBids + d;
                }
            }

            Console.WriteLine("total bids:" + totalBids);
            Console.WriteLine("total asks:" + totalAsks);
        }


        public static void Main(string[] args)
        {
            Program lillebot = new Program();
            lillebot.checkCoinNumber();

            lillebot.checkAmountBtc();
            lillebot.checkAmountCoin();
            lillebot.continousFetching();
        }
    }

    public class Tickers
    {
        public List<Coin> data { get; set; }
    }

    public class Coin
    {
        public string symbol { get; set; }
        public double price { get; set; }
    }

    public class newCoin
    {
        public string symbol { get; set; }
        public double price { get; set; }
    }

    public class Bids
    {
        public int lastUpdateId { get; set; }
        public List<List<object>> bids { get; set; }
        public List<List<object>> asks { get; set; }
    }
}