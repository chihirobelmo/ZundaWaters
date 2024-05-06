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