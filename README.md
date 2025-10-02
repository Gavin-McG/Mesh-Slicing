## Mesh Slicing Project
This is a Tool for Unity which can be used for slicing meshes along a plane. It is intended to be used for runtime effects such as those seen in "Metal Gear Rising: Revengeance". Sliced meshes are divided into 2 separate Meshes by classifying vertices, splitting divided triangles into 3 based on vertex classifications, and using [Constrained Delaunay Triangulation](https://en.wikipedia.org/wiki/Constrained_Delaunay_triangulation) to fill the interior faces to maintain solid meshes. Newly created vertices when splitting triangles are assigned data by interpolating the data of the vertices of that triangle.

The interior triangles of a mesh can optionally be assigned a different material by dedicating a specific submesh as the "Internal" faces. All existing materials and submeshes of an object are preserved.

This project is still a WIP with the primary future changes including:
* Support for multithreading.
* Splitting of objects into multiple parts.
* Support for animated armatures.
* More advanced API for object culling
* More options for creation of internal faces.
* Restructure of Project as a downloadable Unity Package.
