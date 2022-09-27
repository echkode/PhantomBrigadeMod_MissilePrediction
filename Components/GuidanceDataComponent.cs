using Entitas;

using PhantomBrigade.Data;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class GuidanceDataComponent : IComponent
	{
		public DataBlockGuidanceData data;
	}
}
