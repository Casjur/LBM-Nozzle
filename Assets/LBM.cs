//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Problemen:
// 1?. Kleuren zijn verticale strepen, als je in zoomt. Mogelijk gwn bug in Unity
//      * Groen = verticale strepen
//      * Blauw = horizontaal
// 

// Todo:
// 1. Alles


public class LBM : MonoBehaviour
{
    public const int WIDTH = 256;
    public const int HEIGHT = 256;
    //public const float VISCOSITY = 1.0f; // !!! Wordt nergens gebruikt !!!
    public const double RELAXATION_TIME = 1.0f; // 0.1f en waarschijnlijk alles onder 1 breekt de simulatie 

    public LatticeGrid Grid;

    public Material outputMaterial;

    void Start()
    {
        this.Grid = new LatticeGrid(WIDTH, HEIGHT, /* VISCOSITY, */ RELAXATION_TIME);
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
        this.density = density; // Only used to display result
        this.velocityX = velocityX; // Only used to display result
        this.velocityY = velocityY; // Only used to display result
        this.distribution = new double[9]; // Only factor with influence in the function
        for(int i = 0; i < 9; i++)
        {
            distribution[i] = LatticeGrid.weights[i] * density * (1 + 3 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) + 4.5 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) - 1.5 * (velocityX * velocityX + velocityY * velocityY));
        }
    }
}

public class LatticeGrid
{
    // Test variables
    public double sumDensity = 0.0;
    public double minDensity = 0.0;


    // 
    private int gridWidth;
    private int gridHeight;
    private double relaxationTime;
    private double relaxationFactor;
    private double cSqr;
    private LatticeGridNode[] latticeGrid;

    // Lattice constants
    public static readonly double[] eX = { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
    public static readonly double[] eY = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
    public static readonly double[] weights = { 
        4.0 / 9.0, 
        1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 
        1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0 
    };

    public LatticeGrid(int width, int height, /* double viscosity, */ double tau)
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
                latticeGrid[i] = new LatticeGridNode(1.0, 0.0, 0.0);
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
            double r = Random.Range(0.1f, 5.0f);
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
        sumDensity = 0.0;

        for (int j = 0; j < gridHeight; j++)
        {
            for (int i = 0; i < gridWidth; i++)
            {
                // Compute rho and velocity
                double rho = 0.0; // Sum of all distributions in a cell
                double velocityX = 0.0; // General velocity of the gas (X)
                double velocityY = 0.0; // General velocity of the gas (Y)

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

                sumDensity += rho;

                // Compute equilibrium distribution (aangepast, zonder verandering in resultaat)
                double[] feq = new double[9];
                for (int k = 0; k < 9; k++)
                {
                    feq[k] = EquilibriumFunction(rho, velocityX, velocityY, k);
                }

                
                // Collision step
                for (int k = 0; k < 9; k++)
                {
                    //node.distribution[k] += relaxationFactor * (feq[k] - node.distribution[k]); // originele

                    node.distribution[k] = relaxationFactor * feq[k] + (1 - relaxationFactor) * node.distribution[k];
                }

                // Debug
                if (i == 40 && j == 40)
                {
                    double newRho = 0.0;

                    for (int k = 0; k < 9; k++)
                    {
                        newRho += node.distribution[k];
                    }

                    Debug.Log("Previous Rho: " + rho);
                    Debug.Log("NewRho: " + newRho);
                }
            }
        }

        Debug.Log("Collision Density: " + sumDensity);
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
        //double feq = rho * weight * (1.0 + (3.0 * udotcx) + (4.5 * udotcx * udotcx) - (1.5 * (ux * ux + uy * uy) * cSqr)); // originele

        double feq = weight * rho * (1.0 + udotcx / cSqr + 0.5 * (udotcx / cSqr) * (udotcx / cSqr) - (ux * ux + uy * uy) / (2.0 * cSqr)); // WERKT!

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

        sumDensity = 0.0;

        // Update the lattice grid with the streamed values
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            latticeGrid[i].distribution = tempGrid[i].distribution;
            latticeGrid[i].density = tempGrid[i].density;
            latticeGrid[i].velocityX = tempGrid[i].velocityX;
            latticeGrid[i].velocityY = tempGrid[i].velocityY;

            sumDensity += tempGrid[i].density;
        }

        Debug.Log("Streaming Density: " + sumDensity);
    }

    public void UpdateDisplayTexture(int width, int height, ref Material outputMaterial)
    {
        Texture2D outputTexture = new Texture2D(width, height);

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                int index = x + y * gridHeight;

                double r = latticeGrid[index].density;
                double g = latticeGrid[index].velocityX;
                double b = latticeGrid[index].velocityY;
                Color color = new Color(1.0f / (float)r, 
                    (0.5f + (float)g),
                    (0.5f + (float)b));
                //Color color = new Color((float)r / 5.0f, 0, 0);
                outputTexture.SetPixel(x, y, color);
            }
        }

        Debug.Log("testDensity: " + latticeGrid[40].density);

        outputTexture.Apply();

        outputMaterial.mainTexture = outputTexture;
    }
}