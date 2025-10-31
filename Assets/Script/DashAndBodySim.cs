using UnityEngine;

/// Visual-only: steering wheel + 2 speed needles + car body roll/jitter
/// Two modes: CarGo() / CarIdle(), with smooth sound blending, volume, and pitch control.
public class DashAndBodySim : MonoBehaviour
{
    [Header("References")]
    public Transform steeringWheel;
    public Transform speedoNeedle1;
    public Transform speedoNeedle2;
    public Transform carBody;

    [Header("Engine Sound")]
    [Tooltip("Looping audio for driving/engine running state.")]
    public AudioSource goSound;
    [Tooltip("Looping audio for idle/engine stop sound.")]
    public AudioSource idleSound;

    [Header("Sound Dynamics")]
    [Tooltip("Fade speed when switching between Go and Idle.")]
    public float soundFadeSpeed = 2f;
    [Tooltip("Volume of engine sounds (overall multiplier).")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Tooltip("Pitch range for Go sound (min = idle pitch, max = full speed pitch).")]
    public Vector2 goPitchRange = new Vector2(0.9f, 1.6f);
    [Tooltip("Pitch range for Idle sound (min, max).")]
    public Vector2 idlePitchRange = new Vector2(0.8f, 1.0f);
    [Tooltip("Volume range for Go sound (min, max).")]
    public Vector2 goVolumeRange = new Vector2(0.2f, 1.0f);
    [Tooltip("Volume range for Idle sound (min, max).")]
    public Vector2 idleVolumeRange = new Vector2(1.0f, 0.3f);

    [Header("Steering (Z rotation)")]
    public float wheelMaxZ = 240f;
    public float wheelSmoothTime = 0.14f;
    public float wheelDamping = 0.35f;

    [Header("Road Resistance Wobble")]
    public float wheelWobbleAmpDeg = 2.8f;
    public Vector2 wheelWobbleFreq = new Vector2(0.7f, 1.7f);
    public float wheelBumpKickDeg = 3.0f;
    public float wheelBumpIntervalHighSpeed = 1.3f;

    [Header("Speed Simulation")]
    public float maxSpeed = 160f;
    public float accelRate = 60f;
    public float decelRate = 80f;
    public float drag = 12f;

    [Header("Speedometer Needle Mapping (Z)")]
    public float needleMinZ = -130f;
    public float needleMaxZ = 130f;

    [Header("Needle personalities")]
    public float needle1SmoothTime = 0.12f;
    public float needle1BiasDeg = 0.0f;
    public float needle1FlutterDeg = 0.8f;
    public float needle1FlutterFreq = 26f;

    public float needle2SmoothTime = 0.18f;
    public float needle2BiasDeg = -1.2f;
    public float needle2FlutterDeg = 0.5f;
    public float needle2FlutterFreq = 22f;

    [Header("Vibration / Body Feel")]
    public float bodyRollFromSteer = 4f;
    public float vibRollAmpDeg = 0.35f;
    public Vector2 vibPosAmp = new Vector2(0.006f, 0.004f);
    public float vibFreqLow = 7f;
    public float vibFreqHigh = 14f;
    public float vibSpeedScale = 1.0f;

    [Header("Input / Demo")]
    public bool usePlayerInput = true;
    public float autoSteerAmount = 0.6f;
    public float autoThrottle = 0.75f;

    [Header("Mode Transition (Go↔Idle)")]
    public float modeLerpSpeed = 6f;

    // --- internals ---
    float _speed, _targetSpeed;
    float _wheelTargetZ, _wheelCurrentZ;
    float _wheelVel, _wheelBumpVel, _wheelBump;
    float _noiseSeed;
    float _needleZ1, _needleVel1, _needleZ2, _needleVel2;
    Vector3 _carBaseLocalPos;

    // Smooth mode blending
    float _modeBlendTarget = 0f; // 1 = Go, 0 = Idle
    float _modeBlend = 0f;

    void Awake()
    {
        if (carBody) _carBaseLocalPos = carBody.localPosition;
        _noiseSeed = Random.value * 1000f;

        _needleZ1 = needleMinZ;
        _needleZ2 = needleMinZ;

        // Prepare sound sources
        SetupAudioSource(goSound);
        SetupAudioSource(idleSound);

        // Start muted correctly
        UpdateEngineSound(0f, 0f);
    }

