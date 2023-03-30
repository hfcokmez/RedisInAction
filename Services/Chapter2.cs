using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisInAction.Services
{
    public class Chapter2
    {
        private readonly RedisService _redisService;
        
        public Chapter2(RedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task<string> CheckToken(string token)
        {
            return await _redisService.GetDatabase().HashGetAsync($"login:", token);
        }

        public async Task UpdateToken(string token, string user, string item = null)
        {
            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _redisService.GetDatabase().HashSetAsync("login:", token, user);
            await _redisService.GetDatabase().SortedSetAddAsync("recent:", new SortedSetEntry[]{new SortedSetEntry(token, timeStamp)});
            if (item != null)
            {
                await _redisService.GetDatabase().SortedSetAddAsync($"viewed:{token}",new SortedSetEntry[] { new SortedSetEntry(token, timeStamp) });
                await _redisService.GetDatabase().SortedSetRemoveRangeByRankAsync($"viewed:{token}", 0, -26);
            }
        }

        public async Task AddToCart(string session, string item, int count)
        {
            if (count <= 0)
            {
                await _redisService.GetDatabase().HashDeleteAsync($"cart:{session}", item);
            }
            else
            {
                await _redisService.GetDatabase().HashSetAsync($"cart:{session}", item, count.ToString());
            }
        }
        
    }
}