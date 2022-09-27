using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	public enum TimeSliceStatus
	{
		Uninitialized = 0,
		Recalculate,
		Active,
		Destroyed,
	}

	public enum DestructionReason
	{
		Unknown = 0,
		Grounded,
		Expired,
		Proximity,
	}

	public struct MotionTimeSlice
	{
		public TimeSliceStatus Status;
		public Vector3 Position;
		public Vector3 Facing;
		public Quaternion Rotation;
		public Vector3 Velocity;
		public float DriverInputYaw;
		public float DriverInputPitch;
		public float DriverInputThrottle;
		public float GuidanceSuspensionTime;
		public float Progress;
		public float TargetBlend;
		public float TargetHeight;
		public Vector3 TargetPosition;
		public float TimeToLive;
		public bool IsTrackingActive;
		public bool IsGuided;
		public DestructionReason DestroyedBy;
	}
}
