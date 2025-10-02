## Mesh Slicing Project
This is a Tool for Unity which can be used for slicing meshes along a plane. It is intended to be used for runtime effects such as those seen in "Metal Gear Rising: Revengeance". Sliced meshes are divided into 2 separate Meshes by classifying vertices, splitting divided triangles into 3 based on vertex classifications, and using constrained Delaunay Triangulation to fill the interior faces to maintain solid meshes. [https://en.wikipedia.org/wiki/Constrained_Delaunay_triangulation][Triangulation]

This project is still a WIP with the primary future changes including:
* Support for multithreading.
* Splitting of objects into multiple parts.
* More advanced API for object culling
* More options for creation of internal faces.
* Restructure of Project as a downloadable Unity Package.
