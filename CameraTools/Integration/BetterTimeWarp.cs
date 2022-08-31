using UnityEngine;
using System;
using System.Reflection;

namespace CameraTools.Integration
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BetterTimeWarp : MonoBehaviour
	{
		public static BetterTimeWarp instance;
		public bool hasBetterTimeWarp = false;

		object betterTimeWarpInstance = null;
		FieldInfo betterTimeWarpScaleCameraSpeedField = null;
		bool betterTimeWarpScaleCameraSpeedOriginalValue = false;

		void Awake()
		{
			if (instance is not null) Destroy(instance);
			instance = this;
		}

		void Start()
		{
			FindBetterTimeWarpScaleWarpSpeed(); // Better Time Warp's ScaleCameraSpeed breaks CameraTools.
		}

		void FindBetterTimeWarpScaleWarpSpeed()
		{
			try
			{
				foreach (var assy in AssemblyLoader.loadedAssemblies)
				{
					if (assy.assembly.FullName.Contains("BetterTimeWarp")) // BetterTimeWarpContinued
					{
						foreach (var type in assy.assembly.GetTypes())
						{
							if (type == null) continue;
							if (type.Name == "BetterTimeWarp")
							{
								betterTimeWarpInstance = FindObjectOfType(type);
								if (betterTimeWarpInstance != null)
								{
									foreach (var fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
									{
										if (fieldInfo != null && fieldInfo.Name == "ScaleCameraSpeed")
										{
											betterTimeWarpScaleCameraSpeedField = fieldInfo;
											betterTimeWarpScaleCameraSpeedOriginalValue = (bool)fieldInfo.GetValue(betterTimeWarpInstance);
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
				Debug.LogError($"[CameraTools.Integration.BetterTimeWarp]: Failed to locate ScaleCameraSpeed in BetterTimeWarp (Continued): {e.Message}");
			}
		}

		public void SetBetterTimeWarpScaleCameraSpeed(bool restore)
		{
			if (!hasBetterTimeWarp || betterTimeWarpScaleCameraSpeedField == null || !betterTimeWarpScaleCameraSpeedOriginalValue) return; // Not found or it was originally false, so we can ignore it.
			if (restore) Debug.Log("[CameraTools.Integration.BetterTimeWarp]: Restoring ScaleCameraSpeed variable in BetterTimeWarp.BetterTimeWarp to true.");
			else Debug.Log("[CameraTools.Integration.BetterTimeWarp]: Setting ScaleCameraSpeed variable in BetterTimeWarp.BetterTimeWarp to false as it breaks CameraTools when running in slow-mo.");
			betterTimeWarpScaleCameraSpeedField.SetValue(betterTimeWarpInstance, restore);
		}
	}
}