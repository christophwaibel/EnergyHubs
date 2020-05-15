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
Note that this energyhub uses Typical Days for dimension reduction, therefore we cannot use seasonal storages.

### Objective Function
<img src="https://latex.codecogs.com/svg.latex?\min_x&space;f(x)">

### Constraints
In the following, the technical system constraints are described. ![\forall t \in T](https://render.githubusercontent.com/render/math?math=%5Cforall%20t%20%5Cin%20T), if not stated otherwise.

The general sizing constraint for any energy technology states that the operation at any timestep cannot exceed the capacity of the technology (in case of storages, the state of charge cannot exceed the capacity):

![x_{\text{tech},t}^{\text{op}} \leq x_{\text{tech}} .](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Btech%7D%2Ct%7D%5E%7B%5Ctext%7Bop%7D%7D%20%5Cleq%20x_%7B%5Ctext%7Btech%7D%7D%20.)

**Energy Balance**

The general energy balances ensure that demand is met at all timesteps:

![\sum_{i=1}^{N^{\text{demand}}} a_{i,t}^{\text{demand}} x_{i,t}^{\text{op,demand}} = d_t^{\text{demand}} , \forall \text{demand} \in \{\text{heat, cool, elec} \}.](https://render.githubusercontent.com/render/math?math=%5Csum_%7Bi%3D1%7D%5E%7BN%5E%7B%5Ctext%7Bdemand%7D%7D%7D%20a_%7Bi%2Ct%7D%5E%7B%5Ctext%7Bdemand%7D%7D%20x_%7Bi%2Ct%7D%5E%7B%5Ctext%7Bop%2Cdemand%7D%7D%20%3D%20d_t%5E%7B%5Ctext%7Bdemand%7D%7D%20%2C%20%5Cforall%20%5Ctext%7Bdemand%7D%20%5Cin%20%5C%7B%5Ctext%7Bheat%2C%20cool%2C%20elec%7D%20%5C%7D.)

![N^{\text{demand}}](https://render.githubusercontent.com/render/math?math=N%5E%7B%5Ctext%7Bdemand%7D%7D) is the set of energy technologies providing energy of a certain demand type.

**Storages: Batteries and TES**

The storage energy balance is given with:

![x_{\text{stor},t+1}^{\text{soc}}=(1-a^{\text{loss}}_{\text{stor}})x_{\text{stor},t}^{\text{soc}} + a_{\text{stor}}^{\text{ch}}x_{\text{stor},t}^{\text{ch}} + \frac{-1}{a_{\text{stor},t}^{\text{dis}}}x_{\text{stor},t}^{\text{dis}},](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2Ct%2B1%7D%5E%7B%5Ctext%7Bsoc%7D%7D%3D(1-a%5E%7B%5Ctext%7Bloss%7D%7D_%7B%5Ctext%7Bstor%7D%7D)x_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bsoc%7D%7D%20%2B%20a_%7B%5Ctext%7Bstor%7D%7D%5E%7B%5Ctext%7Bch%7D%7Dx_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bch%7D%7D%20%2B%20%5Cfrac%7B-1%7D%7Ba_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bdis%7D%7D%7Dx_%7B%5Ctext%7Bstor%7D%2Ct%7D%5E%7B%5Ctext%7Bdis%7D%7D%2C)

...![\forall t \in T](https://render.githubusercontent.com/render/math?math=%5Cforall%20t%20%5Cin%20T), except for ![x_{\text{stor},t=0}^{\text{soc}}](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2Ct%3D0%7D%5E%7B%5Ctext%7Bsoc%7D%7D), which equals to the ![\Delta Q](https://render.githubusercontent.com/render/math?math=%5CDelta%20Q) computed from the last timestep ![t=\text{End of horizon}](https://render.githubusercontent.com/render/math?math=t%3D%5Ctext%7BEnd%20of%20horizon%7D) instead.

Since we are using typical days, we need to decouple days from each other (no seasonal storage possible):

![x_{\text{stor}, t}^{\text{soc}} = x_{\text{stor}, t-24}^{\text{soc}}, \forall t \in T_{1}](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2C%20t%7D%5E%7B%5Ctext%7Bsoc%7D%7D%20%3D%20x_%7B%5Ctext%7Bstor%7D%2C%20t-24%7D%5E%7B%5Ctext%7Bsoc%7D%7D%2C%20%5Cforall%20t%20%5Cin%20T_%7B1%7D)

...where ![T_{1}](https://render.githubusercontent.com/render/math?math=T_%7B1%7D) denotes the set of timesteps that correspond to the first hour of each day. This constraint enforces the state of charge (SOC) of a storage to start at the same level on each day, therefore effectively disabling seasonal storage. A drawback however is that the initial SOC at t=0 might be nonzero, i.e. the storage starts non-empty (free energy!). However, the optimization should not start with a full storage, because then it needs to end with a full storage each day as well, therefore limiting its actual load-shifting function.

Furthermore, we do not allow discharging and charging of storages from one day to another, i.e. ![\forall t \in T_{24}](https://render.githubusercontent.com/render/math?math=%5Cforall%20t%20%5Cin%20T_%7B24%7D), ![T_{24}](https://render.githubusercontent.com/render/math?math=T_%7B24%7D) denoting the last hour of each day:

![x_{\text{stor}, t}^{\text{ch}} = 0,](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2C%20t%7D%5E%7B%5Ctext%7Bch%7D%7D%20%3D%200%2C)

![x_{\text{stor}, t}^{\text{dis}} = 0.](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2C%20t%7D%5E%7B%5Ctext%7Bdis%7D%7D%20%3D%200.)

The storage charging and discharging constraints are given with:

![x_{\text{stor}, t}^{ch} \leq b_{\text{stor}}^{\text{maxch}} x_{\text{stor}} ,](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2C%20t%7D%5E%7Bch%7D%20%5Cleq%20b_%7B%5Ctext%7Bstor%7D%7D%5E%7B%5Ctext%7Bmaxch%7D%7D%20x_%7B%5Ctext%7Bstor%7D%7D%20%2C)

![x_{\text{stor}, t}^{dis} \leq b_{\text{stor}}^{\text{maxdis}} x_{\text{stor}} .](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bstor%7D%2C%20t%7D%5E%7Bdis%7D%20%5Cleq%20b_%7B%5Ctext%7Bstor%7D%7D%5E%7B%5Ctext%7Bmaxdis%7D%7D%20x_%7B%5Ctext%7Bstor%7D%7D%20.)

Additionally, batteries should not be discharged below a certain SOC:

![x_{\text{battery},t}^{\text{soc}} \geq b_{\text{battery}}^{\text{minsoc}} x_{\text{battery}} .](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bbattery%7D%2Ct%7D%5E%7B%5Ctext%7Bsoc%7D%7D%20%5Cgeq%20b_%7B%5Ctext%7Bbattery%7D%7D%5E%7B%5Ctext%7Bminsoc%7D%7D%20x_%7B%5Ctext%7Bbattery%7D%7D%20.)

For thermal energy storages, an artifact caused simultaneous charging and discharging. ![\Delta Q](https://render.githubusercontent.com/render/math?math=%5CDelta%20Q) was always zero due to the energy balance of the storage, but to avoid confusion when analysing results, following constraints have been added to avoid this artifact:

![x_{\text{tes},t}^{\text{ch}} \leq M y_{\text{tes},t} ,](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Btes%7D%2Ct%7D%5E%7B%5Ctext%7Bch%7D%7D%20%5Cleq%20M%20y_%7B%5Ctext%7Btes%7D%2Ct%7D%20%2C)

![x_{\text{tes},t}^{\text{dis}} \leq M (1 - y_{\text{tes},t}) .](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Btes%7D%2Ct%7D%5E%7B%5Ctext%7Bdis%7D%7D%20%5Cleq%20M%20(1%20-%20y_%7B%5Ctext%7Btes%7D%2Ct%7D)%20.)

