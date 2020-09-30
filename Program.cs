
/*
 * NOTE: Program only understands ASCII stl files!!
 * 
 * This program was writen in the framework of a project during a master course
 * Course: IT in Ship Design
 * University of Rostock
 * 
 * Presented to: Prof. Dr.-Ing. Robert Bronsart, Dipl.-Ing. Hannes Lindner.
 * 
 * Presented by: Chisom Bernard Umunnakwe
 * February, 2020 
 * 
 * This file contains C# implementation of algorithms for removing the ceilings of a ship compartment
 * in order to look into the ship for 3d printing. 
 * It also implements an algorithm described by P.Guigue - O. Devillers
 * for performing three-dimensional triangle-triangle intersection test using predicate 
 * in order to remove overlapping surfaces and make the compartment easy to 3dprint
 * 
 * Several predicates are defined.  Their parameters are all      
 * points of the triangle.  Each point is an array double precision floating point numbers
 * 
 * each function for detecting overlapping surfaces returns true if the triangles
 * (including their boundary) intersect, otherwise false        
 * 
 * The algorithms and underlying theory are described in  "Fast and Robust Triangle-Triangle Overlap Test 
 * Using Orientation Predicates"  P. Guigue - O. Devillers
 * 
 * available at https://hal.inria.fr/inria-00072100/document
 */


