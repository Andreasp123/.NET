using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Newtonsoft.Json;
using Shodan.Net;

namespace Shodan
{
    internal class Program
    {
        private string apiKey = "yourkey";
        private ShodanClient client;
        private static List<string> ips = new List<string>();
        private static List<string> postal = new List<string>();
        private string port = "";
        private string country = "us";
        private string baseurl = "https://api.shodan.io/shodan/host/search?key=";
        private string prePort = "&query=port:";
        private string preCountry = "%20country:";
        private string prePostal = "%20postal:";
        private string faucet = "&facets={facets}";
        
        public Program()
        {
        }


        public void loopPostals()
        {
            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\file.txt");
            foreach (string line in lines)
            {
                if (line.Length != 0)
                {
                    
                    postal.Add(line); 
    
                } 
            }

            foreach (string zip in postal)
            {
                fetchIP(zip);
                Thread.Sleep(1000);
            }
            
        }
        
        public void fetchIP(string zip)
        {
            Console.WriteLine(zip);
            using (WebClient w = new WebClient())
            {
                try
                {
                    string fullQuery = baseurl + apiKey + prePort + port + preCountry + country + prePostal + zip + faucet;
                    
                    var allData =
                        w.DownloadString(fullQuery);
         
                    var data = JsonConvert.DeserializeObject<MyMatches>(allData);

                    foreach (var t in data.matches)
                    {
                        ips.Add(t.ip_str);  
     
                    }

                }
                catch (JsonReaderException e)
                {
                    Console.Write(e);
                }
                catch (WebException e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void printData()
        {
            using (System.IO.StreamWriter file = 
                new System.IO.StreamWriter(@"C:\Users\data.txt"))
            {
                foreach (string line in ips)
                {
                            
                    file.WriteLine(line);
                            
                }
            }
        }



        public static void Main(string[] args)
        {
            Program program = new Program();
            program.loopPostals();
            
            program.printData();
        }
    }

    public class Location
    {
        public string city { get; set; }
        public string region_code { get; set; }
        public object area_code { get; set; }
        public double longitude { get; set; }
        public string country_code3 { get; set; }
        public double latitude { get; set; }
        public string postal_code { get; set; }
        public object dma_code { get; set; }
        public string country_code { get; set; }
        public string country_name { get; set; }
    }



    public class Shodan
    {
        public string crawler { get; set; }
        public string id { get; set; }
        public string module { get; set; }
        public Options options { get; set; }
    }

    public class Match
    {
        public int hash { get; set; }
        public long ip { get; set; }
        public string isp { get; set; }
        public string transport { get; set; }
        public string data { get; set; }
        public string asn { get; set; }
        public int port { get; set; }
        public List<string> hostnames { get; set; }
        public Location location { get; set; }
        public DateTime timestamp { get; set; }
        public List<string> domains { get; set; }
        public string org { get; set; }
        public object os { get; set; }
        public Shodan _shodan { get; set; }
        public string ip_str { get; set; }
        public string product { get; set; }
    }

    public class RootObject
    {
        public List<Match> matches { get; set; }
        public int total { get; set; }
    }
    
    public class MyMatches {
        public Match[] matches {get; set;}
    }
    

}