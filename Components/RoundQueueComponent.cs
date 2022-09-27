using System.Collections.Generic;

using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	[Unique]
	public sealed class RoundQueueComponent : IComponent
	{
		public Queue<RoundInfo> q;
	}
}
