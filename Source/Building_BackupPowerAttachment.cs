// Building_BackupPowerAttachment.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower
{
	public enum BackupPowerStatus
	{
		Standby,
		Running,
		Error
	}

	public class Building_BackupPowerAttachment : Building
	{
		public FloatRange batteryRange = FloatRange.One;
		public bool runOnBatteriesOnly = true;
		private Command_BatteryRange _command_BatteryRange;
		private Command_Toggle _command_RunOnBatteriesOnly;

		private int _lastOnTick;

		private Color _prevColor;
		public override Color DrawColor => Resources.StatusColor(Status);
		public CompFlickable Flickable => Parent?.FlickableComp();

		public Building Parent { get; private set; }

		public PowerNet PowerNet => Parent?.PowerComp?.PowerNet;
		public CompPowerPlant PowerPlant => Parent?.PowerPlantComp();

		private Command_Action forceFlickOff;
		private Command_Action forceFlickOn;


		public BackupPowerStatus Status
		{
			get
			{
				if ((Parent?.BreakdownableComp()?.BrokenDown ?? false) ||
				    (!Parent?.RefuelableComp()?.HasFuel ?? false))
				{
					return BackupPowerStatus.Error;
				}

				if (PowerPlant?.PowerOn ?? false)
				{
					return BackupPowerStatus.Running;
				}

				return BackupPowerStatus.Standby;
			}
		}

		public bool CanTurnOff()
		{
			return _lastOnTick + BackupPower.Settings.MinimumOnTime < Find.TickManager.TicksGame;
		}

		public void CopySettingsTo(Building_BackupPowerAttachment other)
		{
			other.batteryRange = batteryRange;
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			try
			{
				MapComponent_PowerBroker.DeregisterBroker(this);
			}
			catch (Exception err)
			{
				Verse.Log.Error($"Error deregistering broker: {err}");
			}

			base.Destroy(mode);
		}

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Values.Look(ref batteryRange, "batteryRange", FloatRange.One);
			Scribe_Values.Look(ref runOnBatteriesOnly, "runOnBatteriesOnly", true);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			yield return _command_BatteryRange;
			yield return _command_RunOnBatteriesOnly;

			if (DebugSettings.ShowDevGizmos)
			{
				yield return forceFlickOff;
				yield return forceFlickOn;
			}

			foreach (Gizmo _gizmo in base.GetGizmos())
			{
				yield return _gizmo;
			}
		}

		public override string GetInspectString()
		{
			string desc = base.GetInspectString();
			return I18n.StatusString(Status, batteryRange.min, batteryRange.max, PowerNet.StorageLevel()) +
			       (desc.NullOrEmpty() ? "" : $"\n{desc}");
		}

		public override void Notify_ColorChanged()
		{
			base.Notify_ColorChanged();
			// again, for good measure.
			Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things);
			_prevColor = DrawColor;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			_command_BatteryRange = new Command_BatteryRange(this);
			_command_RunOnBatteriesOnly = new Command_Toggle()
			{
				icon = DefDatabase<ThingDef>.GetNamed("Battery").uiIcon,
				iconProportions = new Vector2(2, 3),
				defaultLabel = I18n.RunOnBatteriesOnly_Label,
				defaultDesc = I18n.RunOnBatteriesOnly_Desc,
				isActive = () => runOnBatteriesOnly,
				toggleAction = () => runOnBatteriesOnly = !runOnBatteriesOnly
			};

			forceFlickOff = new Command_Action()
			{
				defaultLabel = "DEV: Force Flick Off",
				defaultDesc = "Force Flick Off",
				action = TurnOff
			};

			forceFlickOn = new Command_Action()
			{
				defaultLabel = "DEV: Force Flick On",
				defaultDesc = "Force Flick On",
				action = TurnOn
			};

			if (!respawningAfterLoad)
			{
				_ = TryAttach(Map);
			}
		}

		public override void Tick()
		{
			if (this.IsHashIntervalTick(60) && _prevColor != DrawColor)
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
			_lastOnTick = Find.TickManager.TicksGame;
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
}