using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ITProjectShipCompartmentation
{
    class Program
    {
        //Define a string variable dirpath for location of output files
        //this is defined here in order to increase its scope
        static string dirpath = @"C:\Users\user2\Desktop\Output4\";

        //the program begins here
        #region  Main method starts here
        static void Main(string[] args)
        {
            //An array of string called dirs, 
            //each of the files gotten from the folder is stored in this array with index begining from 0 
            string[] dirs = Directory.GetFiles(@"C:\Users\user2\Desktop\STL-MainDeck2", "*.*");


            //assign the null reference to the variable triangle of type Triangle just to tell that the value is not set yet
            Triangle triangle = null; 


            //Creates an object of the Triangle class with a list of list of Triangle and assigns to variable triangleList
            List<List<Triangle>> triangleList = new List<List<Triangle>>();
            

            //read through each file in the directory
            foreach (string file in dirs)
            {
                double max = 0; //max acts as a check for each rooms in order to ease the removal of the top most ceilings

                //new list of type Triangle for treating each file read and store in fileTriangleList
                List<Triangle> fileTriangleList = new List<Triangle>();

                
                // StreamReader class for reading the files in the directory
                StreamReader sr = new StreamReader(file); 
                while (!sr.EndOfStream)
                {
                    //text is an array of string for each line being read, trimmed to remove unnecessary whitespaces and split by ' '
                    string[] text = sr.ReadLine().Trim().Split(' '); 


                    //check if the line begins with facet, if yes,
                    //assign to the Normal field of the Triangle class to their respective coordinates X,Y,Z from the Vertex fields
                    if (text[0].ToLower() == "facet")
                    {
                        triangle = new Triangle();
                        triangle.Normal = new Coordinate();
                        triangle.Normal.X = Convert.ToDouble(text[2]);
                        triangle.Normal.Y = Convert.ToDouble(text[3]);
                        triangle.Normal.Z = Convert.ToDouble(text[4]);
                    }


                    //check if the line begins with vertex, if yes,
                    //Create an object of class Coordinate say objCoordinate and access all the X,Y,Z filds 
                    //assign the vertices to their respective coordinates and add to vertices 
                    if (text[0].ToLower() == "vertex")
                    {
                        Coordinate objCoordinate = new Coordinate();
                        objCoordinate.X = Convert.ToDouble(text[1]);
                        objCoordinate.Y = Convert.ToDouble(text[2]);
                        objCoordinate.Z = Convert.ToDouble(text[3]);
                        triangle.Vertices.Add(objCoordinate);
                    }



                    //check if the line begins with outer, if yes,
                    //instantiate a list of of type Coordinate and assign to vertices field of the triangle, 
                    //this meeans that a new list of vertices is to begin in the next line
                    if (text[0].ToLower() == "outer")
                    {
                        triangle.Vertices = new List<Coordinate>();
                    }



                    //check if the line begins endfacet, if yes,
                    // add to fileTriangleList, this means that the first triangle is complete, it will have its norma and vertices too
                    //and the fileTriangleList will have a count of 1, this loops throughout till all files are read line by line
                    if (text[0].ToLower() == "endfacet")
                    {
                        fileTriangleList.Add(triangle);
                        //checks if the normal in the Z coordinate is 1, 
                        //and checks if the first vertex in the Z coordinate is greater than max which is zero now 
                        if (triangle.IsZ(false) && triangle.Vertices[0].Z > max)
                        {
                            //assign the z coordinate of vertices to max
                            max = triangle.Vertices[0].Z; 
                        }
                    }

                }



                //New list of type Triangle named listRemove to take care of top ceilings
                List<Triangle> listRemove = new List<Triangle>();
                foreach (Triangle objTriangle in fileTriangleList)
                {
                    //if the condition is satisfied, add corresponding triangles to the listRemove
                    if (objTriangle.IsZ(false) && objTriangle.Vertices[0].Z == max)
                    {
                        listRemove.Add(objTriangle);
                    }
                }
                //for all triangles in the new list listRemove, delete this triangles from the fileTriangleList
                foreach (Triangle objTriangle in listRemove)
                {
                    fileTriangleList.Remove(objTriangle);
                }
                //after removal, add the new fileTriangleList to the main triangleList
                triangleList.Add(fileTriangleList);
            }






            // Checking for overlaps
            for (int i = 0; i < triangleList.Count - 1; i++) //Checks the first Stl file
            {
                for (int j = i + 1; j < triangleList.Count; j++) //Checks the next stl file until all stl files have be checked
                {
                    //calls the overlap method to compare if both files have overlaping surfaces
                    Overlap(triangleList[i], triangleList[j]);
                }
            }




            
            // Joining files together
            //creates new object of the triangle and assigns to a list of Triangle named netList
            List<Triangle> netList = new List<Triangle>();
            for (int i = 0; i < triangleList.Count; i++) //Accesses all the 24 STL file triangleLists for the joining
            { 
                for (int j = 0; j < triangleList[i].Count; j++)
                {
                    netList.Add(triangleList[i][j]); //combines all the 24 STL files
                }
                // Writes a new Corrected STL file for each 24 files and names each file in ascending order starting from 1.stl
                StlWriter(dirpath + Convert.ToString(i + 1) + ".stl", triangleList[i]);
            }
            //writes a new file named combined.stl when all treated individual compartment have been combined
            StlWriter(dirpath + "combined.stl", netList);
        }
        #endregion main method
        //Main method ends here







        //..............................................................................................//
        //Method for writing STL to files
        //A method that accepts the filename as string and the list of type Triangle
        #region Stlwriter
        public static void StlWriter(string filename, List<Triangle> objTriangleList)
        {
            StreamWriter writer = new StreamWriter(filename);
           //giving the format of the stlfile which starts with the string solid
            writer.WriteLine("solid"); 
            int count = 0;
            foreach (Triangle objTriangle in objTriangleList)
            {
                count++;
                //implements the TriangleStlLine method which writes 
                //all the intermediate format of the stl file (in betwewn solid and endsolid)
                List<string> lines = objTriangle.TriangleStlLine();
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }
            //ends the file with the line endsolid
            writer.WriteLine("endsolid");
            writer.Close();
        }

        #endregion StlWriter








        //********* For X Coordinate

        // New functions begins
        #region All the predicates or boolean functions for X coordinate vertex, edge, and triangle overlap test starts here
        /// <summary>
        /// Predicate/Boolean function to test for vertices intesection in the x direction between two triangles
        /// </summary>
        /// <param name="t1"> first triangle</param>
        /// <param name="t2"> second triangle</param>
        /// <returns>true if vertices of two triangles intersects along X coordinate</returns>
        public static bool IntersectionVertexX(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[1]).OrientX() > 0)
                if (new Triangle(t2.Vertices[2], t2.Vertices[1], t1.Vertices[1]).OrientX() < 0)
                    if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[1]).OrientX() > 0)
                    {
                        if (new Triangle(t1.Vertices[0], t2.Vertices[1], t1.Vertices[1]).OrientX() < 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientX() > 0)
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[0]).OrientX() > 0)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        else
                        {
                            return false;
                        }
                    }
                else
                {
                    if (new Triangle(t1.Vertices[0], t2.Vertices[1], t1.Vertices[1]).OrientX() < 0)
                        if (new Triangle(t2.Vertices[2], t2.Vertices[1], t1.Vertices[2]).OrientX() < 0)
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[1]).OrientX() > 0)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        else
                        {
                            return false;
                        }
                    else
                    {
                        return false;
                    }
                }
            else
            {
                if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[2]).OrientX() > 0)
                    if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[2]).OrientX() > 0)
                    {
                        if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientX() > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[1]).OrientX() > 0)
                        {
                            if (new Triangle(t2.Vertices[2], t1.Vertices[2], t2.Vertices[1]).OrientX() > 0)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                else
                {
                    return false;
                }
            }

        }







        /// <summary>
        /// This predicate / boolean method tests intersection edges along X coordinate between two triangles
        /// </summary>
        /// <param name="t1">first triangle</param>
        /// <param name="t2">second triangle</param>
        /// <returns>returns true if edges intersects along X</returns>

        public static bool IntersectionEdgeX(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[1]).OrientX() > 0)
            {
                if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[1]).OrientX() > 0)
                {
                    if (new Triangle(t1.Vertices[0], t1.Vertices[1], t2.Vertices[2]).OrientX() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[0]).OrientX() > 0)
                    {
                        if (new Triangle(t1.Vertices[2], t1.Vertices[0], t2.Vertices[0]).OrientX() > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[2]).OrientX() > 0)
                {
                    if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientX() > 0)
                    {
                        if (new Triangle(t1.Vertices[0], t1.Vertices[2], t2.Vertices[2]).OrientX() > 0)
                        {
                            return true;
                        }
                        else
                        {
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[2]).OrientX() > 0)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }






        /// <summary>
        /// This predicates/Boolean method checks for triangle intersection along X Coordinate
        /// Each condition takes into account all together the vertices of the triangle and their edges 
        /// </summary>
        /// <param name="t1">First triangle</param>
        /// <param name="t2">Second triangle</param>
        /// <returns>true if triangles intersects along X</returns>

        public static bool IntersectionTriX(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[0], t2.Vertices[1], t1.Vertices[0]).OrientX() > 0)
            {
                if (new Triangle(t2.Vertices[1], t2.Vertices[2], t1.Vertices[0]).OrientX() > 0)
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientX() > 0)
                        return true;
                    else
                        return IntersectionEdgeX(t1, t2);
                }
                else
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientX() > 0)
                        return IntersectionEdgeX(t1, new Triangle(t2.Vertices[2], t2.Vertices[0], t2.Vertices[1]));
                    else
                        return IntersectionVertexX(t1, t2);
                }
            }
            else
            {
                if (new Triangle(t2.Vertices[1], t2.Vertices[2], t1.Vertices[0]).OrientX() > 0)
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientX() > 0)
                        return IntersectionEdgeX(t1, new Triangle(t2.Vertices[1], t2.Vertices[2], t2.Vertices[0]));
                    else
                        return IntersectionVertexX(t1, new Triangle(t2.Vertices[1], t2.Vertices[2], t2.Vertices[0]));
                }
                else
                    return IntersectionVertexX(t1, new Triangle(t2.Vertices[2], t2.Vertices[0], t2.Vertices[1]));
            }
        }








        /// <summary>
        /// predicate/boolean method to check for ovelapping surfaces in X coordinate 
        /// This now takes into consideration triangles and checks for overlapping along X
        /// </summary>
        /// <param name="t1"> first triangle argument of type Triangle</param>
        /// <param name="t2">Second triangle argument of type Triangle</param>
        /// <returns>true if the closed triangles(i.e.the triangles including their boundary) intersect triangles overlap in X</returns>
        public static bool OverlapTriX(Triangle t1, Triangle t2)
        {
            
            if (new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]).OrientX() < 0)
                if (new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]).OrientX() < 0)
                    return IntersectionTriX(new Triangle(t1.Vertices[0], t1.Vertices[2], t1.Vertices[1]), new Triangle(t2.Vertices[0], t2.Vertices[2], t2.Vertices[1]));
                else
                    return IntersectionTriX(new Triangle(t1.Vertices[0], t1.Vertices[2], t1.Vertices[1]), new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]));
            else
            {
                if (new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]).OrientX() < 0)
                    return IntersectionTriX(new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]), new Triangle(t2.Vertices[0], t2.Vertices[2], t2.Vertices[1]));
                else
                    return IntersectionTriX(new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]), new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]));
            }
        }

        #endregion Prediates for X coordinate intersection/overlap test ends here







        // ********For Y Coordinate

        // New functions begins
        #region All the predicates for Y coordinate vertex, edge, and triangle overlap tests start here
        /// <summary>
        /// Predicate/Boolean function to test for vertices intesection in the Y direction between two triangles
        /// </summary>
        /// <param name="t1"> first triangle</param>
        /// <param name="t2"> second triangle</param>
        /// <returns>true if vertices of two triangles intersects along Y coordinate</returns>

        public static bool IntersectionVertexY(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[1]).OrientY() > 0)
                if (new Triangle(t2.Vertices[2], t2.Vertices[1], t1.Vertices[1]).OrientY() < 0)
                    if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[1]).OrientY() > 0)
                    {
                        if (new Triangle(t1.Vertices[0], t2.Vertices[1], t1.Vertices[1]).OrientY() < 0)
                            return true;
                        else
                            return false;
                    }
                    else
                    {
                        if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientY() > 0)
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[0]).OrientY() > 0)
                                return true;
                            else
                                return false;
                        else
                            return false;
                    }
                else
                {
                    if (new Triangle(t1.Vertices[0], t2.Vertices[1], t1.Vertices[1]).OrientY() < 0)
                        if (new Triangle(t2.Vertices[2], t2.Vertices[1], t1.Vertices[2]).OrientY() < 0)
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[1]).OrientY() > 0)
                                return true;
                            else
                                return false;
                        else
                            return false;
                    else
                        return false;
                }
            else
            {
                if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[2]).OrientY() > 0)
                    if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[2]).OrientY() > 0)
                        if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientY() > 0)
                            return true;
                        else
                            return false;
                    else
                        if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[1]).OrientY() > 0)
                        if (new Triangle(t2.Vertices[2], t1.Vertices[2], t2.Vertices[1]).OrientY() > 0)
                            return true;
                        else
                            return false;
                    else
                        return false;
                else
                    return false;
            }

        }








        /// <summary>
        /// This predicate / boolean method tests intersection edges along Y coordinate between two triangles
        /// </summary>
        /// <param name="t1">first triangle</param>
        /// <param name="t2">second triangle</param>
        /// <returns>returns true if edges intersects along Y</returns>
        public static bool IntersectionEdgeY(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[1]).OrientY() > 0)
            {
                if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[1]).OrientY() > 0)
                {
                    if (new Triangle(t1.Vertices[0], t1.Vertices[1], t2.Vertices[2]).OrientY() > 0)
                        return true;
                    else
                        return false;
                }
                else
                {
                    if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[0]).OrientY() > 0)
                    {
                        if (new Triangle(t1.Vertices[2], t1.Vertices[0], t2.Vertices[0]).OrientY() > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[2]).OrientY() > 0)
                {
                    if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientY() > 0)
                    {
                        if (new Triangle(t1.Vertices[0], t1.Vertices[2], t2.Vertices[2]).OrientY() > 0)
                            return true;
                        else
                        {
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[2]).OrientY() > 0)
                                return true;
                            else
                                return false;
                        }
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
        }








        /// <summary>
        /// This predicates/Boolean method checks for triangle intersection along Y Coordinate
        /// Each condition takes into account all together the vertices of the triangle and their edges 
        /// </summary>
        /// <param name="t1">First triangle</param>
        /// <param name="t2">Second triangle</param>
        /// <returns>true if triangles intersects along Y</returns>
        public static bool IntersectionTriY(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[0], t2.Vertices[1], t1.Vertices[0]).OrientY() > 0)
            {
                if (new Triangle(t2.Vertices[1], t2.Vertices[2], t1.Vertices[0]).OrientY() > 0)
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientY() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return IntersectionEdgeY(t1, t2);
                    }
                }
                else
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientY() > 0)
                    {
                        return IntersectionEdgeY(t1, new Triangle(t2.Vertices[2], t2.Vertices[0], t2.Vertices[1]));
                    }
                    else
                    {
                        return IntersectionVertexY(t1, t2);
                    }
                }
            }
            else
            {
                if (new Triangle(t2.Vertices[1], t2.Vertices[2], t1.Vertices[0]).OrientY() > 0)
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientY() > 0)
                    {
                        return IntersectionEdgeY(t1, new Triangle(t2.Vertices[1], t2.Vertices[2], t2.Vertices[0]));
                    }
                    else
                    {
                        return IntersectionVertexY(t1, new Triangle(t2.Vertices[1], t2.Vertices[2], t2.Vertices[0]));
                    }
                }
                else
                {
                    return IntersectionVertexY(t1, new Triangle(t2.Vertices[2], t2.Vertices[0], t2.Vertices[1]));
                }
            }
        }







        /// <summary>
        /// predicate/boolean method to check for ovelapping surfaces in Y coordinate 
        /// This now takes into consideration triangles and checks for overlapping along Y
        /// </summary>
        /// <param name="t1"> first triangle argument of type Triangle</param>
        /// <param name="t2">Second triangle argument of type Triangle</param>
        /// <returns>true if the closed triangles(i.e.the triangles including their boundary) intersect triangles overlap in Y</returns>
        public static bool OverlapTriY(Triangle t1, Triangle t2)
        {
            if (new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]).OrientY() < 0)
                if (new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]).OrientY() < 0)
                {
                    return IntersectionTriY(new Triangle(t1.Vertices[0], t1.Vertices[2], t1.Vertices[1]), new Triangle(t2.Vertices[0], t2.Vertices[2], t2.Vertices[1]));
                }
                else
                {
                    return IntersectionTriY(new Triangle(t1.Vertices[0], t1.Vertices[2], t1.Vertices[1]), new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]));
                }
            else
            {
                if (new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]).OrientY() < 0)
                {
                    return IntersectionTriY(new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]), new Triangle(t2.Vertices[0], t2.Vertices[2], t2.Vertices[1]));
                }
                else
                {
                    return IntersectionTriY(new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]), new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]));
                }
            }
        }
        #endregion








        //********* For Z Coordinate

        // New functions begins
        #region All the predicates for Z coordinate vertex, edge, and triangle overlap tests start here
        /// <summary>
        /// Predicate/Boolean function to test for vertices intesection in the Z direction between two triangles
        /// </summary>
        /// <param name="t1"> first triangle</param>
        /// <param name="t2"> second triangle</param>
        /// <returns>true if vertices of two triangles intersects along Z coordinate</returns>

        public static bool IntersectionVertexZ(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[1]).OrientZ() > 0)
                if (new Triangle(t2.Vertices[2], t2.Vertices[1], t1.Vertices[1]).OrientZ() < 0)
                    if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[1]).OrientZ() > 0)
                    {
                        if (new Triangle(t1.Vertices[0], t2.Vertices[1], t1.Vertices[1]).OrientZ() < 0)
                            return true;
                        else
                            return false;
                    }
                    else
                    {
                        if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientZ() > 0)
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[0]).OrientZ() > 0)
                                return true;
                            else
                                return false;
                        else
                            return false;
                    }
                else
                {
                    if (new Triangle(t1.Vertices[0], t2.Vertices[1], t1.Vertices[1]).OrientZ() < 0)
                        if (new Triangle(t2.Vertices[2], t2.Vertices[1], t1.Vertices[2]).OrientZ() < 0)
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[1]).OrientZ() > 0)
                                return true;
                            else
                                return false;
                        else
                            return false;
                    else
                        return false;
                }
            else
            {
                if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[2]).OrientZ() > 0)
                    if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[2]).OrientZ() > 0)
                        if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientZ() > 0)
                            return true;
                        else
                            return false;
                    else
                        if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[1]).OrientZ() > 0)
                        if (new Triangle(t2.Vertices[2], t1.Vertices[2], t2.Vertices[1]).OrientZ() > 0)
                            return true;
                        else
                            return false;
                    else
                        return false;
                else
                    return false;
            }

        }







        /// <summary>
        /// This predicate / boolean method tests intersection edges along Z coordinate between two triangles
        /// </summary>
        /// <param name="t1">first triangle</param>
        /// <param name="t2">second triangle</param>
        /// <returns>returns true if edges intersects along Z</returns>
        public static bool IntersectionEdgeZ(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[1]).OrientZ() > 0)
            {
                if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[1]).OrientZ() > 0)
                {
                    if (new Triangle(t1.Vertices[0], t1.Vertices[1], t2.Vertices[2]).OrientZ() > 0)
                        return true;
                    else
                        return false;
                }
                else
                {
                    if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[0]).OrientZ() > 0)
                    {
                        if (new Triangle(t1.Vertices[2], t1.Vertices[0], t2.Vertices[0]).OrientZ() > 0)
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            else
            {
                if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[2]).OrientZ() > 0)
                {
                    if (new Triangle(t1.Vertices[0], t2.Vertices[0], t1.Vertices[2]).OrientZ() > 0)
                    {
                        if (new Triangle(t1.Vertices[0], t1.Vertices[2], t2.Vertices[2]).OrientZ() > 0)
                            return true;
                        else
                        {
                            if (new Triangle(t1.Vertices[1], t1.Vertices[2], t2.Vertices[2]).OrientZ() > 0)
                                return true;
                            else
                                return false;
                        }
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
        }






        /// <summary>
        /// This predicates/Boolean method checks for triangle intersection along Z Coordinate
        /// Each condition takes into account all together the vertices of the triangle and their edges 
        /// </summary>
        /// <param name="t1">First triangle</param>
        /// <param name="t2">Second triangle</param>
        /// <returns>true if triangles intersects along Z</returns>

        public static bool IntersectionTriZ(Triangle t1, Triangle t2)
        {
            if (new Triangle(t2.Vertices[0], t2.Vertices[1], t1.Vertices[0]).OrientZ() > 0)
            {
                if (new Triangle(t2.Vertices[1], t2.Vertices[2], t1.Vertices[0]).OrientZ() > 0)
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientZ() > 0)
                        return true;
                    else
                        return IntersectionEdgeZ(t1, t2);
                }
                else
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientZ() > 0)
                        return IntersectionEdgeZ(t1, new Triangle(t2.Vertices[2], t2.Vertices[0], t2.Vertices[1]));
                    else
                        return IntersectionVertexZ(t1, t2);
                }
            }
            else
            {
                if (new Triangle(t2.Vertices[1], t2.Vertices[2], t1.Vertices[0]).OrientZ() > 0)
                {
                    if (new Triangle(t2.Vertices[2], t2.Vertices[0], t1.Vertices[0]).OrientZ() > 0)
                        return IntersectionEdgeZ(t1, new Triangle(t2.Vertices[1], t2.Vertices[2], t2.Vertices[0]));
                    else
                        return IntersectionVertexZ(t1, new Triangle(t2.Vertices[1], t2.Vertices[2], t2.Vertices[0]));
                }
                else
                    return IntersectionVertexZ(t1, new Triangle(t2.Vertices[2], t2.Vertices[0], t2.Vertices[1]));
            }
        }







        /// <summary>
        /// predicate/boolean method to check for ovelapping surfaces in Z coordinate 
        /// This now takes into consideration triangles and checks for overlapping along Z
        /// </summary>
        /// <param name="t1"> first triangle argument of type Triangle</param>
        /// <param name="t2">Second triangle argument of type Triangle</param>
        /// <returns>true if the closed triangles(i.e.the triangles including their boundary) intersect triangles overlap in Z</returns>
        public static bool OverlapTriZ(Triangle t1, Triangle t2)
        {
            if (new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]).OrientZ() < 0)
                if (new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]).OrientZ() < 0)
                    return IntersectionTriZ(new Triangle(t1.Vertices[0], t1.Vertices[2], t1.Vertices[1]), new Triangle(t2.Vertices[0], t2.Vertices[2], t2.Vertices[1]));
                else
                    return IntersectionTriZ(new Triangle(t1.Vertices[0], t1.Vertices[2], t1.Vertices[1]), new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]));
            else
            {
                if (new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]).OrientZ() < 0)
                    return IntersectionTriZ(new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]), new Triangle(t2.Vertices[0], t2.Vertices[2], t2.Vertices[1]));
                else
                    return IntersectionTriZ(new Triangle(t1.Vertices[0], t1.Vertices[1], t1.Vertices[2]), new Triangle(t2.Vertices[0], t2.Vertices[1], t2.Vertices[2]));
            }
        }
        #endregion
        // functions ends









        /// <summary>
        /// method that checks for overlap in the STL files
        /// // Checks for overlapping surfaces and deletes one.
        //if two surfaces of different sizes overlaps, it deletes the smaller surface
        //else if they are of same area, it deletes one
        /// </summary>
        /// <param name="t">is the first triangList/first STL file or initial stl file</param>
        /// <param name="tnew">second triangleList/Second STL file or consecutive stl file</param>
        public static void Overlap(List<Triangle> t, List<Triangle> tnew)
        {

            #region X coordinate Check
            //The Where method filters all values of X when IsX method is true and adds to list of type Triangle name tnewX for the second stl file
            List<Triangle> tnewX = tnew.Where(x => x.IsX(true)).ToList();

            //adds to sections list for all vertices of X coordinate that are same. for the second stlfile
            //The Distinct method helps to gurantee this selection
            List<double> sections = tnewX.Select(x => x.Vertices[0].X).Distinct().ToList();

            //The Where method filters all values of X when IsX method is true and adds to list of type Triangle name tX for the first stl file
            List<Triangle> tX = t.Where(x => x.IsX(true)).ToList();

            List<Triangle> tOverlap = new List<Triangle>();//creates new list to store these overlapping surfaces
            List<Triangle> tnewOverlap = new List<Triangle>();

            foreach (double section in sections)
            {
                //stores all first stlfile vertices where all its vertices in the X coordinate is same with the sections above to a list called tsectionX
                List<Triangle> tsectionX = tX.Where(x => x.Vertices[0].X == section).ToList();

                //stores all second stlfile vertices where all its vertices in the X coordinate to a list called tnewsectionX
                List<Triangle> tnewsectionX = tnewX.Where(x => x.Vertices[0].X == section).ToList();

                //write these two files
                StlWriter(dirpath + "t1.stl", tsectionX);
                StlWriter(dirpath + "t2.stl", tnewsectionX);

                //loop through the triangles of these two files and check if overlapping in the x using the OverlapTriX method
                for (int i = 0; i < tsectionX.Count; i++)
                {
                    bool isoverlap = false;
                    for (int j = 0; j < tnewsectionX.Count; j++)
                    {
                        if (OverlapTriX(tsectionX[i], tnewsectionX[j])) //if condition is true
                        {
                            isoverlap = true;
                            tnewOverlap.Add(tnewsectionX[j]); //add triangles to tnewOverlap list
                        }
                    }
                    if (isoverlap) 
                    {
                        tOverlap.Add(tsectionX[i]); //add the other list to tOverlap
                    }
                }

                //This condition deletes the smaller area of two overlapping surfaces
                if (tsectionX.Count >= tnewsectionX.Count)
                {
                    foreach (Triangle objTriangle in tnewOverlap)
                    {
                        tnew.Remove(objTriangle);
                    }
                }
                else
                { //else delete a overlapping surfaces
                    foreach (Triangle objTriangle in tOverlap)
                    {
                        t.Remove(objTriangle);
                    }
                }

                tnewOverlap = new List<Triangle>();
                tOverlap = new List<Triangle>();
            }
            #endregion X Check










            #region Y Check
            //Its same principle as for #region X Check but considers the Y coordinates
            // Checks for overlapping surfaces and deletes one.
            //if two surfaces of different sizes overlaps, it deletes the smaller surface
            //else if they are of same area, it deletes one

            List<Triangle> tnewY = tnew.Where(x => x.IsY(true)).ToList();
            sections = tnewY.Select(x => x.Vertices[0].Y).Distinct().ToList();
            List<Triangle> tY = t.Where(x => x.IsY(true)).ToList();
            foreach (double section in sections)
            {
                tnewOverlap = new List<Triangle>();
                tOverlap = new List<Triangle>();
                List<Triangle> tsectionY = tY.Where(x => x.Vertices[0].Y == section).ToList();
                List<Triangle> tnewsectionY = tnewY.Where(x => x.Vertices[0].Y == section).ToList();
                StlWriter(dirpath + "t1.stl", tsectionY);
                StlWriter(dirpath + "t2.stl", tnewsectionY);
                for (int i = 0; i < tsectionY.Count; i++)
                {
                    bool isoverlap = false;
                    for (int j = 0; j < tnewsectionY.Count; j++)
                    {
                        if (OverlapTriY(tsectionY[i], tnewsectionY[j]))
                        {
                            isoverlap = true;
                            tnewOverlap.Add(tnewsectionY[j]);
                        }
                    }
                    if (isoverlap)
                    {
                        tOverlap.Add(tsectionY[i]);
                    }
                }
                if (tsectionY.Count >= tnewsectionY.Count)
                {
                    foreach (Triangle objTriangle in tnewOverlap)
                    {
                        tnew.Remove(objTriangle);
                    }
                }
                else
                {
                    foreach (Triangle objTriangle in tOverlap)
                    {
                        t.Remove(objTriangle);
                    }
                }
            }

            #endregion Y Check




            
           

            #region Z Check
            //Same principle as for other coordinates but takes into account the the Z coordinate
            // Checks for overlapping surfaces and deletes one.
            //if two surfaces of different sizes overlaps, it deletes the smaller surface
            //else if they are of same area, it deletes one

            List<Triangle> tnewZ = tnew.Where(x => x.IsZ(true)).ToList();
            sections = tnewZ.Select(x => x.Vertices[0].Z).Distinct().ToList();
            List<Triangle> tZ = t.Where(x => x.IsZ(true)).ToList();
            foreach (double section in sections)
            {
                tnewOverlap = new List<Triangle>();
                tOverlap = new List<Triangle>();
                List<Triangle> tsectionZ = tZ.Where(x => x.Vertices[0].Z == section).ToList();
                List<Triangle> tnewsectionZ = tnewZ.Where(x => x.Vertices[0].Z == section).ToList();
                StlWriter(dirpath + "t1.stl", tsectionZ);
                StlWriter(dirpath + "t2.stl", tnewsectionZ);
                for (int i = 0; i < tsectionZ.Count; i++)
                {
                    bool isoverlap = false;
                    for (int j = 0; j < tnewsectionZ.Count; j++)
                    {
                        if (OverlapTriZ(tsectionZ[i], tnewsectionZ[j]))
                        {
                            isoverlap = true;
                            tnewOverlap.Add(tnewsectionZ[j]);
                        }
                    }
                    if (isoverlap)
                    {
                        tOverlap.Add(tsectionZ[i]);
                    }
                }
                if (tsectionZ.Count >= tnewsectionZ.Count)
                {
                    foreach (Triangle objTriangle in tnewOverlap)
                    {
                        tnew.Remove(objTriangle);
                    }
                }
                else
                {
                    foreach (Triangle objTriangle in tOverlap)
                    {
                        t.Remove(objTriangle);
                    }
                }
            }
            #endregion Z Check
        }
    }




    //Class with fields denoting the three coordinates of both normal and vertices.
    public class Coordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }








    //Class for Triangle 
