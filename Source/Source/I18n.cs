using UnityEngine;
using Verse;

namespace BackupPower;

// ReSharper disable once InconsistentNaming
public static class I18n
{
    // ReSharper disable InconsistentNaming
    public static readonly string BackupPower = "Fluffy.BackupPower".Translate();
    public static readonly string PlaceWorker_PlaceOnPowerPlant = Translate("PlaceWorker.PlaceOnPowerPlant");
    public static readonly string PlaceWorker_PlaceOnFlickable = Translate("PlaceWorker.PlaceOnFlickable");

    public static readonly string PlaceWorker_OnlyOneAttachmentAllowed =
        Translate("PlaceWorker.OnlyOneAttachmentAllowed");

    public static readonly string Settings_UpdateInterval_Tooltip = Translate("Settings.UpdateInterval.Tooltip");
    public static readonly string Settings_MinimumOnTime_Tooltip = Translate("Settings.MinimumOnTime.Tooltip");
    public static readonly string Generator = Translate("Generator");
    public static readonly string CommandLabel = Translate("CommandLabel");

    public static readonly string CopyTo = Translate("CopyTo");
    public static readonly string CopyTo_Room = Translate("CopyTo.Room");
    public static readonly string CopyTo_Connected = Translate("CopyTo.Connected");
    public static readonly string CopyTo_All = Translate("CopyTo.All");

    public static readonly string RunOnBatteriesOnlyLabel = Translate("RunOnBatteriesOnly.Label");
    public static readonly string RunOnBatteriesOnlyDesc = Translate("RunOnBatteriesOnly.Desc");

    public static readonly string DebugForceFlickOffLabel = Translate("ForceFlickOff.Label");
    public static readonly string DebugForceFlickOffDesc = Translate("ForceFlickOff.Desc");
    public static readonly string DebugForceFlickOnLabel = Translate("ForceFlickOn.Label");
    public static readonly string DebugForceFlickOnDesc = Translate("ForceFlickOn.Desc");

    public static readonly string BatteryBackupEnabledLabel = Translate("BatteryBackupEnabled.Label");
    public static readonly string BatteryBackupEnabledDesc = Translate("BatteryBackupEnabled.Desc");

    public static readonly string CommandFlickOnOffLabel = Translate("CommandFlickOnOff.Label");
    public static readonly string CommandFlickOnOffDesc = Translate("CommandFlickOnOff.Desc");
    // ReSharper restore InconsistentNaming


    public static string AttachmentDestroyedBecauseParentGone(string label) =>
        Translate("AttachmentDestroyedBecauseParentGone", label);

    public static string CurrentStatus(BackupPowerStatus status) => Translate("CurrentStatus",
        StatusLabel(status).Colorize(Resources.StatusColor(status)));

    public static string CurrentStorage(float cur) =>
        Translate("CurrentStorage", cur.ToStringPercent().Colorize(Color.white));

    public static string FormatSeconds(this float seconds) => seconds.ToString("##.#'s'");

    public static string Settings_MinimumOnTime(float value, float @default) => Translate("Settings.MinimumOnTime",
        value.FormatSeconds(), @default.FormatSeconds());

    public static string Settings_UpdateInterval(float value, float @default) => Translate("Settings.UpdateInterval",
        value.FormatSeconds(), @default.FormatSeconds());

    public static string StatusLabel(BackupPowerStatus status) => Translate($"Status.{status}");

    public static string StatusString(BackupPowerStatus status, float min, float max, float cur) =>
        CurrentStatus(status) + "\n" +
        CurrentStorage(cur).Colorize(Color.grey) + "\n" +
        TurnsOnAt(min).Colorize(Color.grey) + "\n" +
        TurnsOffAt(max).Colorize(Color.grey);

    public static string TurnsOffAt(float value) =>
        Translate("TurnsOffAbove", value.ToStringPercent().Colorize(Resources.Reddish));

    public static string TurnsOnAt(float value) =>
        Translate("TurnsOnBelow", value.ToStringPercent().Colorize(Resources.Greenish));

    private static string Key(string key) => $"Fluffy.BackupPower.{key}";

    private static string Translate(string key, params NamedArgument[] args) => Key(key).Translate(args).Resolve();
}