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
public class RedisController : ControllerBase
{
    private readonly Chapter4 _chapter4;
    private readonly Chapter5 _chapter5;

    public RedisController(Chapter5 chapter5, Chapter4 chapter4)
    {
        _chapter5 = chapter5;
        _chapter4 = chapter4;
    }

    [HttpGet]
    public async Task<IActionResult> Chapter4()
    {
        return Ok();
    }

    [HttpPost("LogCommon")]
    public async Task<IActionResult> LogCommon(LogDTO logDTO)
    {
        await _chapter5.LogCommonAsync(logDTO.Name, logDTO.Message, logDTO.LogLevel);
        return Ok();
    }
}