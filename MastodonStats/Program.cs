using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using Mastonet.Entities;
using System.Threading;
using IronPython.Hosting;

namespace MastodonStats
{
    class Program
    {
        static string[] configText = File.ReadAllLines("MastodonStats.config").Select(x => x.TrimEnd(new char[] { '\n', '\r' })).ToArray();
        static RegisteredAccountList accountList = new RegisteredAccountList();
        static DateTime time;

        static readonly string CId = configText[1];
        static readonly string CSec = configText[2];
        static readonly string Token = configText[3];
        static void Main(string[] args)
        {
            bool olddraw = false;
            switch (args.Length != 0 ? args[0] : "")
            {
                case "/newday":
                    NewDay();
                    break;
                case "/endday":
                    EndDay();
                    olddraw = true;
                    break;
                case "/drawonly":
                    if (args.Length >= 2 && File.Exists($"accounts{args[1]}.json")) {
                        var str = File.ReadAllText($"accounts{args[1]}.json");
                        accountList.AddRange(JsonConvert.DeserializeObject<IEnumerable<RegisteredAccount>>(str));
                        /*
                        var data = accountList.Select(d => d.TootsToday).ToArray();
                        var engine = Python.CreateEngine();
                        dynamic scope = engine.ExecuteFile("DrawHistogram.py");
                        scope.draw_iron(data);
                        return;
                        /*/
                        
                        time = new FileInfo($"accounts{args[1]}.json").LastWriteTime.Date;
                        olddraw = true;
                    }
                    break;
                case "/getusers":
                    GetUsers();
                    return;
                case "/makescatter":
                    (var x, var y) = MakeScatter();
                    Draw_CallPython($"DrawScatter.py {x} {y}");
                    return;
                default:
                    Update();
                    break;
            }

            var pyArg = "DrawHistogram.py " + (olddraw ? $"old {time.AddDays(-1).ToShortDateString()} " : $"{time.ToShortDateString()} {time.ToShortTimeString()} ") + string.Join(" ", accountList.Select(x => x.TootsToday.ToString()));
            Draw_CallPython(pyArg);
        }

        static void Draw_CallPython(string pyArg)
        {
            Process.Start("python", $"{pyArg}");
        }

