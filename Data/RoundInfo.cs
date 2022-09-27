namespace EchKode.PBMods.MissilePrediction
{
	public sealed class RoundInfo
	{
		public ECS.EkPredictionEntity Predicted;
		public ActionEntity Action;
		public CombatEntity CombatSource;
		public EquipmentEntity Part;
		public EquipmentEntity Subsystem;
		public int SequenceNumber;
		public float StartTime;
		public int StartIndex;

		public static int Compare(RoundInfo x, RoundInfo y) => x.StartTime.CompareTo(y.StartTime);
	}
}
