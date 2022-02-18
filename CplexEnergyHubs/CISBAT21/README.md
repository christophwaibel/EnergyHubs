# Multi-Energy Systems Design Optimization Model for CISBAT 21 Study

CISBAT 2021 conference paper *&quot;Impact of demand response on BIPV and district multi-energy systems design in Singapore and Switzerland&quot;* aims to study the impact of demand response (DR) policy (heating, cooling, electricity) on the design of BIPV systems. In other words, does DR change the feasibility of BIPV sizing, e.g. does it make it more or less attractive? Scenarios considered are Singapore and Switzerland (Suurstoffi), current and future climate. Geometry is from Suurstoffi. Demand modelled in CEA. Solar Potentials modelled in GH_Solar.

The repository here provides the Mixed Integer Linear Programming optimization model for the design of the multi-energy system for a building / neighbourhood (in short: Energyhub). The Energyhub is packaged as an `.exe` and needs several `.csv` files that describe technology parameters, building loads, solar potentials and climate data.

The Energyhub uses the Typical Days approach for dimension reduction. Technologies included are: 
- Natural Gas Boiler, 
- Biomass Boiler, 
- Air Source Heat Pump, 
- Combined Heat and Power, 
- Battery, 
- Thermal Energy Storage,
- Cool storage 
- Electric Chiller, and 
- Photovoltaic. 

District heating and cooling is used, requiring the installation of heat exchangers (capacity based on peak loads per building) for the heating and cooling network respectively. The neworks are simplifid and do not include losses or flow rates etc. Also, only one thermal temperature level is considered, which means space heating and domestic hot water are aggregated.

## How to use
There are 3 modes of the .exe program `Cisbat21.exe`: 0, 1, 2:

In the console, switch between modes:
- 0 = run CISBAT2021 energy hub. You will further be asked, which scenario to run: Singapore, Zurich, current climate or future climate. Results are written into a results folder0
- 1 = (postprocessing for CISBAT21) write annual solar potentials of 4 sensor points for each of the original 193 surfaces into a csv file. That results in 772 actual PV surfaces (193 surfaces are basically split into 4 each). Each column of the csv has: 1st row: SP ID; 2nd row: kWh/m2a ; 3rd row: surface area in m2
- 2 = (For SBE 22 paper with Yufei Zhang). Runs the same CISBAT21 energy hub, but multiple times, using stochastich solar profiles. Building loads and other inputs remain deterministic

**For mode 2 (SBE22 paper):**

**Before running the program**

