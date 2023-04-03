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
// 4. Thrust meter toevoegen
// 5. Array 2d maken (huidige implementatie is veelste traag)


public class LBM : MonoBehaviour
{
    public const int WIDTH = 256; // Moeten hetzelfde zijn (index wordt verkeerd berekend)
    public const int HEIGHT = 256;
    //public const float VISCOSITY = 1.0f; // !!! Wordt nergens gebruikt !!!
    //public const double viscosity = 0.000017; // Lucht op 20km hoogte
    public const double RELAXATION_TIME = 8.0; //1.0 // Min: 0.53 Max: geen? //(2 * viscosity + 1 / 2) / (Math.Pow(c2, 2));

    public const double baseDensity = 1.0;

    public LatticeGrid Grid;

    public Nozzle nozzle;
    public double nozzleDensity = 1.0;

    public int NozzleX = 10;
    public int NozzleY = 180;
    public int NozzleLineRadius = 3;
    public int CombChamRadius = 20;
    public int CombChamLength = 70;
    public int ThroatRadius = 5;
    public int ConvergeLength = 30;
    public int DivergeLength = 40;

    public Material outputMaterial;

    void Start()
    {
        this.nozzle = new Nozzle(NozzleX, NozzleY, NozzleLineRadius, CombChamRadius, CombChamLength, ThroatRadius, ConvergeLength, DivergeLength);
        this.Grid = new LatticeGrid(WIDTH, HEIGHT, RELAXATION_TIME, baseDensity, nozzle);
        this.nozzle.grid = this.Grid;
        //this.Grid.AddCylinder(40, 40, 30, 1.0);
    }

    void Update()
    {
        //this.Grid.AddCylinder(80, 80, 30, 0.5);
        //this.Grid.AddCylinder(40, 40, 30, .01);
        //this.Grid.MaintainCylinder(40, 40, 30, 1.0);
        this.nozzle.UpdateCombustionChamber(nozzleDensity);
        this.Grid.Step();

        this.Grid.UpdateDisplayTexture(WIDTH, HEIGHT, ref outputMaterial);
    }
}

// `rho` is de "macroscopic density" van de vloeistof op elke cell
// `tau` is de "kinematic viscosity / timescale / relaxation time" van de vloeistof in het algemeen.

public interface LatticeGridNode
{
    public void AddDensity(double density);
    public void AddDensityInDirection(double density, int distributionIndex);
    public void SetDensity(double density);
}

public class FluidLatticeNode : LatticeGridNode
{
    // macroscopic variables
    public double density;
    public double velocityX;
    public double velocityY;
    // distribution values
    public double[] distribution;

