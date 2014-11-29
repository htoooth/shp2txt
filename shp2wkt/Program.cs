using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Catfood.Shapefile;
using iBoxDB.LocalServer;
using System.IO;
using System.Text.RegularExpressions;

namespace shp2wkt
{
    public class Assert
    {
        public static void Test(bool condition, System.Func<string> strf)
        {
            if (!condition)
            {
                Console.WriteLine(strf());
            }
        }
        public static void Test(bool condition)
        {
            if (!condition)
            {
                Console.WriteLine("Assertion failed");
            }
        }
    }
    class Program
    {
        public class Item
        {
            public long ID;
            public int Latitude;
            public int Longitude;
            public string Country;
            public string Province;
            public bool IsTreasure;
            public string CountryChineseName;
            public string ProvinceChineseName;
        }
        public class Country
        {
            public long ID;
            public string Name;
            public string Shape;
            public string ChineseName;
        }
        public class Province
        {
            public long ID;
            public string Name;
            public string Country;
            public string Shape;
            public string ChineseName;
        }

        public struct Point
        {
            public double X;
            public double Y;

            public override string ToString()
            {
                return string.Format("{0} {1}", X, Y);
            }
        }

        static DB server = null;
        static DB.AutoBox db = null;

        static public void InitDB()
        {
            string path = string.Empty;
            if (db != null) return;
            DB.Root(path);
            server = new DB(1);
            server.GetConfig().EnsureTable<Province>("Province", "ID");
            server.GetConfig().EnsureTable<Country>("Country", "ID");
            server.GetConfig().EnsureTable<Item>("Item", "ID");

            db = server.Open();
        }

        static void Main(string[] args)
        {
            InitDB();

            // province
            string provincePath = @"d:\projects\data\Export_Output.shp";
            ImportProvinceLayer(provincePath);

            // country
            string countryPath = @"d:\projects\data\New folder\World.shp";
            ImportCountryLayer(countryPath);

            // item
            string itemPath = @"d:\projects\data\longlang.csv";
            ImportItemLayer(itemPath);

            //Test(); 
            //var result =db.Select<Province>("from Province where Country==?", "China");
            //int length = result.Count();
            

            //Console.WriteLine(length);
           
            //foreach (var p in result)
            //{
            //    Console.WriteLine("{0},{1},{2}", p.Name, p.Country, p.Shape);
            //}
            
        }

        static void Test()
        {
            string str = "(1 2,3 4,5 6);(7 8,9 10,11 12)";
            var poly = Str2Poly(str);

            // test
            Assert.Test(poly.Count == 2);
            Assert.Test(poly[0][0].X == 1.0);
            Assert.Test(poly[0][0].Y == 2.0);
            Assert.Test(poly[0][1].X == 3.0);
            Assert.Test(poly[0][1].Y == 4.0);

            PointD[] ps = new PointD[3];
            ps[0] = new PointD(1.0, 2.0);
            ps[1] = new PointD(3.0, 4.0);
            ps[2] = new PointD(5.0, 6.0);

            PointD[] ps1 = new PointD[3];
            ps1[0] = new PointD(1.1, 2.0);
            ps1[1] = new PointD(3.1, 4.0);
            ps1[2] = new PointD(5.1, 6.0);

            List<PointD[]> py = new List<PointD[]> { ps,ps1 };

            var s = Poly2Str(py);

            var poly1 = Str2Poly(s);

            Assert.Test(IsEqualPoly(py, poly1));
            
        }

        static void ImportProvinceLayer(string path)
        {
            using (var shapefile = new Shapefile(path))
            {
                foreach (var shape in shapefile)
                {
                    ShapePolygon polygon = shape as ShapePolygon;

                    Province province = new Province();
                    province.Name = shape.GetMetadata("name");
                    province.Country = shape.GetMetadata("country");
                    province.ChineseName = shape.GetMetadata("chinesename");
                    province.Shape = Poly2Str(polygon.Parts);
                    province.ID = db.Id(1);

                    db.Insert<Province>("Province", province);
                }
            }
        }

        static void ImportCountryLayer(string path)
        {
            using (var shapefile = new Shapefile(path))
            {
                foreach (var shape in shapefile)
                {
                    ShapePolygon polygon = shape as ShapePolygon;

                    var country = new Country();
                    country.Name = shape.GetMetadata("COUNTRY");
                    country.Shape = Poly2Str(polygon.Parts);
                    country.ChineseName = shape.GetMetadata("chinesename");
                    country.ID = db.Id(1);

                    db.Insert<Country>("Country",country);
                }
            }
        }

        static void ImportItemLayer(string path)
        {
            using (var file = new StreamReader(path))
            {
                while (true)
                {
                    var line = file.ReadLine();
                    if (line == null) break;
                    var token = line.Split(',');
                    db.Insert("Item", new Item
                    {
                        ID = db.Id(1),
                        Latitude = int.Parse(token[0]),
                        Longitude = int.Parse(token[1]),
                        Province = token[2],
                        Country = token[3],
                        CountryChineseName = token[4],
                        ProvinceChineseName = token[5],
                        IsTreasure = false
                    });
                }
            }
        }

        static string Poly2Str(List<PointD[]> parts)
        {
            List<string> pointstr = new List<string>();

            foreach (var part in parts)
            {
                string partstr = Part2Str(part);
                pointstr.Add(string.Join(";", partstr));
            }
            return string.Join(";", pointstr.ToArray());
        }

        static string Part2Str(PointD[] part)
        {
            List<Point> pl = new List<Point>();
            foreach (var point in part)
            {
                Point p;
                p.X = point.X;
                p.Y = point.Y;
                pl.Add(p);
            }

            return string.Format("({0})",string.Join(",", pl.ToArray()));
        }

        static Point Str2Point(string str)
        {
            Point point;
            string[] xy = str.Split(' ');
            point.X = double.Parse(xy[0]);
            point.Y = double.Parse(xy[1]);
            return point;
        }

        static Point[] Str2Part(string str)
        {
            List<Point> part = new List<Point>();
            string[] points = str.Substring(1, str.Length - 2).Split(',');

            foreach (var p in points)
            {
                part.Add(Str2Point(p));
            }

            return part.ToArray();
        }

        static List<Point[]> Str2Poly(string str)
        {
            List<Point[]> result = new List<Point[]>();
            string[] parts = str.Split(';');
            foreach (var part in parts)
            {
                result.Add(Str2Part(part));
            }
            return result;
        }

        static bool IsEqual(PointD pd, Point p)
        {
            return pd.X == p.X && pd.Y == p.Y;
        }

        static bool IsNotEqual(PointD pd, Point p)
        {
            return !IsEqual(pd, p);
        }

        static bool IsEqualPart(PointD[] pds, Point[] p)
        {
            if (pds.Length != p.Length) return false;
            for (var i = 0; i < pds.Length; i++)
            {
                if(IsNotEqual(pds[i],p[i])){
                    return false;
                }
            }

            return true;
        }

        static bool IsEqualPoly(List<PointD[]> polyd, List<Point[]> poly)
        {
            var p1 = polyd.ToArray();
            var p2 = poly.ToArray();

            if (p1.Length != p2.Length) return false;

            for (var i = 0; i < p1.Length; i++)
            {
                if (!IsEqualPart(p1[i],p2[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