Replace files in "\\input_stochastic" with your stochastic solar profiles. Use the same naming convention as the example files provided there (`Risch_2020_solar_scenario_<index>'` . So if you have generated 40 stochastic scenarios, please have one csv file per scenario. If there are roof surface profiles that are deterministic, just have those sensorpoints identical in each of the scenario csv files. (Actually, maybe we want to run this code ONLY with Facade points, so the effect of the stochastic profiles is stronger. We can argue, it is an urban scenario where the roof surfaces are utilized by roof terraces and urban agrictulture, or so.)
* Modify the `SurfaceAreas.csv` in the "input_deterministic" to match with your solar profile files. So if you have 772 sensor points, the `SurfaceAreas.csv` should also contain 772 areas. In the example files, there are only 193.
* In `SurfaceAreas.csv`, you can ignore the first two columns (`Building` and `SurfaceIndex`), just put any name in there. What is important is the 4th columne (`Roof`) that indicates whether it is a roof surface, because that will determine the cost of the PV sizing. Also important is the last column `usefularea` in m², because that tells the energyhub how much area we have available per surface

**When running the program**

* double click the .exe file. Make sure it sits in a folder that contains sub-folders `input_stochastic` and `input_deterministic`
* enter `2` ; that's telling the program to run the script for your inputs.
* It will ask for a path, but you can hit Enter and it will take the currend file path of the .exe
* It will loop through all your stochastic scenarios and create 5 results files (per epsilon cut) for each of them
* Done

### Inputs
Per sample, the program needs a csv file pair:
1. `<City>_<year>_demand.csv`
2. `<City>_<year>_DryBulb.csv`
3. `<City>_<year>_GHI.csv`
4. `<City>_<year>_PeakLoads.csv`
5. `<City>_<year>_solar_SP0.csv`
6. `<City>_<year>_solar_SP1.csv`
7. `<City>_<year>_solar_SP4.csv`
8. `<City>_<year>_solar_SP5.csv`
9. `<City>_<year>_tehcnology.csv`
10. SurfaceAreas.csv`

### Dependencies
- IBM CPLEX 12.8 full academic version
- (In some future: Clustering class in BB-O library, once `Clustering.cs` is moved over to that repo)

## MILP optimization model description
Note that this energyhub uses Typical Days for dimension reduction, therefore we cannot easily use seasonal storages. Methods that allow seasonal storages with typical days exist, but are out of scope for this project. 

The formulations are based on [Waibel et al 2019](https://doi.org/10.1016/j.apenergy.2019.03.177), [Mavromatidis 2018](https://doi.org/10.3929/ethz-b-000182697) and [Dominguez-Munoz et al 2011](https://doi.org/10.1016/j.enbuild.2011.07.024). Demand response is from [Rakipour & Barati 2019](https://doi.org/10.1016/j.energy.2019.02.021).

### Objective Functions

The cost and carbon objective functions are given with:

![\min (\sum_{i=1}^{N} c_{i} x_{i} + \sum_{i=1}^{N} \sum_{t=1}^{T} k_t c_{i,t}^{\text{op}} x_{i,t}^{\text{op}}) ,](https://render.githubusercontent.com/render/math?math=%5Cmin%20(%5Csum_%7Bi%3D1%7D%5E%7BN%7D%20c_%7Bi%7D%20x_%7Bi%7D%20%2B%20%5Csum_%7Bi%3D1%7D%5E%7BN%7D%20%5Csum_%7Bt%3D1%7D%5E%7BT%7D%20k_t%20c_%7Bi%2Ct%7D%5E%7B%5Ctext%7Bop%7D%7D%20x_%7Bi%2Ct%7D%5E%7B%5Ctext%7Bop%7D%7D)%20%2C)

where the cost coefficients either represent monetary cost for investment and operation, or embodied and operational carbon emissions. The cost coefficients themselves might include conversion efficiencies, depending on technology. ![k_t](https://render.githubusercontent.com/render/math?math=k_t) is a scaling factor that depends on the typical day and it ensures that the costs of a certain day are weighed according to the size of the cluster that this day belongs to.

Carbon emissions and investment cost of the district heating network and the heat exchangers are added to the objective function as constants, since they are not impacted by the MILP optimization.

### Constraints
In the following, the system constraints are described. ![\forall t \in T](https://render.githubusercontent.com/render/math?math=%5Cforall%20t%20%5Cin%20T), if not stated otherwise.

**Demand Response**

0 <= x_DR,pos^demand,t <= a_DR^demand,t * demand_t... maximal shift is a fraction of the total demand at that timestep. 

0 <= x_DR,neg^demand,t <= a_DR^demand,t * demand_tsame for negative shift

y_DR,neg^demand,t + y_DR,pos^demand,t <= 1... only either positive or negative shifting possible at a each timestep

x_DR,pos^demand,t <= M * y_DR,pos^demand,t... big M method. toggle boolean on, if positive demand response is activated

x_DR,neg^demand,t <= M * y_DR,neg^demand,t... same for negative shift

sum_perDay(x_DR,pos^demand) = sum_perDay(x_DR,neg^demand)... total negative and positive shift per day must balance out. simplification for thermal and occupance dynamics. E.g., you can't take all heating demand away from winter and put it to summer. Also, you can't wait half a year to operate your dish washer...



**Energy Balance**

The general energy balances ensure that demand is met at all timesteps:

![\sum_{i=1}^{N^{\text{demand}}} a_{i,t}^{\text{demand}} x_{i,t}^{\text{op,demand}} = d_t^{\text{demand}} , \forall \text{demand} \in \{\text{heat, cool, elec} \}.](https://render.githubusercontent.com/render/math?math=%5Csum_%7Bi%3D1%7D%5E%7BN%5E%7B%5Ctext%7Bdemand%7D%7D%7D%20a_%7Bi%2Ct%7D%5E%7B%5Ctext%7Bdemand%7D%7D%20x_%7Bi%2Ct%7D%5E%7B%5Ctext%7Bop%2Cdemand%7D%7D%20%3D%20d_t%5E%7B%5Ctext%7Bdemand%7D%7D%20%2C%20%5Cforall%20%5Ctext%7Bdemand%7D%20%5Cin%20%5C%7B%5Ctext%7Bheat%2C%20cool%2C%20elec%7D%20%5C%7D.)

![N^{\text{demand}}](https://render.githubusercontent.com/render/math?math=N%5E%7B%5Ctext%7Bdemand%7D%7D) is the set of energy technologies providing energy of a certain demand type.

**Sizing of Technologies**

The general sizing constraint for any energy technology states that the operation at any timestep cannot exceed the capacity of the technology (in case of storages, the state of charge cannot exceed the capacity):

![x_{\text{tech},t}^{\text{op}} \leq x_{\text{tech}} .](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Btech%7D%2Ct%7D%5E%7B%5Ctext%7Bop%7D%7D%20%5Cleq%20x_%7B%5Ctext%7Btech%7D%7D%20.)


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


**Available Biomass**

Following constraint limits the yearly available biomass:

![x_{\text{biomass}}^{\text{total}} \leq b_{\text{biomass}}^{\text{max}} ,](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bbiomass%7D%7D%5E%7B%5Ctext%7Btotal%7D%7D%20%5Cleq%20b_%7B%5Ctext%7Bbiomass%7D%7D%5E%7B%5Ctext%7Bmax%7D%7D%20%2C)

where:

![x_{\text{biomass}}^{\text{total}} \coloneqq \sum_{t=1}^{T} (\frac{k_t}{a_{\text{bio},t}} x_{\text{bio},t}^{\text{op}}) .](https://render.githubusercontent.com/render/math?math=x_%7B%5Ctext%7Bbiomass%7D%7D%5E%7B%5Ctext%7Btotal%7D%7D%20%5Ccoloneqq%20%5Csum_%7Bt%3D1%7D%5E%7BT%7D%20(%5Cfrac%7Bk_t%7D%7Ba_%7B%5Ctext%7Bbio%7D%2Ct%7D%7D%20x_%7B%5Ctext%7Bbio%7D%2Ct%7D%5E%7B%5Ctext%7Bop%7D%7D)%20.)

**Other remarks**

- Maximal storage capacities are limited as functions of the total floor area. 
- Air Chiller is implemented as variable, even though, because it is the only cooling technology, there is no degree of freedom in the optimization. This is to keep the model flexible for future cooling technology additions
- for technology details, see [Waibel et al 2019](https://doi.org/10.1016/j.apenergy.2019.03.177).
- I used [this web app](https://alexanderrodin.com/github-latex-markdown/) to convert latex equations into markdown compatible syntax
