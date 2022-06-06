using SpecialityWebService;
using SpecialityWebService.Generation;
using SpecialityWebService.Services;
using static SpecialityWebService.MathObjects;

namespace SpecialityUnittests
{
    public class QueryFixture : IDisposable
    {
        public IGMLReader GeoDanmark60, Vejhastigheder;
        public int GeoDanmarkNPoints = 0, VejhastighederNPoints = 0;
        public QueryFixture()
        {
            GeoDanmark60 = new GeoDanmark60_GML(@"C:\Users\chaos\Desktop\Speciality2022MapGraphs\SpecialityWebService\Resources\Vectordata\dfvejedata.gml");
            Vejhastigheder = new Vejmanhastigheder_GML(@"C:\Users\chaos\Desktop\Speciality2022MapGraphs\SpecialityWebService\Resources\Vectordata\vmvejedatalarge.gml");
            GeoDanmarkNPoints = GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()).Aggregate(0, (acc, path) => acc + path.Points.Count);
            VejhastighederNPoints = Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()).Aggregate(0, (acc, path) => acc + path.Points.Count);
        }

        public void Dispose()
        {

        }
    }

    public class QueryStructureTests : IClassFixture<QueryFixture>
    {
        QueryFixture fixture;
        public QueryStructureTests(QueryFixture gml)
        {
            fixture = gml;
        }

        [Fact]
        public void TestQGISGeoDanmarkRtreeStandard()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new Rtree<int>()
            };
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestQGISGeoDanmarkQuadtree()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.GeoDanmark60.GetBoundaryBox(), 2.0)
        };
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestQGISGeoDanmarkQuadtreeLogN()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.GeoDanmark60.GetBoundaryBox(), (int)Math.Log2(fixture.GeoDanmarkNPoints))
            };
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestQGISGeoDanmarkRedBlackRangeTree()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new RedBlackRangeTree2D<int>()
            };
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedGeoDanmarkRtree()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new Rtree<int>()
            };
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedGeoDanmarkQuadtree()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.GeoDanmark60.GetBoundaryBox(), 2.0)
};
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedGeoDanmarkQuadtreeLogN()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.GeoDanmark60.GetBoundaryBox(), (int)Math.Log2(fixture.GeoDanmarkNPoints))
            };
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedGeoDanmarkRedBlackRangeTreeStandard()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new RedBlackRangeTree2D<int>()
            };
            qgis.Generate(fixture.GeoDanmark60.GetPathEnumerator(Rectangle.Infinite()), 0.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestQGISVejhastighederRtreeStandard()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new Rtree<int>()
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestQGISVejhastighederQuadtree()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.Vejhastigheder.GetBoundaryBox(), 2.0)
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestQGISVejhastighederQuadtreeLogN()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.Vejhastigheder.GetBoundaryBox(), (int)Math.Log2(fixture.VejhastighederNPoints))
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestQGISVejhastighederRedBlackRangeTree()
        {
            INetworkGenerator qgis = new QGISReferenceAlgorithm()
            {
                GenerationQueryStructure = new RedBlackRangeTree2D<int>()
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedVejhastighederRtree()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new Rtree<int>()
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedVejhastighederQuadtree()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.Vejhastigheder.GetBoundaryBox(), 2.0)
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedVejhastighederQuadtreeLogN()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new QuadTree<int>(fixture.Vejhastigheder.GetBoundaryBox(), (int)Math.Log2(fixture.VejhastighederNPoints))
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }

        [Fact]
        public void TestProposedVejhastighederRedBlackRangeTreeStandard()
        {
            INetworkGenerator qgis = new ProposedAlgorithm()
            {
                GenerationQueryStructure = new RedBlackRangeTree2D<int>()
            };
            qgis.Generate(fixture.Vejhastigheder.GetPathEnumerator(Rectangle.Infinite()), 2.5, new List<KeyValuePair<string, string>>() { KeyValuePair.Create("distance", "distance") }).Wait();
        }
    }
}