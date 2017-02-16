using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths
{
    public class PathEngine
    {
        private List<IPathProcess> _pathProcesses = new List<IPathProcess>();
        private List<object> _pathProcessFeatures = new List<object>();
        private int _numberOfPaths;


        public PathEngine(int numberOfPaths)
        {
            _numberOfPaths = numberOfPaths;
        }

        public void AddPathProcess(IPathProcess process)
        {
            _pathProcesses.Add(process);
        }



    }
}
