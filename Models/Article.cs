using System;

namespace RedisInAction.Models
{
    public class Article
    {
        public int Id { get; set; }  
        public string Title { get; set; }
        public string Link { get; set; }
        public string Poster { get; set; }
        public long Time { get; set; }
        public int Votes { get; set; }
    }
}