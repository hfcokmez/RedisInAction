using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisInAction.Services
{
    public class Chapter4
    {
        private readonly RedisService _redisService;

        public Chapter4(RedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task<bool> ListItem(string itemId, string sellerId, double price)
        {
            string inventory = $"inventory:{sellerId}";
            string item = $"{itemId}.{sellerId}";
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 5000;
            while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < end)
            {
                ITransaction transaction = _redisService.GetDatabase().CreateTransaction();
                ConditionResult condition = transaction.AddCondition(Condition.SetContains(inventory, itemId));
                _ = transaction.SortedSetAddAsync("market:", item, price);
                _ = transaction.SetRemoveAsync(inventory, itemId);
                bool committed = await transaction.ExecuteAsync(CommandFlags.DemandMaster);
                if (condition.WasSatisfied && committed)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> PurchaseItem(string buyerId, string itemId, string sellerId, double lPrice)
        {
            string buyer = $"users:{buyerId}";
            string seller = $"users:{sellerId}";
            string item = $"{itemId}.{sellerId}";
            string inventory = $"inventory:{buyerId}";
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 5000;
            while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < end)
            {
                try
                {
                    IDatabase db = _redisService.GetDatabase();
                    ITransaction trans = db.CreateTransaction();

                    trans.AddCondition(Condition.KeyExists("market:"));
                    trans.AddCondition(Condition.KeyExists(buyer));

                    var priceTask = trans.SortedSetScoreAsync("market:", item);
                    var fundsTask = trans.HashGetAsync(buyer, "funds");

                    await trans.ExecuteAsync();

                    double? price = await priceTask;
                    int funds = (int)await fundsTask;

                    if (price == null || price != lPrice || price > funds)
                    {
                        return false;
                    }

                    trans = db.CreateTransaction();
                    trans.HashIncrementAsync(seller, "funds", (int)price);
                    trans.HashIncrementAsync(buyer, "funds", -(int)price);
                    trans.SetAddAsync(inventory, itemId);
                    trans.SortedSetRemoveAsync("market:", item);

                    bool success = await trans.ExecuteAsync();
                    if (success)
                    {
                        return true;
                    }
                }
                catch (RedisException)
                {
                    // Retry when a watch error occurs
                }
            }

            return false;
        }
    }
}