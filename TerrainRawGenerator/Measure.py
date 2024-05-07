import rasterio

# Open the file
with rasterio.open('AZE.tif') as src:
    # Get the spatial resolution
    transform = src.transform
    x_res = transform[0]
    y_res = -transform[4]

print(f"Each pixel represents {x_res} meters in x direction and {y_res} meters in y direction.")