using UnityEngine;
using System;
using System.Reflection;

namespace CameraTools.Integration
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class TimeControl : MonoBehaviour
	{
		public static TimeControl instance;
		public bool hasTimeControl = false;

		object timeControlInstance = null;
		PropertyInfo timeControlCameraZoomFixProperty = null;
		bool timeControlCameraZoomFixOriginalValue = false;

		void Awake()
		{
			if (instance is not null) Destroy(instance);
			instance = this;
		}

		void Start()
		{
			FindTimeControlCameraZoomFix(); // Time Control's camera zoom fix breaks CameraTools.
		}

		public void FindTimeControlCameraZoomFix()
		{
			try
			{
				foreach (var assy in AssemblyLoader.loadedAssemblies)
				{
					if (assy.assembly.FullName.Contains("TimeControl"))
					{
						foreach (var type in assy.assembly.GetTypes())
						{
							if (type == null) continue;
							if (type.Name == "GlobalSettings")
							{
								timeControlInstance = FindObjectOfType(type);
								if (timeControlInstance != null)
								{
									foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
									{
										if (propertyInfo != null && propertyInfo.Name == "CameraZoomFix")
										{
											timeControlCameraZoomFixProperty = propertyInfo;
											timeControlCameraZoomFixOriginalValue = (bool)propertyInfo.GetValue(timeControlInstance);
											hasTimeControl = true;
											return;
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"[CameraTools.Integration.TimeControl]: Failed to locate CameraZoomFix in TimeControl: {e.Message}");
			}
		}

		public void SetTimeControlCameraZoomFix(bool restore)
		{
			if (!hasTimeControl || timeControlCameraZoomFixProperty == null || !timeControlCameraZoomFixOriginalValue) return; // Not found or it was originally false, so we can ignore it.
			if (restore) Debug.Log("[CameraTools.Integration.TimeControl]: Restoring CameraZoomFix variable in TimeControl.GlobalSettings to true.");
			else Debug.Log("[CameraTools.Integration.TimeControl]: Setting CameraZoomFix variable in TimeControl.GlobalSettings to false as it breaks CameraTools when running in slow-mo.");
			timeControlCameraZoomFixProperty.SetValue(timeControlInstance, restore && timeControlCameraZoomFixOriginalValue);
		}
	}
}