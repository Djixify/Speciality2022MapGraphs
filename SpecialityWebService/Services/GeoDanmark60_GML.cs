using SpecialityWebService.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService
{
    public class GeoDanmark60_GML : IGMLReader
    {

        public string File { get; set; } = null;
        private FileStream _fileStream { get; set; } = null;
        public XDocument Doc { get; set; } = null;

        public GeoDanmark60_GML(string file)
        {
            File = file;
            _fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            Doc = XDocument.Load(file);
            if (Doc.Root.Name.LocalName != "FeatureCollection")
                throw new XmlException("Invalid gml file, expected a feature collection as the root");
        }

        public Rectangle GetBoundaryBox()
        {
            Rectangle box = Rectangle.InfiniteInverse();
            foreach (XElement elm in Doc.Root.Elements())
            {
                //If file has a boundedBy element, just use that
                if (elm.Name.LocalName == "boundedBy")
                {
                    GetRectangleByBounds(elm, ref box);
                    return box;
                }
                //Else check through all features for linestrings
                else if (elm.Name.LocalName == "featureMember")
                {
                    XElement boundelement = elm.Elements().ElementAt(0).Elements().ElementAt(0);
                    //If Linestring has a boundedBy use that
                    if (boundelement.Name.LocalName == "boundedBy")
                        UpdateRectangleByBounds(boundelement, ref box);
                    //Else compute the new bounds based on every point in the linestring
                    else
                    {
                        foreach (XElement feature in elm.Elements().ElementAt(0).Elements())
                        {
                            if (feature.Name.LocalName == "geometryProperty")
                            {
                                UpdateRectangleByBounds(feature, ref box);
                                break;
                            }
                        }
                    }
                }
            }
            return box;
        }

        private void GetRectangleByBounds(XElement boundedBy, ref Rectangle rect)
        {
            string lowercorner = boundedBy.Elements().ElementAt(0).Elements().ElementAt(0).Value;
            string uppercorner = boundedBy.Elements().ElementAt(0).Elements().ElementAt(1).Value;
            string[] lowercoords = lowercorner.Split(' ');
            string[] uppercoords = uppercorner.Split(' ');
            rect.MinX = Double.Parse(lowercoords[0], CultureInfo.InvariantCulture);
            rect.MinY = Double.Parse(lowercoords[1], CultureInfo.InvariantCulture);
            rect.MaxX = Double.Parse(uppercoords[0], CultureInfo.InvariantCulture);
            rect.MaxY = Double.Parse(uppercoords[1], CultureInfo.InvariantCulture);
        }

        private void UpdateRectangleByBounds(XElement boundedBy, ref Rectangle rect)
        {
            if (boundedBy.Name.LocalName == "boundedBy")
            {
                string lowercorner = boundedBy.Elements().ElementAt(0).Elements().ElementAt(0).Value;
                string uppercorner = boundedBy.Elements().ElementAt(0).Elements().ElementAt(1).Value;
                string[] lowercoords = lowercorner.Split(' ');
                string[] uppercoords = uppercorner.Split(' ');
                rect.MinX = Math.Min(rect.MinX, Double.Parse(lowercoords[0], CultureInfo.InvariantCulture));
                rect.MinY = Math.Min(rect.MinY, Double.Parse(lowercoords[1], CultureInfo.InvariantCulture));
                rect.MaxX = Math.Max(rect.MaxX, Double.Parse(uppercoords[0], CultureInfo.InvariantCulture));
                rect.MaxY = Math.Max(rect.MaxY, Double.Parse(uppercoords[1], CultureInfo.InvariantCulture));
            }
            else if (boundedBy.Name.LocalName == "geometryProperty")
            {
                XElement curve = boundedBy.Elements().ElementAt(0);
                //Do not support anything but linestrings for now
                if (curve.Name.LocalName == "LineString")
                {
                    XElement poslist = curve.Elements().ElementAt(0);
                    string[] positionpairs = poslist.Value.Split(' ');
                    for (int i = 0; i < positionpairs.Length; i += 2)
                    {
                        double x = Double.Parse(positionpairs[i], CultureInfo.InvariantCulture);
                        double y = Double.Parse(positionpairs[i + 1], CultureInfo.InvariantCulture);
                        rect.MinX = Math.Min(rect.MinX, x);
                        rect.MinY = Math.Min(rect.MinY, y);
                        rect.MaxX = Math.Max(rect.MaxX, x);
                        rect.MaxY = Math.Max(rect.MaxY, y);
                    }
                }
                if (curve.Name.LocalName == "MultiCurve")
                {
                    foreach (XElement member in curve.Elements())
                    {
                        if (member.Elements().ElementAt(0).Name.LocalName == "LineString")
                        {
                            XElement path = member.Elements().ElementAt(0);
                            XElement poslist = path.Elements().ElementAt(0);
                            string[] positionpairs = poslist.Value.Split(' ');
                            for (int i = 0; i < positionpairs.Length; i += 2)
                            {
                                double x = Double.Parse(positionpairs[i], CultureInfo.InvariantCulture);
                                double y = Double.Parse(positionpairs[i + 1], CultureInfo.InvariantCulture);
                                rect.MinX = Math.Min(rect.MinX, x);
                                rect.MinY = Math.Min(rect.MinY, y);
                                rect.MaxX = Math.Max(rect.MaxX, x);
                                rect.MaxY = Math.Max(rect.MaxY, y);
                            }
                        }
                    }
                }
            }
        }

        public int GetFeatureCount()
        {
            int count = 0; //Skip first node as it is a boundary box
            foreach (XElement elm in Doc.Root.Elements())
                count += elm.Name.LocalName == "featureMember" ? 1 : 0;
            return count;
        }

        public IEnumerable<XElement> GetFeatureEnumerator()
        {
            return Doc.Root.Elements().Where(elem => elem.Name.LocalName == "featureMember");
        }

        public IEnumerable<Path> GetPathEnumerator(Rectangle bbox)
        {
            return GetPathEnumerator(bbox, new List<string>());
        }

        public IEnumerable<Path> GetPathEnumerator(Rectangle bbox, List<string> columns2extract)
        {
            int i = 0;
            List<Path> paths = new List<Path>();
            foreach (XElement feature in GetFeatureEnumerator())
            {
                List<Path> parsedpaths = Path.FromXML(i, feature, columns2extract);
                paths.AddRange(parsedpaths.Where(p => p.BoundaryBox.Overlapping(bbox)));
                i++;
            }
            return paths;
        }

        public void Dispose()
        {
            _fileStream.Close();
        }
    }
}
