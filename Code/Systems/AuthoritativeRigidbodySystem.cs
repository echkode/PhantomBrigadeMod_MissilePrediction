// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class AuthoritativeRigidbodySystem : ReactiveSystem<ECS.EkPredictionEntity>
	{
		private readonly List<ECS.EkPredictionEntity> updates = new List<ECS.EkPredictionEntity>();

		internal AuthoritativeRigidbodySystem(ECS.Contexts ekContexts) : base(ekContexts.ekPrediction) {}

		protected override bool Filter(ECS.EkPredictionEntity entity) => entity.hasSliceIndex;

		protected override ICollector<ECS.EkPredictionEntity> GetTrigger(IContext<ECS.EkPredictionEntity> context) =>
			context.CreateCollector(ECS.EkPredictionMatcher.SliceIndex);

		protected override void Execute(List<ECS.EkPredictionEntity> entities)
		{
			updates.Clear();
			foreach (var entity in entities)
			{
				if (entity.isPredictionMotionReady)
				{
					continue;
				}
				if (!entity.hasMotionTimeSlices)
				{
					continue;
				}
				if (!entity.hasSliceIndex)
				{
					continue;
				}
				if (entity.sliceIndex.i >= entity.motionTimeSlices.a.Length)
				{
					continue;
				}
				updates.Add(entity);
			}

			foreach (var entity in updates)
			{
				var sliceIndex = entity.sliceIndex.i;
				var timeSlice = entity.motionTimeSlices.a[sliceIndex];
				timeSlice.Status = TimeSliceStatus.Active;

				var rb = entity.authoritativeRigidbody.rb;
				var transform = rb.transform;
				timeSlice.Position = transform.position;
				timeSlice.Facing = transform.forward;
				timeSlice.Rotation = transform.rotation;
				timeSlice.Velocity = rb.velocity;

				entity.motionTimeSlices.a[sliceIndex] = timeSlice;
			}
		}
	}
}
