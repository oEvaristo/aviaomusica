using UnityEngine;
using UnityEngine.InputSystem;

namespace ProximoVoo
{
    public enum FlightPhase { Ready, StartingEngine, Rolling, TakingOff, Flying, Completed, Landing, Farewell }

    public sealed class FlightController : MonoBehaviour
    {
        [Header("Voo")]
        [SerializeField] private float forwardSpeed = 4.5f;
        [SerializeField] private float climbSpeed = 2.8f;
        [SerializeField] private Vector2 altitudeLimits = new Vector2(2.5f, 10f);
        [SerializeField] private float flightDuration = 80f;
        [SerializeField] private Transform aircraftVisual;
        [SerializeField] private Camera flightCamera;
        [Header("Câmera de abertura")]
        [Tooltip("Quanto a aeronave fica ampliada antes de iniciar o voo.")]
        [Range(1f, 2.5f)]
        [SerializeField] private float openingZoomMultiplier = 1.55f;
        [Header("Decolagem")]
        [SerializeField] private float engineStartDuration = 2f;
        [SerializeField] private float groundRollDuration = 6f;
        [SerializeField] private float takeoffDuration = 4f;
        [SerializeField] private float groundAltitude = .6f;
        [Header("Pouso e despedida")]
        [SerializeField] private float landingDuration = 6f;
        [SerializeField, Min(0f)] private float nightfallDelay = 10f;
        [SerializeField, Min(.1f)] private float nightfallDuration = 3f;
        [SerializeField, Min(0f)] private float farewellDuration = 10f;
        [Header("Hélice")]
        [SerializeField] private SpriteRenderer propeller;
        [SerializeField] private Sprite[] propellerFrames;
        [SerializeField] private float propellerFramesPerSecond = 24f;

        private float verticalSpeed, propellerClock, phaseElapsed;
        private Vector3 startPosition;
        private bool paused;
        private InputAction pauseAction, restartAction;
        private AudioSource engine;
        private AudioClip engineClip;
        private float flightCameraSize;
        private Vector3 landingStartPosition;
        private const float GroundPitch = 9f;

