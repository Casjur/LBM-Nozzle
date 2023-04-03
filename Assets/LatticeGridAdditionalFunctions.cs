using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class LatticeGrid
{
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
                    color = Color.black;
                }
                else
                {
                    double d = (latticeGrid[x, y] as FluidLatticeNode).density;
                    //double vx = (latticeGrid[x, y] as FluidLatticeNode).velocityX;
                    //double vy = (latticeGrid[x, y] as FluidLatticeNode).velocityY;

                    //Color color = new Color(10.0f / (float)r,
                    //    (0.5f + (float)g),
                    //    (0.5f + (float)b));
                    //Color color = new Color(1.0f / (float)r, 0, 0);
                    color = GenerateDensityColor(d);
                    //color = GenerateVelocityColor(vx, vy);
                }

                outputTexture.SetPixel(x, y, color);
            }
        }

        //Debug.Log("testDensity: " + latticeGrid[40].density);

        outputTexture.Apply();

        outputMaterial.mainTexture = outputTexture;
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
        float r = (float)density / 2.0f;
        float g = r / 2.0f;
        float b = g / 2.0f;

        return new Color(r, g, b);
    }
}
