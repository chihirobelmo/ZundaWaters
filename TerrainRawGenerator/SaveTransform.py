from rasterio.transform import from_origin

# Define the CRS and transform manually
crs = rasterio.crs.CRS.from_epsg(4326)  # WGS 84
transform = from_origin(-180, 90, 0.01, 0.01)  # 1 pixel = 0.01 degree

with rasterio.open('east_part_1_0.tif', 'w', driver='GTiff', height=height, width=width, count=1, dtype=rasterio.float32, crs=crs, transform=transform) as dst:
    dst.write(image, 1)  # Write the image data to the file