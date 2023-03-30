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
        
    }
}