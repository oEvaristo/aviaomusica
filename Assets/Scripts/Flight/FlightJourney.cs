using System.Collections.Generic;
using UnityEngine;

namespace ProximoVoo
{
    [DefaultExecutionOrder(20)]
    public sealed class FlightJourney : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private Camera flightCamera;
        [SerializeField] private Sprite noteSprite;
        [SerializeField] private Material noteMaterial;
        [SerializeField] private GameObject scorePanel;
        [SerializeField] private TMPro.TMP_Text scoreText;
        [SerializeField] private CanvasGroup messagePanel;
        [SerializeField] private TMPro.TMP_Text messageTitle;
        [SerializeField] private TMPro.TMP_Text messageText;
        private const int TotalNotes = 59;
        [SerializeField] private float messageDuration = 8f;
        [Header("Narração dos blocos")]
        [SerializeField] private AudioSource narrationAudio;
        [SerializeField] private AudioClip[] blockNarrations = new AudioClip[4];
        [SerializeField] private AudioClip finalNarration;
        [SerializeField] private FlightMusic flightMusic;
        [SerializeField, Range(0f, 1f)] private float narrationVolume = 1f;

        private static readonly string[] Titles =
        {
            "Nos hangares", "Compartilhando conhecimento",
            "Uma nova conquista", "Um legado que inspira"
        };
        private static readonly string[] Messages =
        {
            "Lito Sousa dedicou mais de 35 anos à aviação.\nComo mecânico, passou por Varig, Transbrasil e United.",
            "Em 2004, criou o blog Aviões e Músicas.\nEm 2010, levou ao YouTube sua paixão por ensinar.",
            "Em 2021, Lito também se tornou piloto.\nNo mesmo ano, recebeu o Destaque SIPAER do Cenipa.",
            "Com explicações simples, ajudou pessoas a perder o medo de voar.\nSeu legado de conhecimento continua inspirando novos sonhos."
        };

        private sealed class Note
        {
            public SpriteRenderer renderer;
            public Vector3 position;
            public float scale, seed, burstAge;
            public bool collected;
        }

        private readonly List<Note> notes = new List<Note>();
        private AudioSource collectionAudio;
        private AudioClip collectionClip;
        private FlightPhase lastPhase;
        private Vector2 previousPlanePosition;
        private float lastNoteHeight;
        private int lastMessage = -1;
        private bool waitingForFinalNarration;

        public int NotesCollected { get; private set; }
        public int NotesSpawned { get; private set; }
        public int TotalNoteCount => TotalNotes;
        public int ActiveNotes => notes.FindAll(note => !note.collected).Count;
        public int CurrentMessageBlock => lastMessage + 1;

        public void Configure(FlightController controller, Camera camera, Sprite sprite, Material material,
            GameObject counterPanel, TMPro.TMP_Text counterText, CanvasGroup tributePanel,
            TMPro.TMP_Text title, TMPro.TMP_Text body)
        {
            flight = controller;
            flightCamera = camera;
            noteSprite = sprite;
            noteMaterial = material;
            scorePanel = counterPanel;
            scoreText = counterText;
            messagePanel = tributePanel;
            messageTitle = title;
            messageText = body;
        }

        private void Awake()
        {
            if (flight == null || flightCamera == null || noteSprite == null || noteMaterial == null ||
                scorePanel == null || scoreText == null || messagePanel == null || messageTitle == null || messageText == null)
            {
                Debug.LogError("Configure as referências das notas musicais e da homenagem.", this);
                enabled = false;
                return;
            }
            collectionAudio = gameObject.AddComponent<AudioSource>();
            collectionAudio.playOnAwake = false;
            collectionAudio.spatialBlend = 0f;
            collectionAudio.volume = .35f;
            if (narrationAudio == null)
            {
                var narration = new GameObject("Narração dos blocos", typeof(AudioSource));
                narration.transform.SetParent(transform, false);
                narrationAudio = narration.GetComponent<AudioSource>();
            }
            narrationAudio.playOnAwake = false;
            narrationAudio.loop = false;
            narrationAudio.spatialBlend = 0f;
            narrationAudio.volume = narrationVolume;
            CreateCollectionSound();
            ResetJourney();
            lastPhase = flight.Phase;
        }

