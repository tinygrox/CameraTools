using System;
using System.Collections.Generic;
using UnityEngine;

namespace CameraTools
{
	public class CameraPath
	{
		public string PathName;
		int _keyCount = 0;
		public int KeyframeCount
		{
			get
			{
				return _keyCount;
			}
			private set
			{
				_keyCount = value;
			}
		}
		public List<Vector3> Points;
		public List<Quaternion> Rotations;
		public List<float> Times;
		public List<float> Zooms;

		public float LerpRate = 15;
		public float TimeScale = 1;

		Vector3Animation _pointCurve;
		RotationAnimation _rotationCurve;
		AnimationCurve _zoomCurve;

		public CameraPath()
		{
			PathName = "New Path";
			Points = new List<Vector3>();
			Rotations = new List<Quaternion>();
			Times = new List<float>();
			Zooms = new List<float>();
		}

		public static CameraPath Load(ConfigNode node)
		{
			CameraPath newPath = new CameraPath();

			newPath.PathName = node.GetValue("pathName");
			newPath.Points = ParseVectorList(node.GetValue("points"));
			newPath.Rotations = ParseQuaternionList(node.GetValue("rotations"));
			newPath.Times = ParseFloatList(node.GetValue("times"));
			newPath.Zooms = ParseFloatList(node.GetValue("zooms"));
			newPath.LerpRate = float.Parse(node.GetValue("lerpRate"));
			newPath.TimeScale = float.Parse(node.GetValue("timeScale"));
			newPath.Refresh();

			return newPath;
		}

		public void Save(ConfigNode node)
		{
			Debug.Log("Saving path: " + PathName);
			ConfigNode pathNode = node.AddNode("CAMERAPATH");
			pathNode.AddValue("pathName", PathName);
			pathNode.AddValue("points", WriteVectorList(Points));
			pathNode.AddValue("rotations", WriteQuaternionList(Rotations));
			pathNode.AddValue("times", WriteFloatList(Times));
			pathNode.AddValue("zooms", WriteFloatList(Zooms));
			pathNode.AddValue("lerpRate", LerpRate);
			pathNode.AddValue("timeScale", TimeScale);
		}

		public static string WriteVectorList(List<Vector3> list)
		{
			string output = string.Empty;
			foreach(var val in list)
			{
				output += ConfigNode.WriteVector(val) + ";";
			}
			return output;
		}

		public static string WriteQuaternionList(List<Quaternion> list)
		{
			string output = string.Empty;
			foreach(var val in list)
			{
				output += ConfigNode.WriteQuaternion(val) + ";";
			}
			return output;
		}

		public static string WriteFloatList(List<float> list)
		{
			string output = string.Empty;
			foreach(var val in list)
			{
				output += val.ToString() + ";";
			}
			return output;
		}

		public static List<Vector3> ParseVectorList(string arrayString)
		{
			string[] vectorStrings = arrayString.Split(new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
			List<Vector3> vList = new List<Vector3>();
			for(int i = 0; i < vectorStrings.Length; i++)
			{
				Debug.Log("attempting to parse vector: --" + vectorStrings[i] + "--");
				vList.Add(ConfigNode.ParseVector3(vectorStrings[i]));
			}

			return vList;
		}

		public static List<Quaternion> ParseQuaternionList(string arrayString)
		{
			string[] qStrings = arrayString.Split(new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
			List<Quaternion> qList = new List<Quaternion>();
			for(int i = 0; i < qStrings.Length; i++)
			{
				qList.Add(ConfigNode.ParseQuaternion(qStrings[i]));
			}

			return qList;
		}

		public static List<float> ParseFloatList(string arrayString)
		{
			string[] fStrings = arrayString.Split(new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
			List<float> fList = new List<float>();
			for(int i = 0; i < fStrings.Length; i++)
			{
				fList.Add(float.Parse(fStrings[i]));
			}

			return fList;
		}

		public void AddTransform(Transform cameraTransform, float zoom, float time)
		{
			Points.Add(cameraTransform.localPosition);
			Rotations.Add(cameraTransform.localRotation);
			Zooms.Add(zoom);
			Times.Add(time);
			KeyframeCount = Times.Count;
			Sort();
			UpdateCurves();
		}

		public void SetTransform(int index, Transform cameraTransform, float zoom, float time)
		{
			Points[index] = cameraTransform.localPosition;
			Rotations[index] = cameraTransform.localRotation;
			Zooms[index] = zoom;
			Times[index] = time;
			Sort();
			UpdateCurves();
		}

		public void Refresh()
		{
			KeyframeCount = Times.Count;
			Sort();
			UpdateCurves();
		}

		public void RemoveKeyframe(int index)
		{
			Points.RemoveAt(index);
			Rotations.RemoveAt(index);
			Zooms.RemoveAt(index);
			Times.RemoveAt(index);
			KeyframeCount = Times.Count;
			UpdateCurves();
		}

		public void Sort()
		{
			List<CameraKeyframe> keyframes = new List<CameraKeyframe>();
			for(int i = 0; i < Points.Count; i++)
			{
				keyframes.Add(new CameraKeyframe(Points[i], Rotations[i], Zooms[i], Times[i]));
			}
			keyframes.Sort(new CameraKeyframeComparer());

			for(int i = 0; i < keyframes.Count; i++)
			{
				Points[i] = keyframes[i].Position;
				Rotations[i] = keyframes[i].Rotation;
				Zooms[i] = keyframes[i].Zoom;
				Times[i] = keyframes[i].Time;
			}
		}

		public CameraKeyframe GetKeyframe(int index)
		{
			int i = index;
			return new CameraKeyframe(Points[i], Rotations[i], Zooms[i], Times[i]);
		}

		public void UpdateCurves()
		{
			_pointCurve = new Vector3Animation(Points.ToArray(), Times.ToArray());
			_rotationCurve = new RotationAnimation(Rotations.ToArray(), Times.ToArray());
			_zoomCurve = new AnimationCurve();
			for(int i = 0; i < Zooms.Count; i++)
			{
				_zoomCurve.AddKey(new Keyframe(Times[i], Zooms[i]));
			}
		}

		public CameraTransformation Evaulate(float time)
		{
			CameraTransformation tf = new CameraTransformation();
			tf.Position = _pointCurve.Evaluate(time);
			tf.Rotation = _rotationCurve.Evaluate(time);
			tf.Zoom = _zoomCurve.Evaluate(time);

			return tf;
		}


	


	}
}

