using UnityEngine;
using System.Collections.Generic;
namespace CameraTools
{
	public struct CameraKeyframe
	{
		public Vector3 position;
		public PositionInterpolationType positionInterpolationType;
		public Quaternion rotation;
		public RotationInterpolationType rotationInterpolationType;
		public float zoom;
		public float time;

		public CameraKeyframe(Vector3 position, Quaternion rotation, float zoom, float time, PositionInterpolationType positionInterpolationType, RotationInterpolationType rotationInterpolationType)
		{
			this.position = position;
			this.rotation = rotation;
			this.zoom = zoom;
			this.time = time;
			this.positionInterpolationType = positionInterpolationType;
			this.rotationInterpolationType = rotationInterpolationType;
		}

	}

	public class CameraKeyframeComparer : IComparer<CameraKeyframe>
	{
		public int Compare(CameraKeyframe a, CameraKeyframe b)
		{
			return a.time.CompareTo(b.time);
		}
	}
}

