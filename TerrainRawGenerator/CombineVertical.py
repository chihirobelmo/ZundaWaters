import rasterio
from rasterio.merge import merge
from rasterio.plot import show
from rasterio.crs import CRS
from rasterio.transform import from_origin
from affine import Affine
from rasterio.warp import calculate_default_transform, reproject, Resampling
import glob
import os

# File and directory setup
input_dir = "./"  # adjust to your needs
output_file = "./merged.tif"

# File list
search_criteria = "gebco_08_rev_elev_D*_grey_geo.tif"
# search_criteria = "east_part_*_1.tif"
q = os.path.join(input_dir, search_criteria)
file_list = glob.glob(q)

# Sort the file list
# file_list.sort(key=lambda x: int(x.split('_')[2]))

# List for the data
src_files_to_mosaic = []

# Open raster files and add them to list
for i, fp in enumerate(file_list):
    src = rasterio.open(fp)
    if src.crs is None:
        # Define the CRS and transform manually
        dst_crs = CRS.from_epsg(4326)  # replace with the correct EPSG code
        transform = from_origin(0, 0, 1, 1)  # replace with the correct values

        # Calculate the ideal dimensions and transformation in the new crs
        dst_transform, width, height = calculate_default_transform(src.crs, dst_crs, src.width, src.height, *src.bounds)

        # Make sure the pixel size is positive
        dst_transform = Affine(dst_transform.a, dst_transform.b, dst_transform.c, dst_transform.d, abs(dst_transform.e), dst_transform.f)

        # Write to a new file
        new_file_name = f'new_file_{i}.tif'
        with rasterio.open(new_file_name, 'w', driver='GTiff', height=height, width=width, count=1, dtype=src.dtypes[0], crs=dst_crs, transform=dst_transform) as dst:
            reproject(
                source=rasterio.band(src, 1),
                destination=rasterio.band(dst, 1),
                src_transform=src.transform,
                src_crs=src.crs,
                dst_transform=dst_transform,
                dst_crs=dst_crs,
                resampling=Resampling.nearest)
        
        # Open the new file and add it to the list
        src = rasterio.open(new_file_name)
    print(f"CRS: {src.crs}")
    print(f"Transform: {src.transform}")
    print(f"Bounds: {src.bounds}")
    src_files_to_mosaic.append(src)

# Merge function returns a single mosaic array and the transformation info
mosaic, out_trans = merge(src_files_to_mosaic)

# Copy the metadata
out_meta = src.meta.copy()

# Update the metadata
out_meta.update({"driver": "GTiff",
                 "height": mosaic.shape[1],
                 "width": mosaic.shape[2],
                 "transform": out_trans,
                 "crs": src.crs
                 }
                )

# Write the mosaic raster to disk
with rasterio.open(output_file, "w", **out_meta) as dest:
    dest.write(mosaic)

# Close the datasets
for src in src_files_to_mosaic:
    src.close()

print("Mosaic created")