using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

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
                        accountList.AddRange(JsonConvert.DeserializeObject<List<RegisteredAccount>>(str));
                        time = new FileInfo($"accounts{args[1]}.json").LastWriteTime.Date;
                        olddraw = true;
                    }
                    break;
                default:
                    Update();
                    break;
            }

            var pyArg = (olddraw ? $"old {time.AddDays(-1).ToShortDateString()} " : $"{time.ToShortDateString()} {time.ToShortTimeString()} ") + string.Join(" ", accountList.Select(x => x.TootsToday.ToString()));
            Draw_CallPython(pyArg);
        }

        static void Draw_CallPython(string pyArg)
        {
            Process.Start("python", $"DrawHistogram.py {pyArg}");
        }

        static void NewDay()
        {
            var manager = new Manager("imastodon.net", CId, CSec, Token);
            var statuses = manager.Client.GetPublicTimeline(limit: 40, local: true).Result.ToArray();
            var max = statuses.First().Id;
            time = DateTime.Now;
            var startCount = max;
            var startToday = 0;
            for (var i = 0; i < 400; i++)
            {
                try
                {
                    var tl = manager.Client.GetPublicTimeline(limit: 40, maxId: max, local: true).Result.ToArray();
                    Debug.WriteLine($"{i}, {tl.Last().Id} -> {tl.First().Id}");
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
                        var todaysStatus = tl.Where(x => TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == DateTime.Today.ToShortDateString()).ToArray();
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
            var str = File.ReadAllText("accounts.json");
            accountList.AddRange(JsonConvert.DeserializeObject<List<RegisteredAccount>>(str));

            var manager = new Manager("imastodon.net", CId, CSec, Token);
            var statuses = manager.Client.GetPublicTimeline(limit: 40, local: true).Result.ToArray();
            var max = statuses.First().Id;
            var yesterday = DateTime.Today.AddDays(-1);
            time = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0);
            var startCount = max;
            var endCount = int.Parse(File.ReadAllText("PreviousIds.dat"));
            for (var i = 0; i < 400; i++)
            {
                try
                {
                    var tl = manager.Client.GetPublicTimeline(limit: 40, maxId: max, local: true).Result.ToArray();
                    Debug.WriteLine($"{i}, {tl.Last().Id} -> {tl.First().Id}");

                    if(tl.Last().Id > endCount)
                    {
                        var yesterdaysStatus = tl.Where(x => TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == yesterday.ToShortDateString());
                        foreach (var status in yesterdaysStatus)
                        {
                            accountList.Update(status);
                        }
                        max = tl.Last().Id - 1;
                    }
                    else
                    {
                        var yesterdaysStatus = tl.Where(x => x.Id > endCount && TimeZoneInfo.ConvertTimeFromUtc(x.CreatedAt, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToShortDateString() == yesterday.ToShortDateString());
                        foreach (var status in yesterdaysStatus)
                        {
                            accountList.Update(status);
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
            var serialized = JsonConvert.SerializeObject(accountList);
            File.WriteAllText("accounts.json", $"{serialized}\n");
            File.Move("accounts.json", $"accounts{yesterday.Year}{yesterday.Month}{yesterday.Day}.json");
            File.WriteAllText("PreviousIds.dat", $"{startCount}");

            foreach (var account in accountList.OrderBy(x => x.TootsToday))
            {
                Debug.WriteLine($"{account.Account.AccountName}, {account.TootsToday}");
            }
        }

        static void Update()
        {
            var str = File.ReadAllText("accounts.json");
            accountList.AddRange(JsonConvert.DeserializeObject<List<RegisteredAccount>>(str));

            var manager = new Manager("imastodon.net", CId, CSec, Token);
            var statuses = manager.Client.GetPublicTimeline(limit: 40, local: true).Result.ToArray();
            var max = statuses.First().Id;
            time = DateTime.Now;
            var startCount = max;
            var endCount = int.Parse(File.ReadAllText("PreviousIds.dat"));
            for(var i = 0; i < 400; i++) {
                try
                {
                    var tl = manager.Client.GetPublicTimeline(limit: 40, maxId: max, local: true).Result.ToArray();
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
                        var notCountedStatus = tl.Where(x => x.Id > endCount);
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
    }
}
