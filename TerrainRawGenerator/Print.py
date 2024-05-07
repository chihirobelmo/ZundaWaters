import rasterio

with rasterio.open('east_part_1_0.tif') as src:
    print(src.crs)  # Prints the CRS
    print(src.transform)  # Prints the transform