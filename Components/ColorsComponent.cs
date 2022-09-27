using Entitas;

using PhantomBrigade.Data;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class ColorsComponent : IComponent
	{
		public DataBlockFloat01 hueOffset;
		public DataBlockColorInterpolated colorOverride;
	}
}
