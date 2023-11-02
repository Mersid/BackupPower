// PlaceWorker_OnFlickablePowerPlant.cs
// Copyright Karel Kroeze, 2020-2020

using System.Linq;
using Verse;

namespace BackupPower
{
	public class PlaceWorker_OnFlickablePowerPlant : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
			Thing thingToIgnore = null, Thing thing = null)
		{
			// there's a power plant or battery here.
			Building edifice = loc.GetEdifice(map);
			if (edifice?.PowerPlantComp() == null)
			{
				return I18n.PlaceWorker_PlaceOnPowerPlant;
			}

			if (edifice.FlickableComp() == null)
			{
				return I18n.PlaceWorker_PlaceOnFlickable;
			}

			if (edifice.OccupiedRect().Any(c => c.GetFirstThing<Building_BackupPowerAttachment>(map) != null))
			{
				return I18n.PlaceWorker_OnlyOneAttachmentAllowed;
			}

			return true;
		}
	}
}