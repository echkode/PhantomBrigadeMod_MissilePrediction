using System.Collections.Generic;

using Entitas;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class GroundCollisionSystem : ReactiveSystem<ECS.EkPredictionEntity>
	{
		internal GroundCollisionSystem(ECS.Contexts ekContexts) : base(ekContexts.ekPrediction) { }

		protected override bool Filter(ECS.EkPredictionEntity entity) => entity.hasSliceIndex;

		protected override ICollector<ECS.EkPredictionEntity> GetTrigger(IContext<ECS.EkPredictionEntity> context) =>
			context.CreateCollector(ECS.EkPredictionMatcher.SliceIndex);

		protected override void Execute(List<ECS.EkPredictionEntity> entities)
		{
			foreach (var predicted in entities)
			{
				if (predicted.isPredictionMotionReady)
				{
					continue;
				}
				if (!predicted.hasMotionTimeSlices)
				{
					continue;
				}
				if (!predicted.hasSliceIndex)
				{
					continue;
				}

				var timeSlices = predicted.motionTimeSlices.a;
				var sliceIndex = predicted.sliceIndex.i;
				if (0 == sliceIndex || sliceIndex > timeSlices.Length - 1)
				{
					continue;
				}

				if (timeSlices[sliceIndex - 1].Status != TimeSliceStatus.Active)
				{
					continue;
				}
				if (timeSlices[sliceIndex].Status != TimeSliceStatus.Active)
				{
					continue;
				}

				var previousPosition = timeSlices[sliceIndex - 1].Position;
				var currentPosition = timeSlices[sliceIndex].Position;
				if (!Physics.Linecast(previousPosition, currentPosition, out var hitInfo, LayerMasks.environmentMask))
				{
					continue;
				}
				if (!Area.AreaManager.IsPointIndestructible(
					hitInfo.point,
					checkFlag: true,
					checkHeight: false,
					checkEdges: false,
					checkTilesets: true,
					checkFullSurroundings: false))
				{
					continue;
				}

				for (var i = sliceIndex; i < timeSlices.Length; i += 1)
				{
					var ts = timeSlices[i];
					ts.Status = TimeSliceStatus.Destroyed;
					ts.DestroyedBy = DestructionReason.Grounded;
					timeSlices[i] = ts;
				}

				predicted.RemoveSliceIndex();
				predicted.isPredictionMotionReady = true;
			}
		}
	}
}
