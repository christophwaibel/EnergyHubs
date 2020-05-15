# Multi-Energy Systems Design Optimization Model for Adam Bufacchi's MSc Thesis 2020
Adam's MSc Thesis *&quot;Interactions of building- /urban-, and multi-energy systems design variables&quot;* aims to study interaction effects between building design parameters and energy systems parameters. In other words, how important is it that the demand and potential side (a.k.a. the architecture) needs to be coupled on par with the supply side (a.k.a. the energy hub)? Also, by coupling them, can we achieve low-carbon building / neighbourhood designs more efficiently? This thesis is on quantifying such interaction effects.

The repository here provides the Mixed Integer Linear Programming optimization model for the design of the multi-energy system for a building / neighbourhood (in short: Energyhub). The Energyhub is packaged as an `.exe` and needs `.csv` files that contain technology parameters as well as building information, including demand profiles. An arbitrary amount of samples (inputs) can be used and the program will iterate through all of them.

The Energyhub uses the Typical Days approach for dimension reduction. Technologies included are: 
- Natural Gas Boiler, 
- Biomass Boiler, 
- Air Source Heat Pump, 
- Combined Heat and Power, 
- Battery, 
- Thermal Energy Storage, 
- Air Chiller, and 
- Photovoltaic. 

If a sample represents a neighbourhood instead of a single building only, loads need to be aggregated beforehand. District heating network costs are then added to the model, as well as heat exchangers for each building of the aggregated neighbourhood. The district heating nework is simplifid and does not include losses or flow rates etc. Also, only one thermal temperature level is considered, which means space heating and domestic hot water are aggregated.

## How to use
Download and build the solution on your computer, or ask me to send you the program. Execute `AdamMSc2020.exe` and follow the instructions on the console:
- Enter the path to the inputs folder
- The program identifies all valid samples (see below in section **Inputs** for their format). Enter an integer number smaller or equals to that number.
- The program will now generate Energyhubs for each sample. It should take around 5 seconds per sample on an i7-8700K and 32GB RAM. The console will inform you on the iterations, i.e. how many Energyhubs still need to be run.
- The results are written into `<inputs-folder-path>\results\`, with 6 result files for each sample. Each of the 6 result files correspond to one &varepsilon;-cut, with &varepsilon;=0 being the carbon minimal solution and &varepsilon;=5 the cost minimal solution.
- Should something go wrong, the console will print the error exception message.

### Inputs
Per sample, the program needs a csv file pair:
1. `building_input_<index>.csv`
2. `technology_input_<index>.csv`

The index is required by the program to identify, which file pairs belong to each other.

Each sample can describe either a single building, or a neighbourhood of buildings. The parameter `NumberOfBuildingsInEHub` in the `technology_input_<index>.csv` needs to be set accordingly, 1 for single building, any integer number >1 for a neighbourhood. In latter case, `technology_input_<index>.csv` also needs parameters `Peak_B_<building-index>` that describe the peak total heating load (space heating + domestic hot water) of each building in kW. E.g. if `NumberOfBuildingsInEHub=2`, then there need to be parameters `Peak_B_1` and `Peak_B_2` in the `technology_input_<index>.csv`.

Furthermore, `technology_input_<index>.csv` needs information on:
- `TotalFloorArea` of the sample (single building or neighbourhood) in m²,
- `lca_Building` of the sample in kgCO2eq/year, which are the levelized embodied carbon emissions of the building construction.

The `building_input_<index>.csv` contains all energy demand profiles of the sample (as said earlier, if it is a neighbourhood, these need to be aggregated loads), global horizontal irradiation, ambient temperature, but also available areas for placing PV on, and the solar irradiance profiles for each of the surfaces. The program recognizes how many available surface areas you indicate and from that expects the equal amount of solar irradiance profiles. E.g. if you have 20 available areas, you also need to have 20 separate solar irradiance profiles. Follow the example input files that I have sent you for details on how to prepare the input files.

### Dependencies
- IBM CPLEX 12.8 full academic version
- (In some future: Clustering class in BB-O library, once `Clustering.cs` is moved over to that repo)

## MILP optimization model description
This energyhub uses Typical Days for dimension reduction. Model equations are given below.

### Objective Function
<img src="https://latex.codecogs.com/svg.latex?\min_x&space;f(x)">

### Constraints

**Storages - Batteries and TES**

![x_{\text{stor},t+1}^{\text{soc}}=(1-a^{\text{loss}}_{\text{stor}})x_{\text{stor},t}^{\text{soc}} + a_{\text{stor}}^{\text{ch}}x_{\text{stor},t}^{\text{ch}} + \frac{-1}{a_{\text{stor},t}^{\text{dis}}}x_{\text{stor},t}^{\text{dis}}](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2Ct%2B1%7D%5E%7B%5Ctext%7Bsoc%7D%7D%3D(1-a%5E%7B%5Ctext%7Bloss%7D%7D_%7B%5Ctext%7Bstor%7D%7D)x_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bsoc%7D%7D%20%2B%20a_%7B%5Ctext%7Bstor%7D%7D%5E%7B%5Ctext%7Bch%7D%7Dx_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bch%7D%7D%20%2B%20%5Cfrac%7B-1%7D%7Ba_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bdis%7D%7D%7Dx_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bdis%7D%7D)

![\forall t \in T](https://render.githubusercontent.com/render/math?math=%5Cforall%20t%20%5Cin%20T), except for ![x_{\text{stor},t=0}^{\text{soc}}](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2Ct%3D0%7D%5E%7B%5Ctext%7Bsoc%7D%7D), which equals to the delta Q computed from the last timestep instead
https://latex.codecogs.com/
