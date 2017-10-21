using UnityEngine;

namespace CameraTools
{
	public class Curve3D 
	{
		private Vector3[] _points;
		private float[] _times;
		private AnimationCurve[] _curves;

		bool _curveReady = false;

		// Use this for initialization
		public Curve3D() 
		{
			_curves = new AnimationCurve[]{new AnimationCurve(), new AnimationCurve(), new AnimationCurve()};
		}

		public Curve3D(Vector3[] newPoints, float[] newTimes)
		{
			_curves = new AnimationCurve[]{new AnimationCurve(), new AnimationCurve(), new AnimationCurve()};
			SetPoints(newPoints, newTimes);
		}

		public void SetPoints(Vector3[] newPoints, float[] newTimes)
		{
			if(newPoints.Length != newTimes.Length)
			{
				Debug.LogError("Curve3D: points array must be same length as times array");
				return;
			}
			_points = new Vector3[newPoints.Length];
			_times = new float[newPoints.Length];
			for(int i = 0; i < _points.Length; i++)
			{
				_points[i] = newPoints[i];
				_times[i] = newTimes[i];
			}

			UpdateCurve();
		}

		public void SetPoint(int index, Vector3 newPoint, float newTime)
		{
			if(index < _points.Length)
			{
				SetAnimKey(index, newPoint, newTime);
			}
			else
			{
				Debug.LogError("Tried to set new point in a Curve3D beyond the existing array.  Not yet implemented.");
			}
		}


		private void UpdateCurve()
		{
			_curveReady = false;
			//clear existing keys
			for(int i = 0; i < 3; i++)
			{
				_curves[i] = new AnimationCurve();
			}

			if(_points.Length == 0) return;

			for(int i = 0; i < _points.Length; i++)
			{
				SetAnimKey(i, _points[i], _times[i]);
			}

			_curveReady = true;
		}

		void SetAnimKey(int index, Vector3 point, float time)
		{
			if(index >= _curves[0].keys.Length)
			{
				_curves[0].AddKey(time, point.x);
				_curves[1].AddKey(time, point.y);
				_curves[2].AddKey(time, point.z);
			}
			else
			{
				_curves[0].MoveKey(index, new Keyframe(time, point.x));
				_curves[1].MoveKey(index, new Keyframe(time, point.y));
				_curves[2].MoveKey(index, new Keyframe(time, point.z));
			}
		}

		public Vector3 GetPoint(float time)
		{
			if(!_curveReady)
			{
				Debug.LogWarning("Curve was accessed but it was not properly initialized.");
				return Vector3.zero;
			}

			float x = _curves[0].Evaluate(time);
			float y = _curves[1].Evaluate(time);
			float z = _curves[2].Evaluate(time);
			return new Vector3(x,y,z);
		}

		public Vector3 GetTangent(float time)
		{
			if(!_curveReady)
			{
				Debug.LogWarning("Curve was accessed but it was not properly initialized.");
				return Vector3.one;
			}

			if(time < 1)
			{
				return (GetPoint(Mathf.Min(time+0.01f, 1))-GetPoint(time)).normalized;
			}
			else
			{
				return (GetPoint(1)-GetPoint(0.99f)).normalized;
			}
		}


	}
}

