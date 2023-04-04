using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Problemen:
// 1. Extreem instabiel voor sommige densities. Debug log parameters, wanneer extreme waardes / verschil in waardes gemaakt worden
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
// 2. Thrust waardes lijken precies verkeerd om (wanneer er een rechte straal uit de nozzle schiet is de thrust lager, dan bij een uitgespreide straal)

// Todo:
// 1. Variabele zoals de afstand tussen cells in (dx), de velocity tussen 2 cells in (c), en viscosity toevoegen
// 4. !Thrust meter toevoegen!


public class LBM : MonoBehaviour
{
    public bool maximizeDistribution = true;

    public const int WIDTH = 512; //256; // Moeten hetzelfde zijn (index wordt verkeerd berekend)
    public const int HEIGHT = 256;
    //public const float VISCOSITY = 1.0f; // !!! Wordt nergens gebruikt !!!
    //public const double viscosity = 0.000017; // Lucht op 20km hoogte
    public double RELAXATION_TIME = 8.0; //1.0 // Min: 0.53 Max: geen? //(2 * viscosity + 1 / 2) / (Math.Pow(c2, 2));

    public const double baseDensity = 1.0;

    public LatticeGrid Grid;

    public Nozzle nozzle;
    public double nozzleDensity = 0.2;

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
        this.Grid.maximizeDistribution = this.maximizeDistribution;
        this.nozzle.grid = this.Grid;
        //this.Grid.AddCylinder(40, 40, 30, 1.0);
    }

    void Update()
    {
        //this.Grid.AddCylinder(80, 80, 30, 0.5);
        //this.Grid.AddCylinder(40, 40, 30, .01);
        //this.Grid.MaintainCylinder(40, 40, 30, 1.0);

        // Calculations
        this.Grid.maximizeDistribution = this.maximizeDistribution;
        this.Grid.relaxationFactor = 1.0 / this.RELAXATION_TIME;
        this.nozzle.UpdateCombustionChamber(nozzleDensity);
        this.Grid.Step();

        // Display
        double thrust = this.Grid.CalculateThrust();
        Debug.Log("Thrust: " + thrust);
        this.Grid.UpdateDisplayTexture(WIDTH, HEIGHT, ref outputMaterial);
    }
}