        static void NewDay()
        {
            var manager = new Manager("imastodon.net", CId, CSec, Token);
            var statuses = manager.Client.GetPublicTimeline(limit: 40, local: true).Result;
            var max = statuses.First().Id;
            time = DateTime.Now;
            var startCount = max;
            var startToday = 0L;
            for (var i = 0; i < 400; i++)
            {
                try
                {
                    var tl = manager.Client.GetPublicTimeline(limit: 40, maxId: max, local: true).Result;
                    Debug.WriteLine($"{i}, {tl.Last().Id} ({TimeZoneInfo.ConvertTimeFromUtc(tl.Last().CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"))})" +
                        $" -> {tl.First().Id} ({TimeZoneInfo.ConvertTimeFromUtc(tl.First().CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"))})");
                    if (TimeZoneInfo.ConvertTimeFromUtc(tl.Last().CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == DateTime.Today.ToShortDateString())
                    {
                        foreach (var status in tl)
                        {
                            accountList.Update(status);
                        }
                        max = tl.Last().Id - 1;
                    }
                    else
                    {
                        var todaysStatus = tl.AsParallel().Where(x => TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == DateTime.Today.ToShortDateString());
                        foreach (var status in todaysStatus)
                        {
                            accountList.Update(status);
                        }
                        startToday = todaysStatus.Last().Id;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            var serialized = JsonConvert.SerializeObject(accountList);
            File.WriteAllText("accounts.json", $"{serialized}\n");
            File.WriteAllText("PreviousIds.dat", $"{startCount}");
            
            foreach (var account in accountList.OrderBy(x => x.TootsToday))
            {
                Debug.WriteLine($"{account.Account.AccountName}, {account.TootsToday}");
            }
        }

        static void EndDay()
        {
            var str = File.Exists("accounts.json") ? File.ReadAllText("accounts.json") : null;
            if (str != null)
            {
                accountList.AddRange(JsonConvert.DeserializeObject<IEnumerable<RegisteredAccount>>(str));
            }
            var tmpList = new RegisteredAccountList();
            var manager = new Manager("imastodon.net", CId, CSec, Token);
            var statuses = manager.Client.GetPublicTimeline(limit: 40, local: true).Result;
            var max = statuses.First().Id;
            var endCount = long.Parse(File.ReadAllText("PreviousIds.dat"));
            var from = TimeZoneInfo.ConvertTimeFromUtc(manager.Client.GetStatus(endCount).Result.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
            var yesterday = DateTime.Today.AddDays(-1);
            time = DateTime.Now.Date;
            var startCount = max;
            for (var i = 0; i < 600; i++)
            {
                try
                {
                    var tl = manager.Client.GetPublicTimeline(limit: 40, maxId: max, local: true).Result;
                    Debug.WriteLine($"{i}, {tl.Last().Id} ({TimeZoneInfo.ConvertTimeFromUtc(tl.Last().CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"))})" +
                        $" -> {tl.First().Id} ({TimeZoneInfo.ConvertTimeFromUtc(tl.First().CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"))})");

                    if (TimeZoneInfo.ConvertTimeFromUtc(tl.Last().CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToString("yyyyMMdd") == time.ToString("yyyyMMdd"))
                    {
                        max = tl.Last().Id - 1;
                        continue;
                    }
                    else if(tl.Last().Id > endCount)
                    {
                        var yesterdaysStatus = tl.AsParallel().Where(x => TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == yesterday.ToShortDateString());
                        foreach (var status in yesterdaysStatus)
                        {
                            tmpList.Update(status);
                        }
                        max = tl.Last().Id - 1;
                        if(manager.Client.GetStatus(tl.Last().Id).Result.CreatedAt.ToShortDateString() == yesterday.AddDays(-1).ToShortDateString())
                        {
                            var serialized_tmp = JsonConvert.SerializeObject(tmpList);
                            File.WriteAllText($"accounts{yesterday.ToString("yyyyMMdd")}.json", $"{serialized_tmp}\n");
                            tmpList.Clear();
                            yesterday = yesterday.AddDays(-1);
                            yesterdaysStatus = tl.AsParallel().Where(x => TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == yesterday.ToShortDateString());
                            foreach (var status in yesterdaysStatus)
                            {
                                tmpList.Update(status);
                            }
                        }
                    }
                    else
                    {
                        var yesterdaysStatus = tl.AsParallel().Where(x => x.Id > endCount && TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == yesterday.ToShortDateString());
                        foreach (var status in yesterdaysStatus)
                        {
                            tmpList.Update(status);
                        }
                        max = tl.Last().Id - 1;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            foreach(var a in accountList)
            {
                var tmp = tmpList.Find(x => x.Account.AccountName == a.Account.AccountName);
                if (tmp != null)
                {
                    a.TootsToday += tmp.TootsToday;
                    tmpList.RemoveAll(x => x.Account.AccountName == a.Account.AccountName);
                }
            }
            accountList.AddRange(tmpList);
            var serialized = JsonConvert.SerializeObject(accountList);
            File.WriteAllText("accounts.json", $"{serialized}\n");
            File.Move("accounts.json", $"accounts{yesterday.ToString("yyyyMMdd")}.json");
            File.WriteAllText("PreviousIds.dat", $"{startCount}");

            foreach (var account in accountList.OrderBy(x => x.TootsToday))
            {
                Debug.WriteLine($"{account.Account.AccountName}, {account.TootsToday}");
            }
        }

        static void Update()
        {
            var str = File.ReadAllText("accounts.json");
            accountList.AddRange(JsonConvert.DeserializeObject<IEnumerable<RegisteredAccount>>(str));

            var manager = new Manager("imastodon.net", CId, CSec, Token);
            var statuses = manager.Client.GetPublicTimeline(limit: 40, local: true).Result;
            var max = statuses.First().Id;
            time = DateTime.Now;
            var startCount = max;
            var endCount = long.Parse(File.ReadAllText("PreviousIds.dat"));
            for(var i = 0; i < 400; i++) {
                try
                {
                    var tl = manager.Client.GetPublicTimeline(limit: 40, maxId: max, local: true).Result;
                    Debug.WriteLine($"{i}, {tl.Last().Id} -> {tl.First().Id}");

                    if (tl.Last().Id > endCount)
                    {
                        foreach (var status in tl)
                        {
                            accountList.Update(status);
                        }
                        max = tl.Last().Id - 1;
                    }
                    else
                    {
                        var notCountedStatus = tl.AsParallel().Where(x => x.Id > endCount);
                        foreach (var status in notCountedStatus)
                        {
                            accountList.Update(status);
                        }
                        break;
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            var serialized = JsonConvert.SerializeObject(accountList);
            File.WriteAllText("accounts.json", $"{serialized}\n");
            File.WriteAllText("PreviousIds.dat", $"{startCount}");

            foreach (var account in accountList.OrderBy(x => x.TootsToday))
            {
                Debug.WriteLine($"{account.Account.AccountName}, {account.TootsToday}");
            }
        }

        static (string, string) MakeScatter()
        {
            var str = File.ReadAllText("local_accounts_list.json");
            var list = JsonConvert.DeserializeObject<IEnumerable<Account>>(str).Where(a => a.StatusesCount > 0);

            time = DateTime.Today;
            var dateCount = list.Select(a => (time - a.CreatedAt.Date).Days.ToString()).ToArray();
            var postCount = list.Select(a => a.StatusesCount.ToString()).ToArray();
            var x = "[" + string.Join(",", dateCount) + "]";
            var y = "[" + string.Join(",", postCount) + "]";

            return (x, y);
        }

        static void GetUsers()
        {
            FileStream fs;
            StreamWriter sw;
            if (File.Exists($"local_accounts_list_making.json"))
            {
                var str = File.ReadAllText("local_accounts_list_making.json") + "]";
                var tmp = JsonConvert.DeserializeObject<IEnumerable<Account>>(str).Where(x => !x.AccountName.Contains("@")).Select(x => new RegisteredAccount(x));
                accountList.AddRange(tmp);
                fs = File.Open("local_accounts_list_making.json", FileMode.Append);
                sw = new StreamWriter(fs);
            }
            else
            {
                fs = File.OpenWrite("local_accounts_list_making.json");
                sw = new StreamWriter(fs);
                sw.Write("[");
            }

            var manager = new Manager("imastodon.net", CId, CSec, Token);
            string token = configText[5];
            var n = accountList.Last().Account.Id;

            var httpc = new HttpClient();
            var followers = httpc.GetAsync($"https://imastodon.net/api/v1/accounts/23599/followers?access_token={token}");
            var last = JsonConvert.DeserializeObject<IEnumerable<Account>>(followers.Result.Content.ReadAsStringAsync().Result).OrderByDescending(x => x.Id).First().Id;

            var timer = new Timer(new TimerCallback((s) => {
                var response = httpc.GetAsync($"https://imastodon.net/api/v1/accounts/{n}?access_token={token}");
                if (response.Result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    n++;
                    var acc = JsonConvert.DeserializeObject<Account>(response.Result.Content.ReadAsStringAsync().Result);
                    if (!acc.AccountName.Contains("@"))
                    {
                        accountList.Add(new RegisteredAccount(acc));
                    }
                    sw.WriteLine($"{JsonConvert.SerializeObject(acc)},");
                }
                else if((int)response.Result.StatusCode == 429)
                {
                    Debug.WriteLine($"{response.Result.StatusCode}, {response.Result.Content.ReadAsStringAsync().Result}");
                    return;
                }
                else
                {
                    n++;
                    Debug.WriteLine($"{response.Result.StatusCode}, {response.Result.Content.ReadAsStringAsync().Result}");
                }
            }));
            timer.Change(0, 1050);
            while(n < last)
            {
                Debug.WriteLine($"{n}, {accountList.Count}");
                Thread.Sleep(1050);
            }
            var serialized = JsonConvert.SerializeObject(accountList);
            File.WriteAllText("local_accounts_list.json", $"{serialized}\n");
        }
    }
}
