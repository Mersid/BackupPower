using System;
using UnityEngine;
using Verse;

namespace BackupPower;

[StaticConstructorOnStartup]
public static class Resources
{
    public static Color Blueish = GenUI.MouseoverColor;
    public static Color Greenish = new Color(.3725f, .8588f, .6549f);
    public static Color Reddish = new Color(.6667f, .2157f, .2275f);
    public static Texture PowerTexture { get; private set; } = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower");

    static Resources()
    {
    }

    public static Color StatusColor(BackupPowerStatus status)
    {
        return status switch
        {
            BackupPowerStatus.Standby => Blueish,
            BackupPowerStatus.Running => Greenish,
            BackupPowerStatus.Error => Reddish,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}