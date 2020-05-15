# Multi-Energy Systems Design Optimization Model for Adam Bufacchi's MSc Thesis 2020
Adam's MSc Thesis with the title *&quot;Interactions of building- /urban-, and multi-energy systems design variables&quot;* aims to study interaction effects between building design parameters and energy systems parameters. In other words, how important is it that the demand and potential side (a.k.a. the architecture) needs to be coupled with the supply side (a.k.a. the energy hub) in order to achieve efficient low-carbon solutions?

This program runs energyhubs for 1 or more samples (building or district). If a sample represents a district, loads need to be aggregated and a simplified district heating network with heat exchanger per building is added (no losses, no flowrate, nothing, just network length cost and heat exchanger costs).

## How to use
Download and build the solution on your computer, or ask me to send you the program. Execute `AdamMSc2020.exe` and follow the instructions on the console:
- Enter the path to the inputs folder
- The program identifies all valid samples (see below in section **Inputs** for their format). Enter an integer number smaller or equals to that number.
- The program will now generate Energyhubs for each sample. It should take around 5 seconds per sample on an i7-8700K and 32GB RAM. The console will inform you on the iterations, i.e. how many Energyhubs still need to be run.
- The results are written into `<inputs-folder-path>\results\`, with 5 result files for each sample. Each of the 5 result files correspond to one &varepsilon;-cut, with &varepsilon;=0 being the carbon minimal solution and &varepsilon;=5 the cost minimal solution.
- Should something go wrong, the console will print the error exception message.



### Inputs
Per sample, the program needs a csv file pair:
1. `building_input_<index>.csv`
2. `technology_input_<index>.csv`

### Dependencies
- IBM CPLEX 12.6.1 full academic version
- (Clustering class in BB-O library, once `Clustering.cs` is moved over to that repo)

## MILP optimization model description
This energyhub uses Typical Days for dimension reduction. Model equations are given below.

### Objective Function
<img src="https://latex.codecogs.com/svg.latex?\min_x&space;f(x)">

### Constraints


https://latex.codecogs.com/
