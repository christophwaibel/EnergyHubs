# MILP optimization model description
This program runs energyhubs for 1 or more samples (building or district). If a sample represents a district, loads need to be aggregated and a simplified district heating network with heat exchanger per building is added (no losses, no flowrate, nothing, just network length cost and heat exchanger costs).

This energyhub uses Typical Days for dimension reduction. Model equations are given below.

## Objective Function
<img src="https://latex.codecogs.com/svg.latex? min_x f(x)" title="cost function"/>

## Constraints

## Inputs
Per sample, the program needs a csv file pair:
1. `building_input_<index>.csv`
2. `technology_input_<index>.csv`

<img src="https://latex.codecogs.com/svg.latex?\Large&space;x=\frac{-b\pm\sqrt{b^2-4ac}}{2a}" title="\Large x=\frac{-b\pm\sqrt{b^2-4ac}}{2a}" />
