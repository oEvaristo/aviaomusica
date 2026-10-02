using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProximoVoo
{
    [DefaultExecutionOrder(110)]
    public sealed class AirportOpening : MonoBehaviour
    {
        [SerializeField] private FlightController flight;
        [SerializeField] private Camera flightCamera;
        [SerializeField] private Sprite airportSprite;
        [SerializeField] private Sprite arrivalAirportSprite;
        [SerializeField] private Material sceneryMaterial;
        private readonly List<SpriteRenderer> airportTiles = new List<SpriteRenderer>();
        private readonly List<MeshRenderer> runwayRenderers = new List<MeshRenderer>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<GameObject> generatedObjects = new List<GameObject>();
        private Material runwayMaterial;
        private Transform runwayRoot;
        private readonly List<Light2D[]> runwayLights = new List<Light2D[]>();
        // Lamp positions measured in the original 1942 x 809 airport artwork.
        private static readonly Vector2[] LampPixels = {
            new Vector2(96f, 79f), new Vector2(528f, 79f), new Vector2(884f, 79f),
            new Vector2(1313f, 79f), new Vector2(1705f, 79f)
        };
        private bool arrivalInitialized;
        private float arrivalOrigin;
        private const float AirportWidth = 32f;
        public void ConfigureArrival(Sprite sprite) => arrivalAirportSprite = sprite;

        public void Configure(FlightController controller, Camera camera, Sprite airport, Material material)
        {
            flight = controller;
            flightCamera = camera;
            airportSprite = airport;
            sceneryMaterial = material;
        }

        private void Awake()
        {
            if (flight == null || flightCamera == null || airportSprite == null || sceneryMaterial == null)
            {
                Debug.LogError("A abertura precisa do avião, câmera, aeroporto e material.", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                var item = new GameObject("Aeroporto " + i, typeof(SpriteRenderer));
                item.transform.SetParent(transform, false);
                generatedObjects.Add(item);
                var renderer = item.GetComponent<SpriteRenderer>();
                renderer.sprite = airportSprite;
                renderer.sharedMaterial = sceneryMaterial;
                renderer.sortingOrder = -10;
                item.transform.localScale = Vector3.one * (AirportWidth / airportSprite.bounds.size.x);
                airportTiles.Add(renderer);
                var lights = new Light2D[LampPixels.Length];
                for (int lamp = 0; lamp < lights.Length; lamp++)
                {
                    var spot = new GameObject("Spot 2D da lâmpada " + (lamp + 1), typeof(Light2D));
                    spot.transform.SetParent(item.transform, false);
                    var light = spot.GetComponent<Light2D>();
                    light.lightType = Light2D.LightType.Point;
                    light.color = new Color(.35f, .65f, 1f);
                    light.intensity = 1.1f;
                    light.pointLightInnerRadius = .12f;
                    light.pointLightOuterRadius = 2.8f;
                    light.pointLightInnerAngle = 100f;
                    light.pointLightOuterAngle = 170f;
                    light.falloffIntensity = .65f;
                    lights[lamp] = light;
                }
                runwayLights.Add(lights);
            }
            runwayMaterial = new Material(sceneryMaterial);
            runwayMaterial.mainTexture = Texture2D.whiteTexture;
            var root = new GameObject("Pista móvel");
            root.transform.SetParent(transform, false);
            generatedObjects.Add(root);
            runwayRoot = root.transform;
            Quad("Solo", -40f, 100f, -10f, -.6f, new Color(.15f,.26f,.24f), new Color(.26f,.35f,.26f), -3);
            Quad("Pista", -40f, 100f, -.6f, .875f, new Color(.12f,.17f,.23f), new Color(.30f,.34f,.40f), -2);
            Quad("Borda da pista", -40f, 100f, .79f, .84f, new Color(.92f,.86f,.70f), new Color(.92f,.86f,.70f), -1);
            for (int i = -10; i < 26; i++)
            {
                Quad("Faixa " + i, i * 4f, i * 4f + 1.7f, .13f, .22f,
                    new Color(.84f,.82f,.73f), new Color(.84f,.82f,.73f), -1);
            }
        }

        private void Quad(string name, float left, float right, float bottom, float top, Color low, Color high, int order)
        {
            var mesh = new Mesh { name = name,
                vertices = new[] { new Vector3(left,bottom), new Vector3(left,top), new Vector3(right,top), new Vector3(right,bottom) },
                triangles = new[] { 0,1,2,0,2,3 },
                colors = new[] { low,high,high,low }, uv = new Vector2[4] };
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(runwayRoot, false);
            generatedObjects.Add(item);
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = runwayMaterial;
            renderer.sortingOrder = order;
            runwayRenderers.Add(renderer);
        }

        private void LateUpdate()
        {
            if (flight == null || flightCamera == null || airportTiles.Count == 0) return;
            bool arriving = flight.Phase == FlightPhase.Landing || flight.Phase == FlightPhase.Farewell || flight.Phase == FlightPhase.Completed;
            if (arriving && !arrivalInitialized)
            {
                arrivalOrigin = flight.transform.position.x;
                runwayRoot.position = new Vector3(arrivalOrigin, 0f, 0f);
                arrivalInitialized = true;
            }
            if (flight.Phase == FlightPhase.Ready || flight.Phase == FlightPhase.StartingEngine)
            {
                arrivalInitialized = false;
                runwayRoot.position = Vector3.zero;
            }
            Sprite artwork = arriving && arrivalAirportSprite != null ? arrivalAirportSprite : airportSprite;
            float travel = (flightCamera.transform.position.x - (arriving ? arrivalOrigin : 0f)) * .35f;
            int center = Mathf.FloorToInt(travel / AirportWidth);
            for (int i = 0; i < airportTiles.Count; i++)
            {
                int tile = center + i - 1;
                airportTiles[i].sprite = artwork;
                airportTiles[i].transform.localScale = Vector3.one * (AirportWidth / artwork.bounds.size.x);
                airportTiles[i].flipX = !arriving && (tile & 1) != 0;
                airportTiles[i].transform.position = new Vector3(flightCamera.transform.position.x +
                    tile * AirportWidth - travel, 4.2f, 0f);
                for (int lamp = 0; lamp < LampPixels.Length; lamp++)
                {
                    Vector2 pixel = LampPixels[lamp];
                    float x = (pixel.x / 1942f - .5f) * artwork.bounds.size.x;
                    float y = (pixel.y / 809f - .5f) * artwork.bounds.size.y;
                    if (airportTiles[i].flipX) x = -x;
                    var light = runwayLights[i][lamp];
                    light.transform.localPosition = new Vector3(x, y, 0f);
                    // Radius is in world units; compensate the artwork's scale.
                    light.transform.localScale = Vector3.one / airportTiles[i].transform.localScale.x;
                    light.enabled = arriving && flight.AirportVisibility > .001f && flight.Phase != FlightPhase.Completed;
                    light.intensity = 1.1f * flight.AirportVisibility;
                }
            }
            float visibility = flight.AirportVisibility;
            foreach (var tile in airportTiles)
            {
                tile.enabled = visibility > .001f;
                tile.color = new Color(1f,1f,1f,visibility);
            }
            // Fade all runway geometry together as the aircraft leaves the airport.
            runwayMaterial.SetColor("_Color", new Color(1f,1f,1f,visibility));
            // The arrival artwork includes the apron and its lamps. Keep it unobstructed.
            foreach (var renderer in runwayRenderers) renderer.enabled = visibility > .001f && !arriving;
        }

        private void OnDestroy()
        {
            foreach (var item in generatedObjects) if (item != null) Destroy(item);
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
            if (runwayMaterial != null) Destroy(runwayMaterial);
        }
    }
}
