using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface LatticeGridNode
{
    public double GetDensity();
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

    public double GetDensity()
    {
        return this.density;
    }


    public void SetDensity(double density)
    {
        for (int i = 0; i < 9; i++)
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
    public double GetDensity()
    {
        return 0.0;
    }

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

