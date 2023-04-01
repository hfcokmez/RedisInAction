using System;

namespace RedisInAction.DTOs;

public class UpdateHitCounterDTO
{
    public string Name { get; set; }
    public int Count { get; set; }
    public long? CurrentTimeUnix { get; set; }      
}