public class Triangle
{

    #region Properties/Fields
    public List<Coordinate> Vertices;
    public Coordinate Normal;
    #endregion Properties



    //constructor with no parameter for explicit construction
    public Triangle()
    {

    }



    //Constructor for triangle that accepts the coordinate class as type
    /// <summary>
    /// 
    /// </summary>
    /// <param name="v1">first vertices </param>
    /// <param name="v2">second vertices</param>
    /// <param name="v3">third vertices</param>

    public Triangle(Coordinate v1, Coordinate v2, Coordinate v3)
    {
        //creates a new list of coordinates and assign to the vertices field/property of the triangle
        Vertices = new List<Coordinate>();
        Vertices.Add(v1);
        Vertices.Add(v2);
        Vertices.Add(v3);
    }






        /// <summary>
        /// this method simply helps in writing the stl files in the stl format
        /// TriangleStlLine method is a type List of string to write each line of the stl file from facet normal to endfacet
        /// that means for each triangle
        /// the spacecount is a variable for creating whitespaces for each line
        /// </summary>
        /// <returns>list of string, which is the component of the stl file</returns>
        #region Method to write the format of the stl file
        public List<string> TriangleStlLine()
    {
        List<string> lines = new List<string>();
        int spacecount = 2;
        string line = "";
        for (int i = 0; i < spacecount; i++)
        {
            line += " ";
        }
        line += "facet normal ";
        line += (Convert.ToString(Normal.X) + " " + Convert.ToString(Normal.Y) + " " + Convert.ToString(Normal.Z));
        lines.Add(line);



        spacecount = 4;
        line = "";
        for (int i = 0; i < spacecount; i++)
        {
            line += " ";
        }
        line += "outer loop";
        lines.Add(line);

        spacecount = 6;
        foreach (Coordinate objCoordinate in Vertices)
        {
            line = "";
            for (int i = 0; i < spacecount; i++)
            {
                line += " ";
            }
            line += "vertex ";
            line += (Convert.ToString(objCoordinate.X) + " " + Convert.ToString(objCoordinate.Y) + " " + Convert.ToString(objCoordinate.Z));
            lines.Add(line);
        }

        spacecount = 4;
        line = "";//replace "" with string.empty
        for (int i = 0; i < spacecount; i++)
        {
            line += " ";
        }
        line += "endloop";
        lines.Add(line);


        spacecount = 2;
        line = "";//replace "" with string.empty
        for (int i = 0; i < spacecount; i++)
        {
            line += " ";
        }
        line += "endfacet";

        lines.Add(line);

        return lines;
    }
        #endregion






