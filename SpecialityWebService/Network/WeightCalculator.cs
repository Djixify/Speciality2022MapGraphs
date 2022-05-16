using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;

namespace SpecialityWebService.Network
{
    public class WeightCalculator
    {
        public static List<KeyValuePair<string, double>> ComputeWeight(IEnumerable<Point> edgepoints, Path path, List<string> formulas)
        {
            if (edgepoints.Count() < 2)
                throw new ArgumentException("Edge is expected to have 2 or more points as part of its rendered path");

            double distance = 0.0;
            bool firstPoint = true;
            Point prevp = new Point(0.0,0.0);
            foreach (Point p in edgepoints)
            {
                if (firstPoint)
                    distance += prevp.Distance(p);
                firstPoint = false;
                prevp = p;
            }

            //TODO: Get parser from discord bot

            return formulas.Select(formula => new KeyValuePair<string, double>(formula, distance)).ToList();
        }
    }
}
