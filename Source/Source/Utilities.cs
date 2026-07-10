using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower;

public static class Utilities
{
    private static readonly ConditionalWeakTable<ThingWithComps, CompBreakdownable> Breakdownables =
        new ConditionalWeakTable<ThingWithComps, CompBreakdownable>();

    private static readonly MethodInfo DesiredOutputGetterMethodInfo = typeof(CompPowerPlant)
        .GetProperty(
            "DesiredPowerOutput",
            BindingFlags.Instance |
            BindingFlags.NonPublic)
        .GetMethod;

    private static readonly FieldInfo FlickableWantSwitchOnFiendInfo =
        typeof(CompFlickable).GetField("wantSwitchOn", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly ConditionalWeakTable<ThingWithComps, CompFlickable> Flickables =
        new ConditionalWeakTable<ThingWithComps, CompFlickable>();

    private static readonly ConditionalWeakTable<ThingWithComps, CompPowerPlant> Powerplants =
        new ConditionalWeakTable<ThingWithComps, CompPowerPlant>();

    private static readonly ConditionalWeakTable<ThingWithComps, CompRefuelable> Refuelables =
        new ConditionalWeakTable<ThingWithComps, CompRefuelable>();

    public static void AddSafe<T>(this HashSet<T> set, T item)
    {
        if (item == null)
            Log.ErrorOnce("tried adding null element to hashset", 123411);

        if (set.Contains(item))
            Log.ErrorOnce("tried adding duplicate item to hashset", 123412);

        _ = set.Add(item);
    }

    public static string Bold(this string msg) => $"<b>{msg}</b>";

    public static Vector2 BottomLeft(this Rect rect) => new Vector2(rect.xMin, rect.yMax);

    public static CompBreakdownable? BreakdownableComp(this ThingWithComps parent)
    {
        if (Breakdownables.TryGetValue(parent, out CompBreakdownable breakdownable))
            return breakdownable;

        breakdownable = parent.GetComp<CompBreakdownable>();
        Breakdownables.Add(parent, breakdownable);

        // Can, in fact, be null if GetComp returns null - it's not annotated.
        return breakdownable;
    }

    public static float DesiredOutput(this CompPowerPlant plant) =>
        (float)DesiredOutputGetterMethodInfo.Invoke(plant, null);

    public static void DrawLineDashed(Vector2 start, Vector2 end, Color? color = null, float size = 1,
        float stroke = 5,
        float dash = 3)
    {
        float partLength = dash + stroke;
        float totalLength = (end - start).magnitude;
        Vector2 direction = (end - start).normalized;
        float done = 0f;
        while (done < totalLength)
        {
            Vector2 _start = start + done * direction;
            Vector2 _end = start + Mathf.Min(done + stroke, totalLength) * direction;
            Widgets.DrawLine(_start, _end, color.GetValueOrDefault(Color.white), size);
            done += partLength;
        }
    }

    public static CompFlickable? FlickableComp(this ThingWithComps parent)
    {
        if (Flickables.TryGetValue(parent, out CompFlickable flickable))
            return flickable;

        flickable = parent.GetComp<CompFlickable>();
        Flickables.Add(parent, flickable);

        // Can, in fact, be null if GetComp returns null - it's not annotated.
        return flickable;
    }

    public static void Force(this CompFlickable flickable, bool mode)
    {
        if (mode != flickable.SwitchIsOn)
            flickable.SwitchIsOn = mode;

        if (flickable.WantsFlick())
            FlickableWantSwitchOnFiendInfo.SetValue(flickable, mode);
    }

    public static bool HasStorage(this PowerNet net) => !net.batteryComps.NullOrEmpty();

    public static Rect MiddlePart(this Rect rect, float left = 0f, float right = 0f, float top = 0f,
        float bottom = 0f) =>
        new Rect(rect.xMin + rect.width * left,
            rect.yMin + rect.height * top,
            rect.width * (1 - left - right),
            rect.height * (1 - top - bottom));

    public static CompPowerPlant? PowerPlantComp(this ThingWithComps parent)
    {
        if (Powerplants.TryGetValue(parent, out CompPowerPlant powerplant))
            return powerplant;

        powerplant = parent.GetComp<CompPowerPlant>();
        Powerplants.Add(parent, powerplant);

        return powerplant;
    }

    public static CompRefuelable? RefuelableComp(this ThingWithComps parent)
    {
        if (Refuelables.TryGetValue(parent, out CompRefuelable refuelable))
            return refuelable;

        refuelable = parent.GetComp<CompRefuelable>();
        Refuelables.Add(parent, refuelable);

        return refuelable;
    }

    public static void RemoveSafe<T>(this HashSet<T> set, T item)
    {
        if (item == null)
            Log.ErrorOnce("tried removing null element from hashset", 123413);

        if (!set.Contains(item))
            Log.ErrorOnce("tried removing item from hashset that it does not have", 123414);

        _ = set.Remove(item);
    }

    public static float StorageLevel(this PowerNet? net)
    {
        if (net == null || net.batteryComps.NullOrEmpty())
            return 0;

        (float current, float max) = net.batteryComps
            .Select(b => (b.StoredEnergy, b.Props.storedEnergyMax))
            .Aggregate((a, b) => (
                a.StoredEnergy + b.StoredEnergy,
                a.storedEnergyMax + b.storedEnergyMax));
        return current / max;
    }
}