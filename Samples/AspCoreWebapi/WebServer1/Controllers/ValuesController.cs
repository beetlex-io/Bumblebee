using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebServer1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { $"{this.HttpContext.Connection.LocalIpAddress}${this.HttpContext.Connection.LocalPort}|{DateTime.Now}" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return $"{this.HttpContext.Connection.LocalIpAddress}${this.HttpContext.Connection.LocalPort}|{DateTime.Now}";
        }

        // POST api/values
        [HttpPost]
        public ActionResult<string> Post([FromBody] string value)
        {
            return $"{this.HttpContext.Connection.LocalIpAddress}${this.HttpContext.Connection.LocalPort}|{DateTime.Now}";
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public ActionResult<string> Put(int id, [FromBody] string value)
        {
            return $"{this.HttpContext.Connection.LocalIpAddress}${this.HttpContext.Connection.LocalPort}|{DateTime.Now}";
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public ActionResult<string> Delete(int id)
        {
            return $"{this.HttpContext.Connection.LocalIpAddress}${this.HttpContext.Connection.LocalPort}|{DateTime.Now}";
        }
    }
}
