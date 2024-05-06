import rasterio
from rasterio.warp import calculate_default_transform, reproject, Resampling
from pyproj import CRS, Transformer

# Define the source and target coordinate systems
# or the specific area you're interested in (N0-90, E90-180), you might want to center your projection around the center of this area for the least distortion. The center of this area is at 45N, 135E. So, you would define your projection as follows:
#    +proj=aeqd +lat_0=45 +lon_0=135 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs
dst_crs = CRS.from_string("+proj=aeqd +lat_0=45 +lon_0=135 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs")

# Create a transformer object
transformer = Transformer.from_crs("EPSG:4326", dst_crs, always_xy=True)

# Open the source dataset
with rasterio.open('gebco_08_rev_elev_D1_grey_geo.tif') as src:
    transform, width, height = calculate_default_transform(src.crs, dst_crs, src.width, src.height, *src.bounds)
    kwargs = src.meta.copy()
    kwargs.update({
        'crs': dst_crs,
        'transform': transform,
        'width': width,
        'height': height
    })

    # Create the destination dataset
    with rasterio.open('AZEE.tif', 'w', **kwargs) as dst:
        for i in range(1, src.count + 1):
            reproject(
                source=rasterio.band(src, i),
                destination=rasterio.band(dst, i),
                src_transform=src.transform,
                src_crs=src.crs,
                dst_transform=transform,
                dst_crs=dst_crs,
                resampling=Resampling.bilinear)

"""
Q: 
I input 10800 * 10800 images.
After this AzEquidistant.py I get 16020*14699 image, how meter each pixel?

A:
The Azimuthal Equidistant projection preserves distances from the center point of the projection. In your case, the center point is defined as +lat_0=45 +lon_0=135.

The units of the projection are defined as meters (+units=m). This means that the pixel size in meters is the same as the pixel size in the coordinate system of the projection.

To calculate the size of each pixel in meters, you can use the width of the map in meters divided by the width of the map in pixels.

The width of the map in meters can be calculated using the formula for the circumference of a circle, as the Azimuthal Equidistant projection can represent the entire globe as a circle with the radius equal to the distance from the pole to the equator.

The formula for the circumference of a circle is 2 * pi * radius. The radius of the Earth is approximately 6,371 kilometers, and the distance from the pole to the equator is half the circumference of the Earth, so the radius for the projection is 2 * pi * 6371 / 2 = 20015.08 kilometers, or 20015080 meters.

So, if the width of your image is 16020 pixels, the size of each pixel in meters would be 20015080 / 16020 = 1249.06 meters.

Please note that this is an approximation, as the actual size of each pixel can vary depending on the specific parameters of the projection and the area of the map that the image represents.
"""