        //Boolean methods Checks if the normal in the coordinates are 1
        #region Boolean methods to check if the Normals of each coordinate is pointing a direction wrt to coordinate
        /// <summary>
        /// Boolean method Checks if the normal in the X direction is 1
        /// </summary>
        /// <param name="abs">condition that returns true if the normal is absolute 1 be it -1 or 1 in the X coordinate</param>
        /// <returns>the other condition returns true for only +1 value</returns>
        public bool IsX(bool abs)
    {
        if (abs)
        {
            if (Math.Round(Normal.Y, 2) == 0 && Math.Round(Normal.Z, 2) == 0 && Math.Abs(Math.Round(Normal.X)) == 1)
            {
                return true;
            }
            return false;
        }
        else
        {
            if (Math.Round(Normal.Y, 2) == 0 && Math.Round(Normal.Z, 2) == 0 && Math.Round(Normal.X) == 1)
            {
                return true;
            }
            return false;
        }
    }





    /// <summary>
    /// Boolean Method Checks if the normal in the Y direction is 1
    /// </summary>
    /// <param name="abs">condition that returns true if the normal is abosule unity be it -1 or 1 in the Y coordinate</param>
    /// <returns> the other condition returns true for only +1 value</returns>
    public bool IsY(bool abs)
    {
        if (abs)
        {
            if (Math.Round(Normal.X, 2) == 0 && Math.Round(Normal.Z, 2) == 0 && Math.Abs(Math.Round(Normal.Y)) == 1)
            {
                return true;
            }
            return false;
        }
        else
        {
            if (Math.Round(Normal.X, 2) == 0 && Math.Round(Normal.Z, 2) == 0 && Math.Round(Normal.Y) == 1)
            {
                return true;
            }
            return false;
        }
    }





