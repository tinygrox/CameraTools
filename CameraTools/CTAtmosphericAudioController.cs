using UnityEngine;

namespace CameraTools
{
	public class CtAtmosphericAudioController : MonoBehaviour
	{
		AudioSource _windAudioSource;
		AudioSource _windHowlAudioSource;
		AudioSource _windTearAudioSource;

		AudioSource _sonicBoomSource;

		Vessel _vessel;

		bool _playedBoom = false;

		void Awake()
		{
			_vessel = GetComponent<Vessel>();
			_windAudioSource = gameObject.AddComponent<AudioSource>();
			_windAudioSource.minDistance = 10;
			_windAudioSource.maxDistance = 10000;
			_windAudioSource.dopplerLevel = .35f;
			_windAudioSource.spatialBlend = 1;
			AudioClip windclip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windloop");
			if(!windclip)
			{
				Destroy (this);
				return;
			}
			_windAudioSource.clip = windclip;

			_windHowlAudioSource = gameObject.AddComponent<AudioSource>();
			_windHowlAudioSource.minDistance = 10;
			_windHowlAudioSource.maxDistance = 7000;
			_windHowlAudioSource.dopplerLevel = .5f;
			_windHowlAudioSource.pitch = 0.25f;
			_windHowlAudioSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windhowl");
			_windHowlAudioSource.spatialBlend = 1;

			_windTearAudioSource = gameObject.AddComponent<AudioSource>();
			_windTearAudioSource.minDistance = 10;
			_windTearAudioSource.maxDistance = 5000;
			_windTearAudioSource.dopplerLevel = 0.45f;
			_windTearAudioSource.pitch = 0.65f;
			_windTearAudioSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/windtear");
			_windTearAudioSource.spatialBlend = 1;

			_sonicBoomSource = new GameObject().AddComponent<AudioSource>();
			_sonicBoomSource.transform.parent = _vessel.transform;
			_sonicBoomSource.transform.localPosition = Vector3.zero;
			_sonicBoomSource.minDistance = 50;
			_sonicBoomSource.maxDistance = 20000;
			_sonicBoomSource.dopplerLevel = 0;
			_sonicBoomSource.clip = GameDatabase.Instance.GetAudioClip("CameraTools/Sounds/sonicBoom");
			_sonicBoomSource.volume = Mathf.Clamp01(_vessel.GetTotalMass()/4f);
			_sonicBoomSource.Stop();
			_sonicBoomSource.spatialBlend = 1;

			float angleToCam = Vector3.Angle(_vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - _vessel.transform.position);
			angleToCam = Mathf.Clamp(angleToCam, 1, 180);
			if(_vessel.srfSpeed / (angleToCam) < 3.67f)
			{
				_playedBoom = true;
			}

			CamTools.OnResetCTools += OnResetCTools;
		}


		void FixedUpdate()
		{
			if(!_vessel)
			{
				return;
			}
			if(Time.timeScale > 0 && _vessel.dynamicPressurekPa > 0)
			{
				float srfSpeed = (float)_vessel.srfSpeed;
				srfSpeed = Mathf.Min(srfSpeed, 550f);
				float angleToCam = Vector3.Angle(_vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - _vessel.transform.position);
				angleToCam = Mathf.Clamp(angleToCam, 1, 180);
			

				float lagAudioFactor = (75000 / (Vector3.Distance(_vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position) * srfSpeed * angleToCam / 90));
				lagAudioFactor = Mathf.Clamp(lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4);
				lagAudioFactor += srfSpeed / 230;

				float waveFrontFactor = ((3.67f * angleToCam)/srfSpeed);
				waveFrontFactor = Mathf.Clamp(waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2);


				if(_vessel.srfSpeed > CamTools.SpeedOfSound)
				{
					waveFrontFactor =  (srfSpeed / (angleToCam) < 3.67f) ? waveFrontFactor + ((srfSpeed/(float)CamTools.SpeedOfSound)*waveFrontFactor) : 0;
					if(waveFrontFactor > 0)
					{
						if(!_playedBoom)
						{
							_sonicBoomSource.transform.position = _vessel.transform.position + (-_vessel.srf_velocity);
							_sonicBoomSource.PlayOneShot(_sonicBoomSource.clip);
						}
						_playedBoom = true;
					}
					else
					{

					}
				}
				else if(CamTools.SpeedOfSound / (angleToCam) < 3.67f)
				{
					_playedBoom = true;
				}

				lagAudioFactor *= waveFrontFactor;

				float sqrAccel = (float)_vessel.acceleration.sqrMagnitude;

				//windloop
				if(!_windAudioSource.isPlaying)
				{
					_windAudioSource.Play();
					//Debug.Log("vessel dynamic pressure: " + vessel.dynamicPressurekPa);
				}
				float pressureFactor = Mathf.Clamp01((float)_vessel.dynamicPressurekPa / 50f);
				float massFactor = Mathf.Clamp01(_vessel.GetTotalMass() / 60f);
				float gFactor = Mathf.Clamp(sqrAccel / 225, 0, 1.5f);
				_windAudioSource.volume = massFactor * pressureFactor * gFactor * lagAudioFactor;


				//windhowl
				if(!_windHowlAudioSource.isPlaying)
				{
					_windHowlAudioSource.Play();
				}
				float pressureFactor2 = Mathf.Clamp01((float)_vessel.dynamicPressurekPa / 20f);
				float massFactor2 = Mathf.Clamp01(_vessel.GetTotalMass() / 30f);
				_windHowlAudioSource.volume = pressureFactor2 * massFactor2 * lagAudioFactor;
				_windHowlAudioSource.maxDistance = Mathf.Clamp(lagAudioFactor * 2500, _windTearAudioSource.minDistance, 16000);

				//windtear
				if(!_windTearAudioSource.isPlaying)
				{
					_windTearAudioSource.Play();
				}
				float pressureFactor3 = Mathf.Clamp01((float)_vessel.dynamicPressurekPa / 40f);
				float massFactor3 = Mathf.Clamp01(_vessel.GetTotalMass() / 10f);
				//float gFactor3 = Mathf.Clamp(sqrAccel / 325, 0.25f, 1f);
				_windTearAudioSource.volume = pressureFactor3 * massFactor3;

				_windTearAudioSource.minDistance = lagAudioFactor * 1;
				_windTearAudioSource.maxDistance = Mathf.Clamp(lagAudioFactor * 2500, _windTearAudioSource.minDistance, 16000);
			
			}
			else
			{
				if(_windAudioSource.isPlaying)
				{
					_windAudioSource.Stop();
				}

				if(_windHowlAudioSource.isPlaying)
				{
					_windHowlAudioSource.Stop();
				}

				if(_windTearAudioSource.isPlaying)
				{
					_windTearAudioSource.Stop();
				}
			}
		}

		void OnDestroy()
		{
			if(_sonicBoomSource)
			{
				Destroy(_sonicBoomSource.gameObject);
			}
			CamTools.OnResetCTools -= OnResetCTools;
		}

		void OnResetCTools()
		{
			Destroy(_windAudioSource);
			Destroy(_windHowlAudioSource);
			Destroy(_windTearAudioSource);

			Destroy(this);
		}
	}
}

