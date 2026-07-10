using RimWorld;
using UnityEngine;

namespace BackupPower;

public static class CompPowerTraderExtensions
{
    public static float Consumption(this CompPowerTrader comp)
    {
        if (!comp.PowerOn && !FlickUtility.WantsToBeOn(comp.parent))
            return 0;

        return Mathf.Max(-comp.PowerOutput, 0f);
    }

    public static float CurrentProduction(this CompPowerTrader comp)
    {
        if (comp is not CompPowerPlant plant)
            return 0;

        if (!plant.PowerOn)
            return 0;

        return Mathf.Max(plant.PowerOutput, 0);
    }

    public static float PotentialProduction(this CompPowerTrader comp)
    {
        if (comp is not CompPowerPlant plant)
            return 0;

        CompRefuelable? refuelable = plant.parent.RefuelableComp();
        if (refuelable is { HasFuel: false })
            return 0;

        CompBreakdownable? breakdownable = plant.parent.BreakdownableComp();
        if (breakdownable is { BrokenDown: true })
            return 0;

        // TODO: check how this interacts with variable power output buildings, e.g. solar, wind.
        return Mathf.Max(plant.DesiredOutput(), plant.PowerOutput, 0);
    }
}