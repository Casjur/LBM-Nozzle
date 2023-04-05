using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class LatticeGrid
{
    public double CalculateThrust()
    {
        double thrust = 0.0;

        int xRadius = 3;
        
        int outX = this.nozzle.outletX;
        int outYHigh = this.nozzle.outletYHigh;
        int outYLow = this.nozzle.outletYLow;

        //for(int x = outX - xRadius; x < outX + xRadius; x++)
        //{
            for(int y = outYLow + 2; y < outYHigh - 2; y++)
            {
                if(!IsSolid(outX,y))
                {
                    FluidLatticeNode node = (FluidLatticeNode)this.latticeGrid[outX, y];
                    //double vn = ((node.velocityX / node.density) * eX[1]) + ((node.velocityY / node.density) * eY[1]);

                    //double momentumFlux = node.density * vn;

                    //thrust += momentumFlux;

                    thrust += node.velocityX * node.density;
                }
            }
        //}

        int outletArea = Math.Abs((outYHigh - 2) - (outYLow + 2)); //(xRadius * 2) * Math.Abs(outYHigh - outYLow);

        return thrust; /// (double)outletArea;
    }

    public void MaintainAirflow(double density, int directionIndex)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            this.latticeGrid[0, y].SetDensity(density);
        }
    }

    public void AddCylinder(int xPosition, int yPosition, int r, double density)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                bool inCylinder = IsInCylinder(x, y, xPosition, yPosition, r);

                if (inCylinder)
                {
                    latticeGrid[x, y].AddDensity(density);
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
                    latticeGrid[x, y].SetDensity(density);
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

    public void UpdateDisplayTexture(int width, int height, ref Material outputMaterial)
    {
        Texture2D outputTexture = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color;
                if (IsSolid(x, y))
                {
                    color = Color.gray;
                }
                else
                {
                    double d = (latticeGrid[x, y] as FluidLatticeNode).density;
                    double vx = (latticeGrid[x, y] as FluidLatticeNode).velocityX;
                    double vy = (latticeGrid[x, y] as FluidLatticeNode).velocityY;

                    //color = GenerateDensVeloColor(d, vx, vy);
                    
                    //color = new Color(1.0f / (float)r, 0, 0);
                    color = GenerateDensityColor(d);
                    //color = GenerateVelocityColor(vx, vy);

                    // Thrust measuring boundary
                    //if (x > this.nozzle.outletX - 3 && x < this.nozzle.outletX + 3 && y > this.nozzle.outletYLow && y < this.nozzle.outletYHigh)
                    //{
                    //    color += Color.green;
                    //}
                    if (x == this.nozzle.outletX && y > this.nozzle.outletYLow + 2 && y < this.nozzle.outletYHigh - 2)
                    {
                        color += Color.green;
                    }
                }

                outputTexture.SetPixel(x, y, color);
            }
        }

        //Debug.Log("testDensity: " + latticeGrid[40].density);

        outputTexture.Apply();

        outputMaterial.mainTexture = outputTexture;
    }

    public Color GenerateDensVeloColor(double d, double vx, double vy)
    {
        return new Color((float)d / 10.0f,
                        (Math.Abs((float)vx) / 3.0f),
                        (Math.Abs((float)vy)) / 3.0f);
    }

    public Color GenerateVelocityColor(double vx, double vy)
    {
        float r = (float)vx * 20.0f;
        float g = 1.0f;//r / 2.0f;
        float b = (float)vy;

        return new Color(r, g, b);
    }

    public Color GenerateDensityColor(double density)
    {
        float r = (float)density / 20.0f;
        float g = r / 2.0f;
        float b = g / 2.0f;

        return new Color(r, g, b);
    }
}