        private void ResetJourney()
        {
            ClearNotes();
            collectionAudio.Stop();
            narrationAudio.Stop();
            narrationAudio.clip = null;
            waitingForFinalNarration = false;
            NotesCollected = NotesSpawned = 0;
            scoreText.text = "Notas: 0";
            scorePanel.SetActive(false);
            messagePanel.gameObject.SetActive(false);
            messagePanel.alpha = 0f;
            lastMessage = -1;
            lastNoteHeight = 4.5f;
            previousPlanePosition = flight.transform.position;
        }

        private void Update()
        {
            if (flight.Phase != lastPhase)
            {
                if (flight.Phase == FlightPhase.Ready || flight.Phase == FlightPhase.StartingEngine) ResetJourney();
                if (flight.Phase == FlightPhase.Flying)
                {
                    scorePanel.SetActive(true);
                    previousPlanePosition = flight.transform.position;
                }
                if (flight.Phase == FlightPhase.Landing) ClearNotes();
                if (flight.Phase == FlightPhase.Farewell)
                {
                    // Touchdown has stopped the engine. Give the closing narration priority.
                    narrationAudio.Stop();
                    narrationAudio.clip = finalNarration;
                    waitingForFinalNarration = finalNarration != null;
                    if (waitingForFinalNarration) narrationAudio.Play();
                }
                lastPhase = flight.Phase;
            }
            if (flight.IsPaused)
            {
                collectionAudio.Pause();
                narrationAudio.Pause();
                return;
            }
            collectionAudio.UnPause();
            narrationAudio.UnPause();
            narrationAudio.volume = narrationVolume;
            if (waitingForFinalNarration && !narrationAudio.isPlaying)
            {
                waitingForFinalNarration = false;
                if (flightMusic != null) flightMusic.SetVolumePercent(100f);
            }
            if (flight.Phase == FlightPhase.Farewell || flight.Phase == FlightPhase.Completed)
            {
                scorePanel.SetActive(false);
                messagePanel.gameObject.SetActive(false);
                return;
            }
            if (flight.Phase != FlightPhase.Flying && flight.Phase != FlightPhase.Landing) return;
            float elapsed = flight.FlightElapsed;
            UpdateMessage(elapsed);
            if (flight.Phase == FlightPhase.Landing) return;

            float approachTime = 15f / flight.ForwardSpeed;
            float firstSpawn = .8f;
            float lastSpawn = Mathf.Max(firstSpawn, flight.FlightDuration - approachTime - 1f);
            float spawnGap = (lastSpawn - firstSpawn) / (TotalNotes - 1);
            // Fixed total and schedule; only the altitude is randomized. Catch up missed frames.
            while (NotesSpawned < TotalNotes && elapsed >= firstSpawn + NotesSpawned * spawnGap)
            {
                float scheduledTime = firstSpawn + NotesSpawned * spawnGap;
                SpawnNote(Mathf.Min(4f, spawnGap * flight.ClimbSpeed * .85f), elapsed - scheduledTime);
            }
            UpdateNotes(elapsed);
            previousPlanePosition = flight.transform.position;
        }

        private void SpawnNote(float heightChange, float delay)
        {
            // Limit successive altitude changes so every note can be reached with the existing controls.
            float minimum = Mathf.Max(2.9f, lastNoteHeight - heightChange);
            float maximum = Mathf.Min(9.5f, lastNoteHeight + heightChange);
            lastNoteHeight = Random.Range(minimum, maximum);
            var item = new GameObject("Nota musical dourada", typeof(SpriteRenderer));
            item.transform.SetParent(transform, false);
            item.transform.position = new Vector3(flight.transform.position.x + 15f - delay * flight.ForwardSpeed, lastNoteHeight, 0f);
            float scale = .95f / noteSprite.bounds.size.y;
            item.transform.localScale = Vector3.one * scale;
            var renderer = item.GetComponent<SpriteRenderer>();
            renderer.sprite = noteSprite;
            renderer.sharedMaterial = noteMaterial;
            renderer.sortingOrder = 20;
            notes.Add(new Note { renderer = renderer, position = item.transform.position,
                scale = scale, seed = Random.Range(0f, 6.28f) });
            NotesSpawned++;
        }

