# Multi-Energy Systems Design Optimization Model for Adam Bufacchi's MSc Thesis 2020
This program runs energyhubs for 1 or more samples (building or district). If a sample represents a district, loads need to be aggregated and a simplified district heating network with heat exchanger per building is added (no losses, no flowrate, nothing, just network length cost and heat exchanger costs).

## How to use
Run the `.exe`, give path to the inputs, wait. Results are written into `<input-path>\results\`, 5 result files for each input pair. Why 5? Because 5 &epsilon; cuts.

### Inputs
Per sample, the program needs a csv file pair:
1. `building_input_<index>.csv`
2. `technology_input_<index>.csv`



## MILP optimization model description
This energyhub uses Typical Days for dimension reduction. Model equations are given below.

### Objective Function
<img src="https://latex.codecogs.com/svg.latex?\min_x&space;f(x)">

### Constraints


https://latex.codecogs.com/
