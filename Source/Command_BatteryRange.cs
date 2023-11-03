// Command_BatteryRange.cs
// Copyright Karel Kroeze, 2020-2020

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower
{
	public class Command_BatteryRange : Command
	{
		private readonly Building_BackupPowerAttachment parent;

		public Command_BatteryRange(Building_BackupPowerAttachment parent)
		{
			this.parent = parent;
		}

		public override string Desc =>
			I18n.StatusString(parent.Status, parent.BatteryRange.min,
				parent.BatteryRange.max, parent.PowerNet.StorageLevel());

		public override string Label => I18n.CommandLabel;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			// setup
			float width = GetWidth(maxWidth);
			Rect canvas = new Rect(topLeft, new Vector2(width, Height + 10));
			bool mouseOver = Mouse.IsOver(canvas);

			Find.WindowStack.ImmediateWindow(246685, canvas, WindowLayer.GameUI, () =>
			{
				canvas = canvas.AtZero();
				Rect buttonRect = canvas.AtZero().TopPartPixels(Height);
				GUI.color = mouseOver
					? Resources.blueish
					: Color.white;
				Widgets.DrawAtlas(buttonRect, BGTexture);
				GUI.color = Color.white;
				TooltipHandler.TipRegion(buttonRect, () => Desc, 2338712);
				if (Mouse.IsOver(buttonRect) && Input.GetMouseButtonDown(1))
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>
					{
						new FloatMenuOption(I18n.CopyTo_Room, CopyToRoom),
						new FloatMenuOption(I18n.CopyTo_Connected, CopyToConnected),
						new FloatMenuOption(I18n.CopyTo_All, CopyToAll)
					};
					Find.WindowStack.Add(new FloatMenu(options, I18n.CopyTo));
				}

				Rect innerButtonRect = buttonRect.ContractedBy(6);

				// sliders
				Rect minSliderRect = innerButtonRect.LeftPart(.2f);
				Rect maxSliderRect = innerButtonRect.RightPart(.2f);
				float newMin = GUI.VerticalSlider(minSliderRect, parent.BatteryRange.min, 1, 0);
				float newMax = GUI.VerticalSlider(maxSliderRect, parent.BatteryRange.max, 1, 0);

				// enforce min < max to avoid flicker
				if (Mathf.Abs(newMin - parent.BatteryRange.min) > Mathf.Epsilon)
				{
					parent.BatteryRange.min = newMin;
					parent.BatteryRange.max = Mathf.Max(parent.BatteryRange.min, parent.BatteryRange.max);
				}
				else if (Mathf.Abs(newMax - parent.BatteryRange.max) > Mathf.Epsilon)
				{
					parent.BatteryRange.max = newMax;
					parent.BatteryRange.min = Mathf.Min(parent.BatteryRange.min, parent.BatteryRange.max);
				}

				// battery
				GUI.color = Resources.whiteish;
				Rect batteryRect = innerButtonRect.MiddlePart(.2f, .2f).ContractedBy(6f);
				GUI.DrawTexture(batteryRect, Resources.Battery);

				if (parent.PowerNet?.batteryComps.Any() ?? false)
				{
					float pct = parent.PowerNet.StorageLevel();
					GUI.color = Resources.blueish;
					GUI.DrawTextureWithTexCoords(batteryRect.BottomPart(pct), Resources.Battery,
						new Rect(0, 0, 1, pct));
				}

				// draw target lines
				float minY = batteryRect.yMin + (batteryRect.height * (1 - parent.BatteryRange.min));
				float maxY = batteryRect.yMin + (batteryRect.height * (1 - parent.BatteryRange.max));
				Utilities.DrawLineDashed(new Vector2(batteryRect.xMin - 5, minY),
					new Vector2(batteryRect.xMin + (batteryRect.width * 2 / 3f), minY),
					Resources.greenish, 2);
				Utilities.DrawLineDashed(new Vector2(batteryRect.xMax + 5, maxY),
					new Vector2(batteryRect.xMin + (batteryRect.width * 1 / 3f), maxY),
					Resources.reddish, 2);

				GUI.color = Color.white;

				string label = LabelCap;
				if (!label.NullOrEmpty())
				{
					Text.Font = GameFont.Tiny;
					float height = Text.CalcHeight(label, canvas.width);
					Rect labelRect = new Rect(canvas.x, buttonRect.yMax - height + 12f, canvas.width, height);
					GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
					Text.Anchor = TextAnchor.UpperCenter;
					Widgets.Label(labelRect, label);
					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.UpperLeft;
				}
			}, false);

			return mouseOver
				? new GizmoResult(GizmoState.Mouseover)
				: new GizmoResult(GizmoState.Clear);
		}

		private void CopyTo(IEnumerable<Building_BackupPowerAttachment> brokers)
		{
			if (!brokers.EnumerableNullOrEmpty())
			{
				foreach (Building_BackupPowerAttachment broker in brokers)
				{
					parent.CopySettingsTo(broker);
				}
			}
		}

		private void CopyToAll()
		{
			IEnumerable<Building_BackupPowerAttachment> brokers = parent.Map.listerThings
				.ThingsOfDef(DefOf.BackupPower_Attachment)
				.Where(b => b.Faction == Faction.OfPlayer)
				.OfType<Building_BackupPowerAttachment>();
			CopyTo(brokers);
		}

		private void CopyToConnected()
		{
			IEnumerable<Building_BackupPowerAttachment> brokers = parent.PowerNet.powerComps
				.SelectMany(cp => cp.parent.OccupiedRect())
				.SelectMany(c => c.GetThingList(parent.Map))
				.Where(b => b.Faction == Faction.OfPlayer)
				.OfType<Building_BackupPowerAttachment>()
				.Distinct();
			CopyTo(brokers);
		}

		private void CopyToRoom()
		{
			IEnumerable<Building_BackupPowerAttachment> brokers = parent.GetRoom()?
				.ContainedThings(DefOf.BackupPower_Attachment)
				.OfType<Building_BackupPowerAttachment>()
				.Where(b => b.Faction == Faction.OfPlayer);
			CopyTo(brokers);
		}
	}
}