    public FluidLatticeNode(double density = 0.0f, double velocityX = 0.0f, double velocityY = 0.0f)
    {
        // Initialize base fluid properties
        this.density = density; // Only used to display result
        this.velocityX = velocityX; // Only used to display result
        this.velocityY = velocityY; // Only used to display result
        this.distribution = new double[9]; // Only factor with influence in the function
        for (int i = 0; i < 9; i++)
        {
            //distribution[i] = 1 + (double)UnityEngine.Random.Range(0.0f, 1.0f); //(double)UnityEngine.Random.Range(0.0f, 0.01f);
            distribution[i] = LatticeGrid.weights[i] * density * (1 + 3 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) + 4.5 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) - 1.5 * (velocityX * velocityX + velocityY * velocityY));
        }
    }

    public void SetDensity(double density)
    {
        for(int i = 0; i < 9; i++)
        {
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="density"></param>
    /// <param name="i"> distribution index (describes direcition of the density) </param>
    public void AddDensityInDirection(double density, int i)
    {
        //double d = LatticeGrid.weights[i] * density * (1 + 3 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) + 4.5 * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) * (LatticeGrid.eX[i] * velocityX + LatticeGrid.eY[i] * velocityY) - 1.5 * (velocityX * velocityX + velocityY * velocityY));
        //Debug.Log("d: " + d);
        distribution[i] += density; //d;
        
    }
}

public class SolidLatticeNode : LatticeGridNode
{
    public void AddDensity(double density)
    {
        // Do nothing, since the node does not contain liquid
        return;
    }

    public void SetDensity(double density)
    {
        return;
    }

    public void AddDensityInDirection(double density, int distributionIndex)
    {
        return;
    }

}

public class LatticeGrid
{
    public Nozzle nozzle;

    // Test variables
    public double sumDensity = 0.0;
    public double minDensity = Mathf.Infinity;
    public double maxDensity = -Mathf.Infinity;

    // LBM 
    public readonly int gridWidth;
    public readonly int gridHeight;
    public readonly double relaxationTime;
    public readonly double relaxationFactor;
    public readonly double c2;
    public LatticeGridNode[] latticeGrid { get; private set; }

    // Lattice constants
    public static readonly double[] eX = { 0, 1, 0, -1, 0, 1, -1, -1, 1 }; // x direction of distribution at index
    public static readonly double[] eY = { 0, 0, 1, 0, -1, 1, 1, -1, -1 }; // y direction of distribution at index
    public static readonly double[] weights = { 
        4.0 / 9.0, 
        1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 
        1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0 
    };
    public static readonly int[] bounceBack = { 0, 3, 4, 1, 2, 7, 8, 5, 6 }; // index of the opposite direction for each direction

    public LatticeGrid(int width, int height, double tau, double baseDensity, Nozzle nozzle)
    {
        gridWidth = width;
        gridHeight = height;
        relaxationTime = tau;
        relaxationFactor = 1.0 / tau;
        c2 = 1.0 / 3.0;

        this.nozzle = nozzle;

        //Initialize(baseDensity);
        InitializeRandom();
    }

    public bool IsPartOfNozzle(int x, int y)
    {
        return this.nozzle.IsOnNozzle(x, y);
        //return false;
    }

    public static bool IsOnNozzleShape(double throatRadius, double exitRadius, double expansionRatio, double x, double y)
    {
        double throatLength = throatRadius / Math.Sqrt(expansionRatio - 1);
        double exitLength = exitRadius / Math.Sqrt(expansionRatio - 1);

        double lengthRatio = exitLength / throatLength;
        double halfLengthRatio = Math.Sqrt(lengthRatio);

        double invHalfLengthRatio = 1.0 / halfLengthRatio;
        double halfExpansionRatio = Math.Sqrt(expansionRatio);

        double xThroat = 0;
        double yThroat = 0;
        double xExit = lengthRatio;
        double yExit = 0;

        if (x < xThroat || x > xExit)
        {
            return false;
        }

        double eta = Math.Sqrt(x / lengthRatio);
        double theta = 2 * Math.Atan(halfLengthRatio * Math.Tan(halfExpansionRatio * Math.Atan(eta / invHalfLengthRatio)) / invHalfLengthRatio);
        double yi = throatRadius * Math.Sin(theta);

        double tolerance = 2;

        return Math.Abs(y - yi) < tolerance;
    }

    public bool IsSolid(int x, int y)
    {
        return (latticeGrid[x + y * gridHeight] is SolidLatticeNode);
    }

    public void Initialize(double density)
    {
        latticeGrid = new LatticeGridNode[gridWidth * gridHeight];
        for(int x = 0; x < gridWidth; x++)
        {
            for(int y = 0; y < gridHeight; y++)
            {
                int index = x + y * gridHeight;
                if(IsPartOfNozzle(x, y))
                {
                    latticeGrid[index] = new SolidLatticeNode();
                }
                else
                {
                    latticeGrid[index] = new FluidLatticeNode(density);
                }
            }
        }
    }

    //public void InitializeLine()
    //{
    //    latticeGrid = new LatticeGridNode[gridWidth * gridHeight];

    //    for (int i = 0; i < gridWidth * gridHeight; i++)
    //    {
    //        if (i > 2500)
    //        {
    //            latticeGrid[i] = new LatticeGridNode(1.0, 0.0, 0.0);
    //        }
    //        else
    //        {
    //            latticeGrid[i] = new LatticeGridNode(0.5, 0.0, 0.0);
    //        }
    //    }
    //}

    private void InitializeRandom()
    {
        latticeGrid = new LatticeGridNode[gridWidth * gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int index = x + y * gridHeight;
                if(IsPartOfNozzle(x, y))
                {
                    latticeGrid[index] = new SolidLatticeNode();
                }
                else
                {
                    double r = (double)UnityEngine.Random.Range(0.5f, 1.0f);
                    latticeGrid[index] = new FluidLatticeNode(r);
                }
            }
        }
    }

    public void AddCylinder(int xPosition, int yPosition, int r, double density)
    {
        for(int x = 0; x < gridWidth; x++)
        {
            for(int y = 0; y < gridHeight; y++)
            {
                bool inCylinder = IsInCylinder(x, y, xPosition, yPosition, r);

                if (inCylinder)
                {
                    int index = x + y * gridHeight;
                    latticeGrid[index].AddDensity(density);
                }
            }
        }
    }

    public void MaintainCylinder(int xPosition, int yPosition, int r, double density)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                bool inCylinder = IsInCylinder(x, y, xPosition, yPosition, r);

                if (inCylinder)
                {
                    int index = x + y * gridHeight;
                    latticeGrid[index].SetDensity(density);
                }
            }
        }
    }
    public bool IsInCylinder(int x, int y, int cylinderX, int cylinderY, int r)
    {
        int dx = x - cylinderX;
        int dy = y - cylinderY;

        return dx * dx + dy * dy <= r * r;
    }

    public void Step()
    {
        this.CollisionStep();
        this.StreamingStep();
    }

    public void CollisionStep()
    {
        sumDensity = 0.0; // Debug value

        for (int x = 0; x < gridHeight; x++)
        {
            for (int y = 0; y < gridWidth; y++)
            {
                // Dont compute anything for solid nodes
                if (IsSolid(x, y))
                    continue;

                // Compute rho and velocity
                double rho = 0.0; // Sum of all distributions in a cell
                double velocityX = 0.0; // General velocity of the gas (X)
                double velocityY = 0.0; // General velocity of the gas (Y)

                int index = x + y * gridHeight;
                FluidLatticeNode node = (FluidLatticeNode)latticeGrid[index];

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
        //double feq = rho * weight * (1.0 + (3.0 * udotcx) + (4.5 * udotcx * udotcx) - (1.5 * (ux * ux + uy * uy) * c2)); // originele
        // Compute shader version (1 of 3)
        double feq = weight * rho * (1.0 + udotcx / c2 + 0.5 * (udotcx / c2) * (udotcx / c2) - (ux * ux + uy * uy) / (2.0 * c2)); // WERKT! niet altijd :(
        // Python
        //double feq = rho * weight * (
        //        1 + 3 * (ux * cx + uy * cy) + 9 * Math.Pow((ux * cx + uy * cy), 2) / 2 - 3 * (Math.Pow(ux, 2) + Math.Pow(uy, 2)) / 2
        //    );

        return feq;
    }

    public void StreamingStep()
    {
        // Create a temporary distribution grid to perform streaming
        LatticeGridNode[] tempGrid = new LatticeGridNode[gridWidth * gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int index = x + y * gridHeight;
                if(IsSolid(x, y))
                {
                    tempGrid[index] = latticeGrid[index]; // No need to create a new object, since a solid object does nothing anyway
                }
                else
                {
                    tempGrid[index] = new FluidLatticeNode();
                }
            }
        }

        // Perform streaming on all nodes
        for (int x = 0; x < gridHeight; x++)
        {
            for (int y = 0; y < gridWidth; y++)
            {
                // Dont stream solid cells
                if (IsSolid(x, y))
                    continue;

                // Calculate the index of the current node in the 1D grid
                int currentIndex = x + y * gridHeight;

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
                    int newIndex = newX + newY * gridHeight;
                    
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // BOUNCE-BACK WAARSCHIJNLIJK NIET GOED GEDAAN
                    if (IsSolid(newX, newY))
                    {
                        // Solid boundary bounce-back
                        int oppositeIndex = bounceBack[i];
                        (tempGrid[currentIndex] as FluidLatticeNode).distribution[oppositeIndex] = (latticeGrid[currentIndex] as FluidLatticeNode).distribution[i];
                        //newX = (newX + (int)eX[oppositeIndex] * 2) % gridWidth;
                        //newY = (newY + (int)eY[oppositeIndex] * 2) % gridHeight;

                        //newIndex = newX + newY * gridWidth;
                    }
                    // !!!!!!!!!!!!!!!!!
                    else // WAARSCHIJNLIJK NIET GOED!!!!!
                    {
                        // Stream the distribution to the new location
                        (tempGrid[newIndex] as FluidLatticeNode).distribution[i] = (latticeGrid[currentIndex] as FluidLatticeNode).distribution[i];
                    }
                    // !!!!!!!!!!!!!!!!
                    
                }

                // Copy the macroscopic variables to the new node
                (tempGrid[currentIndex] as FluidLatticeNode).density = (latticeGrid[currentIndex] as FluidLatticeNode).density;
                (tempGrid[currentIndex] as FluidLatticeNode).velocityX = (latticeGrid[currentIndex] as FluidLatticeNode).velocityX;
                (tempGrid[currentIndex] as FluidLatticeNode).velocityY = (latticeGrid[currentIndex] as FluidLatticeNode).velocityY;
            }
        }

        // Update the lattice grid with the streamed values
        for (int x = 0; x < gridHeight; x++)
        {
            for (int y = 0; y < gridWidth; y++)
            {
                // Dont stream solid cells
                if (IsSolid(x, y))
                    continue;
                int index = x + y * gridHeight;

                (latticeGrid[index] as FluidLatticeNode).distribution = (tempGrid[index] as FluidLatticeNode).distribution;
                (latticeGrid[index] as FluidLatticeNode).density = (tempGrid[index] as FluidLatticeNode).density;
                (latticeGrid[index] as FluidLatticeNode).velocityX = (tempGrid[index] as FluidLatticeNode).velocityX;
                (latticeGrid[index] as FluidLatticeNode).velocityY = (tempGrid[index] as FluidLatticeNode).velocityY;
            }
        }
    }

    public void UpdateDisplayTexture(int width, int height, ref Material outputMaterial)
    {
        Texture2D outputTexture = new Texture2D(width, height);

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                Color color;
                if (IsSolid(x, y))
                {
                    color = Color.black;
                }
                else
                {
                    int index = x + y * gridHeight;

                    double r = (latticeGrid[index] as FluidLatticeNode).density;
                    //double g = latticeGrid[index].velocityX;
                    //double b = latticeGrid[index].velocityY;

                    //Color color = new Color(10.0f / (float)r,
                    //    (0.5f + (float)g),
                    //    (0.5f + (float)b));
                    //Color color = new Color(1.0f / (float)r, 0, 0);
                    color = GenerateDensityColor(r);
                }

                outputTexture.SetPixel(x, y, color);
            }
        }

        //Debug.Log("testDensity: " + latticeGrid[40].density);

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

