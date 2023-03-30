using RedisInAction.Models;

namespace RedisInAction.DTOs;

public class LogDTO
{
    public string Name { get; set; }
    public string Message { get; set; }
    public LogLevel LogLevel { get; set; }
}