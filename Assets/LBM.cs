using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Problemen:
// 1?. Kleuren zijn verticale strepen, als je in zoomt. Mogelijk gwn bug in Unity
//      * Groen = verticale strepen
//      * Blauw = horizontaal
// 2. Extreem instabiel voor sommige densities. Debug log parameters, wanneer extreme waardes / verschil in waardes gemaakt worden
//      * Dit kan gebeuren wanneer verschil tussen x en y snelheid 0.8165 is. HOUD 0.73 AAN IPV 0.8165!!!
//      * Zolang de velocity in zowel de x als y richting niet groter is dan 0.407 zou de simulatie moeten blijven werken (4.226 zou misschien ook kunnen werken)
//      * 0 < vel.x < 0.73 && vel.y = 0.73 - vel.x        werkt
//      * 0 < vel.y < 0.73 && vel.x = 0.73 - vel.y        werkt
//      * abs(vel.x - vel.y) < 0.73 
//      * https://www.desmos.com/calculator/foa5ikbgrz      (eigen berekeningen)
//      * https://arxiv.org/pdf/2006.07353.pdf              (paper van andere universiteit)
//          - Ma > sqrt(3) − 1 ~= 0.73        (hoort een streepje boven Ma)
//          - Gemiddeld Mach getal
//      * https://www.reddit.com/r/CFD/comments/128x7uq/latticeboltzmann_unstable/?utm_source=share&utm_medium=web2x&context=3 

// Todo:
// 1. Variabele zoals de afstand tussen cells in (dx), de velocity tussen 2 cells in (c), en viscosity toevoegen
// 2. Boundaries toevoegen
// 3. Nozzle generator toevoegen
// 4. Thrust meter toevoegen
// 5. Array 2d maken (huidige implementatie is veelste traag)


public class LBM : MonoBehaviour
{
    public const int WIDTH = 256;
    public const int HEIGHT = 256;
    //public const float VISCOSITY = 1.0f; // !!! Wordt nergens gebruikt !!!
    public const double viscosity = 0.000017; // Lucht op 20km hoogte
    public const double RELAXATION_TIME = 0.53; //(2 * viscosity + 1 / 2) / (Math.Pow(c2, 2));

    public const double baseDensity = 1.0;

    public LatticeGrid Grid;

    public Material outputMaterial;

    void Start()
    {
        this.Grid = new LatticeGrid(WIDTH, HEIGHT, RELAXATION_TIME, baseDensity);
        this.Grid.AddCylinder(80, 80, 30, 1.0);
    }

    void Update()
    {
        //this.Grid.AddCylinder(80, 80, 30, 0.5);
        this.Grid.Step();
        this.Grid.UpdateDisplayTexture(WIDTH, HEIGHT, ref outputMaterial);
    }
}

// `rho` is de "macroscopic density" van de vloeistof op elke cell
// `tau` is de "kinematic viscosity / timescale / relaxation time" van de vloeistof in het algemeen.

public class LatticeGridNode
{
    // If the node should be able to hold a fluid / gas
    public bool isSolid;

    // macroscopic variables
    public double density;
    public double velocityX;
    public double velocityY;
    // distribution values
    public double[] distribution;

    public LatticeGridNode(double density = 0.0f, double velocityX = 0.0f, double velocityY = 0.0f)
    {
        // Dont initialize anything when the cell is solid
        if (isSolid)
            return;

        // Initialize base fluid properties
        this.density = density; // Only used to display result
        this.velocityX = velocityX; // Only used to display result
        this.velocityY = velocityY; // Only used to display result
        this.distribution = new double[9]; // Only factor with influence in the function
        for(int i = 0; i < 9; i++)
        {
            //distribution[i] = 1 + (double)UnityEngine.Random.Range(0.0f, 1.0f); //(double)UnityEngine.Random.Range(0.0f, 0.01f);
            distribution[i] = LatticeGrid.weights[i] * density * (1 + 3 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) + 4.5 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) - 1.5 * (velocityX * velocityX + velocityY * velocityY));
        }
    }

    public void AddDensity(double density)
    {
        for (int i = 0; i < 9; i++)
        {
            distribution[i] += LatticeGrid.weights[i] * density * (1 + 3 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) + 4.5 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) - 1.5 * (velocityX * velocityX + velocityY * velocityY));
        }
    }
}