public class Nozzle
{
    public LatticeGrid grid;

    public int x;
    public int y;
    private int lineRadius; // radius of a solid point / line
    private int combustionChamberRadius;
    private int combustionChamberLength;
    private int throatRadius;
    private int convergeLength;
    //private Func<int, int> curve = x => 1;
    private Func<int, int> convergeFunction = x => -(x/3);
    private int divergeLength;
    private Func<int, int> divergeFunction = x => (int)Math.Sqrt(x);
    //private int exitRadius;

    public Nozzle(int x, int y, int lineRadius, int combustChamberRadius, int combustChamberLength, int throatRadius, int convergeLength, int divergeLength)
    {
        this.x = x;
        this.y = y;
        this.lineRadius = lineRadius;
        this.combustionChamberRadius = combustChamberRadius;
        this.combustionChamberLength = combustChamberLength;
        this.convergeLength = convergeLength;
        this.divergeLength = divergeLength;
        this.throatRadius = throatRadius;
        //this.exitRadius = exitRadius;
    }

    public bool IsOnNozzle(int x, int y)
    {
        int minX = this.x;
        int maxCombChamX = this.x + this.combustionChamberLength;
        int minCombChamY = this.y - this.combustionChamberRadius;
        int maxCombChamY = this.y + this.combustionChamberRadius;
        int maxConvX = maxCombChamX + this.convergeLength;
        int maxConvY = maxCombChamY + this.convergeFunction(this.convergeLength);
        int minConvY = minCombChamY - this.convergeFunction(this.convergeLength);
        int maxDivX = maxConvX + this.divergeLength;

        // Combustion chamber
        if (x < (minX - this.lineRadius))
            return false; // Point is behind nozzle and chamber

        // Check 2 horizontal lines
        if (IsOnHorizontalLine(x, y, minX, maxCombChamX, minCombChamY))
            return true;

        if (IsOnHorizontalLine(x, y, minX, maxCombChamX, maxCombChamY))
            return true;

        // Check if on the back of the combustion chamber
        if (IsOnVerticalLine(x, y, minX, maxCombChamY, minCombChamY))
            return true;

        // Converging
        if(x <= maxConvX && x > maxCombChamX)
        {
            int y_ = this.convergeFunction(x - maxCombChamX) ;
            if (Math.Abs(y - (y_ + maxCombChamY)) < this.lineRadius)
                return true;

            if (Math.Abs(y - (-y_ + minCombChamY)) < this.lineRadius)
                return true;
        }

        // Diverging
        if(x <= maxDivX && x > maxConvX)
        {
            int y_ = this.divergeFunction(x - maxConvX);
            if (Math.Abs(y - (y_ + maxConvY)) < this.lineRadius)
                return true;

            if (Math.Abs(y - (-y_ + minConvY)) < this.lineRadius)
                return true;

            //int diff = maxConvX - minConvY;

            //if (Math.Abs(y - (-y_ - 2 * diff)) < this.lineRadius)
            //    return true;
        }

        return false;
    }

