import rasterio
from rasterio.windows import Window
from rasterio.transform import from_bounds
from rasterio.crs import CRS

def split_into_parts(input_file, output_pattern):
    with rasterio.open(input_file) as src:
        width, height = src.meta['width'], src.meta['height']

        # Calculate the size of each part
        part_width = width // 2
        part_height = height // 2

        # Define the geographical coordinates of the entire image
        left, bottom, right, top = 0, -90, 180, 90

        # Calculate the geographical size of each part
        part_lon = (right - left) / width  # use width instead of part_width
        part_lat = (top - bottom) / 2

        for i in range(2):
            for j in range(2):
                # Define the window at the top-left corner of the image
                window = Window(j * part_width, i * part_height, part_width, part_height)
                data = src.read(window=window)  # read all bands

                # Calculate the geographical coordinates of the part
                part_left = left + 90 * j  # start from left + 90 when j is 1
                part_top = top - i * part_lat
                part_right = part_left + 90  # add 90 to part_left
                part_bottom = part_top - part_lat
                
                # Calculate the transform of the part
                transform = from_bounds(part_left, part_bottom, part_right, part_top, part_width, part_height)

                output_file = output_pattern.format(i=i, j=j)
                with rasterio.open(output_file, 'w', driver='GTiff', height=part_height, width=part_width, count=src.count, dtype=data.dtype, crs=CRS.from_epsg(4326), transform=transform) as dst:
                    dst.write(data)  # write all bands

# Split the west and east files
# split_into_parts('land_shallow_topo_west.tif', 'west_part_{i}_{j}.tif')
split_into_parts('land_shallow_topo_east.tif', 'east_part_{i}_{j}.tif')