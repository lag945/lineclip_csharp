using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LineClip
{
    /// <summary>
    /// A very fast C# library for clipping polylines and polygons by a bounding box.
    /// Transplant https://github.com/mapbox/lineclip/blob/master/index.js，Latest commit 10a422f on 19 Jul 2018 to csharp版
    /// Source license : https://github.com/mapbox/lineclip/blob/master/LICENSE
    /// Newest source link : https://github.com/lag945/lineclip_csharp
    /// </summary>
    class LineClip
    {
        // Cohen-Sutherland line clippign algorithm, adapted to efficiently
        // handle polylines rather than just segments
        public static GeoPoint[] Polyline(GeoPoint[] points, GeoBoundary bbox)
        {
            int len = points.Length;
            int codeA = BitCode(points[0], bbox);
            int lastCode = 0;
            List<GeoPoint> ret = new List<GeoPoint>();
            List<GeoPoint> part = new List<GeoPoint>();

            for (int i = 1; i < len; i++)
            {
                GeoPoint a = new GeoPoint(points[i - 1]);
                GeoPoint b = new GeoPoint(points[i]);
                int codeB = lastCode = BitCode(b, bbox);
                while (true)
                {
                    if ((codeA | codeB) == 0)
                    { // accept
                        part.Add(a);

                        if (codeB != lastCode)
                        { // segment went outside
                            part.Add(b);

                            if (i < len - 1)
                            { // start a new line
                                ret.AddRange(part);
                                part.Clear();
                            }
                        }
                        else if (i == len - 1)
                        {
                            part.Add(b);
                        }
                        break;

                    }
                    else if ((codeA & codeB) != 0)
                    { // trivial reject
                        break;

                    }
                    else if (codeA != 0)
                    { // a outside, intersect with clip edge
                        a = Intersect(a, b, codeA, bbox);
                        codeA = BitCode(a, bbox);

                    }
                    else
                    { // b outside
                        b = Intersect(a, b, codeB, bbox);
                        codeB = BitCode(b, bbox);
                    }
                }
                codeA = lastCode;
            }

            if (part.Count > 0)
            {
                ret.AddRange(part);
            }

            return ret.ToArray();
        }

        // Sutherland-Hodgeman polygon clipping algorithm
        public static GeoPoint[] Polygon(GeoPoint[] points, GeoBoundary bbox)
        {
            List<GeoPoint> ret = new List<GeoPoint>();

            // clip against each side of the clip rectangle
            for (int edge = 1; edge <= 8; edge *= 2)
            {
                ret.Clear();
                GeoPoint prev = new GeoPoint(points[points.Length - 1]);
                bool prevInside = (BitCode(prev, bbox) & edge) == 0;

                for (int i = 0; i < points.Length; i++)
                {
                    GeoPoint p = new GeoPoint(points[i]);
                    bool inside = (BitCode(p, bbox) & edge) == 0;

                    // if segment goes through the clip window, add an intersection
                    if (inside != prevInside) ret.Add(Intersect(prev, p, edge, bbox));

                    if (inside) ret.Add(p); // add a point if it's inside

                    prev = p;
                    prevInside = inside;
                }

                points = ret.ToArray();

                if (points.Length == 0)
                {
                    break;
                }

            }

            return ret.ToArray();
        }

        // intersect a segment against one of the 4 lines that make up the bbox
        protected static GeoPoint Intersect(GeoPoint a, GeoPoint b, int edge, GeoBoundary bbox)
        {
            GeoPoint ret = null;

            if ((edge & 8) != 0)
            {
                ret = new GeoPoint(a.x + (b.x - a.x) * (bbox.north - a.y) / (b.y - a.y), bbox.north);// top
            }
            else if ((edge & 4) != 0)
            {
                ret = new GeoPoint(a.x + (b.x - a.x) * (bbox.south - a.y) / (b.y - a.y), bbox.south);// bottom
            }
            else if ((edge & 2) != 0)
            {
                ret = new GeoPoint(bbox.east, a.y + (b.y - a.y) * (bbox.east - a.x) / (b.x - a.x));// right
            }
            else if ((edge & 1) != 0)
            {
                ret = new GeoPoint(bbox.west, a.y + (b.y - a.y) * (bbox.west - a.x) / (b.x - a.x));// left
            }
            else
            {
                ret = null;
            }

            return ret;
        }

        // bit code reflects the point position relative to the bbox:

        //                  left  mid  right
        //        top  1001  1000  1010
        //       mid  0001  0000  0010
        // bottom  0101  0100  0110
        public static int BitCode(GeoPoint p, GeoBoundary bbox)
        {
            int code = 0;

            if (p.x < bbox.west) code |= 1; // left
            else if (p.x > bbox.east) code |= 2; // right

            if (p.y < bbox.south) code |= 4; // bottom
            else if (p.y > bbox.north) code |= 8; // top

            return code;
        }

    }

    /// <summary>
    /// 幾何點
    /// </summary>
    class GeoPoint
    {
        public double x, y;
        public GeoPoint(double a_x, double a_y)
        {
            x = a_x;
            y = a_y;
        }

        public GeoPoint(GeoPoint a)
        {
            x = a.x;
            y = a.y;
        }


    }

    /// <summary>
    /// 幾何邊界，西南東北
    /// </summary>
    class GeoBoundary
    {
        public double west, south, east, north;

        public GeoBoundary(double a_west, double a_south, double a_east, double a_north)
        {
            west = a_west;
            south = a_south;
            east = a_east;
            north = a_north;
        }
    }

}
