using static SpecialityWebService.MathObjects;

namespace SpecialityWebService
{
    public class GeoDanmark60_WFS
    {
        //{0}: Token; Token generated from webservice to access
        //{1}: BBox minx; format UTM32N
        //{2}: BBox miny; format UTM32N
        //{3}: BBox maxx; format UTM32N
        //{4}: BBox maxy; format UTM32N

        public string Url { get; set; } = @"https://api.dataforsyningen.dk/GeoDanmark60_NOHIST_GML3_DAF?token={0}&servicename=GeoDanmark60_NOHIST_GML3_DAF&SERVICE=WFS&REQUEST=GetFeature&VERSION=2.0.0&TYPENAMES=gdk60:Vejmidte&SRSNAME=urn:ogc:def:crs:EPSG::25832&BBOX={1},{2},{3},{4},urn:ogc:def:crs:EPSG::25832&NAMESPACES=xmlns(gdk60,http://data.gov.dk/schemas/geodanmark60/2/gml3)&NAMESPACE=xmlns(gdk60,http://data.gov.dk/schemas/geodanmark60/2/gml3)";
        public string Token { get; set; } = null;
        public Rectangle BBox { get; set; } = Rectangle.Zero();

        public GeoDanmark60_WFS(string token = "024b9d34348dd56d170f634e067274c6")
        {
            Token = token;
        }

        public void SetBoundaryBox(double minx, double miny, double maxx, double maxy)
        {
            BBox = Rectangle.FromLBRT(minx, miny, maxx, maxy);
        }

        public void SetBoundaryBox(Rectangle rect)
        {
            BBox = rect;
        }

        public string GenerateUrl() => string.Format(Url, Token, BBox.MinX, BBox.MinY, BBox.MaxX, BBox.MaxY);
        public string GetXML()
        {
            return null;
        }
    }
}