        private void UpdateNotes(float elapsed)
        {
            Vector2 currentPosition = flight.transform.position;
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                Note note = notes[i];
                if (note.collected)
                {
                    note.burstAge += Time.deltaTime;
                    float progress = note.burstAge / .3f;
                    note.renderer.transform.localScale = Vector3.one * note.scale * (1f + progress * .8f);
                    note.renderer.color = new Color(1f, 1f, 1f, 1f - progress);
                    if (progress >= 1f) RemoveNote(i);
                    continue;
                }
                note.renderer.transform.position = note.position + Vector3.up * Mathf.Sin(elapsed * 2f + note.seed) * .08f;
                note.renderer.transform.localScale = Vector3.one * note.scale * (1f + .04f * Mathf.Sin(elapsed * 3f + note.seed));
                if (TouchesAircraft(note.renderer.transform.position, previousPlanePosition, currentPosition))
                {
                    note.collected = true;
                    NotesCollected++;
                    scoreText.text = "Notas: " + NotesCollected;
                    // A soft major chord, slightly varied so repeated pickups stay pleasant.
                    collectionAudio.pitch = Random.Range(.98f, 1.04f);
                    collectionAudio.PlayOneShot(collectionClip);
                }
                else if (note.position.x < flightCamera.transform.position.x - flightCamera.orthographicSize * flightCamera.aspect - 2f)
                    RemoveNote(i);
            }
        }

        private static bool TouchesAircraft(Vector2 note, Vector2 previous, Vector2 current)
        {
            // Swept ellipse around the airplane prevents missed pickups on slower frames.
            Vector2 radii = new Vector2(2.4f, 1.05f);
            Vector2 start = new Vector2((previous.x - note.x) / radii.x, (previous.y - note.y) / radii.y);
            Vector2 end = new Vector2((current.x - note.x) / radii.x, (current.y - note.y) / radii.y);
            Vector2 segment = end - start;
            float along = segment.sqrMagnitude > .000001f ? Mathf.Clamp01(-Vector2.Dot(start, segment) / segment.sqrMagnitude) : 0f;
            return (start + segment * along).sqrMagnitude <= 1f;
        }

        private void UpdateMessage(float elapsed)
        {
            float blockLength = flight.FlightDuration / 4f;
            int completedBlocks = Mathf.Clamp(Mathf.FloorToInt(elapsed / blockLength), 0, 4);
            float lastAudioDuration = blockNarrations.Length > 3 && blockNarrations[3] != null ? blockNarrations[3].length : messageDuration;
            // With the current clip this is 70s, leaving silence before landing and AudioFinal.
            float finalBlockStart = flight.FlightDuration - Mathf.Max(10f, lastAudioDuration + 1f);
            if (elapsed >= finalBlockStart) completedBlocks = 4;
            if (completedBlocks == 0)
            {
                messagePanel.gameObject.SetActive(false);
                return;
            }
            int index = completedBlocks - 1;
            if (lastMessage != index)
            {
                lastMessage = index;
                messageTitle.text = Titles[index];
                messageText.text = Messages[index];
                // Start once on the same frame as the corresponding tribute message.
                narrationAudio.Stop();
                narrationAudio.clip = index < blockNarrations.Length ? blockNarrations[index] : null;
                if (narrationAudio.clip != null) narrationAudio.Play();
            }
            float age = elapsed - (index == 3 ? finalBlockStart : completedBlocks * blockLength);
            bool final = index == 3;
            bool visible = final || age < messageDuration;
            messagePanel.gameObject.SetActive(visible);
            messagePanel.alpha = final ? Mathf.Clamp01(age / .5f) : Mathf.Clamp01(Mathf.Min(age / .5f, (messageDuration - age) / .5f));
        }

        private void CreateCollectionSound()
        {
            const int rate = 44100;
            const float duration = .55f;
            float[] samples = new float[Mathf.CeilToInt(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Clamp01(t / .012f) * Mathf.Exp(-t * 8f) * Mathf.Clamp01((duration - t) / .07f);
                float tone = Mathf.Sin(2f * Mathf.PI * 659.255f * t) * .45f +
                    Mathf.Sin(2f * Mathf.PI * 830.609f * t) * .22f +
                    Mathf.Sin(2f * Mathf.PI * 987.767f * t) * .18f;
                samples[i] = tone * envelope;
            }
            collectionClip = AudioClip.Create("Coleta — acorde suave", samples.Length, 1, rate, false);
            collectionClip.SetData(samples, 0);
        }

        private void RemoveNote(int index)
        {
            Destroy(notes[index].renderer.gameObject);
            notes.RemoveAt(index);
        }

        private void ClearNotes()
        {
            foreach (var note in notes) if (note.renderer != null) Destroy(note.renderer.gameObject);
            notes.Clear();
        }

        private void OnDestroy()
        {
            ClearNotes();
            if (collectionClip != null) Destroy(collectionClip);
        }
    }
}
