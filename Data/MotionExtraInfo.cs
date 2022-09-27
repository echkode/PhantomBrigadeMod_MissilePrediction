using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	public struct MotionExtraInfo
	{
		public bool IsValid;
		public float Realtime;
		public Vector3 ChaseDirection;
		public Vector3 TargetVelocity;
		public Vector3 AdjustedTargetVelocity;
		public Vector3 ChasePosition1;
		public Vector3 ChasePosition2;
		public Vector3 ChaseAltitude;
		public Vector3 LocalChase;
		public float PitchError;
		public float YawError;

		public Vector3 Range;
		public Vector3 RelativeVelocity;
		public Vector3 Angles;
		public float PitchInput;
		public float YawInput;
		public Vector3 Torque;
		public Vector3 ThrottleForce;
	}
}
