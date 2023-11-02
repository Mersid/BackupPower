// I18n.cs
// Copyright Karel Kroeze, 2020-2020

using UnityEngine;
using Verse;

namespace BackupPower
{
	public static class I18n
	{
		public static string BackupPower = "Fluffy.BackupPower".Translate();
		public static string PlaceWorker_PlaceOnPowerPlant = Translate("PlaceWorker.PlaceOnPowerPlant");
		public static string PlaceWorker_PlaceOnFlickable = Translate("PlaceWorker.PlaceOnFlickable");
		public static string PlaceWorker_OnlyOneAttachmentAllowed = Translate("PlaceWorker.OnlyOneAttachmentAllowed");
		public static string Settings_UpdateInterval_Tooltip = Translate("Settings.UpdateInterval.Tooltip");
		public static string Settings_MinimumOnTime_Tooltip = Translate("Settings.MinimumOnTime.Tooltip");
		public static string Generator = Translate("Generator");
		public static string CommandLabel = Translate("CommandLabel");

		public static string CopyTo = Translate("CopyTo");
		public static string CopyTo_Room = Translate("CopyTo.Room");
		public static string CopyTo_Connected = Translate("CopyTo.Connected");
		public static string CopyTo_All = Translate("CopyTo.All");


		public static string AttachmentDestroyedBecauseParentGone(string label)
		{
			return Translate("AttachmentDestroyedBecauseParentGone", label);
		}

		public static string CurrentStatus(BackupPowerStatus status)
		{
			return Translate("CurrentStatus", StatusLabel(status).Colorize(Resources.StatusColor(status)));
		}

		public static string CurrentStorage(float cur)
		{
			return Translate("CurrentStorage", cur.ToStringPercent().Colorize(Color.white));
		}

		public static string FormatSeconds(this float seconds)
		{
			return seconds.ToString("##.#'s'");
		}

		public static string Settings_MinimumOnTime(float value, float @default)
		{
			return Translate("Settings.MinimumOnTime", value.FormatSeconds(), @default.FormatSeconds());
		}

		public static string Settings_UpdateInterval(float value, float @default)
		{
			return Translate("Settings.UpdateInterval", value.FormatSeconds(), @default.FormatSeconds());
		}

		public static string StatusLabel(BackupPowerStatus status)
		{
			return Translate($"Status.{status}");
		}

		public static string StatusString(BackupPowerStatus status, float min, float max, float cur)
		{
			return CurrentStatus(status) + "\n" +
			       CurrentStorage(cur).Colorize(Color.grey) + "\n" +
			       TurnsOnAt(min).Colorize(Color.grey) + "\n" +
			       TurnsOffAt(max).Colorize(Color.grey);
		}

		public static string TurnsOffAt(float value)
		{
			return Translate("TurnsOffAbove", value.ToStringPercent().Colorize(Resources.reddish));
		}

		public static string TurnsOnAt(float value)
		{
			return Translate("TurnsOnBelow", value.ToStringPercent().Colorize(Resources.greenish));
		}

		public static string RunOnBatteriesOnly_Label = Translate("RunOnBatteriesOnly.Label");
		public static string RunOnBatteriesOnly_Desc = Translate("RunOnBatteriesOnly.Desc");

		private static string Key(string key)
		{
			return $"Fluffy.BackupPower.{key}";
		}

		private static string Translate(string key, params NamedArgument[] args)
		{
			return Key(key).Translate(args).Resolve();
		}
	}
}