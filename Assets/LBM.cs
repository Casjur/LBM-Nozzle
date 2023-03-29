//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Problemen:
// 1?. Kleuren zijn verticale strepen, als je in zoomt. Mogelijk gwn bug in Unity
//      * Groen = verticale strepen
//      * Blauw = horizontaal

// Todo:
// 1. Alles


public class LBM : MonoBehaviour
{
    public const int WIDTH = 256;
    public const int HEIGHT = 256;
    public const float VISCOSITY = 1.0f; // !!! Wordt nergens gebruikt !!!
    public const float RELAXATION_TIME = 1.0f; // 0.1f en waarschijnlijk alles onder 1 breekt de simulatie 

    public LatticeGrid Grid;

    //public Texture2D outputTexture;
    public Material outputMaterial;

    void Start()
    {
        this.Grid = new LatticeGrid(WIDTH, HEIGHT, VISCOSITY, RELAXATION_TIME);
        //this.Grid.TestInitialize();
    }

    void Update()
    {
        
        this.Grid.CollisionStep();
        this.Grid.StreamingStep();
        this.Grid.UpdateDisplayTexture(WIDTH, HEIGHT, ref outputMaterial);

        Debug.Log("Frame done");
    }
}

// `rho` is de "macroscopic density" van de vloeistof op elke lattice
// `tau` is de "relaxation time" van de vloeistof in het algemeen.

public class LatticeGridNode
{
    // macroscopic variables
    public double density;
    public double velocityX;
    public double velocityY;
    // distribution functions
    public double[] distribution;

    public LatticeGridNode(double density = 0.0f, double velocityX = 0.0f, double velocityY = 0.0f)
    {
        this.density = density;
        this.velocityX = velocityX;
        this.velocityY = velocityY;
        this.distribution = new double[9];
        for(int i = 0; i < 9; i++)
        {
            distribution[i] = LatticeGrid.weights[i] * density * (1 + 3 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) + 4.5 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) - 1.5 * (velocityX * velocityX + velocityY * velocityY));
        }

        //distribution = new double[] { 0, 0, 0.1, 0, 0, 0, 0, 0, 0 }; //new double[9];
    }
}

public class LatticeGrid
{
    private int gridWidth;
    private int gridHeight;
    private double relaxationTime;
    private double relaxationFactor;
    private double cSqr;
    private LatticeGridNode[] latticeGrid;

    // Lattice constants
    public static readonly double[] eX = { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
    public static readonly double[] eY = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
    public static readonly double[] weights = { 4.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0 };

    public LatticeGrid(int width, int height, double viscosity, double tau)
    {
        gridWidth = width;
        gridHeight = height;
        relaxationTime = tau;
        relaxationFactor = 1.0 / tau;
        cSqr = 1.0 / 3.0;

        Initialize2();
    }

    public void Initialize()
    {
        latticeGrid = new LatticeGridNode[gridWidth * gridHeight];

        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            if (i > 2500)
            {
                latticeGrid[i] = new LatticeGridNode(5.0, 0.0, 0.0);
            }
            else
            {
                latticeGrid[i] = new LatticeGridNode(0.5, 0.0, 0.0);
            }
        }
    }