    void SetupAudioSource(AudioSource src)
    {
        if (!src) return;
        src.loop = true;
        src.playOnAwake = false;
        if (!src.isPlaying) src.Play();
    }

    void Update()
    {
        // Smoothly blend modes
        _modeBlend = Mathf.Lerp(_modeBlend, _modeBlendTarget, 1f - Mathf.Exp(-modeLerpSpeed * Time.deltaTime));
        float goK = _modeBlend;
        float idleK = 1f - _modeBlend;

        // ------------ INPUT ------------
        float steer01 = 0f, throttle = 0f, brake = 0f;

        if (usePlayerInput)
        {
            steer01 = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
            float v = Input.GetAxisRaw("Vertical");
            throttle = Mathf.Clamp01(v);
            brake = Mathf.Clamp01(-v);
        }
        else
        {
            steer01 = Mathf.Sin(Time.time * 0.8f) * autoSteerAmount;
            throttle = autoThrottle;
            brake = 0f;
        }

        steer01 *= goK;
        throttle *= goK;
        brake *= goK;

        // ------------ SPEED ------------
        float targetDelta = (throttle - brake) * accelRate;
        float extraDrag = drag * Mathf.Lerp(0f, 1.5f, idleK);

        _targetSpeed = Mathf.Clamp(_targetSpeed + targetDelta * Time.deltaTime, 0f, maxSpeed);
        float rate = (_targetSpeed > _speed) ? accelRate : decelRate;
        _speed = Mathf.MoveTowards(_speed, _targetSpeed, rate * Time.deltaTime);

        if (Mathf.Approximately(throttle, 0f) && Mathf.Approximately(brake, 0f))
            _speed = Mathf.MoveTowards(_speed, 0f, (drag + extraDrag) * Time.deltaTime);

        if (idleK > 0.95f && _speed < 0.05f) { _speed = 0f; _targetSpeed = 0f; }

        float speed01 = (maxSpeed > 0.001f) ? Mathf.Clamp01(_speed / maxSpeed) : 0f;

        // ------------ SOUND DYNAMICS ------------
        UpdateEngineSound(goK, speed01);

        // ------------ STEERING WHEEL ------------
        _wheelTargetZ = wheelMaxZ * -steer01;

        float wobbleScale = Mathf.Lerp(0.25f, 1f, goK);
        float wobbleK = speed01 * wobbleScale;
        float wobbleFreq = Mathf.Lerp(wheelWobbleFreq.x, wheelWobbleFreq.y, wobbleK);

        float wobbleT = Time.time * wobbleFreq;
        float wobble = (Mathf.PerlinNoise(_noiseSeed + wobbleT, 0.37f) * 2f - 1f) * wheelWobbleAmpDeg * wobbleK;

        float expectedInterval = Mathf.Lerp(2.5f, wheelBumpIntervalHighSpeed, speed01);
        float bumpChance = (expectedInterval > 0f) ? Time.deltaTime / expectedInterval : 0f;
        bumpChance *= Mathf.Lerp(0.25f, 1f, goK);
        if (Random.value < Mathf.Clamp01(bumpChance))
        {
            _wheelBumpVel += wheelBumpKickDeg * (0.6f + 0.4f * Random.value) * (0.7f + 0.3f * speed01) * (Random.value < 0.5f ? 1f : -1f);
        }

        _wheelBump = Mathf.Lerp(_wheelBump, 0f, 1f - Mathf.Exp(-6f * Time.deltaTime));
        _wheelBump += _wheelBumpVel * Time.deltaTime;
        _wheelBumpVel = Mathf.Lerp(_wheelBumpVel, 0f, 1f - Mathf.Exp(-12f * Time.deltaTime));

        float centerBias = Mathf.Lerp(0f, 0.65f, idleK);
        float desired = Mathf.Lerp(_wheelTargetZ + wobble + _wheelBump, 0f, centerBias);

        float damp = Mathf.Clamp01(wheelDamping);
        float smooth = Mathf.Max(0.01f, wheelSmoothTime) * (1f + 0.8f * damp);
        _wheelCurrentZ = Mathf.SmoothDampAngle(_wheelCurrentZ, desired, ref _wheelVel, smooth);
        if (steeringWheel) SetLocalZ(steeringWheel, _wheelCurrentZ);

        // ------------ NEEDLES ------------
        float display01 = Mathf.Lerp(0f, speed01, goK);
        float needleTargetZ = Mathf.Lerp(needleMinZ, needleMaxZ, display01);

        float n1Desired = needleTargetZ + needle1BiasDeg + Flutter(needle1FlutterFreq, needle1FlutterDeg, display01, 0.17f);
        _needleZ1 = Mathf.SmoothDampAngle(_needleZ1, n1Desired, ref _needleVel1, Mathf.Max(0.02f, needle1SmoothTime));

        float n2Desired = needleTargetZ + needle2BiasDeg + Flutter(needle2FlutterFreq, needle2FlutterDeg, display01, 0.63f);
        _needleZ2 = Mathf.SmoothDampAngle(_needleZ2, n2Desired, ref _needleVel2, Mathf.Max(0.02f, needle2SmoothTime));

        if (speedoNeedle1) SetLocalZ(speedoNeedle1, _needleZ1);
        if (speedoNeedle2) SetLocalZ(speedoNeedle2, _needleZ2);

        // ------------ BODY ROLL + VIBRATION ------------
        if (carBody)
        {
            float baseRollZ = bodyRollFromSteer * steer01;
            float k = Mathf.Pow(speed01, vibSpeedScale) * Mathf.Lerp(0.25f, 1f, goK);
            float freq = Mathf.Lerp(vibFreqLow, vibFreqHigh, k);
            float t = Time.time * freq;

            float nx = (Mathf.PerlinNoise(_noiseSeed + t, 0.37f) * 2f - 1f);
            float ny = (Mathf.PerlinNoise(_noiseSeed + 0.53f, t) * 2f - 1f);
            float nz = (Mathf.PerlinNoise(_noiseSeed + 1.11f, t * 0.85f) * 2f - 1f);

            float vibRollZ = nz * vibRollAmpDeg * k;
            float rollZ = baseRollZ + vibRollZ;

            var e = carBody.localEulerAngles;
            e.x = 0f; e.y = 0f; e.z = rollZ;
            carBody.localEulerAngles = e;

            Vector3 p = _carBaseLocalPos;
            p.x += nx * vibPosAmp.x * k;
            p.y += ny * vibPosAmp.y * k;
            carBody.localPosition = p;
        }
    }

