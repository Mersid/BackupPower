// Building_BackupPowerAttachment.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower;

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
public class Building_BackupPowerAttachment : Building
{
	public FloatRange BatteryRange = FloatRange.One;
	public bool RunOnBatteriesOnly = true;
	public bool Enabled = true;

	private int LastOnTick { get; set; }

	private Color PrevColor { get; set; }
	public override Color DrawColor => Resources.StatusColor(Status);
	private CompFlickable Flickable => Parent.FlickableComp();



	public PowerNet? PowerNet => Parent.PowerComp?.PowerNet;
	private CompPowerPlant PowerPlant => Parent.PowerPlantComp();

	public BackupPowerStatus Status
	{
		get
		{
			if (Parent.BreakdownableComp().BrokenDown || !Parent.RefuelableComp().HasFuel)
			{
				return BackupPowerStatus.Error;
			}

			if (PowerPlant.PowerOn)
			{
				return BackupPowerStatus.Running;
			}

			return BackupPowerStatus.Standby;
		}
	}

	#region These are marked as null-forgiving because they are initialized in SpawnSetup()

	private Command_BatteryRange CommandBatteryRange { get; set; } = null!;
	private Command_Toggle CommandRunOnBatteriesOnly { get; set; } = null!;
	private Command_Toggle CommandEnabled { get; set; } = null!;
	private Command_Action CommandForceFlickOff { get; set; } = null!;
	private Command_Action CommandForceFlickOn { get; set; } = null!;
	public Building Parent { get; private set; } = null!;

	#endregion


	public bool CanTurnOff()
	{
		return LastOnTick + BackupPower.Settings.MinimumOnTime < Find.TickManager.TicksGame;
	}

	public void CopySettingsTo(Building_BackupPowerAttachment other)
	{
		other.BatteryRange = BatteryRange;
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		try
		{
			MapComponent_PowerBroker.DeregisterBroker(this);
		}
		catch (Exception err)
		{
			Log.Error($"Error deregistering broker: {err}");
		}

		base.Destroy(mode);
	}

	public override void ExposeData()
	{
		base.ExposeData();

		Scribe_Values.Look(ref BatteryRange, "batteryRange", FloatRange.One);
		Scribe_Values.Look(ref RunOnBatteriesOnly, "runOnBatteriesOnly", true);
		Scribe_Values.Look(ref Enabled, "enabled", true);
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		yield return CommandBatteryRange;
		yield return CommandRunOnBatteriesOnly;
		yield return CommandEnabled;

		if (DebugSettings.ShowDevGizmos)
		{
			yield return CommandForceFlickOff;
			yield return CommandForceFlickOn;
		}

		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
	}

	public override string GetInspectString()
	{
		string desc = base.GetInspectString();
		return I18n.StatusString(Status, BatteryRange.min, BatteryRange.max, PowerNet.StorageLevel()) +
		       (desc.NullOrEmpty() ? "" : $"\n{desc}");
	}

	public override void Notify_ColorChanged()
	{
		base.Notify_ColorChanged();
		// again, for good measure.
		Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
		PrevColor = DrawColor;
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		CommandBatteryRange = new Command_BatteryRange(this);
		CommandRunOnBatteriesOnly = new Command_Toggle
		{
			icon = DefDatabase<ThingDef>.GetNamed("Battery").uiIcon,
			iconProportions = new Vector2(2, 3),
			defaultLabel = I18n.RunOnBatteriesOnlyLabel,
			defaultDesc = I18n.RunOnBatteriesOnlyDesc,
			isActive = () => RunOnBatteriesOnly,
			toggleAction = () => RunOnBatteriesOnly = !RunOnBatteriesOnly
		};

		CommandForceFlickOff = new Command_Action
		{
			defaultLabel = I18n.DebugForceFlickOffLabel,
			defaultDesc = I18n.DebugForceFlickOffDesc,
			action = TurnOff
		};

		CommandForceFlickOn = new Command_Action
		{
			defaultLabel = I18n.DebugForceFlickOnLabel,
			defaultDesc = I18n.DebugForceFlickOnDesc,
			action = TurnOn
		};

		CommandEnabled = new Command_Toggle
		{
			icon = Resources.PowerTexture,
			defaultLabel = I18n.BatteryBackupEnabledLabel,
			defaultDesc = I18n.BatteryBackupEnabledDesc,
			isActive = () => Enabled,
			toggleAction = () => Enabled = !Enabled
		};

		if (!respawningAfterLoad)
		{
			_ = TryAttach(Map);
		}
	}

	protected override void Tick()
	{
		if (this.IsHashIntervalTick(60) && PrevColor != DrawColor)
		{
			Notify_ColorChanged();
		}

		// TODO: think about refactoring this and hooking onto parents' Destroy() instead.
		base.Tick();
		if (Parent.DestroyedOrNull() && !TryAttach(Map, true))
		{
			Messages.Message(I18n.AttachmentDestroyedBecauseParentGone(Parent?.Label ?? I18n.Generator),
				MessageTypeDefOf.NegativeEvent,
				false);
			Destroy(DestroyMode.Refund);
		}
	}

	public void TurnOff()
	{
		Flickable.Force(false);
	}

	public void TurnOn()
	{
		LastOnTick = Find.TickManager.TicksGame;
		Flickable.Force(true);
	}

	private bool TryAttach(Map map, bool reAttach = false)
	{
		Parent = Position.GetEdifice(map);
		bool success = PowerPlant != null && Flickable != null;
		if (success)
		{
			MapComponent_PowerBroker.RegisterBroker(this, reAttach);
		}

		return success;
	}
}