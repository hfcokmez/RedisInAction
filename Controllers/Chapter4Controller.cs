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
    public class Chapter4Controller : ControllerBase
    {
        private readonly Chapter4 _chapter4;
    
        public Chapter4Controller(Chapter4 chapter4)
        {
            _chapter4 = chapter4;
        }
    }
}