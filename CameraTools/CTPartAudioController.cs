using UnityEngine;
namespace CameraTools
{
	public class CtPartAudioController : MonoBehaviour
	{
		Vessel _vessel;
		Part _part;

		public AudioSource AudioSource;


		float _origMinDist = 1;
		float _origMaxDist = 1;

		float _modMinDist = 10;
		float _modMaxDist = 10000;

		AudioRolloffMode _origRolloffMode;

		void Awake()
		{
			_part = GetComponentInParent<Part>();
			_vessel = _part.vessel;

			CamTools.OnResetCTools += OnResetCTools;
		}

		void Start()
		{
			if(!AudioSource)
			{
				Destroy(this);
				return;
			}

			_origMinDist = AudioSource.minDistance;
			_origMaxDist = AudioSource.maxDistance;
			_origRolloffMode = AudioSource.rolloffMode;
			AudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
			AudioSource.spatialBlend = 1;
	
		}

		void FixedUpdate()
		{
			if(!AudioSource)
			{
				Destroy(this);
				return;
			}

			if(!_part || !_vessel)
			{
				Destroy(this);
				return;
			}


			float angleToCam = Vector3.Angle(_vessel.srf_velocity, FlightCamera.fetch.mainCamera.transform.position - _vessel.transform.position);
			angleToCam = Mathf.Clamp(angleToCam, 1, 180);

			float srfSpeed = (float)_vessel.srfSpeed;
			srfSpeed = Mathf.Min(srfSpeed, 550f);

			float lagAudioFactor = (75000 / (Vector3.Distance(_vessel.transform.position, FlightCamera.fetch.mainCamera.transform.position) * srfSpeed * angleToCam / 90));
			lagAudioFactor = Mathf.Clamp(lagAudioFactor * lagAudioFactor * lagAudioFactor, 0, 4);
			lagAudioFactor += srfSpeed / 230;

			float waveFrontFactor = ((3.67f * angleToCam)/srfSpeed);
			waveFrontFactor = Mathf.Clamp(waveFrontFactor * waveFrontFactor * waveFrontFactor, 0, 2);
			if(_vessel.srfSpeed > CamTools.SpeedOfSound)
			{
				waveFrontFactor = (srfSpeed / (angleToCam) < 3.67f) ? waveFrontFactor + ((srfSpeed/(float)CamTools.SpeedOfSound)*waveFrontFactor): 0;
			}

			lagAudioFactor *= waveFrontFactor;
		
			AudioSource.minDistance = Mathf.Lerp(_origMinDist, _modMinDist * lagAudioFactor, Mathf.Clamp01((float)_vessel.srfSpeed/30));
			AudioSource.maxDistance = Mathf.Lerp(_origMaxDist,Mathf.Clamp(_modMaxDist * lagAudioFactor, AudioSource.minDistance, 16000), Mathf.Clamp01((float)_vessel.srfSpeed/30));
				
		}

		void OnDestroy()
		{
			CamTools.OnResetCTools -= OnResetCTools;

	
		}

		void OnResetCTools()
		{
			AudioSource.minDistance = _origMinDist;
			AudioSource.maxDistance = _origMaxDist;
			AudioSource.rolloffMode = _origRolloffMode;
			Destroy(this);
		}


	}
}

