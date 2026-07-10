using JetBrains.Annotations;
using Verse;

// ReSharper disable InconsistentNaming
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
// RimWorld will throw an error on startup if these fields are not found.

namespace BackupPower;

[RimWorld.DefOf]
public static class DefOf
{
    [UsedImplicitly] public static ThingDef BackupPower_Attachment;
}