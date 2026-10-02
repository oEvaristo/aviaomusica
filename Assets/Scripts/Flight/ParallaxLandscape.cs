using System.Collections.Generic;
using UnityEngine;

namespace ProximoVoo
{
    [DefaultExecutionOrder(100)]
    public sealed class ParallaxLandscape : MonoBehaviour
    {
        [SerializeField] private Camera flightCamera;
        [Header("Arte do cenário")]
        [SerializeField] private Sprite skySprite;
        [SerializeField] private Sprite coastSprite;
        [SerializeField] private Sprite forestSprite;
        [SerializeField] private Sprite cloudSprite;
        [SerializeField] private Material sceneryMaterial;
        private Transform sky;
        private readonly List<Panorama> panoramas = new List<Panorama>();
        private readonly List<Cloud> clouds = new List<Cloud>();
        private readonly List<GameObject> generatedObjects = new List<GameObject>();
        private Material fallbackMaterial;

        private sealed class Panorama
        {
            public SpriteRenderer[] tiles;
            public float width, speed, height, verticalFollow;
        }

        private sealed class Cloud
        {
            public Transform transform;
            public float offset, height, speed;
        }

        public void Configure(Camera camera) => flightCamera = camera;

        public void Configure(Camera camera, Sprite skyArt, Sprite coastArt, Sprite forestArt,
            Sprite cloudArt, Material material)
        {
            flightCamera = camera;
            skySprite = skyArt;
            coastSprite = coastArt;
            forestSprite = forestArt;
            cloudSprite = cloudArt;
            sceneryMaterial = material;
        }

        private void Awake()
        {
            if (flightCamera == null) flightCamera = Camera.main;
            if (flightCamera == null || skySprite == null || coastSprite == null ||
                forestSprite == null || cloudSprite == null)
            {
                Debug.LogError("O cenário precisa da câmera e das quatro imagens de paisagem.", this);
                enabled = false;
                return;
            }
            if (sceneryMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                fallbackMaterial = new Material(shader);
                sceneryMaterial = fallbackMaterial;
            }

            sky = CreateSprite("Céu cinematográfico", skySprite, -100).transform;
            AddPanorama("Costa e montanhas", coastSprite, 32f, .12f, 3.5f, .72f, -60);
            AddPanorama("Floresta próxima", forestSprite, 32f, .40f, .6f, .58f, -20);
            for (int i = 0; i < 6; i++)
            {
                var renderer = CreateSprite("Nuvem " + (i+1), cloudSprite, -40);
                float width = i % 2 == 0 ? 4.6f : 6.5f;
                renderer.transform.localScale = Vector3.one * (width / cloudSprite.bounds.size.x);
                renderer.color = new Color(1f, 1f, 1f, i % 2 == 0 ? .80f : .65f);
                clouds.Add(new Cloud { transform = renderer.transform, offset = i * 11f + 6f,
                    height = i % 2 == 0 ? 9.2f : 10.8f, speed = .32f + (i%3) * .06f });
            }
            UpdateLandscape();
        }

        private SpriteRenderer CreateSprite(string name, Sprite sprite, int order)
        {
            var item = new GameObject(name, typeof(SpriteRenderer));
            item.transform.SetParent(transform, false);
            generatedObjects.Add(item);
            var renderer = item.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = sceneryMaterial;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void AddPanorama(string name, Sprite sprite, float width, float speed,
            float height, float verticalFollow, int order)
        {
            var layer = new Panorama { tiles = new SpriteRenderer[3], width = width,
                speed = speed, height = height, verticalFollow = verticalFollow };
            for (int i = 0; i < layer.tiles.Length; i++)
            {
                layer.tiles[i] = CreateSprite(name + " " + i, sprite, order);
                layer.tiles[i].transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
            }
            panoramas.Add(layer);
        }

        private void LateUpdate() => UpdateLandscape();

        private void UpdateLandscape()
        {
            if (flightCamera == null || sky == null) return;
            Vector3 camera = flightCamera.transform.position;
            float viewHeight = flightCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * flightCamera.aspect;
            float skyScale = Mathf.Max(viewWidth / skySprite.bounds.size.x,
                viewHeight / skySprite.bounds.size.y) * 1.04f;
            sky.localScale = Vector3.one * skyScale;
            sky.position = new Vector3(camera.x, camera.y, 0f);

            foreach (var layer in panoramas)
            {
                float travel = camera.x * layer.speed;
                int centerTile = Mathf.FloorToInt(travel / layer.width);
                for (int i = 0; i < layer.tiles.Length; i++)
                {
                    int tileIndex = centerTile + i - 1;
                    var tile = layer.tiles[i];
                    // Mirrored neighbours join the identical image edge.
                    tile.flipX = (tileIndex & 1) != 0;
                    tile.transform.position = new Vector3(camera.x + tileIndex * layer.width - travel,
                        layer.height + (camera.y - 5f) * layer.verticalFollow, 0f);
                }
            }
            const float cloudCycle = 66f;
            foreach (var cloud in clouds)
            {
                float x = Mathf.Repeat(cloud.offset - camera.x * cloud.speed + cloudCycle/2f,
                    cloudCycle) - cloudCycle/2f;
                cloud.transform.position = new Vector3(camera.x + x,
                    cloud.height + (camera.y-5f)*.82f, 0f);
            }
        }

        private void OnDestroy()
        {
            foreach (var item in generatedObjects) if (item != null) Destroy(item);
            if (fallbackMaterial != null) Destroy(fallbackMaterial);
        }
    }
}
