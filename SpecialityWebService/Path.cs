using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService
{
    public class Path
    {

        public List<Point> Points;
        public Point this[int i] { 
            get { return Points[i]; }
            set 
            {
                Points[i] = value;
                UpdateBoundaryBox();
            }
        }

        public Rectangle BoundaryBox { get; set; }

        private void UpdateBoundaryBox()
        {
            Rectangle rect = Rectangle.InfiniteInverse();
            foreach (Point p in Points)
            {
                rect.MinX = Math.Min(p.X, rect.MinX);
                rect.MinY = Math.Min(p.Y, rect.MinY);
                rect.MaxX = Math.Max(p.X, rect.MaxX);
                rect.MaxY = Math.Max(p.Y, rect.MaxY);
            }
            BoundaryBox = rect;
        }

        public int Id { get; set; } = -1;
        public string Fid { get; set; } = null;
        public Dictionary<string, string> ColumnValues { get; private set; } = null;

        public static Path FromXML(int id, XElement featureMember, List<string> columns2extract)
        {
            if (featureMember.Name.LocalName != "featureMember")
                throw new XmlException("Was not given a featureMember element");
            List<Point> _points = new List<Point>();
            string _fid = null;
            int _id = id;
            Rectangle _boundarybox = Rectangle.InfiniteInverse();

            XElement data = featureMember.Elements().ElementAt(0);
            _fid = data.FirstAttribute.Value;
            bool hadBoundedBy = false;
            Dictionary<string, string> columnvalues = new Dictionary<string, string>();
            foreach (XElement column in data.Elements())
            {
                if (column.Name.LocalName == "boundedBy")
                {
                    hadBoundedBy = true;
                    string lowercorner = column.Elements().ElementAt(0).Elements().ElementAt(0).Value;
                    string uppercorner = column.Elements().ElementAt(0).Elements().ElementAt(1).Value;
                    string[] lowercoords = lowercorner.Split(' ');
                    string[] uppercoords = uppercorner.Split(' ');
                    _boundarybox = Rectangle.InfiniteInverse();
                    _boundarybox.MinX = Double.Parse(lowercoords[0], CultureInfo.InvariantCulture);
                    _boundarybox.MinY = Double.Parse(lowercoords[1], CultureInfo.InvariantCulture);
                    _boundarybox.MaxX = Double.Parse(uppercoords[0], CultureInfo.InvariantCulture);
                    _boundarybox.MaxY = Double.Parse(uppercoords[1], CultureInfo.InvariantCulture);
                }
                else if (columns2extract.Contains(column.Name.LocalName))
                {
                    columnvalues.Add(column.Name.LocalName, column.Value);
                }
                else if (column.Name.LocalName == "geometryProperty")
                {
                    XElement geom = column.Elements().ElementAt(0);
                    if (geom.Name.LocalName == "LineString")
                    {
                        _boundarybox = Rectangle.InfiniteInverse();
                        XElement poslist = geom.Elements().ElementAt(0);
                        string[] positionpairs = poslist.Value.Split(' ');
                        for (int i = 0; i < positionpairs.Length; i += 2)
                        {
                            double x = Double.Parse(positionpairs[i], CultureInfo.InvariantCulture);
                            double y = Double.Parse(positionpairs[i + 1], CultureInfo.InvariantCulture);
                            _points.Add(new Point(x, y));
                            if (!hadBoundedBy)
                            {
                                _boundarybox.MinX = Math.Min(_boundarybox.MinX, x);
                                _boundarybox.MinY = Math.Min(_boundarybox.MinY, y);
                                _boundarybox.MaxX = Math.Max(_boundarybox.MaxX, x);
                                _boundarybox.MaxY = Math.Max(_boundarybox.MaxY, y);
                            }
                        }
                    }
                    else if (geom.Name.LocalName == "MultiCurve")
                    {
                        foreach (XElement member in geom.Elements())
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
                                    _points.Add(new Point(x, y));
                                    if (!hadBoundedBy)
                                    {
                                        _boundarybox.MinX = Math.Min(_boundarybox.MinX, x);
                                        _boundarybox.MinY = Math.Min(_boundarybox.MinY, y);
                                        _boundarybox.MaxX = Math.Max(_boundarybox.MaxX, x);
                                        _boundarybox.MaxY = Math.Max(_boundarybox.MaxY, y);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Path p = new Path();
            p.Points = _points;
            p.BoundaryBox = _boundarybox;
            p.Id = _id;
            p.Fid = _fid;
            p.ColumnValues = columnvalues;
            return p;
        }
    }
}
