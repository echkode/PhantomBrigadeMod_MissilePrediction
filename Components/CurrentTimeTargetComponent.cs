using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkTime]
	[Unique]
	public sealed class CurrentTimeTargetComponent : IComponent
	{
		public float f;
	}
}
