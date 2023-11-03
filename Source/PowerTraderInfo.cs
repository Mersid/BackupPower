using JetBrains.Annotations;
using RimWorld;

namespace BackupPower
{
	/// <summary>
	/// Represents a power producer or consumer and some information about it.
	/// It beats using a ridiculously large tuple.
	/// </summary>
	public class PowerTraderInfo
	{
		/// <summary>
		/// The power trader itself. These are producers or consumers, but not batteries.
		/// </summary>
		public CompPowerTrader Comp { get; set; }

		/// <summary>
		/// The backup battery box building, if any.
		/// </summary>
		[CanBeNull]
		public Building_BackupPowerAttachment Broker { get; set; }

		public float Consumption { get; set; }
		public float CurrentProduction { get; set; }
		public float PotentialProduction { get; set; }
	}
}