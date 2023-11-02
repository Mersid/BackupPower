// Resources.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using UnityEngine;
using Verse;

namespace BackupPower
{
	[StaticConstructorOnStartup]
	public static class Resources
	{
		public static Texture2D BackupPowerAttachment;
		public static Texture2D Battery;

		public static Color blueish = GenUI.MouseoverColor;
		public static Color greenish = new Color(.3725f, .8588f, .6549f);
		public static Color reddish = new Color(.6667f, .2157f, .2275f);
		public static Color whiteish = new Color(1f, 1f, 1f, .3f);

		static Resources()
		{
			Battery = ContentFinder<Texture2D>.Get("UI/Battery");
			BackupPowerAttachment = ContentFinder<Texture2D>.Get("BackupPowerAttachment");
		}

		public static Color StatusColor(BackupPowerStatus status)
		{
			return status switch
			{
				BackupPowerStatus.Standby => blueish,
				BackupPowerStatus.Running => greenish,
				BackupPowerStatus.Error => reddish,
				_ => throw new ArgumentOutOfRangeException(),
			};
		}
	}
}