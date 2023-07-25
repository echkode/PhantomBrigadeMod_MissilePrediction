// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

namespace EchKode.PBMods.MissilePrediction
{
	sealed class MotionComputeFeature : Feature
	{
		internal sealed class Options
		{
			internal int SlicesPerFrame;
			internal float ChaseDistance;
			internal float TriggerDistance;
		}

		private readonly int slicesPerFrame;

		internal MotionComputeFeature(
			Contexts contexts,
			ECS.Contexts ekContexts,
			Options options)
		{
			slicesPerFrame = options.SlicesPerFrame;
			Add(new PredictionRigidbodyDriverSystem(ekContexts));
			Add(new AuthoritativeRigidbodySystem(ekContexts));
			Add(new GroundCollisionSystem(ekContexts));
			Add(new FlightTerminationSystem(ekContexts, options.TriggerDistance));
			Add(new PredictionGuidanceProgramSystem(contexts, ekContexts, options.ChaseDistance));
			Add(new PredictionGuidedControlSystem(ekContexts, options.ChaseDistance));
		}

		public override void Execute()
		{
			for (var i = 0; i < slicesPerFrame; i += 1)
			{
				for (int index = 0; index < _executeSystems.Count; index += 1)
				{
					_executeSystems[index].Execute();
				}
			}
		}
	}
}
