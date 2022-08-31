using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System;
using UnityEngine;


namespace CameraTools.Integration
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BDArmory : MonoBehaviour
	{
		#region Public fields
		public static BDArmory instance;
		public bool hasBDA = false;

		[CTPersistantField] public bool autoEnableForBDA = false;
		[CTPersistantField] public bool useBDAutoTarget = false;
		[CTPersistantField] public bool autoTargetIncomingMissiles = true;
		[CTPersistantField] public bool useCentroid = false;
		public bool autoEnableOverride = false;
		public bool hasBDAI = false;
		public bool hasPilotAI = false;
		public List<Vessel> bdWMVessels
		{
			get
			{
				if (hasBDA && (_bdWMVessels is null || Time.time - _bdWMVesselsLastUpdate > 1f)) GetBDVessels(); // Update once per second.
				return _bdWMVessels;
			}
		}
		#endregion

		#region Private fields
		CamTools camTools => CamTools.fetch;
		Vessel vessel => CamTools.fetch.vessel;
		object bdCompetitionInstance = null;
		Func<object, bool> bdCompetitionStartingFieldGetter = null;
		Func<object, bool> bdCompetitionIsActiveFieldGetter = null;
		object bdVesselSpawnerInstance = null;
		Func<object, bool> bdVesselsSpawningFieldGetter = null;
		Func<object, object> bdVesselsSpawningPropertyGetter = null;
		Type bdBDATournamentType = null;
		object bdBDATournamentInstance = null;
		Func<object, bool> bdTournamentWarpInProgressFieldGetter = null;
		bool hasBDWM = false;
		object aiComponent = null;
		object wmComponent = null;
		Func<object, Vessel> bdAITargetFieldGetter = null;
		Func<object, Vessel> bdWmThreatFieldGetter = null;
		Func<object, Vessel> bdWmMissileFieldGetter = null;
		Func<object, bool> bdWmUnderFireFieldGetter = null;
		Func<object, bool> bdWmUnderAttackFieldGetter = null;
		object bdLoadedVesselSwitcherInstance = null;
		Func<object, object> bdLoadedVesselSwitcherVesselsPropertyGetter = null;
		Dictionary<string, List<Vessel>> bdActiveVessels = new Dictionary<string, List<Vessel>>();
		float AItargetUpdateTime = 0;
		Vessel newAITarget = null;
		List<Vessel> _bdWMVessels = new List<Vessel>();
		float _bdWMVesselsLastUpdate = 0;
		#endregion

		void Awake()
		{
			if (instance is not null) Destroy(instance);
			instance = this;
			CTPersistantField.Load("BDArmoryIntegration", typeof(BDArmory), this);
		}

		void Start()
		{
			CheckForBDA();
			if (hasBDA)
			{
				GetAITargetField();
				GetThreatField();
				GetMissileField();
				GetUnderFireField();
				GetUnderAttackField();
				if (FlightGlobals.ActiveVessel is not null)
				{
					CheckForBDAI(FlightGlobals.ActiveVessel);
					CheckForBDWM(FlightGlobals.ActiveVessel);
				}
			}
		}

		void OnDestroy()
		{
			CTPersistantField.Save("BDArmoryIntegration", typeof(BDArmory), this);
		}

		void CheckForBDA()
		{
			// This checks for the existence of a BDArmory assembly and picks out the BDACompetitionMode and VesselSpawner singletons.
			bdCompetitionInstance = null;
			bdCompetitionIsActiveFieldGetter = null;
			bdCompetitionStartingFieldGetter = null;
			bdVesselSpawnerInstance = null;
			bdVesselsSpawningFieldGetter = null;
			bdVesselsSpawningPropertyGetter = null;
			bdLoadedVesselSwitcherVesselsPropertyGetter = null;
			bdBDATournamentType = null;
			bdBDATournamentInstance = null;
			foreach (var assy in AssemblyLoader.loadedAssemblies)
			{
				if (assy.assembly.FullName.Contains("BDArmory"))
				{
					hasBDA = true;
					foreach (var t in assy.assembly.GetTypes())
					{
						if (t != null)
						{
							switch (t.Name)
							{
								case "BDACompetitionMode":
									bdCompetitionInstance = FindObjectOfType(t);
									foreach (var fieldInfo in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
										if (fieldInfo != null)
										{
											switch (fieldInfo.Name)
											{
												case "competitionStarting":
													bdCompetitionStartingFieldGetter = ReflectionUtils.CreateGetter<object, bool>(fieldInfo);
													break;
												case "competitionIsActive":
													bdCompetitionIsActiveFieldGetter = ReflectionUtils.CreateGetter<object, bool>(fieldInfo);
													break;
												default:
													break;
											}
										}
									break;
								case "VesselSpawnerStatus":
									foreach (var propertyInfo in t.GetProperties(BindingFlags.Public | BindingFlags.Static))
										if (propertyInfo != null && propertyInfo.Name == "inhibitCameraTools")
										{
											bdVesselsSpawningPropertyGetter = ReflectionUtils.BuildGetAccessor(propertyInfo.GetGetMethod());
											if (bdVesselsSpawningFieldGetter != null) // Clear the deprecated field.
											{ bdVesselsSpawningFieldGetter = null; }
											break;
										}
									break;
								case "LoadedVesselSwitcher":
									bdLoadedVesselSwitcherInstance = FindObjectOfType(t);
									foreach (var propertyInfo in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
										if (propertyInfo != null && propertyInfo.Name == "Vessels")
										{
											bdLoadedVesselSwitcherVesselsPropertyGetter = ReflectionUtils.BuildGetAccessor(propertyInfo.GetGetMethod());
											break;
										}
									break;
								case "VesselSpawner":
									if (bdVesselsSpawningPropertyGetter == null)
									{
										if (!t.IsSubclassOf(typeof(UnityEngine.Object))) continue; // In BDArmory v1.5.0 and upwards, VesselSpawner is a static class.
										bdVesselSpawnerInstance = FindObjectOfType(t);
										foreach (var fieldInfo in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
											if (fieldInfo != null && fieldInfo.Name == "vesselsSpawning") // deprecated in favour of VesselSpawnerStatus.inhibitCameraTools
											{
												bdVesselsSpawningFieldGetter = ReflectionUtils.CreateGetter<object, bool>(fieldInfo);
												break;
											}
									}
									break;
								case "BDATournament":
									bdBDATournamentType = t;
									bdBDATournamentInstance = FindObjectOfType(bdBDATournamentType);
									foreach (var fieldInfo in bdBDATournamentType.GetFields(BindingFlags.Public | BindingFlags.Instance))
										if (fieldInfo != null && fieldInfo.Name == "warpingInProgress")
										{
											bdTournamentWarpInProgressFieldGetter = ReflectionUtils.CreateGetter<object, bool>(fieldInfo);
											break;
										}
									break;
								default:
									break;
							}
						}
					}
				}
			}
		}

		public void CheckForBDAI(Vessel v)
		{
			hasBDAI = false;
			hasPilotAI = false;
			aiComponent = null;
			if (v)
			{
				foreach (Part p in v.parts)
				{ // We actually want BDGenericAIBase, but we can't use GetComponent(string) to find it, so we look for its subclasses.
					if (p.GetComponent("BDModulePilotAI"))
					{
						hasBDAI = true;
						hasPilotAI = true;
						aiComponent = (object)p.GetComponent("BDModulePilotAI");
						return;
					}
					if (p.GetComponent("BDModuleVTOLAI"))
					{
						hasBDAI = true;
						hasPilotAI = true;
						aiComponent = (object)p.GetComponent("BDModuleVTOLAI");
						return;
					}
					if (p.GetComponent("BDModuleSurfaceAI"))
					{
						hasBDAI = true;
						hasPilotAI = false;
						aiComponent = (object)p.GetComponent("BDModuleSurfaceAI");
						return;
					}
				}
			}
		}

		public bool CheckForBDWM(Vessel v)
		{
			hasBDWM = false;
			wmComponent = null;
			if (v)
			{
				foreach (Part p in v.parts)
				{
					if (p.GetComponent("MissileFire"))
					{
						hasBDWM = true;
						wmComponent = (object)p.GetComponent("MissileFire");
						return true;
					}
				}
			}
			return false;
		}

		Vessel GetAITargetedVessel()
		{
			// Missiles are high priority.
			if (autoTargetIncomingMissiles && hasBDWM && wmComponent != null && bdWmMissileFieldGetter != null)
			{
				var missile = bdWmMissileFieldGetter(wmComponent); // Priority 1: incoming missiles.
				if (missile != null) return missile;
			}

			// Don't update too often unless there's no target.
			if (camTools.dogfightTarget != null && Time.time - AItargetUpdateTime < 3) return camTools.dogfightTarget;

			// Threats
			if (hasBDWM && wmComponent != null && bdWmThreatFieldGetter != null)
			{
				bool underFire = bdWmUnderFireFieldGetter(wmComponent); // Getting attacked by guns.
				bool underAttack = autoTargetIncomingMissiles && bdWmUnderAttackFieldGetter(wmComponent); // Getting attacked by guns or missiles.

				if (underFire || underAttack)
				{
					var threat = bdWmThreatFieldGetter(wmComponent); // Priority 2: incoming fire (can also be missiles).
					if (threat != null) return threat;
				}
			}

			// Targets
			if (hasBDAI && aiComponent != null && bdAITargetFieldGetter != null)
			{
				var target = bdAITargetFieldGetter(aiComponent); // Priority 3: the current vessel's target.
				if (target != null) return target;
			}
			return null;
		}

		Type AIModuleType()
		{
			foreach (var assy in AssemblyLoader.loadedAssemblies)
			{
				if (assy.assembly.FullName.Contains("BDArmory"))
				{
					foreach (var t in assy.assembly.GetTypes())
					{
						if (t.Name == "BDGenericAIBase")
						{
							if (CamTools.DEBUG) Debug.Log("[CameraTools]: Found BDGenericAIBase type.");
							return t;
						}
					}
				}
			}

			return null;
		}

		Type WeaponManagerType()
		{
			foreach (var assy in AssemblyLoader.loadedAssemblies)
			{
				if (assy.assembly.FullName.Contains("BDArmory"))
				{
					foreach (var t in assy.assembly.GetTypes())
					{
						if (t.Name == "MissileFire")
						{
							if (CamTools.DEBUG) Debug.Log("[CameraTools]: Found MissileFire type.");
							return t;
						}
					}
				}
			}

			return null;
		}

		public void UpdateAIDogfightTarget()
		{
			if (hasBDAI && hasBDWM && useBDAutoTarget)
			{
				newAITarget = GetAITargetedVessel();
				if (newAITarget != null && newAITarget != camTools.dogfightTarget)
				{
					if (CamTools.DEBUG)
					{
						var message = "Switching dogfight target to " + newAITarget.vesselName + (camTools.dogfightTarget != null ? " from " + camTools.dogfightTarget.vesselName : "");
						Debug.Log("[CameraTools]: " + message);
						CamTools.DebugLog(message);
					}
					camTools.dogfightTarget = newAITarget;
					AItargetUpdateTime = Time.time;
				}
			}
		}

		public void AutoEnableForBDA()
		{
			if (!hasBDA) return;
			try
			{
				if (bdVesselsSpawningPropertyGetter != null && (bool)bdVesselsSpawningPropertyGetter(null))
				{
					if (autoEnableOverride)
						return; // Still spawning.
					else
					{
						Debug.Log("[CameraTools]: Deactivating CameraTools while spawning vessels.");
						autoEnableOverride = true;
						camTools.RevertCamera();
						return;
					}
				}

				if (bdVesselsSpawningFieldGetter != null && bdVesselsSpawningFieldGetter(bdVesselSpawnerInstance)) // Deprecated.
				{
					if (autoEnableOverride)
						return; // Still spawning.
					else
					{
						Debug.Log("[CameraTools]: Deactivating CameraTools while spawning vessels.");
						autoEnableOverride = true;
						camTools.RevertCamera();
						return;
					}
				}

				if (bdTournamentWarpInProgressFieldGetter != null && bdTournamentWarpInProgressFieldGetter(bdBDATournamentInstance))
				{
					if (autoEnableOverride)
						return; // Still warping.
					else
					{
						Debug.Log("[CameraTools]: Deactivating CameraTools while warping between rounds.");
						autoEnableOverride = true;
						camTools.RevertCamera();
						return;
					}
				}

				autoEnableOverride = false;
				if (camTools.cameraToolActive) return; // It's already active.

				if (vessel == null || (hasPilotAI && vessel.LandedOrSplashed)) return; // Don't activate for landed/splashed planes.
				if (bdCompetitionStartingFieldGetter != null && bdCompetitionStartingFieldGetter(bdCompetitionInstance))
				{
					Debug.Log("[CameraTools]: Activating CameraTools for BDArmory competition as competition is starting.");
					camTools.cameraActivate();
					return;
				}
				else if (bdCompetitionIsActiveFieldGetter != null && bdCompetitionIsActiveFieldGetter(bdCompetitionInstance))
				{
					Debug.Log("[CameraTools]: Activating CameraTools for BDArmory competition as competition is active.");
					UpdateAIDogfightTarget();
					camTools.cameraActivate();
					return;
				}
			}
			catch (Exception e)
			{
				Debug.LogError("[CameraTools]: Checking competition state of BDArmory failed: " + e.Message);
				bdCompetitionIsActiveFieldGetter = null;
				bdCompetitionStartingFieldGetter = null;
				bdCompetitionInstance = null;
				bdVesselsSpawningFieldGetter = null;
				bdVesselsSpawningPropertyGetter = null;
				bdVesselSpawnerInstance = null;
				CheckForBDA();
			}
		}

		FieldInfo GetThreatField()
		{
			Type wmModType = WeaponManagerType();
			if (wmModType == null) return null;

			FieldInfo[] fields = wmModType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var f in fields)
			{
				if (f.Name == "incomingThreatVessel")
				{
					bdWmThreatFieldGetter = ReflectionUtils.CreateGetter<object, Vessel>(f);
					if (CamTools.DEBUG) Debug.Log($"[CameraTools]: Created bdWmThreatFieldGetter.");
					return f;
				}
			}

			return null;
		}

		FieldInfo GetMissileField()
		{
			Type wmModType = WeaponManagerType();
			if (wmModType == null) return null;

			FieldInfo[] fields = wmModType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var f in fields)
			{
				if (f.Name == "incomingMissileVessel")
				{
					bdWmMissileFieldGetter = ReflectionUtils.CreateGetter<object, Vessel>(f);
					if (CamTools.DEBUG) Debug.Log($"[CameraTools]: Created bdWmMissileFieldGetter.");
					return f;
				}
			}

			return null;
		}

		FieldInfo GetUnderFireField()
		{
			Type wmModType = WeaponManagerType();
			if (wmModType == null) return null;

			FieldInfo[] fields = wmModType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var f in fields)
			{
				if (f.Name == "underFire")
				{
					bdWmUnderFireFieldGetter = ReflectionUtils.CreateGetter<object, bool>(f);
					if (CamTools.DEBUG) Debug.Log($"[CameraTools]: Created bdWmUnderFireFieldGetter.");
					return f;
				}
			}

			return null;
		}

		FieldInfo GetUnderAttackField()
		{
			Type wmModType = WeaponManagerType();
			if (wmModType == null) return null;

			FieldInfo[] fields = wmModType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var f in fields)
			{
				if (f.Name == "underAttack")
				{
					bdWmUnderAttackFieldGetter = ReflectionUtils.CreateGetter<object, bool>(f);
					if (CamTools.DEBUG) Debug.Log("[CameraTools]: Created bdWmUnderAttackFieldGetter.");
					return f;
				}
			}

			return null;
		}

		FieldInfo GetAITargetField()
		{
			Type aiModType = AIModuleType();
			if (aiModType == null) return null;

			FieldInfo[] fields = aiModType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var f in fields)
			{
				if (f.Name == "targetVessel")
				{
					bdAITargetFieldGetter = ReflectionUtils.CreateGetter<object, Vessel>(f);
					if (CamTools.DEBUG) Debug.Log("[CameraTools]: Created bdAITargetFieldGetter.");
					return f;
				}
			}

			return null;
		}

		public void GetBDVessels()
		{
			if (!hasBDA || bdLoadedVesselSwitcherVesselsPropertyGetter == null || bdLoadedVesselSwitcherInstance == null) return;
			bdActiveVessels = (Dictionary<string, List<Vessel>>)bdLoadedVesselSwitcherVesselsPropertyGetter(bdLoadedVesselSwitcherInstance);
			_bdWMVessels = bdActiveVessels.SelectMany(kvp => kvp.Value).ToList(); // FIXME Remove this once SI updates the Centroid mode using bdActiveVessels.
			_bdWMVesselsLastUpdate = Time.time;
		}

		public Vector3 GetCentroid()
		{
			Vector3 centroid = Vector3.zero;
			int count = 1;

			foreach (var v in bdWMVessels)
			{
				if (v == null || !v.loaded || v.packed) continue;
				if ((v.CoM - FlightGlobals.ActiveVessel.CoM).magnitude > 20000) continue;
				if (!v.isActiveVessel)
				{
					centroid += v.transform.position;
					++count;
				}
			}
			centroid /= (float)count;
			return centroid;
		}
	}
}