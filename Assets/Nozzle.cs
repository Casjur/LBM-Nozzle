using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nozzle
{
    public LatticeGrid grid;

    // Coordinates of the nozzle (on the back of the pressure chamber)
    public readonly int x;
    public readonly int y;

    // Most outer point of the nozzle, where thrust is measured
    public readonly int outletX;
    public readonly int outletYHigh;
    public readonly int outletYLow;

    public readonly int lineRadius; // radius of a solid point / line
    public readonly int combustionChamberRadius;
    public readonly int combustionChamberLength;
    private readonly int throatRadius;
    private readonly int convergeLength;
    //private Func<int, int> curve = x => 1;
    private readonly Func<int, int> convergeFunction = x => -x; //-(int)Math.Sqrt(x); //-(x / 3);
    private readonly int divergeLength;
    private readonly Func<int, int> divergeFunction = x => x; //(int)Math.Sqrt(x); //x; //
    //private int exitRadius;

    private int minX;
    public readonly int maxCombChamX;
    public readonly int minCombChamY;
    public readonly int maxCombChamY;
    private int maxConvX;
    private int maxConvY;
    private int minConvY;
    private int maxDivX;

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

        this.minX = x;
        this.maxCombChamX = x + combustChamberLength;
        this.minCombChamY = y - combustChamberRadius;// + lineRadius;
        this.maxCombChamY = y + combustChamberRadius;// - lineRadius;
        this.maxConvX = maxCombChamX + convergeLength;
        this.maxConvY = maxCombChamY + convergeFunction(convergeLength);
        this.minConvY = minCombChamY - convergeFunction(convergeLength);
        this.maxDivX = maxConvX + divergeLength;

        this.outletX = x + combustChamberLength + convergeLength + divergeLength;
        this.outletYHigh = divergeFunction(divergeLength) + maxConvY;
        this.outletYLow = -divergeFunction(divergeLength) + minConvY;
    }

    public bool IsOnNozzle(int x, int y)
    {  
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
        }

        return false;
    }

    // Checks if a point (x, y) is on a vertical line (lineX, lineYHigh, lineYLow)
    private bool IsOnHorizontalLine(int x, int y, int lineXLeft, int lineXRight, int lineY)
    {
        if (y < lineY + this.lineRadius && y > lineY - this.lineRadius)
        {
            if (x < lineXRight + this.lineRadius && x > lineXLeft - this.lineRadius)
                return true;
        }

        return false;
    }

    // Checks if a point (x, y) is on a vertical line (lineX, lineYHigh, lineYLow)
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
        //int maxX = this.x + this.combustionChamberLength - this.lineRadius;
        //int minY = this.y - this.combustionChamberRadius + this.lineRadius;
        //int maxY = this.y + this.combustionChamberRadius - this.lineRadius;

        for (int x = minX; x < this.maxCombChamX; x++)
        {
            for (int y = this.minCombChamY + this.lineRadius; y <= this.maxCombChamY - this.lineRadius; y++)
            {
                //this.grid.latticeGrid[x, y].SetDensity(density);
                //this.grid.latticeGrid[x, y].AddDensityInDirection(density, 1);
                this.grid.latticeGrid[x, y].AddDensity(density);
            }
        }
    }


}
