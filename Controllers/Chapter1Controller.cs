using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedisInAction.Services;

namespace RedisInAction.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Chapter1Controller : ControllerBase
{
    private readonly Chapter1 _chapter1;
    
    public Chapter1Controller(Chapter1 chapter1)
    {
        _chapter1 = chapter1;
    }
    
}