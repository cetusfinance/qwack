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
        public OutputPathsToImage(PathEngine engine)
        {
            var height = 1000;
            var width = engine.BlockSet.Steps * 100;
            var image = new Image<Argb32>(width, height);
            var rnd = new Random();
            
            var minMax = engine.BlockSet.Select(b => b.RawData.MinMax()).Aggregate((currentValues, next) => (System.Math.Min(currentValues.min, next.min), System.Math.Max(currentValues.max, next.max)));

            var range = minMax.max - minMax.min;
            var pixelsPerPoint = height / range;

            foreach (var block in engine.BlockSet)
            {
                for (var p = 0; p < block.NumberOfPaths; p++)
                {
                    var points = new Vector2[block.Factors][];
                    for(var i = 0; i < block.Factors;i++)
                    {
                        points[i] = new Vector2[block.NumberOfSteps];
                    }
                    for (var s = 0; s < block.NumberOfSteps; s++)
                    {
                        for (var f = 0; f < block.Factors; f++)
                        {
                            var nextX = s * 100;
                            var nextY = (int)((block[block.GetDoubleIndex(p, f, s)] - minMax.min) * pixelsPerPoint);
                            points[f][s] = new Vector2(nextX, nextY);
                        }
                    }
                    for (var f = 0; f < block.Factors; f++)
                    {
                        var bytes = new byte[2];
                        rnd.NextBytes(bytes);

                        var pen = new ImageSharp.Drawing.Pens.Pen<Argb32>(new Argb32(bytes[0], bytes[1], (byte)((255/block.Factors) * f) ), 2.0f);
                        image.DrawLines(pen, points[f]);
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
