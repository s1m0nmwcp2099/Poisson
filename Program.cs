using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace Poisson
{
    class Program
    {
        static bool URLExists(string url)
        {
            bool result = true;
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Timeout = 6000; // miliseconds
            webRequest.Method = "HEAD";
            try
            {
                webRequest.GetResponse();
            }
            catch
            {
                result = false;
            }
            return result;
        }
        static double HowManyDays(string a, string b){
            DateTime x = Convert.ToDateTime(a); DateTime y = Convert.ToDateTime(b);
            TimeSpan thisPeriod = x - y;
            int howLong = Convert.ToInt32(thisPeriod.Days);
            return Convert.ToDouble(howLong);
        }
        static int fact(int x){
            if (x == 0 || x == 1){
                return 1;
            }else{
                int result = x;
                for (int i=2;i<x;i++){
                    result *= i;
                }
                return result;
            }
        }
        static void Main(string[] args)
        {
            List<string> Leagues = new List<string>();
            Leagues.Add("B1"); Leagues.Add("D1"); Leagues.Add("D2"); Leagues.Add("E0"); Leagues.Add("E1"); Leagues.Add("E2"); 
            Leagues.Add("E3"); Leagues.Add("EC"); Leagues.Add("F1"); Leagues.Add("F2"); Leagues.Add("G1"); Leagues.Add("I1");
            Leagues.Add("I2"); Leagues.Add("N1"); Leagues.Add("P1"); Leagues.Add("SC0"); Leagues.Add("SC1"); Leagues.Add("SC2");
            Leagues.Add("SC3"); Leagues.Add("SP1"); Leagues.Add("SP2"); Leagues.Add("T1");
            
            //Download fixtures and results
            //fixtures
            Console.WriteLine("Do you want to update fixtures? y or n");
            string ans = Console.ReadLine();
            if (ans == "y" || ans =="Y"){
                string webAddr = "https://www.football-data.co.uk/fixtures.csv";
                string fileName = "Data/fixtures.csv";
                if (URLExists(webAddr) == true){
                    if (File.Exists(fileName)){
                        File.Delete(fileName);
                    }
                    Console.WriteLine("Downloading fixture info...");
                    using (var client = new WebClient()){
                        client.DownloadFile(webAddr, fileName);
                    }
                    Console.WriteLine("Fixture information downloaded");
                }else{
                    Console.WriteLine("Cannot access online fixture information");
                }
                //results info for each league
                foreach (string lg in Leagues){
                    webAddr = "https://www.football-data.co.uk/mmz4281/1920/" + lg + ".csv";
                    fileName = "Data/" + lg + ".csv";
                    if (URLExists(webAddr) ==true){
                        if (File.Exists(fileName)){
                            File.Delete(fileName);
                        }
                        Console.WriteLine("Downloading results for {0}", lg);
                        using (var client = new WebClient()){
                            client.DownloadFile(webAddr, fileName);
                        }
                        Console.WriteLine("Results for {0} downloaded", lg);
                    }else{
                        Console.WriteLine("Cannot access online league information for {0}", lg);
                    }
                }
            }
            //put fixtures into list
            List<string> Fixtures = new List<string>();
            using (StreamReader sr = new StreamReader("Data/fixtures.csv")){
                while (sr.Peek() > 0){
                    Fixtures.Add(sr.ReadLine());
                }
            }    
            //get data for each league and fixtures
            List<string> SeasonSoFar = new List<string>();
            foreach (string lg in Leagues){
                string thisLgFileName = "Data/" + lg + ".csv";
                using (StreamReader sr = new StreamReader(thisLgFileName)){
                    while (sr.Peek() > 0){
                        SeasonSoFar.Add(sr.ReadLine());
                    }
                }
            }
            List<string> Bets = new List<string>(); List<string> AccaBets = new List<string>();
            foreach (string fixture in Fixtures){
                //Console.WriteLine(fixture);
                string[] thisFixture = fixture.Split(',');
                if (thisFixture[0] != "Div"){
                    string homeTeam = thisFixture[3]; string awayTeam = thisFixture[4];
                    double hF = 0; double hA = 0; double hPld = 0; double aF = 0; double aA = 0; double aPld = 0;
                    double allH = 0; double allA = 0; double allPld = 0;
                    foreach (string previousMatch in SeasonSoFar){
                        string[] thisPreviousMatch = previousMatch.Split(',');
                        if (thisPreviousMatch[0] == thisFixture[0]){
                            allH += Convert.ToDouble(thisPreviousMatch[5]);
                            allA += Convert.ToDouble(thisPreviousMatch[6]);
                            allPld += 1;
                        }
                        if (thisPreviousMatch[3] == homeTeam){
                            double daysAgo = HowManyDays(thisFixture[1], thisPreviousMatch[1]);
                            double thisExp = Math.Exp(-0.007 * daysAgo);
                            hF += (thisExp * Convert.ToDouble(thisPreviousMatch[5])); 
                            hA += (thisExp * Convert.ToDouble(thisPreviousMatch[6]));
                            hPld += thisExp;
                        }
                        if (thisPreviousMatch[4] == awayTeam){
                            double daysAgo = HowManyDays(thisFixture[1], thisPreviousMatch[1]);
                            double thisExp = Math.Exp(-0.007 * daysAgo);
                            aF += (thisExp * Convert.ToDouble(thisPreviousMatch[6])); 
                            aA += (thisExp * Convert.ToDouble(thisPreviousMatch[5]));
                            aPld += thisExp;
                        }
                    }
                    double hFpG = hF / hPld; double hApG = hA / hPld;
                    double aFpG = aF / aPld; double aApG = aA / aPld;
                    double allHpG = allH / allPld; double allApG = allA / allPld;
                    
                    //Coefficients
                    double homeAttack = hFpG / allHpG; double homeDefence = hApG / allApG;
                    double awayAttack = aFpG / allApG; double awayDefence = aApG / allHpG;
                    double lambda = homeAttack * awayDefence / allHpG;
                    double mu = awayAttack * homeDefence / allApG;

                    //Poisson
                    int max = 10;
                    double e=2.7182818284590452;
                    double[] homeGoalProb = new double[max]; double[] awayGoalProb = new double[max];
                    for (int g=0;g<max;g++){
                        homeGoalProb[g] = ((Math.Pow(lambda,g)) * (Math.Pow(e,-1*lambda)) / fact(g));
                        awayGoalProb[g] = ((Math.Pow(mu,g)) * (Math.Pow(e,-1*mu)) / fact(g));
                    }
                    //home and away arrays for specific number goal likelihood
                    //(lambda^goals)*(2.718^-lambda)/Goals!
                    //(mu^goals)*(2.718^-mu)/Goals!
                    double[,] goalGrid = new double[max,max];
                    for (int h=0;h<max;h++){
                        for (int a=0;a<max;a++){
                            if (homeGoalProb[h] * awayGoalProb[a]>0){
                                goalGrid[h,a] = homeGoalProb[h] * awayGoalProb[a];
                            }
                        }
                    }
                    double homeWinProb = 0; double drawProb = 0; double awayWinProb = 0;
                    double oversProb = 0; double undersProb = 0;
                    double totalProb = 0;
                    for (int h=0;h<max;h++){
                        for (int a=0;a<max;a++){
                            if (h>a){
                                homeWinProb += goalGrid[h,a];
                            }else if (h==a){
                                drawProb += goalGrid[h,a];
                            }else if (h<a){
                                awayWinProb += goalGrid[h,a];
                            }
                            if (h+a>2){
                                oversProb += goalGrid[h,a];
                            }else if (h+a<3){
                                undersProb += goalGrid[h,a];
                            }
                            totalProb += goalGrid[h,a];
                        }
                    }
                    //Show goal grid
                    /*
                    double total=0;
                    Console.WriteLine("{0} GF:{1} GA:{2}",homeTeam,hFpG,hApG);
                    Console.WriteLine("{0} GF:{1} GA:{2}",awayTeam,aFpG,aApG);
                    for (int h=0;h<max;h++){
                        for (int a=0;a<max;a++){
                            total+=(goalGrid[h,a]);
                            Console.Write(Math.Round(goalGrid[h,a],2)+" ");
                        }
                        Console.Write("\n");
                    }
                    Console.WriteLine("Total = "+total);
                    Console.ReadLine();*/

                    string strWhenMatch=thisFixture[1]+" "+thisFixture[2]+":00";
                    DateTime whenMatch=Convert.ToDateTime(strWhenMatch);


                    //Calculate value
                    //columns 23,24,25 for h,d,l and 47,48 for overs,unders
                    if (whenMatch.CompareTo(DateTime.Now)>0){
                        double minHmResultValue=1.15;
                        double minResultValue = 2.5; double minOvUnValue = 2.3; double minAccaProb = 0.85;
                        double hmOdds = 0;
                        if (thisFixture[23] != ""){
                            hmOdds = Convert.ToDouble(thisFixture[23]);
                        }
                        double hwVal = homeWinProb * hmOdds;
                        if (hwVal>minHmResultValue){
                            string thisBet = thisFixture[0]+","+thisFixture[1]+","+thisFixture[2]+","+homeTeam+","+awayTeam+",H,"+
                                Convert.ToString(Math.Round(homeWinProb,2))+","+thisFixture[23]+","+Convert.ToString(Math.Round(hwVal,2));
                            Bets.Add(thisBet);
                            if (homeWinProb>minAccaProb){
                                AccaBets.Add(thisBet);
                            }
                        }
                        double drOdds = 0;
                        if (thisFixture[24] != ""){
                            drOdds = Convert.ToDouble(thisFixture[24]);
                        }
                        double dVal = drawProb * drOdds;
                        if (dVal>minResultValue){
                            string thisBet = thisFixture[0]+","+thisFixture[1]+","+thisFixture[2]+","+homeTeam+","+awayTeam+",D,"+
                                Convert.ToString(Math.Round(drawProb,2))+","+thisFixture[24]+","+Convert.ToString(Math.Round(dVal,2));
                            Bets.Add(thisBet);
                            if (drawProb>minAccaProb){
                                AccaBets.Add(thisBet);
                            }
                        }
                        double awOdds = 0;
                        if (thisFixture[25] != ""){
                            awOdds = Convert.ToDouble(thisFixture[25]);
                        }
                        double awVal = awayWinProb * awOdds;
                        if (awVal>minResultValue){
                            string thisBet = thisFixture[0]+","+thisFixture[1]+","+thisFixture[2]+","+homeTeam+","+awayTeam+",A,"+
                                Convert.ToString(Math.Round(awayWinProb,2))+","+thisFixture[25]+","+Convert.ToString(Math.Round(awVal,2));
                            Bets.Add(thisBet);
                            if (awayWinProb>minAccaProb){
                                AccaBets.Add(thisBet);
                            }
                        }
                        double ovOdds = 0;
                        if (thisFixture[35] != ""){
                            ovOdds = Convert.ToDouble(thisFixture[35]);
                        }
                        double ovVal = oversProb * ovOdds;
                        if (ovVal>minOvUnValue){
                            string thisBet = thisFixture[0]+","+thisFixture[1]+","+thisFixture[2]+","+homeTeam+","+awayTeam+",>2.5,"+
                                Convert.ToString(Math.Round(oversProb,2))+","+thisFixture[35]+","+Convert.ToString(Math.Round(ovVal,2));
                            Bets.Add(thisBet);
                            if (oversProb>minAccaProb){
                                AccaBets.Add(thisBet);
                            }
                        }
                        double unOdds = 0;
                        if (thisFixture[36] != ""){
                            unOdds = Convert.ToDouble(thisFixture[36]);
                        }
                        double unVal = undersProb * unOdds;
                        if (unVal>minOvUnValue){
                            string thisBet = thisFixture[0]+","+thisFixture[1]+","+thisFixture[2]+","+homeTeam+","+awayTeam+",<2.5,"+
                                Convert.ToString(Math.Round(undersProb,2))+","+thisFixture[36]+","+Convert.ToString(Math.Round(unVal,2));
                            Bets.Add(thisBet);
                            if (undersProb>minAccaProb){
                                AccaBets.Add(thisBet);
                            }
                        }
                    }
                    /*
                    Console.WriteLine("{0} v {1}", homeTeam,awayTeam);
                    Console.WriteLine("Home for per game = {0}, against per game = {1}",hFpG,hApG);
                    Console.WriteLine("Away for per game = {0}, against per game = {1}",aFpG,aApG);
                    Console.WriteLine("Home win probability = {0}", homeWinProb);
                    Console.WriteLine("Draw probability = {0}", drawProb);
                    Console.WriteLine("Away win probability = {0}", awayWinProb);
                    Console.WriteLine("Overs probability = {0}", oversProb);
                    Console.WriteLine("Unders win probability = {0}", undersProb);
                    Console.WriteLine("Total probability = {0}", totalProb);
                    Console.WriteLine("lamda = {0}, mu = {1}",lambda,mu);
                    Console.ReadLine();
                    */
                }
            }
            //Write bets to csv file
            string outputFileName = "OutputData/valueBets.csv";
            if (File.Exists(outputFileName)){
                File.Delete(outputFileName);
            }
            using (StreamWriter sw = new StreamWriter(outputFileName,true)){
                sw.WriteLine("LEAGUE,DATE,TIME,HOME,AWAY,BET,PROB,ODDS,VALUE");
                foreach (string thisBet in Bets){
                    sw.WriteLine(thisBet);
                }
            }
            outputFileName = "OutputData/accaValueBets.csv";
            if (File.Exists(outputFileName)){
                File.Delete(outputFileName);
            }
            using (StreamWriter sw = new StreamWriter(outputFileName,true)){
                sw.WriteLine("LEAGUE,DATE,TIME,HOME,AWAY,BET,PROB,ODDS,VALUE");
                foreach (string thisBet in AccaBets){
                    sw.WriteLine(thisBet);
                }
            }
            Console.WriteLine("{0} single bets",Bets.Count);
            Console.WriteLine("{0} accumulator bets",AccaBets.Count);
        }
    }
}
