import numpy as np

# Open the raw file
with open('BD.raw', 'rb') as f:
    data = np.fromfile(f, dtype=np.uint16)

# Reshape the data into an image
img = data.reshape((14699, 16020))

# Calculate the coordinates for the crop
left = img.shape[1] // 2 - 4096 // 2
upper = img.shape[0] - 4096
right = left + 4096
lower = upper + 4096

# Crop the image
cropped_img = img[upper:lower, left:right]

# Save the cropped image data to a raw file
cropped_img.tofile('BD_cropped.raw')

print("Done")