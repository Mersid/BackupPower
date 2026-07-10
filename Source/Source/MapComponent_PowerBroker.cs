using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower;

// ReSharper disable once InconsistentNaming
[UsedImplicitly] // MapComponents are instantiated by Rimworld
public class MapComponent_PowerBroker(Map map) : MapComponent(map)
{
    private readonly HashSet<Building_BackupPowerAttachment> _brokers = [];

    public static void DeregisterBroker(Building_BackupPowerAttachment broker)
    {
        GetMapComponentFor(broker.Map)._brokers.RemoveSafe(broker);
    }

    public static void RegisterBroker(Building_BackupPowerAttachment broker, bool update = false)
    {
        MapComponent_PowerBroker comp = GetMapComponentFor(broker.Map);
        if (update)
            _ = comp._brokers.Remove(broker);

        comp._brokers.AddSafe(broker);
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (Find.TickManager.TicksGame % BackupPower.Settings.UpdateInterval != 0)
            return;

        foreach (IGrouping<PowerNet, Building_BackupPowerAttachment> group in _brokers
                     .Where(b => b.PowerNet is not null)
                     .GroupBy(b => b.PowerNet!)) // We've filtered out null power nets.
            PowerNetUpdate(group.Key, [..group]);
    }

    public void PowerNetUpdate(PowerNet net, HashSet<Building_BackupPowerAttachment> brokers)
    {
        // get desired power
        List<PowerTraderInfo> users = net.powerComps.Select(p => new PowerTraderInfo
        {
            Comp = p,
            Broker = p.parent is Building building
                ? brokers.FirstOrDefault(b => b.Parent == building)
                : null,
            Consumption = p.Consumption(),
            CurrentProduction = p.CurrentProduction(),
            PotentialProduction = p.PotentialProduction()
        }).ToList();

        float need = users.Sum(u => u.Consumption);
        float production = users.Sum(u => u.CurrentProduction);
        bool hasStorage = net.HasStorage();
        float storageLevel = net.StorageLevel();

        if (production > need || (hasStorage && storageLevel > 0))
        {
            // try to shut backups off
            List<PowerTraderInfo> backups = users.Where(u =>
                    u.Broker is { Enabled: true }
                    && u.CurrentProduction > 0
                    && (u.CurrentProduction <= production - need || u.Broker.RunOnBatteriesOnly)
                    && ((!hasStorage && !u.Broker.RunOnBatteriesOnly) || storageLevel >= u.Broker.BatteryRange.max)
                    && u.Broker.CanTurnOff())
                .ToList();

            if (backups.TryRandomElementByWeight(c => 1 / c.CurrentProduction,
                    out PowerTraderInfo backup) && backup.Broker is not null)
                backup.Broker.TurnOff();
        }

        if (production < need || (hasStorage && storageLevel < 1))
        {
            // try to turn backups on
            List<PowerTraderInfo> backups = users.Where(u =>
                u.Broker is { Enabled: true }
                && Math.Abs(u.CurrentProduction) < Mathf.Epsilon
                // && u.PotentialProduction > 0 // Some things like the Helixien generators set PotentialProduction to 0 when off. Dunno why.
                && (!hasStorage || storageLevel <= u.Broker.BatteryRange.min)
            ).ToList();

            if (backups.TryRandomElementByWeight(_ => 1,
                    out PowerTraderInfo backup) && backup.Broker is not null)
                backup.Broker.TurnOn();
        }
    }

    private static MapComponent_PowerBroker GetMapComponentFor(Map map)
    {
        MapComponent_PowerBroker? component = map.GetComponent<MapComponent_PowerBroker>();
        if (component is not null)
            return component;

        component = new MapComponent_PowerBroker(map);
        map.components.Add(component);

        return component;
    }
}