    private void Initialize2()
    {
        latticeGrid = new LatticeGridNode[gridWidth * gridHeight];

        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            double r = Random.Range(0.0f, 1.0f);
            latticeGrid[i] = new LatticeGridNode(r, 0.0, 0.0);
        }
    }

    public void TestInitialize()
    {
        for(int x = 0; x < 70; x++)
        {
            for(int y = 0; y < 70; y++)
            {
                this.latticeGrid[x + y * gridHeight].distribution[1] = 0.1f;
                //this.latticeGrid[x * y].density = 1000f;
            }
        }
        //this.latticeGrid[40 * 40].density = 1.0f;
    }

    public void TestInitialize2()
    {
        for (int x = 0; x < 70; x++)
        {
            for (int y = 0; y < 70; y++)
            {
                for(int k = 0; k < 9; k++)
                {
                    float randomValue = Random.Range(0.0f, 1.0f);
                    this.latticeGrid[x + y * gridHeight].distribution[k] = randomValue;
                }
                
                //this.latticeGrid[x * y].density = 1000f;
            }
        }
        //this.latticeGrid[40 * 40].density = 1.0f;
    }

    public void TestInitialize3()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int k = 0; k < 9; k++)
                {
                    //float randomValue = Random.Range(0.0f, 1.0f);
                    this.latticeGrid[x + y * gridHeight].distribution[k] = Mathf.Abs(Mathf.Sin(k * 37.0f));
                    //Debug.Log(this.latticeGrid[x + y * gridHeight].distribution[k]);
                }

                //this.latticeGrid[x * y].density = 1000f;
            }
        }
        //this.latticeGrid[40 * 40].density = 1.0f;
    }

    public void CollisionStep()
    {
        for (int j = 0; j < gridHeight; j++)
        {
            for (int i = 0; i < gridWidth; i++)
            {
                // Compute rho and velocity
                double rho = 0.0; // Sum of all distributions in a cell
                double velocityX = 0.0;
                double velocityY = 0.0;

                LatticeGridNode node = latticeGrid[i + j * gridWidth];

                for (int k = 0; k < 9; k++)
                {
                    rho += node.distribution[k];
                    velocityX += eX[k] * node.distribution[k];
                    velocityY += eY[k] * node.distribution[k];
                }

                velocityX /= rho;
                velocityY /= rho;

                node.density = rho;
                node.velocityX = velocityX;
                node.velocityY = velocityY;

                // Compute equilibrium distribution
                //double uSqr = velocityX * velocityX + velocityY * velocityY;
                //double us = velocityX * eX[0] + velocityY * eY[0]; // NOT USED !?
                //double[] feq = new double[9];

                //for (int k = 0; k < 9; k++)
                //{
                //    double cu = eX[k] * velocityX + eY[k] * velocityY;
                //    double usq = eX[k] * eX[k] + eY[k] * eY[k];

                //    feq[k] = weights[k] * rho * (1.0 + 3.0 * cu / cSqr + 4.5 * cu * cu / (cSqr * cSqr) - 1.5 * uSqr / cSqr);
                //}


                double[] feq = new double[9];
                for (int k = 0; k < 9; k++)
                {
                    feq[k] = EquilibriumFunction(rho, velocityX, velocityY, k);
                }

                // Collision step
                for (int k = 0; k < 9; k++)
                {
                    // nodeDistr = nodeDistr + (feq - nodeDistr) / tau
                    // nodeDistr = nodeDistr + (1 / tau) * (feq - nodeDistr)
                    // nodeDistr += (1 / tau) * (feq - nodeDistr)
                    node.distribution[k] -= relaxationFactor * (feq[k] - node.distribution[k]);
                }
            }
        }
    }

    double EquilibriumFunction(double rho, double ux, double uy, int i)
    {
        double weight = weights[i];
        double cx = eX[i]; //0.0;
        double cy = eY[i];
        //double c2 = 1.0 / 3.0;

        // Calculate the dot product of the velocity and lattice vectors
        double udotcx = ux * cx + uy * cy;

        // Calculate the equilibrium distribution function for the given velocity component
        double feq = rho * weight * (1.0 + 3.0 * udotcx + 4.5 * udotcx * udotcx - 1.5 * (ux * ux + uy * uy) * cSqr); // originele

        //double feq = weight * rho * (1.0f + 3.0f * udotcx + 4.5f * udotcx * udotcx - 1.5f * (ux * ux));
        return feq;
    }

    public void StreamingStep()
    {
        // Create a temporary distribution grid to perform streaming
        LatticeGridNode[] tempGrid = new LatticeGridNode[gridWidth * gridHeight];
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            tempGrid[i] = new LatticeGridNode();
        }

        // Perform streaming on all nodes
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // Calculate the index of the current node in the 1D grid
                int currentIndex = x + y * gridWidth;

                // Stream the distributions to their new locations
                for (int i = 0; i < 9; i++)
                {
                    // Calculate the new x and y position of the distribution
                    int newX = x + (int)eX[i];
                    int newY = y + (int)eY[i];

                    // Periodic boundary conditions (if something leaves on the left, it returns on the right)
                    if (newX < 0)
                    {
                        newX += gridWidth;
                    }
                    else if (newX >= gridWidth)
                    {
                        newX -= gridWidth;
                    }

                    if (newY < 0)
                    {
                        newY += gridHeight;
                    }
                    else if (newY >= gridHeight)
                    {
                        newY -= gridHeight;
                    }

                    // Calculate the index of the new node in the 1D grid
                    int newIndex = newX + newY * gridWidth;

                    // Stream the distribution to the new location
                    tempGrid[newIndex].distribution[i] = latticeGrid[currentIndex].distribution[i];
                }

                // Copy the macroscopic variables to the new node
                tempGrid[currentIndex].density = latticeGrid[currentIndex].density;
                tempGrid[currentIndex].velocityX = latticeGrid[currentIndex].velocityX;
                tempGrid[currentIndex].velocityY = latticeGrid[currentIndex].velocityY;
            }
        }

        // Update the lattice grid with the streamed values
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            latticeGrid[i].distribution = tempGrid[i].distribution;
            latticeGrid[i].density = tempGrid[i].density;
            latticeGrid[i].velocityX = tempGrid[i].velocityX;
            latticeGrid[i].velocityY = tempGrid[i].velocityY;
        }
    }

    public void UpdateDisplayTexture(int width, int height, ref Material outputMaterial)
    {
        Texture2D outputTexture = new Texture2D(width, height);

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                //double d = latticeGrid[x + y * gridHeight].distribution[1];
                //Debug.Log("distribution: " + d);
                int index = x + y * gridHeight;

                double r = latticeGrid[index].density;
                double g = latticeGrid[index].velocityX;
                double b = latticeGrid[index].velocityY;
                Color color = new Color((float)r / 5.0f, (float)g * 10.0f, (float)b * 10.0f);
                //Color color = new Color((float)r / 5.0f, 0, 0);
                outputTexture.SetPixel(x, y, color);
            }
        }

        Debug.Log("testDensity: " + latticeGrid[40].density);

        outputTexture.Apply();

        outputMaterial.mainTexture = outputTexture;
    }
}


