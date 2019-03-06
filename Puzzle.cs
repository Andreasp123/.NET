
using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using System.Linq;

namespace ModularFinance
{
    internal class Puzzle
    {
        /*
         * Used the daily high to calculate profit.
         * Perhaps using closing price is better although
         * buy at daily low and sell at daily high obviously best results =)
         */
        
        private string baseUrl = "https://www.modularfinance.se/api/puzzles/index-trader.json";
        private int buyDate;
        private int sellDate;
        private double buyPrice;
        private double sellPrice;
        private double profit = 0;
        
        public void fetchData()
        {
            using (WebClient w = new WebClient())
            {
                
                var json = w.DownloadString(baseUrl);
                
                var data = JsonConvert.DeserializeObject<Ticker>(json);
                foreach(Data buy in data.data)
                {
                    int tempDate = buy.quote_date;
                    double tempBuy = buy.high;
                    foreach (Data sell in data.data)
                    {
                        if (sell.quote_date > tempDate)
                        {
                            if ((sell.high - tempBuy) > profit)
                            {
                                sellDate = sell.quote_date;
                                buyDate = buy.quote_date;
                                buyPrice = buy.high;
                                sellPrice = sell.high;
                                profit = sell.high - tempBuy;
                            }
                        }
                    }

                }
                
            }
            Console.WriteLine("Bought at " + buyDate + " for "+ buyPrice);
            Console.WriteLine("Sold at " + sellDate + " for " + sellPrice);
            Console.WriteLine("Total profit: " + profit);
        }
        
        
        
        
        
        
        
        public static void Main(string[] args)
        {
            Puzzle p = new Puzzle();
            p.fetchData();
        }
    }









    public class Ticker
    {
        public List<Data> data { get; set; }
    }


    
    public class Data
    {
        public int quote_date { get; set; }
        public string paper { get; set; }
        public string exch { get; set; }
        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public int volume { get; set; }
        public int value { get; set; }
    }

    public class RootObject
    {
        public string puzzle { get; set; }
        public string info { get; set; }
        public string submission { get; set; }
        public List<Data> data { get; set; }
    }
}