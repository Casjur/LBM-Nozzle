using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private Func<int, int> convergeFunction = x => -(x / 3);
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
        if (x <= maxConvX && x > maxCombChamX)
        {
            int y_ = this.convergeFunction(x - maxCombChamX);
            if (Math.Abs(y - (y_ + maxCombChamY)) < this.lineRadius)
                return true;

            if (Math.Abs(y - (-y_ + minCombChamY)) < this.lineRadius)
                return true;
        }

        // Diverging
        if (x <= maxDivX && x > maxConvX)
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
        if (y < lineY + this.lineRadius && y > lineY - this.lineRadius)
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

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                //this.grid.latticeGrid[index].SetDensity(density);
                this.grid.latticeGrid[x, y].AddDensityInDirection(density, 1);
                //this.grid.latticeGrid[index].AddDensity(density);
            }
        }
    }


}
