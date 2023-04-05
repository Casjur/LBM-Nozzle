using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


// `rho` is de "macroscopic density" van de vloeistof op elke cell
// `tau` is de "kinematic viscosity / timescale / relaxation time" van de vloeistof in het algemeen.
public partial class LatticeGrid
{
    public bool maximizeDistribution;

    public Nozzle nozzle;

    // Test variables
    public double sumDensity = 0.0;

    // LBM 
    public readonly int gridWidth;
    public readonly int gridHeight;
    public readonly double relaxationTime;
    public double relaxationFactor;
    public double c2;
    public LatticeGridNode[,] latticeGrid { get; private set; }

    // Lattice constants
    public static readonly double[] eX = { 0, 1, 0, -1, 0, 1, -1, -1, 1 }; // x direction of distribution at index
    public static readonly double[] eY = { 0, 0, 1, 0, -1, 1, 1, -1, -1 }; // y direction of distribution at index
    public static readonly double[] weights = {
        4.0 / 9.0,
        1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0,
        1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0
    };
    public static readonly int[] bounceBack = { 0, 3, 4, 1, 2, 7, 8, 5, 6 }; // index of the opposite direction for each direction

    public LatticeGrid(int width, int height, double tau, double c, double baseDensity, Nozzle nozzle)
    {
        gridWidth = width;
        gridHeight = height;
        relaxationTime = tau;
        relaxationFactor = 1.0 / tau;
        c2 = c * c; //0.000833333333333; //1.0 / 3.0;

        this.nozzle = nozzle;

        Initialize(baseDensity);
        //InitializeRandom();
    }

    public bool IsPartOfNozzle(int x, int y)
    {
        return this.nozzle.IsOnNozzle(x, y);
    }

    public bool IsSolid(int x, int y)
    {
        return (latticeGrid[x, y] is SolidLatticeNode);
    }

    public void Initialize(double density)
    {
        latticeGrid = new LatticeGridNode[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (IsPartOfNozzle(x, y))
                {
                    latticeGrid[x, y] = new SolidLatticeNode();
                }
                else
                {
                    latticeGrid[x, y] = new FluidLatticeNode(density);
                }
            }
        }
    }

    private void InitializeRandom()
    {
        latticeGrid = new LatticeGridNode[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (IsPartOfNozzle(x, y))
                {
                    latticeGrid[x, y] = new SolidLatticeNode();
                }
                else
                {
                    double r = (double)UnityEngine.Random.Range(0.5f, 1.0f);
                    latticeGrid[x, y] = new FluidLatticeNode(r);
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

        //Parallel.For()

        //for (int x = 0; x < gridWidth; x++)
        //{
        Parallel.For(0, gridWidth, x =>
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Dont compute anything for solid nodes
                if (IsSolid(x, y))
                    continue;

                // Compute rho and velocity
                double rho = 0.0; // Sum of all distributions in a cell
                double velocityX = 0.0; // General velocity of the gas (X)
                double velocityY = 0.0; // General velocity of the gas (Y)

                FluidLatticeNode node = (FluidLatticeNode)latticeGrid[x, y];

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
                    if (this.maximizeDistribution)
                        node.distribution[k] = Math.Max(0, node.distribution[k] - relaxationFactor * (node.distribution[k] - feq[k]));
                    else
                        node.distribution[k] = node.distribution[k] - relaxationFactor * (node.distribution[k] - feq[k]);
                }
            }
        });

        if(LBM.Iteration % LBM.EveryIterations == 0)
        {
            LBM.DataOuput.Write(sumDensity);
        }
        //Debug.Log("Collision Density: " + sumDensity);
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
        LatticeGridNode[,] tempGrid = new LatticeGridNode[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (IsSolid(x, y))
                {
                    tempGrid[x, y] = latticeGrid[x, y]; // No need to create a new object, since a solid object does nothing anyway
                }
                else
                {
                    tempGrid[x, y] = new FluidLatticeNode();
                }
            }
        }

        // Perform streaming on all nodes
        //for (int x = 0; x < gridWidth; x++)
        //{
        Parallel.For(0, gridWidth, x =>
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Dont stream solid cells
                if (IsSolid(x, y))
                    continue;

                // Stream the distributions to their new locations
                for (int i = 0; i < 9; i++)
                {
                    // Calculate the new x and y position of the distribution
                    int newX = x + (int)eX[i];
                    int newY = y + (int)eY[i];

                    // Periodic boundary conditions (if something leaves on the left, it returns on the right)

                    if (newX < 0 || newX >= gridWidth || newY < 0 || newY >= gridHeight)
                    {
                        continue;
                    }

                    //if (newX < 0)
                    //{
                    //    newX += gridWidth;
                    //}
                    //else if (newX >= gridWidth)
                    //{
                    //    newX -= gridWidth;
                    //}

                    //if (newY < 0)
                    //{
                    //    newY += gridHeight;
                    //}
                    //else if (newY >= gridHeight)
                    //{
                    //    newY -= gridHeight;
                    //}

                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // BOUNCE-BACK WAARSCHIJNLIJK NIET GOED GEDAAN
                    if (IsSolid(newX, newY))
                    {
                        // Solid boundary bounce-back
                        int oppositeIndex = bounceBack[i];
                        (tempGrid[x, y] as FluidLatticeNode).distribution[oppositeIndex] = (latticeGrid[x, y] as FluidLatticeNode).distribution[i];
                    }
                    // !!!!!!!!!!!!!!!!!
                    else // WAARSCHIJNLIJK NIET GOED!!!!!
                    {

                        // Stream the distribution to the new location
                        (tempGrid[newX, newY] as FluidLatticeNode).distribution[i] = (latticeGrid[x, y] as FluidLatticeNode).distribution[i];

                    }
                    // !!!!!!!!!!!!!!!!

                }

                // Copy the macroscopic variables to the new node
                (tempGrid[x, y] as FluidLatticeNode).density = (latticeGrid[x, y] as FluidLatticeNode).density;
                (tempGrid[x, y] as FluidLatticeNode).velocityX = (latticeGrid[x, y] as FluidLatticeNode).velocityX;
                (tempGrid[x, y] as FluidLatticeNode).velocityY = (latticeGrid[x, y] as FluidLatticeNode).velocityY;
            }
        });

        // Update the lattice grid with the streamed values
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Dont stream solid cells
                if (IsSolid(x, y))
                    continue;

                (latticeGrid[x, y] as FluidLatticeNode).distribution = (tempGrid[x, y] as FluidLatticeNode).distribution;
                (latticeGrid[x, y] as FluidLatticeNode).density = (tempGrid[x, y] as FluidLatticeNode).density;
                (latticeGrid[x, y] as FluidLatticeNode).velocityX = (tempGrid[x, y] as FluidLatticeNode).velocityX;
                (latticeGrid[x, y] as FluidLatticeNode).velocityY = (tempGrid[x, y] as FluidLatticeNode).velocityY;
            }
        }
    }
}
