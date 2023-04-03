using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedisInAction.DTOs;
using RedisInAction.Models;
using RedisInAction.Services;

namespace RedisInAction.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Chapter5Controller : ControllerBase
{
    private readonly Chapter5 _chapter5;

    public Chapter5Controller(Chapter5 chapter5)
    {
        _chapter5 = chapter5;
    }

    [HttpPost("LogRecent")]
    public async Task<IActionResult> LogRecent(LogDTO logDTO)
    {
        await _chapter5.LogRecentAsync(logDTO.Name, logDTO.Message, logDTO.LogLevel);
        return Ok();
    }
    
    [HttpPost("LogCommon")]
    public async Task<IActionResult> LogCommon(LogDTO logDTO)
    {
        await _chapter5.LogCommonAsync(logDTO.Name, logDTO.Message, logDTO.LogLevel);
        return Ok();
    }
    
    [HttpPost("UpdateCounter")]
    public async Task<IActionResult> UpdateCounter(UpdateHitCounterDTO hitDTO)
    {
        await _chapter5.UpdateCounterAsync(hitDTO.Name, hitDTO.Count, hitDTO.CurrentTimeUnix);
        return Ok();
    }
    
    [HttpPost("GetCounter")]
    public async Task<IActionResult> GetCounter(GetCounterDTO getCounter)
    {
        var result = await _chapter5.GetCounterAsync(getCounter.Name, getCounter.Precision);
        return Ok(result);
    }
    
    [HttpPost("CleanCounter")]
    public async Task<IActionResult> CleanCounter()
    {
        await _chapter5.CleanCountersAsync();
        return Ok();
    }
}