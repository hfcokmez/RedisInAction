using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisInAction.Models;
using StackExchange.Redis;

namespace RedisInAction.Services
{
    public class Chapter1
    {
        private readonly RedisService _redisService;
        private readonly int _oneWeekInSeconds = 7 * 86400;
        private readonly int _voteScore = 432;
        private readonly int _articlesPerPage = 25;

        public Chapter1(RedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task VoteForArticle(string userId, string articleId)
        {
            var cutoff = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds - _oneWeekInSeconds;
            var isToday = await _redisService.GetDatabase().SortedSetScoreAsync("time:", articleId);
            if (isToday < cutoff)
                return;

            var vote = await _redisService.GetDatabase().SetAddAsync(string.Format("voted:", articleId), userId);
            if (vote)
            {
                await Task.WhenAll(_redisService.GetDatabase().SortedSetIncrementAsync("score:", articleId, _voteScore),
                    _redisService.GetDatabase().HashIncrementAsync(articleId, "votes", 1));
            }
        }
        
        public async Task<string> PostArticle(string user, string title, string link)
        {
            var articleId = _redisService.GetDatabase().StringIncrementAsync("article").ToString();
            string voted = $"voted:{articleId}";
            await _redisService.GetDatabase().SetAddAsync(voted, user);
            await _redisService.GetDatabase().KeyExpireAsync(voted, TimeSpan.FromSeconds(_oneWeekInSeconds));
            var now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            string article = $"article:{articleId}";

            _redisService.GetDatabase().HashSetAsync(article, new HashEntry[]
            {
                new HashEntry("title", title),
                new HashEntry("link", link),
                new HashEntry("poster", user),
                new HashEntry("time", now.ToString()),
                new HashEntry("votes", 1)
            });
            
            await _redisService.GetDatabase().SortedSetAddAsync("score:", article, now);
            return articleId;
        }
        
        public async Task<List<Dictionary<string, string>>> GetArticles(int page, string order)
        {
            int start = (page - 1) * _articlesPerPage;
            int end = start + _articlesPerPage - 1;

            var ids = await _redisService.GetDatabase().SortedSetRangeByRankAsync(order, start, end, Order.Descending);
            List<Dictionary<string, string>> articles = new List<Dictionary<string, string>>();

            foreach (var id in ids)
            {
                var hashData = await _redisService.GetDatabase().HashGetAllAsync(id.ToString());
                var articleData = hashData.ToStringDictionary();
                articleData["id"] = id;
                articles.Add(articleData);
            }
            return articles;
        }

        public async Task AddRemoveGroups(string articleId, string[] toAdd, string[] toRemove)
        {
            string article = $"article:{articleId}";
            foreach (var group in toAdd)
            {
                await _redisService.GetDatabase().SetAddAsync($"group:{group}", article);
            }

            foreach (var group in toRemove)
            {
                await _redisService.GetDatabase().SetRemoveAsync($"group:{group}", article);
            }
        }

        public async Task<List<Dictionary<string, string>>> GetGroupArticles(string group, int page, string order = "score:")
        {
            string key = order + group;
            if (await _redisService.GetDatabase().KeyExistsAsync(key) == false)
            {
                await _redisService.GetDatabase().SortedSetCombineAndStoreAsync(SetOperation.Intersect, key,
                    new RedisKey[] { "group:" + group, order });

                await _redisService.GetDatabase().KeyExpireAsync(key, TimeSpan.FromSeconds(60));
            }

            return await GetArticles(page, key);
        }
    }
}