public class LatticeGrid
{
    // Test variables
    public double sumDensity = 0.0;
    public double minDensity = Mathf.Infinity;
    public double maxDensity = -Mathf.Infinity;

    // LBM 
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

    public LatticeGrid(int width, int height, double tau, double baseDensity)
    {
        gridWidth = width;
        gridHeight = height;
        relaxationTime = tau;
        relaxationFactor = 1.0 / tau;
        cSqr = 1.0 / 3.0;

        //Initialize(baseDensity);
        InitializeRandom();
    }

    public void Initialize(double density)
    {
        latticeGrid = new LatticeGridNode[gridWidth * gridHeight];

        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            latticeGrid[i] = new LatticeGridNode(density, 0.0, 0.0);
        }
    }

    public void InitializeLine()
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

    private void InitializeRandom()
    {
        latticeGrid = new LatticeGridNode[gridWidth * gridHeight];

        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            double r = (double)UnityEngine.Random.Range(0.5f, 1.0f);
            latticeGrid[i] = new LatticeGridNode(r, 0.0, 0.0);
        }
    }

    public void AddCylinder(int xPosition, int yPosition, int r, double density)
    {
        Debug.Log("AddCylinder()");
        for(int x = 0; x < gridWidth; x++)
        {
            for(int y = 0; y < gridHeight; y++)
            {
                int dx = x - xPosition;
                int dy = y - yPosition;

                float distanceSquare = dx * dx + dy * dy;
                bool inCylinder = distanceSquare <= r * r;

                if (inCylinder)
                {
                    int index = x + y * gridHeight;
                    latticeGrid[index].AddDensity(density);
                }
            }
        }
    }

    public void Step()
    {
        this.CollisionStep();
        this.StreamingStep();
    }

    public void CollisionStep()
    {
        sumDensity = 0.0; // Debug value

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

                // Compute equilibrium distribution
                double[] feq = new double[9];
                for (int k = 0; k < 9; k++)
                {
                    feq[k] = EquilibriumFunction(rho, velocityX, velocityY, k);
                }

                // Collision step
                for (int k = 0; k < 9; k++)
                {
                    // Original
                    //node.distribution[k] += relaxationFactor * (feq[k] - node.distribution[k]); // originele
                    // Compute shader (1 of 3)
                    //node.distribution[k] = relaxationFactor * feq[k] + (1 - relaxationFactor) * node.distribution[k];  
                    // Python
                    node.distribution[k] = node.distribution[k] - relaxationFactor * (node.distribution[k] - feq[k]);
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
        // Original
        //double feq = rho * weight * (1.0 + (3.0 * udotcx) + (4.5 * udotcx * udotcx) - (1.5 * (ux * ux + uy * uy) * cSqr)); // originele
        // Compute shader version (1 of 3)
        //double feq = weight * rho * (1.0 + udotcx / cSqr + 0.5 * (udotcx / cSqr) * (udotcx / cSqr) - (ux * ux + uy * uy) / (2.0 * cSqr)); // WERKT! niet altijd :(
        // Python
        double feq = rho * weight * (
                1 + 3 * (ux * cx + uy * cy) + 9 * Math.Pow((ux * cx + uy * cy), 2) / 2 - 3 * (Math.Pow(ux, 2) + Math.Pow(uy, 2)) / 2
            );

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
                int index = x + y * gridHeight;

                double r = latticeGrid[index].density;
                //double g = latticeGrid[index].velocityX;
                //double b = latticeGrid[index].velocityY;
                
                Color color;
                if(latticeGrid[index].isSolid)
                {
                    color = Color.black;
                }
                else
                {
                    //Color color = new Color(10.0f / (float)r,
                    //    (0.5f + (float)g),
                    //    (0.5f + (float)b));
                    //Color color = new Color(1.0f / (float)r, 0, 0);
                    color = GenerateDensityColor(r);
                }
                outputTexture.SetPixel(x, y, color);
            }
        }

        Debug.Log("testDensity: " + latticeGrid[40].density);

        outputTexture.Apply();

        outputMaterial.mainTexture = outputTexture;
    }

    public Color GenerateDensityColor(double density)
    {
        float r = (float)density / 2.0f;
        float g = r / 2.0f;
        float b = g / 2.0f;

        return new Color(r, g, b);
    }
}