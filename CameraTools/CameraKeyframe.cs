using UnityEngine;
using System.Collections.Generic;
namespace CameraTools
{
	public struct CameraKeyframe 
	{
		public Vector3 Position;
		public Quaternion Rotation;
		public float Zoom;
		public float Time;

		public CameraKeyframe(Vector3 pos, Quaternion rot, float z, float t)
		{
			Position = pos;
			Rotation = rot;
			Zoom = z;
			Time = t;
		}

	}

	public class CameraKeyframeComparer : IComparer<CameraKeyframe>
	{
		public int Compare(CameraKeyframe a, CameraKeyframe b)
		{
			return a.Time.CompareTo(b.Time);
		}
	}
}

