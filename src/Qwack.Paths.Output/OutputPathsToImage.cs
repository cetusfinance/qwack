using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImageSharp;
using ImageSharp.PixelFormats;
using Qwack.Math.Extensions;

namespace Qwack.Paths.Output
{
    public class OutputPathsToImage
    {
        public OutputPathsToImage(PathEngine engine, int width, int height)
        {
            var image = new Image<Argb32>(width, height);
            var rnd = new Random();
            
            var minMax = engine.BlockSet.Select(b => b.RawData.MinMax()).Aggregate((currentValues, next) => (System.Math.Min(currentValues.min, next.min), System.Math.Max(currentValues.max, next.max)));

            var range = minMax.max - minMax.min;
            var pixelsPerPointY = height / range;
            var pixelsPerPointX = width / engine.BlockSet.Steps;

            foreach (var block in engine.BlockSet)
            {
                for (var factor = 0; factor < block.Factors; factor++)
                {
                    for(var path = 0; path < block.NumberOfPaths; path++)
                    {
                        var indexOfPath = block.GetIndexOfPathStart(path, factor);
                        var bytes = new byte[3];
                        rnd.NextBytes(bytes);
                        var pen = new ImageSharp.Drawing.Pens.Pen<Argb32>(new Argb32(bytes[0], bytes[1], bytes[2]), 2.0f);
                        var points = new Vector2[block.NumberOfSteps];
                        for(var step = 0; step < block.NumberOfSteps;step++)
                        {
                            var nextX = (float)(step * pixelsPerPointX);
                            var nextY = (float)((block[indexOfPath + step * Vector<double>.Count] - minMax.min) * pixelsPerPointY);
                            points[step] = new Vector2(nextX, nextY);
                        }
                        image.DrawLines(pen, points);
                    }                    
                }

            }
            using (var fs = System.IO.File.Create("C:\\code\\output.png"))
            {
                image.SaveAsPng(fs);
            }
        }
    }
}
