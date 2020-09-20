# STL-file-healing-for-ship-hulls
C# implementation of algorithms for removing the ceilings of a ship compartment in order to look into the ship for 3d printing.  
It also implements an algorithm described by P.Guigue - O. Devillers for performing three-dimensional triangle-triangle intersection test  
using predicate in order to remove overlapping surfaces and make the compartment easy to 3dprint  
Several predicates are defined. Their parameters are all points of the triangle. 
Each point is an array double precision floating point numbers  
each function for detecting overlapping surfaces returns true if the triangles (including their boundary) intersect, otherwise false


//TODO:
In line 45, insert the folder where you want to write your stl file into
In line 53, paste the folder you want to read all your stl files from

//Output
The treated stl files stored in the output folder and named from 1 to the equivalent total number of stl files read 
in same folder, there is a file named "combined.stl" which is the main output. This is equivalent to joining all single treated stl files together
 
 
Similarly, there are two other files named t1 and t2, which are not that important and can be prevented from writing to the folder by commenting out 
line 901,902, 967, 968, 1024 and 1025.

t1 and t2 in this case represents two stl files where all triangles are with same value of vertices in same coordinates, it is just to show a representation