//public void CollisionStep()
//{
//    for (int j = 0; j < gridHeight; j++)
//    {
//        for (int i = 0; i < gridWidth; i++)
//        {
//            // Compute rho and velocity
//            double rho = 0.0;
//            double velocityX = 0.0;
//            double velocityY = 0.0;

//            LatticeGridNode node = latticeGrid[i + j * gridWidth];

//            for (int k = 0; k < 9; k++)
//            {
//                rho += node.distribution[k];
//                velocityX += eX[k] * node.distribution[k];
//                velocityY += eY[k] * node.distribution[k];
//            }

//            velocityX /= rho;
//            velocityY /= rho;

//            node.density = rho;
//            node.velocityX = velocityX;
//            node.velocityY = velocityY;

//            // Compute equilibrium distribution
//            double uSqr = velocityX * velocityX + velocityY * velocityY;
//            double us = velocityX * eX[0] + velocityY * eY[0];
//            double[] feq = new double[9];

//            for (int k = 0; k < 9; k++)
//            {
//                double cu = eX[k] * velocityX + eY[k] * velocityY;
//                double usq = eX[k] * eX[k] + eY[k] * eY[k];

//                feq[k] = weights[k] * rho * (1.0 + 3.0 * cu / cSqr + 4.5 * cu * cu / (cSqr * cSqr) - 1.5 * uSqr / cSqr);
//            }

//            // Collision step
//            for (int k = 0; k < 9; k++)
//            {
//                node.distribution[k] += relaxationFactor * (feq[k] - node.distribution[k]);
//            }
//        }
//    }
//}


//private void StreamingStep()
//{
//    // Create a temporary distribution grid to perform streaming
//    LatticeGridNode[] tempGrid = new LatticeGridNode[gridWidth * gridHeight];
//    for (int i = 0; i < gridWidth * gridHeight; i++)
//    {
//        tempGrid[i] = new LatticeGridNode();
//    }

//    // Perform streaming on all nodes
//    for (int y = 0; y < gridHeight; y++)
//    {
//        for (int x = 0; x < gridWidth; x++)
//        {
//            // Calculate the index of the current node in the 1D grid
//            int currentIndex = x + y * gridWidth;

//            // Stream the distributions to their new locations
//            for (int i = 0; i < 9; i++)
//            {
//                // Calculate the new x and y position of the distribution
//                int newX = x + (int)eX[i];
//                int newY = y + (int)eY[i];

//                // Periodic boundary conditions
//                if (newX < 0)
//                {
//                    newX += gridWidth;
//                }
//                else if (newX >= gridWidth)
//                {
//                    newX -= gridWidth;
//                }

//                if (newY < 0)
//                {
//                    newY += gridHeight;
//                }
//                else if (newY >= gridHeight)
//                {
//                    newY -= gridHeight;
//                }

//                // Calculate the index of the new node in the 1D grid
//                int newIndex = newX + newY * gridWidth;

//                // Stream the distribution to the new location
//                tempGrid[newIndex].distribution[i] = latticeGrid[currentIndex].distribution[i];
//            }

//            // Copy the macroscopic variables to the new node
//            tempGrid[currentIndex].density = latticeGrid[currentIndex].density;
//            tempGrid[currentIndex].velocityX = latticeGrid[currentIndex].velocityX;
//            tempGrid[currentIndex].velocityY = latticeGrid[currentIndex].velocityY;
//        }
//    }

//    // Update the lattice grid with the streamed values
//    for (int i = 0; i < gridWidth * gridHeight; i++)
//    {
//        latticeGrid[i].distribution = tempGrid[i].distribution;
//        latticeGrid[i].density = tempGrid[i].density;
//        latticeGrid[i].velocityX = tempGrid[i].velocityX;
//        latticeGrid[i].velocityY = tempGrid[i].velocityY;
//    }
//}
