from PIL import Image
import numpy as np

# Increase the maximum image size
Image.MAX_IMAGE_PIXELS = None

# Open the TIFF files
img1 = Image.open('AZE.tif')  # Bathymetry
img2 = Image.open('AZEE.tif')  # Topography

# Convert images to numpy arrays
arr1 = np.array(img1)
arr2 = np.array(img2)

# Check if the images are grayscale or color and adjust accordingly
if arr1.ndim == 3:
    arr1 = arr1[:, :, 0]
if arr2.ndim == 3:
    arr2 = arr2[:, :, 0]

# Calculate the depth values
bathymetry = (((255 - arr1) / 255.0) * -8000).astype(int)  # Scale to -8000 to 0
topography = ((arr2 / 255.0) * 6400).astype(int)  # Scale to 0 to 6400

# Combine bathymetry and topography
depth = bathymetry + topography

# Scale the depth values to 16-bit
depth16 = ((depth + 8000) * 65535 // (8000 + 6400)).astype(np.uint16)  # Scale to 0 to 65535

# Write the raw image data to a file
depth16.tofile('BD.raw')

print("Done")