    //
    /// <summary>
    /// boolean method to check if the normal in the Z direction is 1
    /// </summary>
    /// <param name="abs"> a condition which returns true if the abs z is unity be it -1 or 1</param>
    /// <returns>the other condition returns true for only +1 value</returns>
    public bool IsZ(bool abs)
    {
        if (abs)
        {
            if (Math.Round(Normal.X, 2) == 0 && Math.Round(Normal.Y, 2) == 0 && Math.Abs(Math.Round(Normal.Z)) == 1)
            {
                return true;
            }
            return false;
        }
        else
        {
            if (Math.Round(Normal.X, 2) == 0 && Math.Round(Normal.Y, 2) == 0 && Math.Round(Normal.Z) == 1)
            {
                return true;
            }
            return false;
        }
    }
        #endregion






        // In this section, using the algorithm, we have to compute the determinants and compare
        // the signs of the three determinants in order to know if intersection exists or not
        #region Coordinates Oreintation tests by calculating the determinants of their matrices
        

        //basically computing the determinant of the 3d triangle matrix in x coordinate
        public double OrientX()
    {
        return ((Vertices[0].Y - Vertices[2].Y) * (Vertices[1].Z - Vertices[2].Z) - (Vertices[0].Z - Vertices[2].Z) * (Vertices[1].Y - Vertices[2].Y));
    }




    //simply computes the determinant in the y coordinate
    public double OrientY()
    {
        return ((Vertices[0].X - Vertices[2].X) * (Vertices[1].Z - Vertices[2].Z) - (Vertices[0].Z - Vertices[2].Z) * (Vertices[1].X - Vertices[2].X));
    }




    //simply computes the determinant in the z cordinate 
    public double OrientZ()
    {
        return ((Vertices[0].Y - Vertices[2].Y) * (Vertices[1].X - Vertices[2].X) - (Vertices[0].X - Vertices[2].X) * (Vertices[1].Y - Vertices[2].Y));
    }
    #endregion Methods
}

}
