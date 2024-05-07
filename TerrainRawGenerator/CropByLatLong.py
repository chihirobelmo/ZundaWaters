import rasterio
from rasterio.windows import Window
from pyproj import Proj, transform as proj_transform

def crop_tif(input_file, output_file, center_lat, center_lon, length_nm):
    # Convert length from nautical miles to meters
    length_m = length_nm * 1852

    with rasterio.open(input_file) as src:
        # Convert center coordinates to the image's CRS
        in_proj = Proj(init='epsg:4326')  # WGS84
        out_proj = Proj(src.crs)
        center_x, center_y = proj_transform(in_proj, out_proj, center_lon, center_lat)

        # Calculate the row and column of the center point
        center_col, center_row = ~src.transform * (center_x, center_y)

        # Calculate the size of the window
        window_size = length_m / src.transform[0]  # assuming square pixels

        # Calculate the window
        window = Window(center_col - window_size / 2, center_row - window_size / 2, window_size, window_size)

        # Read the data in the window
        data = src.read(window=window)

        # Calculate the transform of the window
        transform = src.window_transform(window)

        # Write the data to a new file
        with rasterio.open(output_file, 'w', driver='GTiff', height=window.height, width=window.width, count=src.count, dtype=data.dtype, crs=src.crs, transform=transform) as dst:
            dst.write(data)

crop_tif('E90-180Elev.tif', 'Theater.tif', 26, 124, 863)