Each tif are 10800 * 10800 pixels

images from:

# Bathymetry

https://visibleearth.nasa.gov/images/73963/bathymetry

Bathymetry
Bathymetry is a digital image of the undersea land surface and water depth. Bathymetry is the underwater equivalent of land topography. In the maps provided here, shading indicates changes in slope or depth. Much of the data on ocean bathymetry come from "soundings." To collect a sounding, scientists use sonar devices to emit a sound wave that passes into the water. By measuring how long it takes the sound wave to bounce off the ocean floor and return to the sonar, scientists can estimate the depth of the water. Other characteristics of the returned sound wave can help reveal the shape and size of features on the sea floor.

These images represent ocean depths between -8000m and 0m (surface). Land regions are black in the colored versions. The full resolution of these images has been broken up into tiles as listed below. To understand the naming scheme please read about our global image grid.

Imagery by Jesse Allen, NASA's Earth Observatory, using data from the General Bathymetric Chart of the Oceans (GEBCO) produced by the British Oceanographic Data Centre.

Published July 21, 2005

# Topography

https://visibleearth.nasa.gov/images/73934/topography

Land topography is a digital image of the three-dimensional structure of the Earth's surface. Shading indicates changes in slope or elevation. The relief shading in this topographic map comes mostly from elevation data collected by space-based radars. A radar in space sends a pulse of radio waves toward the earth and measures the strength and length of time it takes a signal to bounce back. From this information, scientists can determine the height and shape of the features on the surface.

Topography not only gives a realistic picture of what the Earth's surface actually looks like, it also helps scientists determine things like how rivers and streams drain through the landscape, where lowlands are prone to flooding, how plate tectonics or erosion are building or wearing away mountains, where hills may be prone to landslides, or how a volcanic eruption changed the shape of a mountain. Topography is also one of the factors that influences where particular ecosystems exist. Therefore topography is one of the factors that scientists can use to predict where certain plants or animals, such as endangered species, might be found.

Data in these images were scaled 0-6400 meters. The full resolution of these images has been broken up into tiles as listed below. To understand the naming scheme please read about our global image grid.

Imagery by Jesse Allen, NASA's Earth Observatory, using data from the General Bathymetric Chart of the Oceans (GEBCO) produced by the British Oceanographic Data Centre.

Published July 21, 2005

# Blue Marble: Land Surface, Shallow Water, and Shaded Topography

https://visibleearth.nasa.gov/images/57752/blue-marble-land-surface-shallow-water-and-shaded-topography

This spectacular “blue marble” image is the most detailed true-color image of the entire Earth to date. Using a collection of satellite-based observations, scientists and visualizers stitched together months of observations of the land surface, oceans, sea ice, and clouds into a seamless, true-color mosaic of every square kilometer (.386 square mile) of our planet. These images are freely available to educators, scientists, museums, and the public. This record includes preview images and links to full resolution versions up to 21,600 pixels across.

Much of the information contained in this image came from a single remote-sensing device-NASA’s Moderate Resolution Imaging Spectroradiometer, or MODIS. Flying over 700 km above the Earth onboard the Terra satellite, MODIS provides an integrated tool for observing a variety of terrestrial, oceanic, and atmospheric features of the Earth. The land and coastal ocean portions of these images are based on surface observations collected from June through September 2001 and combined, or composited, every eight days to compensate for clouds that might block the sensor’s view of the surface on any single day. Two different types of ocean data were used in these images: shallow water true color data, and global ocean color (or chlorophyll) data. Topographic shading is based on the GTOPO 30 elevation dataset compiled by the U.S. Geological Survey’s EROS Data Center. MODIS observations of polar sea ice were combined with observations of Antarctica made by the National Oceanic and Atmospheric Administration’s AVHRR sensor—the Advanced Very High Resolution Radiometer. The cloud image is a composite of two days of imagery collected in visible light wavelengths and a third day of thermal infra-red imagery over the poles. Global city lights, derived from 9 months of observations from the Defense Meteorological Satellite Program, are superimposed on a darkened land surface map.

NASA Goddard Space Flight Center Image by Reto Stöckli (land surface, shallow water, clouds). Enhancements by Robert Simmon (ocean color, compositing, 3D globes, animation). Data and technical support: MODIS Land Group; MODIS Science Data Support Team; MODIS Atmosphere Group; MODIS Ocean Group Additional data: USGS EROS Data Center (topography); USGS Terrestrial Remote Sensing Flagstaff Field Center (Antarctica); Defense Meteorological Satellite Program (city lights).

# Global Image Grid

https://visibleearth.nasa.gov/grid

Some of our higher resolution global imagery are provided as individual tiles to facilitate easier downloading and manipulation. These large image tiles have column and row indicators as part of their file name (e.g., “A1”, “B2”, etc.) and are tiled according to the grid and table listed below.

| column/row | Upper left | Lower right | 
|:-|:-|:-|
| A1 | 90N 180W | 0N 90W | 
| B1 | 90N 90W | 0N 0W | 
| C1 | 90N 0W | 0N 90E | 
| D1 | 90N 90E | 0N 180E | 
| A2 | 0N 180W | 90S 90W | 
| B2 | 0N 90W | 90S 0W | 
| C2 | 0N 0W | 90S 90E | 
| D2 | 0N 90E | 90S 180E | 

# Image Use Policy

Most images published in Visible Earth are freely available for re-publication or re-use, including commercial purposes, except for where copyright is indicated. In those cases you must obtain the copyright holder’s permission; we usually provide links to the organization that holds the copyright.

We ask that you use the credit statement attached with each image or else credit Visible Earth; the only mandatory credit is NASA.

For more information about using NASA imagery visit https://www.nasa.gov/multimedia/guidelines/index.html.

# Disclaimer from GEBCO

https://www.gebco.net/disclaimer/

All information provided by GEBCO on its web pages is made available to provide immediate access for the convenience of interested persons. Whilst GEBCO believes the information to be reliable, human or mechanical error remains a possibility. Therefore, GEBCO does not guarantee the accuracy, completeness, timeliness or correct sequencing of the information. Neither GEBCO nor any of the sources of the information shall be responsible for any errors or omissions, or for the use of, or results obtained from, the use of this information.

GEBCO is essentially a deep ocean product and does not include detailed bathymetry for shallow shelf waters. Even to the present day, most areas of the world’s oceans have not been fully surveyed and, for the most part, bathymetric mapping is an interpretation based on random tracklines of data from many different sources. The quality and coverage of data from these sources is highly variable. Although the GEBCO grid is presented at one minute intervals of latitude and longitude, this does not imply that knowledge is available on sea floor depth at this resolution - the depth in most one minute squares of the world’s oceans has yet to be measured.

GEBCO's data sets are not to be used for navigation or for any purpose relating to safety at sea.

# Enviromental Setup

install python3 then
```
pip install pyproj
pip install rasterio
pip install pillow
```