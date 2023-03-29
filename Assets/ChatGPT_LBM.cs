using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatGPT_LBM : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

