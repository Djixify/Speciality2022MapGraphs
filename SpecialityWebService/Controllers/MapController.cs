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
        [HttpGet("generate/token={token};{minx},{miny},{maxx},{maxy}")]
        public FileContentResult Get(string token, double minx, double miny, double maxx, double maxy)
        {
            Byte[] b = System.IO.File.ReadAllBytes(@".\Resources\Images\gandalf_nyhed.jpg"); 
            MemoryStream s = new MemoryStream(b);

            return null;
        }

        [HttpGet("service={service}")]
        public FileContentResult Get(string service)
        {
            if (service == "wms")
            {
                DataforsyningenBackground_WMS wms = new DataforsyningenBackground_WMS();
                wms.SetBoundaryBox(588352.5683496139245, 6136975.095706283115, 588872.8597855410771, 6138732.095496748574);
                double width = 500;
                double height = 500 * (wms.BBox.Height / wms.BBox.Width);
                wms.PixelWidth = (int)width;
                wms.PixelHeight = (int)height;

                return File(wms.GetImageBytes(), "image/jpg");
            }
            else if (service == "wfs")
            {
                GeoDanmark60_WFS wfs = new GeoDanmark60_WFS();
                wfs.SetBoundaryBox(588352.5683496139245, 6136975.095706283115, 588872.8597855410771, 6138732.095496748574);

                string result = null;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(wfs.GenerateUrl());
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();
                }
                return File(Encoding.UTF8.GetBytes(result), "text/xml");
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
