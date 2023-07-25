// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class PredictionRigidbodyDriverSystem : ReactiveSystem<ECS.EkPredictionEntity>
	{
		private readonly ECS.EkTimeContext ekTime;
		private float accumulatedDelta = 0f;

		internal PredictionRigidbodyDriverSystem(ECS.Contexts ekContexts)
			: base(ekContexts.ekPrediction)
		{
			ekTime = ekContexts.ekTime;
		}

		protected override bool Filter(ECS.EkPredictionEntity entity) => entity.hasSliceIndex;

		protected override ICollector<ECS.EkPredictionEntity> GetTrigger(IContext<ECS.EkPredictionEntity> context) =>
			context.CreateCollector(ECS.EkPredictionMatcher.SliceIndex);

		protected override void Execute(List<ECS.EkPredictionEntity> entities)
		{
			var dt = ekTime.timeStep.f;
			var runPhysics = false;
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
				if (sliceIndex >= timeSlices.Length)
				{
					continue;
				}
				if (sliceIndex == timeSlices.Length - 1)
				{
					predicted.RemoveSliceIndex();
					predicted.isPredictionMotionReady = true;
					continue;
				}

				var timeSlice = timeSlices[sliceIndex];
				if (timeSlice.Status != TimeSliceStatus.Active)
				{
					continue;
				}

				var rb = predicted.authoritativeRigidbody.rb;
				var transform = rb.transform;

				var localEulerAngles = transform.localEulerAngles;
				transform.localEulerAngles = new Vector3(localEulerAngles.x, localEulerAngles.y, 0f);
				if (predicted.hasMotionExtraData)
				{
					var ed = predicted.motionExtraData.a[sliceIndex];
					ed.Angles = localEulerAngles;
					predicted.motionExtraData.a[sliceIndex] = ed;
				}

				var guidanceData = predicted.guidanceData.data;
				var steeringChoke = guidanceData.inputSteering != null
					? Mathf.Clamp01(guidanceData.inputSteering.Evaluate(timeSlice.Progress))
					: 1f;
				var throttleChoke = guidanceData.inputThrottle != null
					? Mathf.Clamp01(guidanceData.inputThrottle.Evaluate(timeSlice.Progress))
					: 1f;
				var yaw = timeSlice.DriverInputYaw * steeringChoke * dt;
				var pitch = timeSlice.DriverInputPitch * steeringChoke * dt;
				var throttle = timeSlice.DriverInputThrottle * throttleChoke * dt;
				var torque = new Vector3(pitch * guidanceData.driverPitchForce, -yaw * guidanceData.driverSteeringForce, 0f);
				var throttleForce = throttle * guidanceData.driverAccelerationForce * timeSlice.Facing;
				rb.AddRelativeTorque(torque, ForceMode.VelocityChange);
				rb.AddForce(throttleForce, ForceMode.VelocityChange);

				if (predicted.hasMotionExtraData)
				{
					var ed = predicted.motionExtraData.a[sliceIndex];
					ed.Torque = torque;
					ed.ThrottleForce = throttleForce;
					predicted.motionExtraData.a[sliceIndex] = ed;
				}

				runPhysics = true;

				predicted.ReplaceSliceIndex(sliceIndex + 1);
			}

			if (!runPhysics || Physics.autoSimulation)
			{
				return;
			}

			Physics.SyncTransforms();
			SimulatePhysics(dt);
		}

		private void SimulatePhysics(float dt)
		{
			var defaultPhysicsStep = DataShortcuts.sim.defaultPhysicsStep;
			accumulatedDelta += dt;
			if (DataShortcuts.sim.allowPhysicsSubstepping)
			{
				if (accumulatedDelta <= DataShortcuts.sim.minimumSubstepLength)
				{
					return;
				}

				if (accumulatedDelta <= defaultPhysicsStep)
				{
					Physics.Simulate(accumulatedDelta);
					accumulatedDelta = 0.0f;
					return;
				}

				var step = defaultPhysicsStep;
				if (accumulatedDelta > DataShortcuts.sim.maximumStepLength)
				{
					step = DataShortcuts.sim.maximumStepLength;
				}
				accumulatedDelta -= step;
				Physics.Simulate(step);

				return;
			}

			if (accumulatedDelta < defaultPhysicsStep)
			{
				return;
			}

			accumulatedDelta -= defaultPhysicsStep;
			Physics.Simulate(defaultPhysicsStep);
		}
	}
}
