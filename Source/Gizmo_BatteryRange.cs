// Gizmo_BatteryRange.cs
// Copyright Karel Kroeze, 2020-2025

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower;

/// <summary>
///     Backup Power's concrete range slider: lets the player drag the on/off battery thresholds
///     for a <see cref="Building_BackupPowerAttachment" />. Replaces the legacy
///     <see cref="Command_BatteryRange" /> with the vanilla-aligned <see cref="Gizmo_RangeSlider" />
///     look and feel.
/// </summary>
public class Gizmo_BatteryRange : Gizmo_RangeSlider
{
	private static bool _draggingMin;
	private static bool _draggingMax;

	private readonly List<Building_BackupPowerAttachment> _merged = new();

	private Building_BackupPowerAttachment Parent { get; }

	public Gizmo_BatteryRange(Building_BackupPowerAttachment parent)
	{
		Parent = parent;

		// "Copy settings to" actions, registered through the general right-click hook so the
		// base gizmo stays free of domain knowledge.
		AddRightClickOption(I18n.CopyTo_Room, CopyToRoom);
		AddRightClickOption(I18n.CopyTo_Connected, CopyToConnected);
		AddRightClickOption(I18n.CopyTo_All, CopyToAll);
	}

	protected override FloatRange Target
	{
		get => Parent.BatteryRange;
		set
		{
			Parent.BatteryRange = value;
			foreach (Building_BackupPowerAttachment other in _merged)
			{
				other.BatteryRange = value;
			}
		}
	}

	protected override float ValuePercent => Parent.PowerNet?.StorageLevel() ?? 0f;

	protected override string Title => I18n.CommandLabel;

	protected override bool DraggingMin
	{
		get => _draggingMin;
		set => _draggingMin = value;
	}

	protected override bool DraggingMax
	{
		get => _draggingMax;
		set => _draggingMax = value;
	}

	protected override string GetTooltip()
	{
		return I18n.StatusString(Parent.Status, Parent.BatteryRange.min,
			Parent.BatteryRange.max, Parent.PowerNet.StorageLevel());
	}

	// Green = "turns on below" threshold, red = "turns off above" threshold; matches the legacy semantics.
	protected override Color MinMarkerColor => Resources.Greenish;
	protected override Color MaxMarkerColor => Resources.Reddish;

	protected override int Increments => 100;

	public override bool GroupsWith(Gizmo other)
	{
		return other is Gizmo_BatteryRange;
	}

	public override void MergeWith(Gizmo other)
	{
		if (other is Gizmo_BatteryRange otherRange)
		{
			_merged.Add(otherRange.Parent);
		}
	}

	private void CopyTo(IEnumerable<Building_BackupPowerAttachment>? brokers)
	{
		if (brokers.EnumerableNullOrEmpty())
		{
			return;
		}

		foreach (Building_BackupPowerAttachment broker in brokers!)
		{
			Parent.CopySettingsTo(broker);
		}
	}

	private void CopyToAll()
	{
		IEnumerable<Building_BackupPowerAttachment> brokers = Parent.Map.listerThings
			.ThingsOfDef(DefOf.BackupPower_Attachment)
			.Where(b => b.Faction == Faction.OfPlayer)
			.OfType<Building_BackupPowerAttachment>();
		CopyTo(brokers);
	}

	private void CopyToConnected()
	{
		if (Parent.PowerNet is not { } net)
		{
			return;
		}

		IEnumerable<Building_BackupPowerAttachment> brokers = net.powerComps
			.SelectMany(cp => cp.parent.OccupiedRect())
			.SelectMany(c => c.GetThingList(Parent.Map))
			.Where(b => b.Faction == Faction.OfPlayer)
			.OfType<Building_BackupPowerAttachment>()
			.Distinct();
		CopyTo(brokers);
	}

	private void CopyToRoom()
	{
		IEnumerable<Building_BackupPowerAttachment>? brokers = Parent.GetRoom()?
			.ContainedThings(DefOf.BackupPower_Attachment)
			.OfType<Building_BackupPowerAttachment>()
			.Where(b => b.Faction == Faction.OfPlayer);
		CopyTo(brokers);
	}
}
