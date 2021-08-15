using UnityEngine;
using System;
using System.Collections;

namespace CameraTools
{
	// C# generics doesn't have a generic numeric type, so we can't do this generically.
	public class FloatInputField : MonoBehaviour
	{
		public FloatInputField Initialise(double lastUpdated, float currentValue, float minValue = float.MinValue, float maxValue = float.MaxValue) { this.lastUpdated = lastUpdated; this.currentValue = currentValue; this.minValue = minValue; this.maxValue = maxValue; return this; }
		public double lastUpdated;
		public string possibleValue = string.Empty;
		private float _value;
		public float currentValue { get { return _value; } set { _value = value; possibleValue = _value.ToString("G6"); } }
		private float minValue;
		private float maxValue;
		private bool coroutineRunning = false;
		private Coroutine coroutine;

		public void tryParseValue(string v)
		{
			if (v != possibleValue)
			{
				lastUpdated = !string.IsNullOrEmpty(v) ? Time.time : Time.time + 0.5; // Give the empty string an extra 0.5s.
				possibleValue = v;
				if (!coroutineRunning)
				{
					coroutine = StartCoroutine(UpdateValueCoroutine());
				}
			}
		}

		IEnumerator UpdateValueCoroutine()
		{
			coroutineRunning = true;
			while (Time.time - lastUpdated < 0.5)
				yield return new WaitForFixedUpdate();
			tryParseCurrentValue();
			coroutineRunning = false;
			yield return new WaitForFixedUpdate();
		}

		void tryParseCurrentValue()
		{
			float newValue;
			if (float.TryParse(possibleValue, out newValue))
			{
				currentValue = Math.Min(Math.Max(newValue, minValue), maxValue);
				lastUpdated = Time.time;
			}
			possibleValue = currentValue.ToString("G6");
		}

		// Parse the current possible value immediately.
		public void tryParseValueNow()
		{
			tryParseCurrentValue();
			if (coroutineRunning)
			{
				StopCoroutine(coroutine);
				coroutineRunning = false;
			}
		}

		// Update the min aand max values. Note: if min > max, then the max value will always be set.
		public void UpdateLimits(float minValue, float maxValue)
		{
			this.minValue = minValue;
			this.maxValue = maxValue;
		}
	}
}