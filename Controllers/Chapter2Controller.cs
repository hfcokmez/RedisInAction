using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedisInAction.Services;

namespace RedisInAction.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Chapter2Controller : ControllerBase
    {
        private readonly Chapter2 _chapter2;
    
        public Chapter2Controller(Chapter2 chapter2)
        {
            _chapter2 = chapter2;
        }
    }
}