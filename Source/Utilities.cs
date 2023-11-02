// Utilities.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower
{
	public static class Utilities
	{
		private static readonly ConditionalWeakTable<ThingWithComps, CompBreakdownable> _breakdownables =
			new ConditionalWeakTable<ThingWithComps, CompBreakdownable>();

		private static readonly MethodInfo _desiredOutputGetter_MI = typeof(CompPowerPlant)
			.GetProperty(
				"DesiredPowerOutput",
				BindingFlags.Instance |
				BindingFlags.NonPublic)
			.GetMethod;

		private static readonly FieldInfo _flickable_wantSwitchOn_FI =
			typeof(CompFlickable).GetField("wantSwitchOn", BindingFlags.Instance | BindingFlags.NonPublic);

		private static readonly ConditionalWeakTable<ThingWithComps, CompFlickable> _flickables =
			new ConditionalWeakTable<ThingWithComps, CompFlickable>();

		private static readonly ConditionalWeakTable<ThingWithComps, CompPowerPlant> _powerplants =
			new ConditionalWeakTable<ThingWithComps, CompPowerPlant>();

		private static readonly ConditionalWeakTable<ThingWithComps, CompRefuelable> _refuelables =
			new ConditionalWeakTable<ThingWithComps, CompRefuelable>();

		public static void AddSafe<T>(this HashSet<T> set, T item)
		{
			if (item == null)
			{
				Verse.Log.ErrorOnce("tried adding null element to hashset", 123411);
			}

			if (set.Contains(item))
			{
				Verse.Log.ErrorOnce("tried adding duplicate item to hashset", 123412);
			}

			_ = set.Add(item);
		}

		public static string Bold(this string msg)
		{
			return $"<b>{msg}</b>";
		}

		public static Vector2 BottomLeft(this Rect rect)
		{
			return new Vector2(rect.xMin, rect.yMax);
		}

		public static CompBreakdownable BreakdownableComp(this ThingWithComps parent)
		{
			if (_breakdownables.TryGetValue(parent, out CompBreakdownable breakdownable))
			{
				return breakdownable;
			}

			breakdownable = parent.GetComp<CompBreakdownable>();
			_breakdownables.Add(parent, breakdownable);
			return breakdownable;
		}

		public static float DesiredOutput(this CompPowerPlant plant)
		{
			return (float)_desiredOutputGetter_MI.Invoke(plant, null);
		}

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
				Vector2 _start = start + (done * direction);
				Vector2 _end = start + (Mathf.Min(done + stroke, totalLength) * direction);
				Widgets.DrawLine(_start, _end, color.GetValueOrDefault(Color.white), size);
				done += partLength;
			}
		}

		public static CompFlickable FlickableComp(this ThingWithComps parent)
		{
			if (_flickables.TryGetValue(parent, out CompFlickable flickable))
			{
				return flickable;
			}

			flickable = parent.GetComp<CompFlickable>();
			_flickables.Add(parent, flickable);
			return flickable;
		}

		public static void Force(this CompFlickable flickable, bool mode)
		{
			if (mode != flickable.SwitchIsOn)
			{
				flickable.SwitchIsOn = mode;
			}

			if (flickable.WantsFlick())
			{
				_flickable_wantSwitchOn_FI.SetValue(flickable, mode);
			}
		}

		public static bool HasStorage(this PowerNet net)
		{
			return !net.batteryComps.NullOrEmpty();
		}

		public static Rect MiddlePart(this Rect rect, float left = 0f, float right = 0f, float top = 0f,
			float bottom = 0f)
		{
			return new Rect(rect.xMin + (rect.width * left),
				rect.yMin + (rect.height * top),
				rect.width * (1 - left - right),
				rect.height * (1 - top - bottom));
		}

		public static CompPowerPlant PowerPlantComp(this ThingWithComps parent)
		{
			if (_powerplants.TryGetValue(parent, out CompPowerPlant powerplant))
			{
				return powerplant;
			}

			powerplant = parent.GetComp<CompPowerPlant>();
			_powerplants.Add(parent, powerplant);
			return powerplant;
		}

		public static CompRefuelable RefuelableComp(this ThingWithComps parent)
		{
			if (_refuelables.TryGetValue(parent, out CompRefuelable refuelable))
			{
				return refuelable;
			}

			refuelable = parent.GetComp<CompRefuelable>();
			_refuelables.Add(parent, refuelable);
			return refuelable;
		}

		public static void RemoveSafe<T>(this HashSet<T> set, T item)
		{
			if (item == null)
			{
				Verse.Log.ErrorOnce("tried removing null element from hashset", 123413);
			}

			if (!set.Contains(item))
			{
				Verse.Log.ErrorOnce("tried removing item from hashset that it does not have", 123414);
			}

			_ = set.Remove(item);
		}

		public static float StorageLevel(this PowerNet net)
		{
			if (net == null || net.batteryComps.NullOrEmpty())
			{
				return 0;
			}

			(float current, float max) = net.batteryComps
				.Select(b => (b.StoredEnergy, b.Props.storedEnergyMax))
				.Aggregate((a, b) => (
					a.StoredEnergy + b.StoredEnergy,
					a.storedEnergyMax + b.storedEnergyMax));
			return current / max;
		}
	}

	[RimWorld.DefOf]
	public static class DefOf
	{
		public static ThingDef BackupPower_Attachment;
	}
}