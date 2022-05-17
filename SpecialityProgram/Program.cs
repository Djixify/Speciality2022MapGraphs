// See https://aka.ms/new-console-template for more information
using SpecialityWebService.Network;
using System.Net;

namespace SpecialityProgram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TestLexer();
            //while (true)
                //GetWMS().Wait();
        }

        public static void TestLexer()
        {
            var testcases = new List<Tuple<string, double>>()
            {
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("9", 9),
                new Tuple<string, double>("2 + 2", 4),
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("0", 0),
                new Tuple<string, double>("0", 0)
            };

            foreach(Tuple<string, double> testcase in testcases)
            {
                double computed = Parser.ExecuteExpression(testcase.Item1).Value;
                bool match = Math.Abs(computed - testcase.Item2) < 0.00000001;
                System.Diagnostics.Debug.WriteLine($"Test: {testcase.Item1}, expected: {testcase.Item2}, result: {(match ? "Matched!" : "No match, got: " + Lexer.GetTokenExpression(testcase.Item1))}");
            }

            //Parser.ExecuteExpression()
        }

        public static async Task GetWMS()
        {
            
            await Task.Delay(1000);

            List<byte> image = new List<byte>();
            HttpClient client = new HttpClient();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://localhost:44342/Map/service=wms");

            using (HttpResponseMessage response = await client.GetAsync(@"https://localhost:44342/Map/service=wms"))
            using (BinaryWriter writer = new BinaryWriter(new FileStream(@"..\..\..\image.jpg", FileMode.OpenOrCreate)))
                writer.Write(await response.Content.ReadAsByteArrayAsync());
            /*
            using (HttpResponseMessage response = await client.GetAsync(@"https://localhost:44342/Map/service=wms"))
            using (BinaryReader reader = new BinaryReader(response.Content.ReadAsStream()))
            using (BinaryWriter writer = new BinaryWriter(new FileStream(@"..\..\..\image.jpg", FileMode.OpenOrCreate)))
            {
                byte[] bytes = null;
                do
                {
                    bytes = reader.ReadBytes(10 * 1024 * 1024); //10 MB
                    writer.Write(bytes);
                } while (bytes.Length > 0);
            }*/

            Console.ReadKey();
        }
    }
}
