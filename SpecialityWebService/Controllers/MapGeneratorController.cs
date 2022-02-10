using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SpecialityWebService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MapGeneratorController : ControllerBase
    {
        // GET: /MapGenerator
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET /MapGenerator/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST /MapGenerator
        [HttpPost]
        public void Post([FromBody] JsonDocument value)
        {
            System.Diagnostics.Debug.WriteLine("Test: " + value);
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] JsonDocument value)
        {
            System.Diagnostics.Debug.WriteLine("Test " + id + ": " + value);
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
