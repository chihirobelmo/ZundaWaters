import rasterio
from rasterio.crs import CRS

with rasterio.open('east_part_1_1.tif', 'r+') as src:
    src.crs = CRS.from_epsg(4326)