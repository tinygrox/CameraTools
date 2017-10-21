using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using KSP.UI.Screens;
namespace CameraTools
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class CamTools : MonoBehaviour
	{
		public static CamTools Fetch;

		GameObject _cameraParent;
		Vessel _vessel;
		Vector3 _origPosition;
		Quaternion _origRotation;
		Transform _origParent;
		float _origNearClip;
		FlightCamera _flightCamera;
		
		Part _camTarget = null;

		[CtPersistantField]
		public ReferenceModes ReferenceMode = ReferenceModes.Surface;
		Vector3 _cameraUp = Vector3.up;

		string _fmUpKey = "[7]";
		string _fmDownKey = "[1]";
		string _fmForwardKey = "[8]";
		string _fmBackKey = "[5]";
		string _fmLeftKey = "[4]";
		string _fmRightKey = "[6]";
		string _fmZoomInKey = "[9]";
		string _fmZoomOutKey = "[3]";
		//
		
		
		//current camera setting
		bool _cameraToolActive = false;
		
		
		//GUI
		public static bool GuiEnabled = false;
		public static bool HasAddedButton = false;
		bool _updateFov = false;
		float _windowWidth = 250;
		float _windowHeight = 400;
		float _draggableHeight = 40;
		float _leftIndent = 12;
		float _entryHeight = 20;

		[CtPersistantField]
		public ToolModes ToolMode = ToolModes.StationaryCamera;

		Rect _windowRect = new Rect(0,0,0,0);
		bool _gameUiToggle = true;
		float _incrButtonWidth = 26;
		
		//stationary camera vars
		[CtPersistantField]
		public bool AutoFlybyPosition = false;
		[CtPersistantField]
		public bool AutoFov = false;
		float _manualFov = 60;
		float _currentFov = 60;
		Vector3 _manualPosition = Vector3.zero;
		[CtPersistantField]
		public float FreeMoveSpeed = 10;
		string _guiFreeMoveSpeed = "10";
		[CtPersistantField]
		public float KeyZoomSpeed = 1;
		string _guiKeyZoomSpeed = "1";
		float _zoomFactor = 1;
		[CtPersistantField]
		public float ZoomExp = 1;
		[CtPersistantField]
		public bool EnableKeypad = false;
		[CtPersistantField]
		public float MaxRelV = 2500;
		
		bool _setPresetOffset = false;
		Vector3 _presetOffset = Vector3.zero;
		bool _hasSavedRotation = false;
		Quaternion _savedRotation;
		[CtPersistantField]
		public bool ManualOffset = false;
		[CtPersistantField]
		public float ManualOffsetForward = 500;
		[CtPersistantField]
		public float ManualOffsetRight = 50;
		[CtPersistantField]
		public float ManualOffsetUp = 5;
		string _guiOffsetForward = "500";
		string _guiOffsetRight = "50";
		string _guiOffsetUp = "5";
		
		Vector3 _lastVesselPosition = Vector3.zero;
		Vector3 _lastTargetPosition = Vector3.zero;
		bool _hasTarget = false;

		[CtPersistantField]
		public bool UseOrbital = false;

		[CtPersistantField]
		public bool TargetCoM = false;

		bool _hasDied = false;
		float _diedTime = 0;
		//vessel reference mode
		Vector3 _initialVelocity = Vector3.zero;
		Vector3 _initialPosition = Vector3.zero;
		Orbit _initialOrbit = null;
		double _initialUt;
		
		//retaining position and rotation after vessel destruction
		Vector3 _lastPosition;
		Quaternion _lastRotation;
		
		
		//click waiting stuff
		bool _waitingForTarget = false;
		bool _waitingForPosition = false;

		bool _mouseUp = false;

		//Keys
		[CtPersistantField]
		public string CameraKey = "home";
		[CtPersistantField]
		public string RevertKey = "end";

		//recording input for key binding
		bool _isRecordingInput = false;
		bool _isRecordingActivate = false;
		bool _isRecordingRevert = false;

		Vector3 _resetPositionFix;//fixes position movement after setting and resetting camera
			
		//floating origin shift handler
		Vector3d _lastOffset = FloatingOrigin.fetch.offset;

		AudioSource[] _audioSources;
		float[] _originalAudioSourceDoppler;
		bool _hasSetDoppler = false;

		[CtPersistantField]
		public bool UseAudioEffects = true;

		//camera shake
		Vector3 _shakeOffset = Vector3.zero;
		float _shakeMagnitude = 0;
		[CtPersistantField]
		public float ShakeMultiplier = 1;

		public delegate void ResetCTools();
		public static event ResetCTools OnResetCTools;
		public static double SpeedOfSound = 330;

		//dogfight cam
		Vessel _dogfightPrevTarget;
		Vessel _dogfightTarget;
		[CtPersistantField]
		float _dogfightDistance = 30;
		[CtPersistantField]
		float _dogfightOffsetX = 10;
		[CtPersistantField]
		float _dogfightOffsetY = 4;
		float _dogfightMaxOffset = 50;
		float _dogfightLerp = 20;
		[CtPersistantField]
		float _autoZoomMargin = 20;
		List<Vessel> _loadedVessels;
		bool _showingVesselList = false;
		bool _dogfightLastTarget = false;
		Vector3 _dogfightLastTargetPosition;
		Vector3 _dogfightLastTargetVelocity;
		bool _dogfightVelocityChase = false;
		//bdarmory
		bool _hasBdai = false;
		[CtPersistantField]
		public bool UseBdAutoTarget = false;
		object _aiComponent = null;
		FieldInfo _bdAiTargetField;


		//pathing
		int _selectedPathIndex = -1;
		List<CameraPath> _availablePaths;
		CameraPath CurrentPath
		{
			get
			{
				if(_selectedPathIndex >= 0 && _selectedPathIndex < _availablePaths.Count)
				{
					return _availablePaths[_selectedPathIndex];
				}
				else
				{
					return null;
				}
			}
		}
		int _currentKeyframeIndex = -1;
		float _currentKeyframeTime;
		string _currKeyTimeString;
		bool _showKeyframeEditor = false;
		float _pathStartTime;
		bool _isPlayingPath = false;
		float PathTime
		{
			get
			{
				return Time.time - _pathStartTime;
			}
		}
		Vector2 _keysScrollPos;

		void Awake()
		{
			if(Fetch)
			{
				Destroy(Fetch);
			}

			Fetch = this;

			Load();

			_guiOffsetForward = ManualOffsetForward.ToString();
			_guiOffsetRight = ManualOffsetRight.ToString();
			_guiOffsetUp = ManualOffsetUp.ToString();
			_guiKeyZoomSpeed = KeyZoomSpeed.ToString();
			_guiFreeMoveSpeed = FreeMoveSpeed.ToString();
		}

		void Start()
		{
			_windowRect = new Rect(Screen.width-_windowWidth-40, 0, _windowWidth, _windowHeight);
			_flightCamera = FlightCamera.fetch;
			_cameraToolActive = false;
			SaveOriginalCamera();
			
			AddToolbarButton();
			
			GameEvents.onHideUI.Add(GameUiDisable);
			GameEvents.onShowUI.Add(GameUiEnable);
			//GameEvents.onGamePause.Add (PostDeathRevert);
			GameEvents.OnVesselRecoveryRequested.Add(PostDeathRevert);
			GameEvents.onFloatingOriginShift.Add(OnFloatingOriginShift);
			GameEvents.onGameSceneLoadRequested.Add(PostDeathRevert);
			
			_cameraParent = new GameObject("StationaryCameraParent");
			//cameraParent.SetActive(true);
			//cameraParent = (GameObject) Instantiate(cameraParent, Vector3.zero, Quaternion.identity);
			
			if(FlightGlobals.ActiveVessel != null)
			{
				_cameraParent.transform.position = FlightGlobals.ActiveVessel.transform.position;
				_vessel = FlightGlobals.ActiveVessel;

				CheckForBdai(FlightGlobals.ActiveVessel);
			}
			_bdAiTargetField = GetAiTargetField();
			GameEvents.onVesselChange.Add(SwitchToVessel);
		}

		void OnDestroy()
		{
			GameEvents.onVesselChange.Remove(SwitchToVessel);
		}
		
		void Update()
		{
			if(!_isRecordingInput)
			{
				if(Input.GetKeyDown(KeyCode.KeypadDivide))
				{
					GuiEnabled = !GuiEnabled;	
				}
				
				if(Input.GetKeyDown(RevertKey))
				{
					RevertCamera();	
				}
				else if(Input.GetKeyDown(CameraKey))
				{
					if(ToolMode == ToolModes.StationaryCamera)
					{
						if(!_cameraToolActive)
						{
							SaveOriginalCamera();
							StartStationaryCamera();
						}
						else
						{
							//RevertCamera();
							StartStationaryCamera();
						}
					}
					else if(ToolMode == ToolModes.DogfightCamera)
					{
						if(!_cameraToolActive)
						{
							SaveOriginalCamera();
							StartDogfightCamera();
						}
						else
						{
							StartDogfightCamera();
						}
					}
					else if(ToolMode == ToolModes.Pathing)
					{
						if(!_cameraToolActive)
						{
							SaveOriginalCamera();
						}
						StartPathingCam();
						PlayPathingCam();
					}
				}


			}

			if(Input.GetMouseButtonUp(0))
			{
				_mouseUp = true;
			}
			
			
			//get target transform from mouseClick
			if(_waitingForTarget && _mouseUp && Input.GetKeyDown(KeyCode.Mouse0))
			{
				Part tgt = GetPartFromMouse();
				if(tgt!=null)
				{
					_camTarget = tgt;
					_hasTarget = true;
				}
				else 
				{
					Vector3 pos = GetPosFromMouse();
					if(pos != Vector3.zero)
					{
						_lastTargetPosition = pos;
						_hasTarget = true;
					}
				}
				
				_waitingForTarget = false;
			}
			
			//set position from mouseClick
			if(_waitingForPosition && _mouseUp && Input.GetKeyDown(KeyCode.Mouse0))
			{
				Vector3 pos = GetPosFromMouse();
				if(pos!=Vector3.zero)// && isStationaryCamera)
				{
					_presetOffset = pos;
					_setPresetOffset = true;
				}
				else Debug.Log ("No pos from mouse click");
				
				_waitingForPosition = false;
			}
			
			
			
		}

		public void ShakeCamera(float magnitude)
		{
			_shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
		}
		
		
		int _posCounter = 0;//debug
		void FixedUpdate()
		{
			if(!FlightGlobals.ready)
			{
				return;
			}

			if(FlightGlobals.ActiveVessel != null && (_vessel==null || _vessel!=FlightGlobals.ActiveVessel))
			{
				_vessel = FlightGlobals.ActiveVessel;
			}
				
			if(_vessel != null)
			{
				_lastVesselPosition = _vessel.transform.position;
			}


			//stationary camera
			if(_cameraToolActive)
			{
				if(ToolMode == ToolModes.StationaryCamera)
				{
					UpdateStationaryCamera();
				}
				else if(ToolMode == ToolModes.DogfightCamera)
				{
					UpdateDogfightCamera();
				}
				else if(ToolMode == ToolModes.Pathing)
				{
					UpdatePathingCam();
				}
			}
			else
			{
				if(!AutoFov)
				{
					_zoomFactor = Mathf.Exp(ZoomExp)/Mathf.Exp(1);
				}
			}

			if(ToolMode == ToolModes.DogfightCamera)
			{
				if(_dogfightTarget && _dogfightTarget.isActiveVessel)
				{
					_dogfightTarget = null;
					if(_cameraToolActive)
					{
						RevertCamera();
					}
				}
			}
			
			
			if(_hasDied && Time.time-_diedTime > 2)
			{
				RevertCamera();	
			}
		}

		void StartDogfightCamera()
		{
			if(FlightGlobals.ActiveVessel == null)
			{
				Debug.Log("No active vessel.");
				return;
			}



			if(!_dogfightTarget)
			{
				_dogfightVelocityChase = true;
			}
			else
			{
				_dogfightVelocityChase = false;
			}

			_dogfightPrevTarget = _dogfightTarget;

			_hasDied = false;
			_vessel = FlightGlobals.ActiveVessel;
			_cameraUp = -FlightGlobals.getGeeForceAtPosition(_vessel.CoM).normalized;

            _flightCamera.SetTargetNone();
            _flightCamera.transform.parent = _cameraParent.transform;
            _flightCamera.DeactivateUpdate();
            _cameraParent.transform.position = _vessel.transform.position+_vessel.rb_velocity*Time.fixedDeltaTime;

			_cameraToolActive = true;

			ResetDoppler();
			if(OnResetCTools != null)
			{
				OnResetCTools();
			}

			SetDoppler(false);
			AddAtmoAudioControllers(false);
		}

		void UpdateDogfightCamera()
		{
			if(!_vessel || (!_dogfightTarget && !_dogfightLastTarget && !_dogfightVelocityChase))
			{
				RevertCamera();
				return;
			}
				

			if(_dogfightTarget)
			{
				_dogfightLastTarget = true;
				_dogfightLastTargetPosition = _dogfightTarget.CoM;
				_dogfightLastTargetVelocity = _dogfightTarget.rb_velocity;
			}
			else if(_dogfightLastTarget)
			{
				_dogfightLastTargetPosition += _dogfightLastTargetVelocity * Time.fixedDeltaTime;
			}

			_cameraParent.transform.position = (_vessel.CoM - (_vessel.rb_velocity * Time.fixedDeltaTime));	

			if(_dogfightVelocityChase)
			{
				if(_vessel.srfSpeed > 1)
				{
					_dogfightLastTargetPosition = _vessel.CoM + (_vessel.srf_velocity.normalized * 5000);
				}
				else
				{
					_dogfightLastTargetPosition = _vessel.CoM + (_vessel.ReferenceTransform.up * 5000);
				}
			}

			Vector3 offsetDirection = Vector3.Cross(_cameraUp, _dogfightLastTargetPosition - _vessel.CoM).normalized;
			Vector3 camPos = _vessel.CoM + ((_vessel.CoM - _dogfightLastTargetPosition).normalized * _dogfightDistance) + (_dogfightOffsetX * offsetDirection) + (_dogfightOffsetY * _cameraUp);

            Vector3 localCamPos = _cameraParent.transform.InverseTransformPoint(camPos);
			_flightCamera.transform.localPosition = Vector3.Lerp(_flightCamera.transform.localPosition, localCamPos, _dogfightLerp * Time.fixedDeltaTime);

			//rotation
			Quaternion vesselLook = Quaternion.LookRotation(_vessel.CoM-_flightCamera.transform.position, _cameraUp);
			Quaternion targetLook = Quaternion.LookRotation(_dogfightLastTargetPosition-_flightCamera.transform.position, _cameraUp);
			Quaternion camRot = Quaternion.Lerp(vesselLook, targetLook, 0.5f);
			_flightCamera.transform.rotation = Quaternion.Lerp(_flightCamera.transform.rotation, camRot, _dogfightLerp * Time.fixedDeltaTime);

			//autoFov
			if(AutoFov)
			{
				float targetFoV;
				if(_dogfightVelocityChase)
				{
					targetFoV = Mathf.Clamp((7000 / (_dogfightDistance + 100)) - 14 + _autoZoomMargin, 2, 60);
				}
				else
				{
					float angle = Vector3.Angle((_dogfightLastTargetPosition + (_dogfightLastTargetVelocity * Time.fixedDeltaTime)) - _flightCamera.transform.position, (_vessel.CoM + (_vessel.rb_velocity * Time.fixedDeltaTime)) - _flightCamera.transform.position);
					targetFoV = Mathf.Clamp(angle + _autoZoomMargin, 0.1f, 60f);
				}
				_manualFov = targetFoV;
			}
			//FOV
			if(!AutoFov)
			{
				_zoomFactor = Mathf.Exp(ZoomExp) / Mathf.Exp(1);
				_manualFov = 60 / _zoomFactor;
				_updateFov = (_currentFov != _manualFov);
				if(_updateFov)
				{
					_currentFov = Mathf.Lerp(_currentFov, _manualFov, 0.1f);
					_flightCamera.SetFoV(_currentFov);
					_updateFov = false;
				}
			}
			else
			{
				_currentFov = Mathf.Lerp(_currentFov, _manualFov, 0.1f);
				_flightCamera.SetFoV(_currentFov);	
				_zoomFactor = 60 / _currentFov;
			}

			//free move
			if(EnableKeypad)
			{
				if(Input.GetKey(_fmUpKey))
				{
					_dogfightOffsetY += FreeMoveSpeed * Time.fixedDeltaTime;
					_dogfightOffsetY = Mathf.Clamp(_dogfightOffsetY, -_dogfightMaxOffset, _dogfightMaxOffset);
				}
				else if(Input.GetKey(_fmDownKey))
				{
					_dogfightOffsetY -= FreeMoveSpeed * Time.fixedDeltaTime;	
					_dogfightOffsetY = Mathf.Clamp(_dogfightOffsetY, -_dogfightMaxOffset, _dogfightMaxOffset);
				}
				if(Input.GetKey(_fmForwardKey))
				{
					_dogfightDistance -= FreeMoveSpeed * Time.fixedDeltaTime;
					_dogfightDistance = Mathf.Clamp(_dogfightDistance, 1f, 100f);
				}
				else if(Input.GetKey(_fmBackKey))
				{
					_dogfightDistance += FreeMoveSpeed * Time.fixedDeltaTime;
					_dogfightDistance = Mathf.Clamp(_dogfightDistance, 1f, 100f);
				}
				if(Input.GetKey(_fmLeftKey))
				{
					_dogfightOffsetX -= FreeMoveSpeed * Time.fixedDeltaTime;
					_dogfightOffsetX = Mathf.Clamp(_dogfightOffsetX, -_dogfightMaxOffset, _dogfightMaxOffset);
				}
				else if(Input.GetKey(_fmRightKey))
				{
					_dogfightOffsetX += FreeMoveSpeed * Time.fixedDeltaTime;
					_dogfightOffsetX = Mathf.Clamp(_dogfightOffsetX, -_dogfightMaxOffset, _dogfightMaxOffset);
				}

				//keyZoom
				if(!AutoFov)
				{
					if(Input.GetKey(_fmZoomInKey))
					{
						ZoomExp = Mathf.Clamp(ZoomExp + (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
					else if(Input.GetKey(_fmZoomOutKey))
					{
						ZoomExp = Mathf.Clamp(ZoomExp - (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
				}
				else
				{
					if(Input.GetKey(_fmZoomInKey))
					{
						_autoZoomMargin = Mathf.Clamp(_autoZoomMargin + (KeyZoomSpeed  * 10 * Time.fixedDeltaTime), 0, 50);
					}
					else if(Input.GetKey(_fmZoomOutKey))
					{
						_autoZoomMargin = Mathf.Clamp(_autoZoomMargin - (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50);
					}
				}
			}

			//vessel camera shake
			if(ShakeMultiplier > 0)
			{
				foreach(var v in FlightGlobals.Vessels)
				{
					if(!v || !v.loaded || v.packed || v.isActiveVessel) continue;
					VesselCameraShake(v);
				}
			}
			UpdateCameraShake();

			if(_hasBdai && UseBdAutoTarget)
			{
				Vessel newAiTarget = GetAiTargetedVessel();
				if(newAiTarget)
				{
					_dogfightTarget = newAiTarget;
				}
			}

			if(_dogfightTarget != _dogfightPrevTarget)
			{
				//RevertCamera();
				StartDogfightCamera();
			}
		}

		void UpdateStationaryCamera()
		{
			if(UseAudioEffects)
			{
				SpeedOfSound = 233 * Math.Sqrt(1 + (FlightGlobals.getExternalTemperature(_vessel.GetWorldPos3D(), _vessel.mainBody) / 273.15));
				//Debug.Log("speed of sound: " + speedOfSound);
			}

			if(_posCounter < 3)
			{
				_posCounter++;
				Debug.Log("flightCamera position: " + _flightCamera.transform.position);
				_flightCamera.transform.position = _resetPositionFix;
				if(_hasSavedRotation)
				{
					_flightCamera.transform.rotation = _savedRotation;
				}
			}
			if(_flightCamera.Target != null) _flightCamera.SetTargetNone(); //dont go to next vessel if vessel is destroyed

            if (_camTarget != null)
			{
				Vector3 lookPosition = _camTarget.transform.position;
				if(TargetCoM)
				{
					lookPosition = _camTarget.vessel.CoM;
				}

				lookPosition += 2*_camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
				if(TargetCoM)
				{
					lookPosition += _camTarget.vessel.rb_velocity * Time.fixedDeltaTime;
				}

				_flightCamera.transform.rotation = Quaternion.LookRotation(lookPosition - _flightCamera.transform.position, _cameraUp);
				_lastTargetPosition = lookPosition;
			}
			else if(_hasTarget)
			{
				_flightCamera.transform.rotation = Quaternion.LookRotation(_lastTargetPosition - _flightCamera.transform.position, _cameraUp);
			}



			if(_vessel != null)
			{
				_cameraParent.transform.position = _manualPosition + (_vessel.CoM - _vessel.rb_velocity * Time.fixedDeltaTime);	

				if(ReferenceMode == ReferenceModes.Surface)
				{
					_flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp((float)_vessel.srf_velocity.magnitude, 0, MaxRelV) * _vessel.srf_velocity.normalized;
				}
				else if(ReferenceMode == ReferenceModes.Orbit)
				{
					_flightCamera.transform.position -= Time.fixedDeltaTime * Mathf.Clamp((float)_vessel.obt_velocity.magnitude, 0, MaxRelV) * _vessel.obt_velocity.normalized;
				}
				else if(ReferenceMode == ReferenceModes.InitialVelocity)
				{
					Vector3 camVelocity = Vector3.zero;
					if(UseOrbital && _initialOrbit != null)
					{
						camVelocity = (_initialOrbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).xzy - _vessel.GetObtVelocity());
					}
					else
					{
						camVelocity = (_initialVelocity - _vessel.srf_velocity);
					}
					_flightCamera.transform.position += camVelocity * Time.fixedDeltaTime;
				}
			}


			//mouse panning, moving
			Vector3 forwardLevelAxis = (Quaternion.AngleAxis(-90, _cameraUp) * _flightCamera.transform.right).normalized;
			Vector3 rightAxis = (Quaternion.AngleAxis(90, forwardLevelAxis) * _cameraUp).normalized;

			//free move
			if(EnableKeypad)
			{
				if(Input.GetKey(_fmUpKey))
				{
					_manualPosition += _cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;	
				}
				else if(Input.GetKey(_fmDownKey))
				{
					_manualPosition -= _cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;	
				}
				if(Input.GetKey(_fmForwardKey))
				{
					_manualPosition += forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
				}
				else if(Input.GetKey(_fmBackKey))
				{
					_manualPosition -= forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
				}
				if(Input.GetKey(_fmLeftKey))
				{
					_manualPosition -= _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
				}
				else if(Input.GetKey(_fmRightKey))
				{
					_manualPosition += _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
				}

				//keyZoom
				if(!AutoFov)
				{
					if(Input.GetKey(_fmZoomInKey))
					{
						ZoomExp = Mathf.Clamp(ZoomExp + (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
					else if(Input.GetKey(_fmZoomOutKey))
					{
						ZoomExp = Mathf.Clamp(ZoomExp - (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8);
					}
				}
				else
				{
					if(Input.GetKey(_fmZoomInKey))
					{
						_autoZoomMargin = Mathf.Clamp(_autoZoomMargin + (KeyZoomSpeed  * 10 * Time.fixedDeltaTime), 0, 50);
					}
					else if(Input.GetKey(_fmZoomOutKey))
					{
						_autoZoomMargin = Mathf.Clamp(_autoZoomMargin - (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50);
					}
				}
			}


			if(_camTarget == null && Input.GetKey(KeyCode.Mouse1))
			{
				_flightCamera.transform.rotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * 1.7f, Vector3.up); //*(Mathf.Abs(Mouse.delta.x)/7)
				_flightCamera.transform.rotation *= Quaternion.AngleAxis(-Input.GetAxis("Mouse Y") * 1.7f, Vector3.right);
				_flightCamera.transform.rotation = Quaternion.LookRotation(_flightCamera.transform.forward, _cameraUp);
			}
			if(Input.GetKey(KeyCode.Mouse2))
			{
				_manualPosition += _flightCamera.transform.right * Input.GetAxis("Mouse X") * 2;
				_manualPosition += forwardLevelAxis * Input.GetAxis("Mouse Y") * 2;
			}
			_manualPosition += _cameraUp * 10 * Input.GetAxis("Mouse ScrollWheel");

			//autoFov
			if(_camTarget != null && AutoFov)
			{
				float cameraDistance = Vector3.Distance(_camTarget.transform.position, _flightCamera.transform.position);
				float targetFoV = Mathf.Clamp((7000 / (cameraDistance + 100)) - 14 + _autoZoomMargin, 2, 60);
				//flightCamera.SetFoV(targetFoV);	
				_manualFov = targetFoV;
			}
			//FOV
			if(!AutoFov)
			{
				_zoomFactor = Mathf.Exp(ZoomExp) / Mathf.Exp(1);
				_manualFov = 60 / _zoomFactor;
				_updateFov = (_currentFov != _manualFov);
				if(_updateFov)
				{
					_currentFov = Mathf.Lerp(_currentFov, _manualFov, 0.1f);
					_flightCamera.SetFoV(_currentFov);
					_updateFov = false;
				}
			}
			else
			{
				_currentFov = Mathf.Lerp(_currentFov, _manualFov, 0.1f);
				_flightCamera.SetFoV(_currentFov);	
				_zoomFactor = 60 / _currentFov;
			}
			_lastPosition = _flightCamera.transform.position;
			_lastRotation = _flightCamera.transform.rotation;



			//vessel camera shake
			if(ShakeMultiplier > 0)
			{
				foreach(var v in FlightGlobals.Vessels)
				{
					if(!v || !v.loaded || v.packed) continue;
					VesselCameraShake(v);
				}
			}
			UpdateCameraShake();
		}

		
		void LateUpdate()
		{
			
			//retain pos and rot after vessel destruction
			if (_cameraToolActive && _flightCamera.transform.parent != _cameraParent.transform)	
			{
				_flightCamera.SetTargetNone();
                _flightCamera.transform.parent = null;
				_flightCamera.transform.position = _lastPosition;
				_flightCamera.transform.rotation = _lastRotation;
				_hasDied = true;
				_diedTime = Time.time;
			}
			
		}

		void UpdateCameraShake()
		{
			if(ShakeMultiplier > 0)
			{
				if(_shakeMagnitude > 0.1f)
				{
					Vector3 shakeAxis = UnityEngine.Random.onUnitSphere;
					_shakeOffset = Mathf.Sin(_shakeMagnitude * 20 * Time.time) * (_shakeMagnitude / 10) * shakeAxis;
				}


				_flightCamera.transform.rotation = Quaternion.AngleAxis((ShakeMultiplier/2) * _shakeMagnitude / 50f, Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, _flightCamera.transform.forward)) * _flightCamera.transform.rotation;
			}

			_shakeMagnitude = Mathf.Lerp(_shakeMagnitude, 0, 5*Time.fixedDeltaTime);
		}

		public void VesselCameraShake(Vessel vessel)
		{
			//shake
			float camDistance = Vector3.Distance(_flightCamera.transform.position, vessel.CoM);

			float distanceFactor = 50f / camDistance;
			float fovFactor = 2f / _zoomFactor;
			float thrustFactor = GetTotalThrust() / 1000f;

			float atmosphericFactor = (float)vessel.dynamicPressurekPa / 2f;

			float angleToCam = Vector3.Angle(vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - vessel.transform.position);
			angleToCam = Mathf.Clamp(angleToCam, 1, 180);

			float srfSpeed = (float)vessel.srfSpeed;

			float lagAudioFactor = (75000 / (Vector3.Distance(vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position) * srfSpeed * angleToCam / 90));
			lagAudioFactor = Mathf.Clamp(lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4);
			lagAudioFactor += srfSpeed / 230;

			float waveFrontFactor = ((3.67f * angleToCam) / srfSpeed);
			waveFrontFactor = Mathf.Clamp(waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2);
			if(vessel.srfSpeed > 330)
			{
				waveFrontFactor = (srfSpeed / (angleToCam) < 3.67f) ? srfSpeed / 15 : 0;
			}

			lagAudioFactor *= waveFrontFactor;

			lagAudioFactor = Mathf.Clamp01(lagAudioFactor) * distanceFactor * fovFactor;

			atmosphericFactor *= lagAudioFactor;

			thrustFactor *= distanceFactor * fovFactor * lagAudioFactor;

			ShakeCamera(atmosphericFactor + thrustFactor);
		}

		float GetTotalThrust()
		{
			float total = 0;
			foreach(var engine in _vessel.FindPartModulesImplementing<ModuleEngines>())
			{
				total += engine.finalThrust;
			}
			return total;
		}

		void AddAtmoAudioControllers(bool includeActiveVessel)
		{
			if(!UseAudioEffects)
			{
				return;
			}

			foreach(var vessel in FlightGlobals.Vessels)
			{
				if(!vessel || !vessel.loaded || vessel.packed || (!includeActiveVessel && vessel.isActiveVessel))
				{
					continue;
				}

				vessel.gameObject.AddComponent<CtAtmosphericAudioController>();
			}
		}
		
		void SetDoppler(bool includeActiveVessel)
		{
			if(_hasSetDoppler)
			{
				return;
			}

			if(!UseAudioEffects)
			{
				return;
			}

			_audioSources = FindObjectsOfType<AudioSource>();
			_originalAudioSourceDoppler = new float[_audioSources.Length];

			for(int i = 0; i < _audioSources.Length; i++)
			{
				_originalAudioSourceDoppler[i] = _audioSources[i].dopplerLevel;

				if(!includeActiveVessel)
				{
					Part p = _audioSources[i].GetComponentInParent<Part>();
					if(p && p.vessel.isActiveVessel) continue;
				}

				_audioSources[i].dopplerLevel = 1;
				_audioSources[i].velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
				_audioSources[i].bypassEffects = false;
				_audioSources[i].spatialBlend = 1;
				
				if(_audioSources[i].gameObject.GetComponentInParent<Part>())
				{
					//Debug.Log("Added CTPartAudioController to :" + audioSources[i].name);
					CtPartAudioController pa = _audioSources[i].gameObject.AddComponent<CtPartAudioController>();
					pa.AudioSource = _audioSources[i];
				}
			}

			_hasSetDoppler = true;
		}

		void ResetDoppler()
		{
			if(!_hasSetDoppler)
			{
				return;
			}

			for(int i = 0; i < _audioSources.Length; i++)
			{
				if(_audioSources[i] != null)
				{
					_audioSources[i].dopplerLevel = _originalAudioSourceDoppler[i];
					_audioSources[i].velocityUpdateMode = AudioVelocityUpdateMode.Auto;
				}
			}

		

			_hasSetDoppler = false;
		}

		
		void StartStationaryCamera()
		{
			Debug.Log ("flightCamera position init: "+_flightCamera.transform.position);
			if(FlightGlobals.ActiveVessel != null)
			{				
				_hasDied = false;
				_vessel = FlightGlobals.ActiveVessel;
				_cameraUp = -FlightGlobals.getGeeForceAtPosition(_vessel.GetWorldPos3D()).normalized;
				if(FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel(_vessel) == FlightCamera.Modes.ORBITAL))
				{
					_cameraUp = Vector3.up;
				}

                _flightCamera.SetTargetNone();
                _flightCamera.transform.parent = _cameraParent.transform;
                _flightCamera.DeactivateUpdate();
                _cameraParent.transform.position = _vessel.transform.position+_vessel.rb_velocity*Time.fixedDeltaTime;
				_manualPosition = Vector3.zero;
				
				
				_hasTarget = (_camTarget != null) ? true : false;
				
				
				Vector3 rightAxis = -Vector3.Cross(_vessel.srf_velocity, _vessel.upAxis).normalized;
				//Vector3 upAxis = flightCamera.transform.up;
				

				if(AutoFlybyPosition)
				{
					_setPresetOffset = false;
					Vector3 velocity = _vessel.srf_velocity;
					if(ReferenceMode == ReferenceModes.Orbit) velocity = _vessel.obt_velocity;
					
					Vector3 clampedVelocity = Mathf.Clamp((float) _vessel.srfSpeed, 0, MaxRelV) * velocity.normalized;
					float clampedSpeed = clampedVelocity.magnitude;
					float sideDistance = Mathf.Clamp(20 + (clampedSpeed/10), 20, 150);
					float distanceAhead = Mathf.Clamp(4 * clampedSpeed, 30, 3500);
					
					_flightCamera.transform.rotation = Quaternion.LookRotation(_vessel.transform.position - _flightCamera.transform.position, _cameraUp);
					
					
					if(ReferenceMode == ReferenceModes.Surface && _vessel.srfSpeed > 0)
					{
						_flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.srf_velocity.normalized);
					}
					else if(ReferenceMode == ReferenceModes.Orbit && _vessel.obt_speed > 0)
					{
						_flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.obt_velocity.normalized);
					}
					else
					{
						_flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.vesselTransform.up);
					}
					
					
					if(_flightCamera.mode == FlightCamera.Modes.FREE || FlightCamera.GetAutoModeForVessel(_vessel) == FlightCamera.Modes.FREE)
					{
						_flightCamera.transform.position += (sideDistance * rightAxis) + (15 * _cameraUp);
					}
					else if(_flightCamera.mode == FlightCamera.Modes.ORBITAL || FlightCamera.GetAutoModeForVessel(_vessel) == FlightCamera.Modes.ORBITAL)
					{
						_flightCamera.transform.position += (sideDistance * FlightGlobals.getUpAxis()) + (15 * Vector3.up);
					}


				}
				else if(ManualOffset)
				{
					_setPresetOffset = false;
					float sideDistance = ManualOffsetRight;
					float distanceAhead = ManualOffsetForward;
					
					
					_flightCamera.transform.rotation = Quaternion.LookRotation(_vessel.transform.position - _flightCamera.transform.position, _cameraUp);
					
					if(ReferenceMode == ReferenceModes.Surface && _vessel.srfSpeed > 4)
					{
						_flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.srf_velocity.normalized);
					}
					else if(ReferenceMode == ReferenceModes.Orbit && _vessel.obt_speed > 4)
					{
						_flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.obt_velocity.normalized);
					}
					else
					{
						_flightCamera.transform.position = _vessel.transform.position + (distanceAhead * _vessel.vesselTransform.up);
					}
					
					if(_flightCamera.mode == FlightCamera.Modes.FREE || FlightCamera.GetAutoModeForVessel(_vessel) == FlightCamera.Modes.FREE)
					{
						_flightCamera.transform.position += (sideDistance * rightAxis) + (ManualOffsetUp * _cameraUp);
					}
					else if(_flightCamera.mode == FlightCamera.Modes.ORBITAL || FlightCamera.GetAutoModeForVessel(_vessel) == FlightCamera.Modes.ORBITAL)
					{
						_flightCamera.transform.position += (sideDistance * FlightGlobals.getUpAxis()) + (ManualOffsetUp * Vector3.up);
					}
				}
				else if(_setPresetOffset)
				{
					_flightCamera.transform.position = _presetOffset;
					//setPresetOffset = false;
				}
				
				_initialVelocity = _vessel.srf_velocity;
				_initialOrbit = new Orbit();
				_initialOrbit.UpdateFromStateVectors(_vessel.orbit.pos, _vessel.orbit.vel, FlightGlobals.currentMainBody, Planetarium.GetUniversalTime());
				_initialUt = Planetarium.GetUniversalTime();
				
				_cameraToolActive = true;

				SetDoppler(true);
				AddAtmoAudioControllers(true);
			}
			else
			{
				Debug.Log ("CameraTools: Stationary Camera failed. Active Vessel is null.");	
			}
			_resetPositionFix = _flightCamera.transform.position;
			Debug.Log ("flightCamera position post init: "+_flightCamera.transform.position);
		}
		
		void RevertCamera()
		{
			_posCounter = 0;

			if(_cameraToolActive)
			{
				_presetOffset = _flightCamera.transform.position;
				if(_camTarget==null)
				{
					_savedRotation = _flightCamera.transform.rotation;
					_hasSavedRotation = true;
				}
				else
				{
					_hasSavedRotation = false;
				}
			}
			_hasDied = false;
		    if (FlightGlobals.ActiveVessel != null && HighLogic.LoadedScene == GameScenes.FLIGHT)
		    {
                _flightCamera.SetTarget(FlightGlobals.ActiveVessel.transform, FlightCamera.TargetMode.Vessel);
            }
            _flightCamera.transform.parent = _origParent;
            _flightCamera.transform.position = _origPosition;
            _flightCamera.transform.rotation = _origRotation;
            Camera.main.nearClipPlane = _origNearClip;

			_flightCamera.SetFoV(60);
		    _flightCamera.ActivateUpdate();
			_currentFov = 60;
		
			_cameraToolActive = false;

			ResetDoppler();
			if(OnResetCTools != null)
			{
				OnResetCTools();
			}

			StopPlayingPath();
		}
		
		void SaveOriginalCamera()
		{
			_origPosition = _flightCamera.transform.position;
			_origRotation = _flightCamera.transform.localRotation;
			_origParent = _flightCamera.transform.parent;
			_origNearClip = Camera.main.nearClipPlane;	
		}
		
		Part GetPartFromMouse()
		{
			Vector3 mouseAim = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
			Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mouseAim);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, 10000, 1<<0))
			{
				Part p = hit.transform.GetComponentInParent<Part>();
				return p;
			}
			else return null;
		}
		
		Vector3 GetPosFromMouse()
		{
			Vector3 mouseAim = new Vector3(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height, 0);
			Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mouseAim);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, 15000, 557057))
			{
				return 	hit.point - (10 * ray.direction);
			}
			else return Vector3.zero;
		}
		
		void PostDeathRevert()
		{
			if(_cameraToolActive)	
			{
				RevertCamera();	
			}
		}

		void PostDeathRevert(GameScenes f)
		{
			if(_cameraToolActive)	
			{
				RevertCamera();	
			}
		}
		
		void PostDeathRevert(Vessel v)
		{
			if(_cameraToolActive)	
			{
				RevertCamera();	
			}
		}
		
		//GUI
		void OnGui()
		{
			if(GuiEnabled && _gameUiToggle) 
			{
				_windowRect = GUI.Window(320, _windowRect, GuiWindow, "");

				if(_showKeyframeEditor)
				{
					KeyframeEditorWindow();
				}
				if(_showPathSelectorWindow)
				{
					PathSelectorWindow();
				}
			}
		}
				
		void GuiWindow(int windowId)
		{
			GUI.DragWindow(new Rect(0,0,_windowWidth, _draggableHeight));
			
			GUIStyle centerLabel = new GUIStyle();
			centerLabel.alignment = TextAnchor.UpperCenter;
			centerLabel.normal.textColor = Color.white;
			
			GUIStyle leftLabel = new GUIStyle();
			leftLabel.alignment = TextAnchor.UpperLeft;
			leftLabel.normal.textColor = Color.white;

			GUIStyle leftLabelBold = new GUIStyle(leftLabel);
			leftLabelBold.fontStyle = FontStyle.Bold;
			
			
			
			float line = 1;
			float contentWidth = (_windowWidth) - (2*_leftIndent);
			float contentTop = 20;
			GUIStyle titleStyle = new GUIStyle(centerLabel);
			titleStyle.fontSize = 24;
			titleStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(new Rect(0, contentTop, _windowWidth, 40), "Camera Tools", titleStyle);
			line++;
			float parseResult;

			//tool mode switcher
			GUI.Label(new Rect(_leftIndent, contentTop+(line*_entryHeight), contentWidth, _entryHeight), "Tool: "+ToolMode.ToString(), leftLabelBold);
			line++;
			if(!_cameraToolActive)
			{
				if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), 25, _entryHeight - 2), "<"))
				{
					CycleToolMode(false);
				}
				if(GUI.Button(new Rect(_leftIndent + 25 + 4, contentTop + (line * _entryHeight), 25, _entryHeight - 2), ">"))
				{
					CycleToolMode(true);
				}
			}
			line++;
			line++;
			if(AutoFov)
			{
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), "Autozoom Margin: ");
				line++;
				_autoZoomMargin = GUI.HorizontalSlider(new Rect(_leftIndent, contentTop + ((line) * _entryHeight), contentWidth - 45, _entryHeight), _autoZoomMargin, 0, 50);
				GUI.Label(new Rect(_leftIndent + contentWidth - 40, contentTop + ((line - 0.15f) * _entryHeight), 40, _entryHeight), _autoZoomMargin.ToString("0.0"), leftLabel);
			}
			else
			{
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Zoom:", leftLabel);
				line++;
				ZoomExp = GUI.HorizontalSlider(new Rect(_leftIndent, contentTop + ((line) * _entryHeight), contentWidth - 45, _entryHeight), ZoomExp, 1, 8);
				GUI.Label(new Rect(_leftIndent + contentWidth - 40, contentTop + ((line - 0.15f) * _entryHeight), 40, _entryHeight), _zoomFactor.ToString("0.0") + "x", leftLabel);
			}
			line++;

			if(ToolMode != ToolModes.Pathing)
			{
				AutoFov = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), AutoFov, "Auto Zoom");//, leftLabel);
				line++;
			}
			line++;
			UseAudioEffects = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), UseAudioEffects, "Use Audio Effects");
			line++;
			GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Camera shake:");
			line++;
			ShakeMultiplier = GUI.HorizontalSlider(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth - 45, _entryHeight), ShakeMultiplier, 0f, 10f);
			GUI.Label(new Rect(_leftIndent + contentWidth - 40, contentTop + ((line - 0.25f) * _entryHeight), 40, _entryHeight), ShakeMultiplier.ToString("0.00") + "x");
			line++;
			line++;

			//Stationary camera GUI
			if(ToolMode == ToolModes.StationaryCamera)
			{
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Frame of Reference: " + ReferenceMode.ToString(), leftLabel);
				line++;
				if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), 25, _entryHeight - 2), "<"))
				{
					CycleReferenceMode(false);
				}
				if(GUI.Button(new Rect(_leftIndent + 25 + 4, contentTop + (line * _entryHeight), 25, _entryHeight - 2), ">"))
				{
					CycleReferenceMode(true);
				}
				
				line++;
				
				if(ReferenceMode == ReferenceModes.Surface || ReferenceMode == ReferenceModes.Orbit)
				{
					GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), "Max Rel. V: ", leftLabel);
					MaxRelV = float.Parse(GUI.TextField(new Rect(_leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), MaxRelV.ToString()));	
				}
				else if(ReferenceMode == ReferenceModes.InitialVelocity)
				{
					UseOrbital = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), UseOrbital, " Orbital");
				}
				line++;

				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Camera Position:", leftLabel);
				line++;
				string posButtonText = "Set Position w/ Click";
				if(_setPresetOffset) posButtonText = "Clear Position";
				if(_waitingForPosition) posButtonText = "Waiting...";
				if(FlightGlobals.ActiveVessel != null && GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2), posButtonText))
				{
					if(_setPresetOffset)
					{
						_setPresetOffset = false;
					}
					else
					{
						_waitingForPosition = true;
						_mouseUp = false;
					}
				}
				line++;
				

				AutoFlybyPosition = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), AutoFlybyPosition, "Auto Flyby Position");
				if(AutoFlybyPosition) ManualOffset = false;
				line++;
				
				ManualOffset = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), ManualOffset, "Manual Flyby Position");
				line++;

				Color origGuiColor = GUI.color;
				if(ManualOffset)
				{
					AutoFlybyPosition = false;
				}
				else
				{
					GUI.color = new Color(0.5f, 0.5f, 0.5f, origGuiColor.a);
				}
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), 60, _entryHeight), "Fwd:", leftLabel);
				float textFieldWidth = 42;
				Rect fwdFieldRect = new Rect(_leftIndent + contentWidth - textFieldWidth - (3 * _incrButtonWidth), contentTop + (line * _entryHeight), textFieldWidth, _entryHeight);
				_guiOffsetForward = GUI.TextField(fwdFieldRect, _guiOffsetForward.ToString());
				if(float.TryParse(_guiOffsetForward, out parseResult))
				{
					ManualOffsetForward = parseResult;	
				}
				DrawIncrementButtons(fwdFieldRect, ref ManualOffsetForward);
				_guiOffsetForward = ManualOffsetForward.ToString();

				line++;
				Rect rightFieldRect = new Rect(fwdFieldRect.x, contentTop + (line * _entryHeight), textFieldWidth, _entryHeight);
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), 60, _entryHeight), "Right:", leftLabel);
				_guiOffsetRight = GUI.TextField(rightFieldRect, _guiOffsetRight);
				if(float.TryParse(_guiOffsetRight, out parseResult))
				{
					ManualOffsetRight = parseResult;	
				}
				DrawIncrementButtons(rightFieldRect, ref ManualOffsetRight);
				_guiOffsetRight = ManualOffsetRight.ToString();
				line++;

				Rect upFieldRect = new Rect(fwdFieldRect.x, contentTop + (line * _entryHeight), textFieldWidth, _entryHeight);
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), 60, _entryHeight), "Up:", leftLabel);
				_guiOffsetUp = GUI.TextField(upFieldRect, _guiOffsetUp);
				if(float.TryParse(_guiOffsetUp, out parseResult))
				{
					ManualOffsetUp = parseResult;	
				}
				DrawIncrementButtons(upFieldRect, ref ManualOffsetUp);
				_guiOffsetUp = ManualOffsetUp.ToString();
				GUI.color = origGuiColor;

				line++;
				line++;
				
				string targetText = "None";
				if(_camTarget != null) targetText = _camTarget.gameObject.name;
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Camera Target: " + targetText, leftLabel);
				line++;
				string tgtButtonText = "Set Target w/ Click";
				if(_waitingForTarget) tgtButtonText = "waiting...";
				if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2), tgtButtonText))
				{
					_waitingForTarget = true;
					_mouseUp = false;
				}
				line++;
				if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), (contentWidth / 2) - 2, _entryHeight - 2), "Target Self"))
				{
					_camTarget = FlightGlobals.ActiveVessel.GetReferenceTransformPart();
					_hasTarget = true;
				}
				if(GUI.Button(new Rect(2 + _leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), (contentWidth / 2) - 2, _entryHeight - 2), "Clear Target"))
				{
					_camTarget = null;
					_hasTarget = false;
				}
				line++;

				TargetCoM = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2), TargetCoM, "Vessel Center of Mass");
			}
			else if(ToolMode == ToolModes.DogfightCamera)
			{
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Secondary target:");
				line++;
				string tVesselLabel;
				if(_showingVesselList)
				{
					tVesselLabel = "Clear";
				}
				else if(_dogfightTarget)
				{
					tVesselLabel = _dogfightTarget.vesselName;
				}
				else
				{
					tVesselLabel = "None";
				}
				if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), tVesselLabel))
				{
					if(_showingVesselList)
					{
						_showingVesselList = false;
						_dogfightTarget = null;
					}
					else
					{
						UpdateLoadedVessels();
						_showingVesselList = true;
					}
				}
				line++;

				if(_showingVesselList)
				{
					foreach(var v in _loadedVessels)
					{
						if(!v || !v.loaded) continue;
						if(GUI.Button(new Rect(_leftIndent + 10, contentTop + (line * _entryHeight), contentWidth - 10, _entryHeight), v.vesselName))
						{
							_dogfightTarget = v;
							_showingVesselList = false;
						}
						line++;
					}
				}
				line++;

				if(_hasBdai)
				{
					UseBdAutoTarget = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight - 2), UseBdAutoTarget, "BDA AI Auto target");
					line++;
				}

				line++;
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), "Distance: " + _dogfightDistance.ToString("0.0"));
				line++;
				_dogfightDistance = GUI.HorizontalSlider(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), _dogfightDistance, 1, 100);
				line += 1.5f;

				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Offset:");
				line++;
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), 15, _entryHeight), "X: ");
				_dogfightOffsetX = GUI.HorizontalSlider(new Rect(_leftIndent + 15, contentTop + (line * _entryHeight) + 6, contentWidth - 45, _entryHeight), _dogfightOffsetX, -_dogfightMaxOffset, _dogfightMaxOffset);
				GUI.Label(new Rect(_leftIndent + contentWidth - 25, contentTop + (line * _entryHeight), 25, _entryHeight), _dogfightOffsetX.ToString("0.0"));
				line++;
				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), 15, _entryHeight), "Y: ");
				_dogfightOffsetY = GUI.HorizontalSlider(new Rect(_leftIndent + 15, contentTop + (line * _entryHeight) + 6, contentWidth - 45, _entryHeight), _dogfightOffsetY, -_dogfightMaxOffset, _dogfightMaxOffset);
				GUI.Label(new Rect(_leftIndent + contentWidth - 25, contentTop + (line * _entryHeight), 25, _entryHeight), _dogfightOffsetY.ToString("0.0"));
				line += 1.5f;
			}
			else if(ToolMode == ToolModes.Pathing)
			{
				if(_selectedPathIndex >= 0)
				{
					GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Path:");
					CurrentPath.PathName = GUI.TextField(new Rect(_leftIndent + 34, contentTop + (line * _entryHeight), contentWidth-34, _entryHeight), CurrentPath.PathName);
				}
				else
				{
					GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Path: None");
				}
				line += 1.25f;
				if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Open Path"))
				{
					TogglePathList();
				}
				line++;
				if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth/2, _entryHeight), "New Path"))
				{
					CreateNewPath();
				}
				if(GUI.Button(new Rect(_leftIndent + (contentWidth/2), contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), "Delete Path"))
				{
					DeletePath(_selectedPathIndex);
				}
				line ++;
				if(_selectedPathIndex >= 0)
				{
					GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Interpolation rate: " + CurrentPath.LerpRate.ToString("0.0"));
					line++;
					CurrentPath.LerpRate = GUI.HorizontalSlider(new Rect(_leftIndent, contentTop + (line * _entryHeight) + 4, contentWidth - 50, _entryHeight), CurrentPath.LerpRate, 1f, 15f);
					CurrentPath.LerpRate = Mathf.Round(CurrentPath.LerpRate * 10) / 10;
					line++;
					GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Path timescale " + CurrentPath.TimeScale.ToString("0.00"));
					line++;
					CurrentPath.TimeScale = GUI.HorizontalSlider(new Rect(_leftIndent, contentTop + (line * _entryHeight) + 4, contentWidth - 50, _entryHeight), CurrentPath.TimeScale, 0.05f, 4f);
					CurrentPath.TimeScale = Mathf.Round(CurrentPath.TimeScale * 20) / 20;
					line++;
					float viewHeight = Mathf.Max(6 * _entryHeight, CurrentPath.KeyframeCount * _entryHeight);
					Rect scrollRect = new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, 6 * _entryHeight);
					GUI.Box(scrollRect, string.Empty);
					float viewContentWidth = contentWidth - (2 * _leftIndent);
					_keysScrollPos = GUI.BeginScrollView(scrollRect, _keysScrollPos, new Rect(0, 0, viewContentWidth, viewHeight));
					if(CurrentPath.KeyframeCount > 0)
					{
						Color origGuiColor = GUI.color;
						for(int i = 0; i < CurrentPath.KeyframeCount; i++)
						{
							if(i == _currentKeyframeIndex)
							{
								GUI.color = Color.green;
							}
							else
							{
								GUI.color = origGuiColor;
							}
							string kLabel = "#" + i.ToString() + ": " + CurrentPath.GetKeyframe(i).Time.ToString("0.00") + "s";
							if(GUI.Button(new Rect(0, (i * _entryHeight), 3 * viewContentWidth / 4, _entryHeight), kLabel))
							{
								SelectKeyframe(i);
							}
							if(GUI.Button(new Rect((3 * contentWidth / 4), (i * _entryHeight), (viewContentWidth / 4) - 20, _entryHeight), "X"))
							{
								DeleteKeyframe(i);
								break;
							}
							//line++;
						}
						GUI.color = origGuiColor;
					}
					GUI.EndScrollView();
					line += 6;
					line += 0.5f;
					if(GUI.Button(new Rect(_leftIndent, contentTop + (line * _entryHeight), 3 * contentWidth / 4, _entryHeight), "New Key"))
					{
						CreateNewKeyframe();
					}
				}
			}

			line += 1.25f;

			EnableKeypad = GUI.Toggle(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), EnableKeypad, "Keypad Control");
			if(EnableKeypad)
			{
				line++;

				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), "Move Speed:");
				_guiFreeMoveSpeed = GUI.TextField(new Rect(_leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), _guiFreeMoveSpeed);
				if(float.TryParse(_guiFreeMoveSpeed, out parseResult))
				{
					FreeMoveSpeed = Mathf.Abs(parseResult);
					_guiFreeMoveSpeed = FreeMoveSpeed.ToString();
				}

				line++;

				GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), "Zoom Speed:");
				_guiKeyZoomSpeed = GUI.TextField(new Rect(_leftIndent + contentWidth / 2, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight), _guiKeyZoomSpeed);
				if(float.TryParse(_guiKeyZoomSpeed, out parseResult))
				{
					KeyZoomSpeed = Mathf.Abs(parseResult);	
					_guiKeyZoomSpeed = KeyZoomSpeed.ToString();
				}
			}
			else
			{
				line++;
				line++;
			}

			line++;
			line++;
			GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Keys:", centerLabel);
			line++;

			//activate key binding
			GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Activate: ", leftLabel);
			GUI.Label(new Rect(_leftIndent + 60, contentTop + (line * _entryHeight), 60, _entryHeight), CameraKey, leftLabel);
			if(!_isRecordingInput)
			{
				if(GUI.Button(new Rect(_leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight), "Bind Key"))
				{
					_mouseUp = false;
					_isRecordingInput = true;
					_isRecordingActivate = true;
				}
			}
			else if(_mouseUp && _isRecordingActivate)
			{
				GUI.Label(new Rect(_leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight), "Press a Key", leftLabel);

				string inputString = CCInputUtils.GetInputString();
				if(inputString.Length > 0)
				{
					CameraKey = inputString;
					_isRecordingInput = false;
					_isRecordingActivate = false;
				}
			}

			line++;

			//revert key binding
			GUI.Label(new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth, _entryHeight), "Revert: ", leftLabel);
			GUI.Label(new Rect(_leftIndent + 60, contentTop + (line * _entryHeight), 60, _entryHeight), RevertKey);
			if(!_isRecordingInput)
			{
				if(GUI.Button(new Rect(_leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight), "Bind Key"))
				{
					_mouseUp = false;
					_isRecordingInput = true;
					_isRecordingRevert = true;
				}
			}
			else if(_mouseUp && _isRecordingRevert)
			{
				GUI.Label(new Rect(_leftIndent + 125, contentTop + (line * _entryHeight), 100, _entryHeight), "Press a Key", leftLabel);
				string inputString = CCInputUtils.GetInputString();
				if(inputString.Length > 0)
				{
					RevertKey = inputString;
					_isRecordingInput = false;
					_isRecordingRevert = false;
				}
			}

			line++;
			line++;
			Rect saveRect = new Rect(_leftIndent, contentTop + (line * _entryHeight), contentWidth / 2, _entryHeight);
			if(GUI.Button(saveRect, "Save"))
			{
				Save();
			}

			Rect loadRect = new Rect(saveRect);
			loadRect.x += contentWidth / 2;
			if(GUI.Button(loadRect, "Reload"))
			{
				Load();
			}
			
			//fix length
			_windowHeight = contentTop+(line*_entryHeight)+_entryHeight+_entryHeight;
			_windowRect.height = _windowHeight;// = new Rect(windowRect.x, windowRect.y, windowWidth, windowHeight);
		}

		public static string PathSaveUrl = "GameData/CameraTools/paths.cfg";
		void Save()
		{
			CtPersistantField.Save();

			ConfigNode pathFileNode = ConfigNode.Load(PathSaveUrl);
			ConfigNode pathsNode = pathFileNode.GetNode("CAMERAPATHS");
			pathsNode.RemoveNodes("CAMERAPATH");

			foreach(var path in _availablePaths)
			{
				path.Save(pathsNode);
			}
			pathFileNode.Save(PathSaveUrl);
		}

		void Load()
		{
			CtPersistantField.Load();
			_guiOffsetForward = ManualOffsetForward.ToString();
			_guiOffsetRight = ManualOffsetRight.ToString();
			_guiOffsetUp = ManualOffsetUp.ToString();
			_guiKeyZoomSpeed = KeyZoomSpeed.ToString();
			_guiFreeMoveSpeed = FreeMoveSpeed.ToString();

			DeselectKeyframe();
			_selectedPathIndex = -1;
			_availablePaths = new List<CameraPath>();
			ConfigNode pathFileNode = ConfigNode.Load(PathSaveUrl);
			foreach(var node in pathFileNode.GetNode("CAMERAPATHS").GetNodes("CAMERAPATH"))
			{
				_availablePaths.Add(CameraPath.Load(node));
			}
		}

		void KeyframeEditorWindow()
		{
			float width = 300;
			float height = 130;
			Rect kWindowRect = new Rect(_windowRect.x - width, _windowRect.y + 365, width, height);
			GUI.Box(kWindowRect, string.Empty);
			GUI.BeginGroup(kWindowRect);
			GUI.Label(new Rect(5, 5, 100, 25), "Keyframe #"+_currentKeyframeIndex);
			if(GUI.Button(new Rect(105, 5, 180, 25), "Revert Pos"))
			{
				ViewKeyframe(_currentKeyframeIndex);
			}
			GUI.Label(new Rect(5, 35, 80, 25), "Time: ");
			_currKeyTimeString = GUI.TextField(new Rect(100, 35, 195, 25), _currKeyTimeString, 16);
			float parsed;
			if(float.TryParse(_currKeyTimeString, out parsed))
			{
				_currentKeyframeTime = parsed;
			}
			bool applied = false;
			if(GUI.Button(new Rect(100, 65, 195, 25), "Apply"))
			{
				Debug.Log("Applying keyframe at time: " + _currentKeyframeTime);
				CurrentPath.SetTransform(_currentKeyframeIndex, _flightCamera.transform, ZoomExp, _currentKeyframeTime);
				applied = true;
			}
			if(GUI.Button(new Rect(100, 105, 195, 20), "Cancel"))
			{
				applied = true;
			}
			GUI.EndGroup();

			if(applied)
			{
				DeselectKeyframe();
			}
		}

		bool _showPathSelectorWindow = false;
		Vector2 _pathSelectScrollPos;
		void PathSelectorWindow()
		{
			float width = 300;
			float height = 300;
			float indent = 5;
			float scrollRectSize = width - indent - indent;
			Rect pSelectRect =  new Rect(_windowRect.x - width, _windowRect.y + 290, width, height);
			GUI.Box(pSelectRect, string.Empty);
			GUI.BeginGroup(pSelectRect);

			Rect scrollRect = new Rect(indent, indent, scrollRectSize, scrollRectSize);
			float scrollHeight = Mathf.Max(scrollRectSize, _entryHeight * _availablePaths.Count);
			Rect scrollViewRect = new Rect(0, 0, scrollRectSize-20, scrollHeight);
			_pathSelectScrollPos = GUI.BeginScrollView(scrollRect, _pathSelectScrollPos, scrollViewRect);
			bool selected = false;
			for(int i = 0; i < _availablePaths.Count; i++)
			{
				if(GUI.Button(new Rect(0, i * _entryHeight, scrollRectSize - 90, _entryHeight), _availablePaths[i].PathName))
				{
					SelectPath(i);
					selected = true;
				}
				if(GUI.Button(new Rect(scrollRectSize-80, i * _entryHeight, 60, _entryHeight), "Delete"))
				{
					DeletePath(i);
					break;
				}
			}

			GUI.EndScrollView();

			GUI.EndGroup();
			if(selected)
			{
				_showPathSelectorWindow = false;
			}
		}

		void DrawIncrementButtons(Rect fieldRect, ref float val)
		{
			Rect incrButtonRect = new Rect(fieldRect.x-_incrButtonWidth, fieldRect.y, _incrButtonWidth, _entryHeight); 
			if(GUI.Button(incrButtonRect, "-"))
			{
				val -= 5;
			}

			incrButtonRect.x -= _incrButtonWidth;

			if(GUI.Button(incrButtonRect, "--"))
			{
				val -= 50;
			}

			incrButtonRect.x = fieldRect.x + fieldRect.width;

			if(GUI.Button(incrButtonRect, "+"))
			{
				val += 5;
			}

			incrButtonRect.x += _incrButtonWidth;

			if(GUI.Button(incrButtonRect, "++"))
			{
				val += 50;
			}
		}
		
		//AppLauncherSetup
		void AddToolbarButton()
		{
			if(!HasAddedButton)
			{
				Texture buttonTexture = GameDatabase.Instance.GetTexture("CameraTools/Textures/icon", false);
				ApplicationLauncher.Instance.AddModApplication(EnableGui, DisableGui, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
				CamTools.HasAddedButton = true;
			}
			
		}
		
		void EnableGui()
		{
			GuiEnabled = true;
			Debug.Log ("Showing CamTools GUI");
		}
		
		void DisableGui()
		{
			GuiEnabled = false;	
			Debug.Log ("Hiding CamTools GUI");
		}
			
		void Dummy()
		{}
		
		void GameUiEnable()
		{
			_gameUiToggle = true;	
		}
		
		void GameUiDisable()
		{
			_gameUiToggle = false;	
		}
		
		void CycleReferenceMode(bool forward)
		{
			var length = System.Enum.GetValues(typeof(ReferenceModes)).Length;
			if(forward)
			{
				ReferenceMode++;
				if((int)ReferenceMode == length) ReferenceMode = 0;
			}
			else
			{
				ReferenceMode--;
				if((int)ReferenceMode == -1) ReferenceMode = (ReferenceModes) length-1;
			}
		}

		void CycleToolMode(bool forward)
		{
			var length = System.Enum.GetValues(typeof(ToolModes)).Length;
			if(forward)
			{
				ToolMode++;
				if((int)ToolMode == length) ToolMode = 0;
			}
			else
			{
				ToolMode--;
				if((int)ToolMode == -1) ToolMode = (ToolModes) length-1;
			}
		}
		
		void OnFloatingOriginShift(Vector3d offset, Vector3d data1)
		{
			/*
			Debug.LogWarning ("======Floating origin shifted.======");
			Debug.LogWarning ("======Passed offset: "+offset+"======");
			Debug.LogWarning ("======FloatingOrigin offset: "+FloatingOrigin.fetch.offset+"======");
			Debug.LogWarning("========Floating Origin threshold: "+FloatingOrigin.fetch.threshold+"==========");
			*/
		}

		void UpdateLoadedVessels()
		{
			if(_loadedVessels == null)
			{
				_loadedVessels = new List<Vessel>();
			}
			else
			{
				_loadedVessels.Clear();
			}

			foreach(var v in FlightGlobals.Vessels)
			{
				if(v.loaded && v.vesselType != VesselType.Debris && !v.isActiveVessel)
				{
					_loadedVessels.Add(v);
				}
			}
		}

		private void CheckForBdai(Vessel v)
		{
			_hasBdai = false;
			_aiComponent = null;
			if(v)
			{
				foreach(Part p in v.parts)
				{
					if(p.GetComponent("BDModulePilotAI"))
					{
						_hasBdai = true;
						_aiComponent = (object)p.GetComponent("BDModulePilotAI");
						return;
					}
				}
			}
		}

		private Vessel GetAiTargetedVessel()
		{
			if(!_hasBdai || _aiComponent==null || _bdAiTargetField==null)
			{
				return null;
			}

			return (Vessel) _bdAiTargetField.GetValue(_aiComponent);
		}

		private Type AiModuleType()
		{
			//Debug.Log("loaded assy's: ");
			foreach(var assy in AssemblyLoader.loadedAssemblies)
			{
				//Debug.Log("- "+assy.assembly.FullName);
				if(assy.assembly.FullName.Contains("BahaTurret,"))
				{
					foreach(var t in assy.assembly.GetTypes())
					{
						if(t.Name == "BDModulePilotAI")
						{
							return t;
						}
					}
				}
			}

			return null;
		}

		private FieldInfo GetAiTargetField()
		{
			Type aiModType = AiModuleType();
			if(aiModType == null) return null;

			FieldInfo[] fields = aiModType.GetFields(BindingFlags.NonPublic|BindingFlags.Instance);
			//Debug.Log("bdai fields: ");
			foreach(var f in fields)
			{
				//Debug.Log("- " + f.Name);
				if(f.Name == "targetVessel")
				{
					return f;
				}
			}

			return null;
		}


		void SwitchToVessel(Vessel v)
		{
			_vessel = v;

			CheckForBdai(v);

			if(_cameraToolActive)
			{
				if(ToolMode == ToolModes.DogfightCamera)
				{
					StartCoroutine(ResetDogfightCamRoutine());
				}
			}
		}

		IEnumerator ResetDogfightCamRoutine()
		{
			yield return new WaitForEndOfFrame();
			RevertCamera();
			StartDogfightCamera();
		}

		void CreateNewPath()
		{
			_showKeyframeEditor = false;
			_availablePaths.Add(new CameraPath());
			_selectedPathIndex = _availablePaths.Count - 1;
		}

		void DeletePath(int index)
		{
			if(index < 0) return;
			if(index >= _availablePaths.Count) return;
			_availablePaths.RemoveAt(index);
			_selectedPathIndex = -1;
		}

		void SelectPath(int index)
		{
			_selectedPathIndex = index;
		}

		void SelectKeyframe(int index)
		{
			if(_isPlayingPath)
			{
				StopPlayingPath();
			}
			_currentKeyframeIndex = index;
			UpdateCurrentValues();
			_showKeyframeEditor = true;
			ViewKeyframe(_currentKeyframeIndex);
		}

		void DeselectKeyframe()
		{
			_currentKeyframeIndex = -1;
			_showKeyframeEditor = false;
		}

		void DeleteKeyframe(int index)
		{
			CurrentPath.RemoveKeyframe(index);
			if(index == _currentKeyframeIndex)
			{
				DeselectKeyframe();
			}
			if(CurrentPath.KeyframeCount > 0 && _currentKeyframeIndex >= 0)
			{
				SelectKeyframe(Mathf.Clamp(_currentKeyframeIndex, 0, CurrentPath.KeyframeCount - 1));
			}
		}

		void UpdateCurrentValues()
		{
			if(CurrentPath == null) return;
			if(_currentKeyframeIndex < 0 || _currentKeyframeIndex >= CurrentPath.KeyframeCount)
			{
				return;
			}
			CameraKeyframe currentKey = CurrentPath.GetKeyframe(_currentKeyframeIndex);
			_currentKeyframeTime = currentKey.Time;

			_currKeyTimeString = _currentKeyframeTime.ToString();
		}

		void CreateNewKeyframe()
		{
			if(!_cameraToolActive)
			{
				StartPathingCam();
			}

			_showPathSelectorWindow = false;

			float time = CurrentPath.KeyframeCount > 0 ? CurrentPath.GetKeyframe(CurrentPath.KeyframeCount - 1).Time + 1 : 0;
			CurrentPath.AddTransform(_flightCamera.transform, ZoomExp, time);
			SelectKeyframe(CurrentPath.KeyframeCount - 1);

			if(CurrentPath.KeyframeCount > 6)
			{
				_keysScrollPos.y += _entryHeight;
			}
		}

		void ViewKeyframe(int index)
		{
			if(!_cameraToolActive)
			{
				StartPathingCam();
			}
			CameraKeyframe currentKey = CurrentPath.GetKeyframe(index);
			_flightCamera.transform.localPosition = currentKey.Position;
			_flightCamera.transform.localRotation = currentKey.Rotation;
			ZoomExp = currentKey.Zoom;
		}

		void StartPathingCam()
		{
			_vessel = FlightGlobals.ActiveVessel;
			_cameraUp = -FlightGlobals.getGeeForceAtPosition(_vessel.GetWorldPos3D()).normalized;
			if(FlightCamera.fetch.mode == FlightCamera.Modes.ORBITAL || (FlightCamera.fetch.mode == FlightCamera.Modes.AUTO && FlightCamera.GetAutoModeForVessel(_vessel) == FlightCamera.Modes.ORBITAL))
			{
				_cameraUp = Vector3.up;
			}

			_cameraParent.transform.position = _vessel.transform.position+_vessel.rb_velocity*Time.fixedDeltaTime;
			_cameraParent.transform.rotation = _vessel.transform.rotation;
            _flightCamera.SetTargetNone();
            _flightCamera.transform.parent = _cameraParent.transform;
            _flightCamera.DeactivateUpdate();

               _cameraToolActive = true;
		}

		void PlayPathingCam()
		{
			if(_selectedPathIndex < 0)
			{
				RevertCamera();
				return;
			}

			if(CurrentPath.KeyframeCount <= 0)
			{
				RevertCamera();
				return;
			}

			DeselectKeyframe();

			if(!_cameraToolActive)
			{
				StartPathingCam();
			}

			CameraTransformation firstFrame = CurrentPath.Evaulate(0);
			_flightCamera.transform.localPosition = firstFrame.Position;
			_flightCamera.transform.localRotation = firstFrame.Rotation;
			ZoomExp = firstFrame.Zoom;

			_isPlayingPath = true;
			_pathStartTime = Time.time;
		}

		void StopPlayingPath()
		{
			_isPlayingPath = false;
		}

		void TogglePathList()
		{
			_showKeyframeEditor = false;
			_showPathSelectorWindow = !_showPathSelectorWindow;
		}

		void UpdatePathingCam()
		{
			_cameraParent.transform.position = _vessel.transform.position+_vessel.rb_velocity*Time.fixedDeltaTime;
			_cameraParent.transform.rotation = _vessel.transform.rotation;

			if(_isPlayingPath)
			{
				CameraTransformation tf = CurrentPath.Evaulate(PathTime * CurrentPath.TimeScale);
				_flightCamera.transform.localPosition = Vector3.Lerp(_flightCamera.transform.localPosition, tf.Position, CurrentPath.LerpRate*Time.fixedDeltaTime);
				_flightCamera.transform.localRotation = Quaternion.Slerp(_flightCamera.transform.localRotation, tf.Rotation, CurrentPath.LerpRate*Time.fixedDeltaTime);
				ZoomExp = Mathf.Lerp(ZoomExp, tf.Zoom, CurrentPath.LerpRate*Time.fixedDeltaTime);
			}
			else
			{
				//move
				//mouse panning, moving
				Vector3 forwardLevelAxis = _flightCamera.transform.forward;//(Quaternion.AngleAxis(-90, cameraUp) * flightCamera.transform.right).normalized;
				Vector3 rightAxis = _flightCamera.transform.right;//(Quaternion.AngleAxis(90, forwardLevelAxis) * cameraUp).normalized;
				if(EnableKeypad)
				{
					if(Input.GetKey(_fmUpKey))
					{
						_flightCamera.transform.position += _cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;	
					}
					else if(Input.GetKey(_fmDownKey))
					{
						_flightCamera.transform.position -= _cameraUp * FreeMoveSpeed * Time.fixedDeltaTime;	
					}
					if(Input.GetKey(_fmForwardKey))
					{
						_flightCamera.transform.position += forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
					}
					else if(Input.GetKey(_fmBackKey))
					{
						_flightCamera.transform.position -= forwardLevelAxis * FreeMoveSpeed * Time.fixedDeltaTime;
					}
					if(Input.GetKey(_fmLeftKey))
					{
						_flightCamera.transform.position -= _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
					}
					else if(Input.GetKey(_fmRightKey))
					{
						_flightCamera.transform.position += _flightCamera.transform.right * FreeMoveSpeed * Time.fixedDeltaTime;
					}

					//keyZoom
					if(!AutoFov)
					{
						if(Input.GetKey(_fmZoomInKey))
						{
							ZoomExp = Mathf.Clamp(ZoomExp + (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8);
						}
						else if(Input.GetKey(_fmZoomOutKey))
						{
							ZoomExp = Mathf.Clamp(ZoomExp - (KeyZoomSpeed * Time.fixedDeltaTime), 1, 8);
						}
					}
					else
					{
						if(Input.GetKey(_fmZoomInKey))
						{
							_autoZoomMargin = Mathf.Clamp(_autoZoomMargin + (KeyZoomSpeed  * 10 * Time.fixedDeltaTime), 0, 50);
						}
						else if(Input.GetKey(_fmZoomOutKey))
						{
							_autoZoomMargin = Mathf.Clamp(_autoZoomMargin - (KeyZoomSpeed * 10 * Time.fixedDeltaTime), 0, 50);
						}
					}
				}

				if(Input.GetKey(KeyCode.Mouse1) && Input.GetKey(KeyCode.Mouse2))
				{
					_flightCamera.transform.rotation = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * -1.7f, _flightCamera.transform.forward) * _flightCamera.transform.rotation;
				}
				else
				{
					if(Input.GetKey(KeyCode.Mouse1))
					{
						_flightCamera.transform.rotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * 1.7f/(ZoomExp*ZoomExp), Vector3.up); //*(Mathf.Abs(Mouse.delta.x)/7)
						_flightCamera.transform.rotation *= Quaternion.AngleAxis(-Input.GetAxis("Mouse Y") * 1.7f/(ZoomExp*ZoomExp), Vector3.right);
						_flightCamera.transform.rotation = Quaternion.LookRotation(_flightCamera.transform.forward, _flightCamera.transform.up);
					}
					if(Input.GetKey(KeyCode.Mouse2))
					{
						_flightCamera.transform.position += _flightCamera.transform.right * Input.GetAxis("Mouse X") * 2;
						_flightCamera.transform.position += forwardLevelAxis * Input.GetAxis("Mouse Y") * 2;
					}
				}
				_flightCamera.transform.position += _flightCamera.transform.up * 10 * Input.GetAxis("Mouse ScrollWheel");

			}

			//zoom
			_zoomFactor = Mathf.Exp(ZoomExp) / Mathf.Exp(1);
			_manualFov = 60 / _zoomFactor;
			_updateFov = (_currentFov != _manualFov);
			if(_updateFov)
			{
				_currentFov = Mathf.Lerp(_currentFov, _manualFov, 0.1f);
				_flightCamera.SetFoV(_currentFov);
				_updateFov = false;
			}
		}
		
	}


	
	
	
	public enum ReferenceModes {InitialVelocity, Surface, Orbit}
	
	public enum ToolModes {StationaryCamera, DogfightCamera, Pathing};
}

