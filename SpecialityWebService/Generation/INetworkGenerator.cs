using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Generation
{
    public enum Generator
    {
        QGIS = 1,
        Proposed = 2
    }

    public interface INetworkGenerator
    {
        public int TotalSteps { get; }
        public int CurrentStep { get; }
        public string StepInfo { get; }
        public int TotalPaths { get; }
        public int CurrentPath { get; }
        public bool IsGenerating { get; }
        public bool Done { get; }
        public long TimeElapsed { get; }
        public Task<(List<Vertex>, List<Edge>)> Generate(IEnumerable<Path> paths, double tolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null);
        public Task<(List<Vertex>, List<Edge>)> Generate(IEnumerable<Path> paths, double endpointtolerance, double midpointtolerance, List<KeyValuePair<string, string>> weightcalculations, string directioncolumn = null, string forwardsdirection = null, string backwardsdirection = null);
        public void Cancel();
    }
}
