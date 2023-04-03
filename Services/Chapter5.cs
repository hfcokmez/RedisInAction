using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using RedisInAction.Models;
namespace RedisInAction.Services
{
    public class Chapter5
    {
        private Dictionary<LogLevel, string> Severity;
        private readonly RedisService _redisService;
        private readonly int[] _precision = {1, 5, 60, 300, 3600, 18000, 86400};

        public Chapter5(RedisService redisService)
        {
            _redisService = redisService;
            Severity = new Dictionary<LogLevel, string>
            {
                { LogLevel.Debug, "debug" },
                { LogLevel.Info, "info" },
                { LogLevel.Warning, "warning" },
                { LogLevel.Error, "error" },
                { LogLevel.Critical, "critical" }
            };

            foreach (var value in Enum.GetValues(typeof(LogLevel)))
            {
                var name = Enum.GetName(typeof(LogLevel), value);
                Severity[(LogLevel)value] = name.ToLower();
            }
        }

        public async Task LogRecentAsync(string name, string message, LogLevel severity = LogLevel.Info,
            ITransaction transaction = null)
        {
            string severityString = Severity.ContainsKey(severity)
                ? Severity[severity].ToLower()
                : severity.ToString().ToLower();
            string destination = $"recent:{name}:{severityString}";
            string timestamp = DateTime.Now.ToString("r");
            string fullMessage = $"{timestamp} {message}";

            var db = _redisService.GetDatabase();
            bool shouldExecute = transaction == null;

            transaction ??= db.CreateTransaction();
            transaction.ListLeftPushAsync(destination, fullMessage);
            transaction.ListTrimAsync(destination, 0, 99);

            //Should execute koymamızın sebebi eğer buraya bir transaction göndermişsek bunu sadece pipeline'a ekleyip execute etmemesi içindir.
            if (shouldExecute)
            {
                await transaction.ExecuteAsync();
            }
        }
        
        public async Task LogCommonAsync(string name, string message, LogLevel severity = LogLevel.Info, int timeout = 5)
        {
            string severityString = Severity.ContainsKey(severity)
                ? Severity[severity].ToLower()
                : severity.ToString().ToLower();
            string destination = $"common:{name}:{severityString}";
            string startKey = $"{destination}:start";
            var db = _redisService.GetDatabase();

            DateTime end = DateTime.UtcNow.AddSeconds(timeout);
            while (DateTime.UtcNow < end)
            {
                try
                {
                    var transaction = db.CreateTransaction();
                    DateTime now = DateTime.UtcNow;
                    DateTime hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

                    string existing = await db.StringGetAsync(startKey);
                    if (!string.IsNullOrEmpty(existing) && DateTime.Parse(existing) < hourStart)
                    {
                        transaction.KeyRenameAsync(destination, $"{destination}:last");
                        transaction.KeyRenameAsync(startKey, $"{destination}:pstart");
                        transaction.StringSetAsync(startKey, hourStart.ToString("o"));
                    }
                    else if (string.IsNullOrEmpty(existing))
                    {
                        transaction.StringSetAsync(startKey, hourStart.ToString("o"));
                    }

                    transaction.SortedSetIncrementAsync(destination, message, 1);

                    await LogRecentAsync(name, message, severity, transaction);
                    await transaction.ExecuteAsync();
                    break;
                }
                catch (TaskCanceledException)
                {
                    // Retry the loop
                }
            }
        }
        
        public async Task<bool> UpdateCounterAsync(string name, int count = 1, long? now = null)
        {
            var nowUnix = now ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var db = _redisService.GetDatabase();
            var transaction = db.CreateTransaction();

            foreach (var prec in _precision)
            {
                long pnow = (nowUnix / prec) * prec;
                string hash = $"{prec}:{name}";
                transaction.SortedSetAddAsync("known:", hash, prec);
                transaction.HashIncrementAsync($"count:{hash}", pnow.ToString(), count);
            }

            var result = await transaction.ExecuteAsync();
            return result;
        }
        
        public async Task<List<Tuple<int, int>>> GetCounterAsync(string name, int precision)
        {
            string hash = $"{precision}:{name}";
            var db = _redisService.GetDatabase();
            var data = await db.HashGetAllAsync($"count:{hash}");
            var results = new List<Tuple<int, int>>();

            foreach (var entry in data)
            {
                int key = int.Parse(entry.Name);
                int value = int.Parse(entry.Value);
                results.Add(new Tuple<int, int>(key, value));
            }
            results.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return results;
        }

        private const int SampleCount = 100;
        private bool quit = false;
        
        public async Task CleanCountersAsync()
        {
            var db = _redisService.GetDatabase();
            int passes = 0;
            while (!quit)
            {
                long start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int index = 0;
                while (index < await db.SortedSetLengthAsync("known:"))
                {
                    var hashSet = await db.SortedSetRangeByRankAsync("known:", index, index);
                    index++;
                    if (hashSet.Length == 0)
                    {
                        break;
                    }
                    string hash = hashSet[0].ToString();
                    int prec = int.Parse(hash.Substring(0, hash.IndexOf(':')));
                    int bprec = (int)Math.Floor(prec / 60.0) != 0 ? (int)Math.Floor(prec / 60.0) : 1;

                    if (bprec == 0)
                    {
                        bprec = 1;
                    }
                    if (passes % bprec != 0)
                    {
                        continue;
                    }
                    string hkey = "count:" + hash;
                    long cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - SampleCount * prec;
                    RedisValue[] samples = db.HashKeys(hkey);
                    Array.Sort(samples);
                    int remove = Array.BinarySearch(samples, cutoff.ToString());
                    if (remove >= 0)
                    {
                        db.HashDelete(hkey, samples[0..remove]);
                        if (remove == samples.Length)
                        {
                            ITransaction trans = db.CreateTransaction();
                            trans.AddCondition(Condition.KeyNotExists(hkey));
                            if (db.HashLength(hkey) == 0)
                            {
                                trans.SortedSetRemoveAsync("known:", hash);
                                await trans.ExecuteAsync();
                                index--;
                            }
                        }
                    }
                }
            }
        }
    }
}