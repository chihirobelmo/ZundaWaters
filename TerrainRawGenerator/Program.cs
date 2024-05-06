using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace TerrainRawGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {

            // Open the TIFF files
            Bitmap img1 = new Bitmap("../../gebco_08_rev_bath_D1_grey_geo.tif"); // Bathymetry
            Bitmap img2 = new Bitmap("../../gebco_08_rev_elev_D1_grey_geo.tif"); // Topography

            // Create a byte array to hold the raw image data
            byte[] data = new byte[img1.Width * img1.Height * 2 /*16-bit depth*/];

            Enumerable.Range(0, img1.Width - 1).ToList().ForEach(x =>
            {
                Enumerable.Range(0, img1.Height - 1).ToList().ForEach(y =>
                {
                    // Get the pixel from each image
                    Color pixel1 = img1.GetPixel(x, y); // Bathymetry
                    Color pixel2 = img2.GetPixel(x, y); // Topography

                    // Calculate the depth value
                    int bathymetry = (int)(((255 - pixel1.R) / 255.0) * -8000); // Scale to -8000 to 0
                    int topography = (int)((pixel2.R / 255.0) * 6400); // Scale to 0 to 6400

                    // Combine bathymetry and topography
                    int depth = bathymetry + topography;

                    // Scale the depth value to a 16-bit value
                    ushort depth16 = (ushort)((depth + 8000) * 65535 / (8000 + 6400)); // Scale to 0 to 65535

                    // Calculate the index for this pixel
                    int i = (y * img1.Width + x) * 2;

                    // Add the depth value to the data array
                    data[i] = (byte)(depth16 & 0xFF); // Lower byte
                    data[i + 1] = (byte)(depth16 >> 8); // Upper byte

                    // Console.WriteLine("X:" + x + " Y:" + y + " Depth:" + depth);
                });
                Console.WriteLine("X:" + x);
            });

            // Write the raw image data to a file
            File.WriteAllBytes("../../output.raw", data);

            Console.WriteLine("Done");
        }
    }
}