        public FlightPhase Phase { get; private set; } = FlightPhase.Ready;
        public float PhaseElapsed => phaseElapsed;
        public float NightfallDelay => nightfallDelay;
        public float NightfallDuration => nightfallDuration;
        public float FarewellStart => nightfallDelay + nightfallDuration;
        public bool PlayerHasControl => Phase == FlightPhase.Flying;
        public float FlightDuration => flightDuration;
        public float ForwardSpeed => forwardSpeed;
        public float ClimbSpeed => climbSpeed;
        public float FlightElapsed => Phase == FlightPhase.Completed || Phase == FlightPhase.Landing || Phase == FlightPhase.Farewell ? flightDuration :
            Phase == FlightPhase.Flying ? Mathf.Min(phaseElapsed, flightDuration) : 0f;
        public bool IsPaused => paused;
        public float DistanceTravelled => transform.position.x - startPosition.x;
        public int PropellerFrame { get; private set; }
        public float AirportVisibility => Phase == FlightPhase.Flying ? 0f :
            Phase == FlightPhase.Landing ? Mathf.SmoothStep(0f, 1f, phaseElapsed / 1.2f) :
            Phase == FlightPhase.TakingOff ? 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.25f, 1f, phaseElapsed / takeoffDuration)) : 1f;

        private void Awake()
        {
            if (flightCamera != null) flightCameraSize = flightCamera.orthographicSize;
            startPosition = new Vector3(transform.position.x, groundAltitude, transform.position.z);
            engine = GetComponent<AudioSource>();
            if (engine == null) engine = gameObject.AddComponent<AudioSource>();
            engine.playOnAwake = false;
            engine.loop = true;
            engine.spatialBlend = 0f;
            CreateEngineSound();
            RestartFlight();
        }

        private void OnEnable()
        {
            pauseAction = new InputAction("Pausar", InputActionType.Button, "<Keyboard>/space");
            restartAction = new InputAction("Reiniciar", InputActionType.Button, "<Keyboard>/r");
            pauseAction.performed += context => TogglePause();
            restartAction.performed += context => RestartFlight();
            pauseAction.Enable();
            restartAction.Enable();
        }

        private void OnDisable()
        {
            pauseAction?.Dispose();
            restartAction?.Dispose();
            pauseAction = null;
            restartAction = null;
            if (engine != null) engine.Stop();
        }

        public void StartFlight()
        {
            if (Phase != FlightPhase.Ready) return;
            SetPhase(FlightPhase.StartingEngine);
            ApplyCameraPose();
            engine.volume = .02f;
            engine.pitch = .45f;
            engine.Play();
        }

        public void PlayAgain()
        {
            RestartFlight();
            StartFlight();
        }

        private void TogglePause()
        {
            if (Phase == FlightPhase.Ready) return;
            paused = !paused;
            if (engine == null) return;
            if (paused) engine.Pause(); else engine.UnPause();
        }

        public void RestartFlight()
        {
            transform.position = startPosition;
            verticalSpeed = 0f;
            propellerClock = 0f;
            paused = false;
            SetPhase(FlightPhase.Ready);
            if (engine != null) engine.Stop();
            if (aircraftVisual != null) aircraftVisual.localRotation = Quaternion.Euler(0f, 0f, GroundPitch);
            PropellerFrame = 0;
            if (propeller != null && propellerFrames != null && propellerFrames.Length > 0)
                propeller.sprite = propellerFrames[0];
            ApplyCameraPose();
        }

        private void SetPhase(FlightPhase phase)
        {
            Phase = phase;
            phaseElapsed = 0f;
        }

        private void Update()
        {
            if (paused || Phase == FlightPhase.Ready || Phase == FlightPhase.Completed) return;
            phaseElapsed += Time.deltaTime;
            float pitch;
            float rpm = 1f;
            Vector3 position = transform.position;
            switch (Phase)
            {
                case FlightPhase.Landing:
                    float landing = Mathf.Clamp01(phaseElapsed / landingDuration);
                    position.x = landingStartPosition.x + forwardSpeed * landingDuration * (landing - .5f * landing * landing);
                    position.y = Mathf.Lerp(landingStartPosition.y, groundAltitude, Mathf.SmoothStep(0f, 1f, landing));
                    pitch = Mathf.Lerp(-5f, GroundPitch, Mathf.SmoothStep(0f, 1f, landing));
                    engine.volume = Mathf.Lerp(.12f, .05f, landing);
                    if (landing >= 1f)
                    {
                        position.y = groundAltitude;
                        transform.position = position;
                        if (aircraftVisual != null) aircraftVisual.localRotation = Quaternion.Euler(0f, 0f, GroundPitch);
                        engine.Stop();
                        SetPhase(FlightPhase.Farewell);
                        return;
                    }
                    break;
                case FlightPhase.Farewell:
                    if (phaseElapsed >= FarewellStart + farewellDuration) SetPhase(FlightPhase.Completed);
                    return;
                case FlightPhase.StartingEngine:
                    rpm = Mathf.Clamp01(phaseElapsed / engineStartDuration);
                    pitch = GroundPitch;
                    engine.pitch = Mathf.Lerp(.45f, 1.1f, rpm);
                    engine.volume = Mathf.Lerp(.02f, .19f, rpm);
                    if (phaseElapsed >= engineStartDuration) SetPhase(FlightPhase.Rolling);
                    break;
                case FlightPhase.Rolling:
                    float roll = Mathf.Clamp01(phaseElapsed / groundRollDuration);
                    position.x += forwardSpeed * roll * Time.deltaTime;
                    pitch = Mathf.Lerp(GroundPitch, 0f, Mathf.SmoothStep(0f, 1f, roll));
                    // Raise the tail while keeping the main wheels on the runway.
                    float radians = pitch * Mathf.Deg2Rad;
                    position.y = RunwayWheelHeight - (1.37f * Mathf.Sin(radians) - Mathf.Cos(radians));
                    if (phaseElapsed >= groundRollDuration) SetPhase(FlightPhase.TakingOff);
                    break;
                case FlightPhase.TakingOff:
                    float lift = Mathf.Clamp01(phaseElapsed / takeoffDuration);
                    position.x += forwardSpeed * Time.deltaTime;
                    position.y = Mathf.Lerp(RunwayWheelHeight + 1f, 4f, Mathf.SmoothStep(0f, 1f, lift));
                    pitch = Mathf.Sin(lift * Mathf.PI) * 12f;
                    if (phaseElapsed >= takeoffDuration) SetPhase(FlightPhase.Flying);
                    break;
                default:
                    if (phaseElapsed >= flightDuration)
                    {
                        landingStartPosition = transform.position;
                        verticalSpeed = 0f;
                        SetPhase(FlightPhase.Landing);
                        return;
                    }
                    float input = ReadVerticalInput();
                    verticalSpeed = Mathf.MoveTowards(verticalSpeed, input * climbSpeed, 6f * Time.deltaTime);
                    position.x += forwardSpeed * Time.deltaTime;
                    position.y = Mathf.Clamp(position.y + verticalSpeed * Time.deltaTime, altitudeLimits.x, altitudeLimits.y);
                    if ((position.y <= altitudeLimits.x && verticalSpeed < 0f) ||
                        (position.y >= altitudeLimits.y && verticalSpeed > 0f)) verticalSpeed = 0f;
                    pitch = verticalSpeed / climbSpeed * 10f;
                    engine.volume = Mathf.Lerp(engine.volume, .12f, Time.deltaTime);
                    break;
            }
            transform.position = position;
            if (aircraftVisual != null)
                aircraftVisual.localRotation = Quaternion.Slerp(aircraftVisual.localRotation,
                    Quaternion.Euler(0f, 0f, pitch), 1f - Mathf.Exp(-5f * Time.deltaTime));
            if (propeller != null && propellerFrames != null && propellerFrames.Length > 0)
            {
                propellerClock += Time.deltaTime * propellerFramesPerSecond * rpm;
                PropellerFrame = Mathf.FloorToInt(propellerClock) % propellerFrames.Length;
                propeller.sprite = propellerFrames[PropellerFrame];
            }
        }

        // Contact line follows the parked aircraft's height through the entire ground roll.
        private float RunwayWheelHeight => groundAltitude +
            1.37f * Mathf.Sin(GroundPitch * Mathf.Deg2Rad) - Mathf.Cos(GroundPitch * Mathf.Deg2Rad);

        private float ReadVerticalInput()
        {
            float input = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input -= 1f;
            }
            if (Gamepad.current != null)
            {
                float stick = Gamepad.current.leftStick.ReadValue().y;
                if (Mathf.Abs(stick) > .15f) input = stick;
            }
            return input;
        }

        private void LateUpdate()
        {
            ApplyCameraPose();
        }

        private void ApplyCameraPose()
        {
            if (flightCamera == null) return;
            float progress = Phase == FlightPhase.Ready ? 0f : 1f;
            Vector3 openingPosition = transform.position + new Vector3(1f, 1.7f, -10f);
            Vector3 flightPosition = new Vector3(transform.position.x + 3.5f,
                5f + (Mathf.Max(transform.position.y, 1.65f) - 4f) * .45f, -10f);
            flightCamera.transform.position = Vector3.Lerp(openingPosition, flightPosition, progress);
            flightCamera.orthographicSize = Mathf.Lerp(flightCameraSize / openingZoomMultiplier, flightCameraSize, progress);
        }

        private void CreateEngineSound()
        {
            const int rate = 44100;
            var samples = new float[rate];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float pulse = .75f + .25f * Mathf.Sin(2f * Mathf.PI * 20f * t);
                float tone = .42f * Mathf.Sin(2f * Mathf.PI * 80f * t) +
                    .20f * Mathf.Sin(2f * Mathf.PI * 160f * t) +
                    .11f * Mathf.Sin(2f * Mathf.PI * 240f * t);
                float airflow = .06f * Mathf.Sin(2f * Mathf.PI * 421f * t) +
                    .04f * Mathf.Sin(2f * Mathf.PI * 631f * t);
                samples[i] = tone * pulse + airflow;
            }
            engineClip = AudioClip.Create("Motor do avião", rate, 1, rate, false);
            engineClip.SetData(samples, 0);
            engine.clip = engineClip;
        }

        private void OnDestroy()
        {
            if (engineClip != null) Destroy(engineClip);
        }

        public void Configure(Transform visual, Camera camera, SpriteRenderer blades, Sprite[] frames)
        {
            aircraftVisual = visual;
            flightCamera = camera;
            propeller = blades;
            propellerFrames = frames;
        }
    }
}
