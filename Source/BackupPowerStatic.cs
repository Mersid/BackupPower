using UnityEngine;
using Verse;

namespace BackupPower
{
	[StaticConstructorOnStartup]
	public static class BackupPowerStatic
	{
		public static Texture PowerTexture { get; private set; } = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower");

		static BackupPowerStatic()
		{

		}
	}
}