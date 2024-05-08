using System.Drawing;
using DotSpatial.Projections;

public class Program
{
    public static void Main()
    {
        // Load the TIFF file
        Bitmap bitmap = new Bitmap("path_to_your_tiff_file.tif");

        // Define the projection info for the TIFF file and WGS84
        ProjectionInfo src = KnownCoordinateSystems.Geographic.World.WGS1984;
        ProjectionInfo dest = KnownCoordinateSystems.Projected.World.WebMercator;

        // Define the x, y pixel positions
        int x = 100;
        int y = 200;

        // Convert pixel positions to map coordinates
        double mapX = x * pixelWidth + xOrigin;
        double mapY = yOrigin - y * pixelHeight;

        // Convert map coordinates to latitude and longitude
        double[] xy = new double[] { mapX, mapY };
        double[] z = new double[] { 0 };
        Reproject.ReprojectPoints(xy, z, src, dest, 0, 1);

        // Print the latitude and longitude
        Console.WriteLine("Latitude: " + xy[1]);
        Console.WriteLine("Longitude: " + xy[0]);
    }
}