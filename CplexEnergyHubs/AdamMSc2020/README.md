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

Each sample can describe either a single building, or a neighbourhood of buildings. The parameter `NumberOfBuildingsInEHub` in the `technology_input_<index>.csv` needs to be set accordingly, 1 for single building, any integer number >1 for a neighbourhood. In latter case, `technology_input_<index>.csv` also needs parameters `Peak_B_<building-index>` that describe the peak total heating load (space heating + domestic hot water) of each building in kW. E.g. if `NumberOfBuildingsInEHub=2`, then there need to be parameters `Peak_B_1` and `Peak_B_2` in the `technology_input_<index>.csv`. The peak heating load is required to size the heat exchanger between a building and the district heating network.

Furthermore, `technology_input_<index>.csv` needs information on:
- `TotalFloorArea` of the sample (single building or neighbourhood) in m²,
- `lca_Building` of the sample in kgCO2eq/year, which are the levelized embodied carbon emissions of the building construction.
- `GridLengthDistrictHeating` as a fractional number from 0 - 1. It assumes that the maximal (i.e. value of 1 for this parameter) district heating network length is 500 m per building in the district. With a value of 0.1, it means the network length is 50 m per building. So with 2 buildings and factor 0.1, we have a network length of 100 m.

The `building_input_<index>.csv` contains all energy demand profiles of the sample (as said earlier, if it is a neighbourhood, these need to be aggregated loads), global horizontal irradiation, ambient temperature, but also available areas for placing PV on, and the solar irradiance profiles for each of the surfaces. The program recognizes how many available surface areas you indicate and from that expects the equal amount of solar irradiance profiles. E.g. if you have 20 available areas, you also need to have 20 separate solar irradiance profiles. Follow the example input files that I have sent you for details on how to prepare the input files.

### Dependencies
- IBM CPLEX 12.8 full academic version
- (In some future: Clustering class in BB-O library, once `Clustering.cs` is moved over to that repo)

## MILP optimization model description
Note that this energyhub uses Typical Days for dimension reduction, therefore we cannot easily use seasonal storages. Methods that allow seasonal storages with typical days exist, but are out of scope for this project. 

The formulations are based on [Waibel et al 2019](https://doi.org/10.1016/j.apenergy.2019.03.177), [Mavromatidis 2018](https://doi.org/10.3929/ethz-b-000182697) and [Dominguez-Munoz et al 2011](https://doi.org/10.1016/j.enbuild.2011.07.024).

### Objective Functions

The cost and carbon objective functions are given with:
$$\min (\sum_{i=1}^{N} c_{i} x_{i} + \sum_{i=1}^{N} \sum_{t=1}^{T} k_t c_{i,t}^{\text{op}} x_{i,t}^{\text{op}}) ,$$

where the cost coefficients either represent monetary cost for investment and operation, or embodied and operational carbon emissions. The cost coefficients themselves might include conversion efficiencies, depending on technology. $k_t$ is a scaling factor that depends on the typical day and it ensures that the costs of a certain day are weighed according to the size of the cluster that this day belongs to.

Carbon emissions and investment cost of the district heating network and the heat exchangers are added to the objective function as constants, since they are not impacted by the MILP optimization.

### Constraints
In the following, the system constraints are described. $\forall t \in T$, if not stated otherwise.

**Energy Balance**

The general energy balances ensure that demand is met at all timesteps:

$$\sum_{i=1}^{N^{\text{demand}}} a_{i,t}^{\text{demand}} x_{i,t}^{\text{op,demand}} = d_t^{\text{demand}} , \forall \text{demand} \in \{\text{heat, cool, elec} \}.$$

$N^{\text{demand}}$ is the set of energy technologies providing energy of a certain demand type.

**Sizing of Technologies**

The general sizing constraint for any energy technology states that the operation at any timestep cannot exceed the capacity of the technology (in case of storages, the state of charge cannot exceed the capacity):

$$x_{\text{tech},t}^{\text{op}} \leq x_{\text{tech}} .$$


**Storages: Batteries and TES**

The storage energy balance is given with:

$$x_{\text{stor},t+1}^{\text{soc}}=(1-a^{\text{loss}}_{\text{stor}})x_{\text{stor},t}^{\text{soc}} + a_{\text{stor}}^{\text{ch}}x_{\text{stor},t}^{\text{ch}} + \frac{-1}{a_{\text{stor},t}^{\text{dis}}}x_{\text{stor},t}^{\text{dis}} ,$$

and $\forall t \in T$, except for $x_{\text{stor},t=0}^{\text{soc}}$, which equals to the $\Delta Q$ computed from the last timestep $t=\text{End of horizon}$ instead.

Since we are using typical days, we need to decouple days from each other (no seasonal storage possible):

$$x_{\text{stor}, t}^{\text{soc}} = x_{\text{stor}, t-24}^{\text{soc}}, \forall t \in T_{1}$$

...where $T_{1}$ denotes the set of timesteps that correspond to the first hour of each day. This constraint enforces the state of charge (SOC) of a storage to start at the same level on each day, therefore effectively disabling seasonal storage. A drawback however is that the initial SOC at t=0 might be nonzero, i.e. the storage starts non-empty (free energy!). However, the optimization should not start with a full storage, because then it needs to end with a full storage each day as well, therefore limiting its actual load-shifting function.

Furthermore, we do not allow discharging and charging of storages from one day to another, i.e. $\forall t \in T_{24}$, $T_{24}$ denoting the last hour of each day:

$$x_{\text{stor}, t}^{\text{ch}} = 0,$$

$$x_{\text{stor}, t}^{\text{dis}} = 0.$$

The storage charging and discharging constraints are given with:

$$x_{\text{stor}, t}^{ch} \leq b_{\text{stor}}^{\text{maxch}} x_{\text{stor}} ,$$

$$x_{\text{stor}, t}^{dis} \leq b_{\text{stor}}^{\text{maxdis}} x_{\text{stor}} .$$

Additionally, batteries should not be discharged below a certain SOC:

$$x_{\text{battery},t}^{\text{soc}} \geq b_{\text{battery}}^{\text{minsoc}} x_{\text{battery}} .$$

For thermal energy storages, an artifact caused simultaneous charging and discharging. $\Delta Q$ was always zero due to the energy balance of the storage, but to avoid confusion when analysing results, following constraints have been added to avoid this artifact:

$$x_{\text{tes},t}^{\text{ch}} \leq M y_{\text{tes},t} ,$$

$$x_{\text{tes},t}^{\text{dis}} \leq M (1 - y_{\text{tes},t}) .$$


**Available Biomass**

Following constraint limits the yearly available biomass:

$$x_{\text{biomass}}^{\text{total}} \leq b_{\text{biomass}}^{\text{max}} ,$$

where:

$$x_{\text{biomass}}^{\text{total}} \coloneqq \sum_{t=1}^{T} (\frac{k_t}{a_{\text{bio},t}} x_{\text{bio},t}^{\text{op}}) .$$

**Other remarks**

- Maximal storage capacities are limited as functions of the total floor area. 
- Air Chiller is implemented as variable, even though, because it is the only cooling technology, there is no degree of freedom in the optimization. This is to keep the model flexible for future cooling technology additions
- for technology details, see [Waibel et al 2019](https://doi.org/10.1016/j.apenergy.2019.03.177).
- I used [this web app](https://alexanderrodin.com/github-latex-markdown/) to convert latex equations into markdown compatible syntax