    // --------- Public API ---------
    public void CarGo() => _modeBlendTarget = 1f;
    public void CarIdle() => _modeBlendTarget = 0f;

    // --------- SOUND HANDLER ---------
    void UpdateEngineSound(float goK, float speed01)
    {
        if (!goSound && !idleSound) return;

        // Crossfade volumes
        float goVol = Mathf.Lerp(goVolumeRange.x, goVolumeRange.y, speed01) * goK * masterVolume;
        float idleVol = Mathf.Lerp(idleVolumeRange.x, idleVolumeRange.y, speed01) * (1f - goK) * masterVolume;

        if (goSound)
        {
            goSound.volume = Mathf.Lerp(goSound.volume, goVol, Time.deltaTime * soundFadeSpeed);
            goSound.pitch = Mathf.Lerp(goPitchRange.x, goPitchRange.y, speed01);
        }

        if (idleSound)
        {
            idleSound.volume = Mathf.Lerp(idleSound.volume, idleVol, Time.deltaTime * soundFadeSpeed);
            idleSound.pitch = Mathf.Lerp(idlePitchRange.x, idlePitchRange.y, speed01);
        }
    }

    // --------- Helpers ---------
    float Flutter(float freq, float ampDeg, float speed01, float seedOffset)
    {
        if (ampDeg <= 0f || speed01 <= 0f) return 0f;
        float f = Mathf.Lerp(freq * 0.25f, freq, speed01);
        float t = (Time.time + seedOffset) * f;
        float n = Mathf.PerlinNoise(_noiseSeed + t, seedOffset) * 2f - 1f;
        return n * ampDeg * speed01;
    }

    void SetLocalZ(Transform t, float zDegrees)
    {
        var e = t.localEulerAngles;
        e.z = zDegrees;
        t.localEulerAngles = e;
    }
}