    private bool IsOnHorizontalLine(int x, int y, int lineXLeft, int lineXRight, int lineY)
    {
        if(y < lineY + this.lineRadius && y > lineY - this.lineRadius)
        {
            if (x < lineXRight + this.lineRadius && x > lineXLeft - this.lineRadius)
                return true;
        }

        return false;
    }

    private bool IsOnVerticalLine(int x, int y, int lineX, int lineYHigh, int lineYLow)
    {
        if (x < lineX + this.lineRadius && x > lineX - this.lineRadius)
        {
            if (y < lineYHigh + this.lineRadius && y > lineYLow - this.lineRadius)
                return true;
        }

        return false;
    }

    public void UpdateCombustionChamber(double density)
    {
        int minX = this.x + this.lineRadius;
        int maxX = this.x + this.combustionChamberLength - this.lineRadius;
        int minY = this.y - this.combustionChamberRadius + this.lineRadius;
        int maxY = this.y + this.combustionChamberRadius - this.lineRadius;

        for(int x = minX; x < maxX; x++)
        {
            for(int y = minY; y < maxY; y++)
            {
                int index = x + y * this.grid.gridWidth;
                //this.grid.latticeGrid[index].SetDensity(density);
                this.grid.latticeGrid[index].AddDensityInDirection(density, 1);
                //this.grid.latticeGrid[index].AddDensity(density);
            }
        }
    }

    
}