using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SpecialityWebService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MapController : ControllerBase
    {
        [HttpGet]
        public FileContentResult Get()
        {
            Byte[] b = System.IO.File.ReadAllBytes(@".\Resources\Images\gandalf_nyhed.jpg");
            return File(b, "image/jpeg");
        }

        // GET: /Map
        [HttpGet("generate/token={wmstoken};dataset={dataset};bbox={minx},{miny},{maxx},{maxy}")]
        public FileContentResult Get(string wmstoken = "024b9d34348dd56d170f634e067274c6", string dataset = "geodanmark60/vejmanhastigheder", double minx = 586835.1, double miny = 6135927.2, double maxx = 591812.3, double maxy = 6139738.0)
        {
            DataforsyningenBackground_WMS wms = new DataforsyningenBackground_WMS(wmstoken);

            Map.Dataset ds;
            switch (dataset)
            {
                case "geodanmark60":
                    ds = Map.Dataset.GeoDanmark60;
                    break;
                case "vejmanhastigheder":
                    ds = Map.Dataset.VejmanHastigheder;
                    break;
                default:
                    ds = Map.Dataset.VejmanHastigheder;
                    break;
            }

            Map map = new Map(wmstoken, ds, 1280, minx, miny, maxx, maxy);

            return File(map.RenderImage(System.Drawing.Imaging.ImageFormat.Jpeg), "image/jpg");
        }

        [HttpGet("service={service}")]
        public FileContentResult Get(string service)
        {
            if (service == "wms")
            {
                DataforsyningenBackground_WMS wms = new DataforsyningenBackground_WMS();

                return File(wms.GetImageBytes(588352.5683496139245, 6136975.095706283115, 588872.8597855410771, 6138732.095496748574, 1280), "image/jpg");
            }
            else
            {
                return null;
            }
        }

        // POST /Map
        [HttpPost]
        public void Post([FromBody] JsonDocument value)
        {
            System.Diagnostics.Debug.WriteLine("Test: " + value.ToString());
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
