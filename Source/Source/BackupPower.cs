using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace BackupPower;

[UsedImplicitly] // By RimWorld
public class BackupPower(ModContentPack content) : Mod(content)
{
    public static Settings Settings => LoadedModManager.GetMod<BackupPower>().GetSettings<Settings>();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        Settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory() => I18n.BackupPower;
}