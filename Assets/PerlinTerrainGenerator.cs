using System.Collections.Generic;
using UnityEngine;

public class PerlinTerrainGenerator : MonoBehaviour
{
    public int textureWidth = 256;
    public int textureHeight = 256;

    public float scale = 20f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float offsetX = 100f;
    public float offsetY = 100f;

    public Gradient terrainGradient; // Use Unity's Gradient for multiple colors

    public Texture2D terrainTexture;
    public Texture2D textureCopy;
    public SpriteRenderer spriteRenderer;


    private float[,] pixelHardness;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GenerateTexture();
        ApplyTexture();
        InitializePixelHardness();
    }

    void GenerateTexture()
    {
        terrainTexture = new Texture2D(textureWidth, textureHeight);
        terrainTexture.filterMode = FilterMode.Point;

        // Generate random offsets for variation
        offsetX = Random.Range(0f, 99999f);
        offsetY = Random.Range(0f, 99999f);

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float sample = FractalNoise(x, y);

                // Map the noise value to a color using the gradient
                Color color = terrainGradient.Evaluate(sample);

                terrainTexture.SetPixel(x, y, color);
            }
        }

        terrainTexture.Apply();

        // Create a copy for modification during digging
        textureCopy = Instantiate(terrainTexture);
        textureCopy.Apply();
    }

    void InitializePixelHardness()
    {
        int width = textureCopy.width;
        int height = textureCopy.height;
        pixelHardness = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = textureCopy.GetPixel(x, y);
                // Calculate greyscale value (0 = black, 1 = white)
                float greyscale = pixelColor.grayscale;
                // Hardness is inversely proportional to greyscale (darker pixels are harder)
                float hardness = 1f - greyscale;
                // Store hardness value
                pixelHardness[x, y] = hardness;
            }
        }
    }

    public Bounds GetTerrainBounds()
    {
        return spriteRenderer.bounds;
    }

    float FractalNoise(int x, int y)
    {
        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;
        float maxAmplitude = 0;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = offsetX + (float)x / textureWidth * scale * frequency;
            float yCoord = offsetY + (float)y / textureHeight * scale * frequency;

            float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);
            noiseHeight += perlinValue * amplitude;

            maxAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize the noise value between 0 and 1
        noiseHeight /= maxAmplitude;
        return noiseHeight;
    }

    void ApplyTexture()
    {
        Sprite sprite = Sprite.Create(textureCopy, new Rect(0, 0, textureCopy.width, textureCopy.height), new Vector2(0.5f, 0.5f), 100);
        spriteRenderer.sprite = sprite;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            DigAtPosition(worldPos);
        }
    }

    void DigAtPosition(Vector2 worldPos)
    {
        Vector2 localPos = transform.InverseTransformPoint(worldPos);

        float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
        Vector2 pixelPos = new Vector2(
            localPos.x * pixelsPerUnit + textureCopy.width / 2,
            localPos.y * pixelsPerUnit + textureCopy.height / 2);

        int x = Mathf.RoundToInt(pixelPos.x);
        int y = Mathf.RoundToInt(pixelPos.y);

        if (x >= 0 && x < textureCopy.width && y >= 0 && y < textureCopy.height)
        {
            // Modify a small area (e.g., 5x5 pixels) for better digging effect
            int radius = 2;
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    int xi = x + i;
                    int yj = y + j;

                    if (xi >= 0 && xi < textureCopy.width && yj >= 0 && yj < textureCopy.height)
                    {
                        textureCopy.SetPixel(xi, yj, new Color(0, 0, 0, 0));
                    }
                }
            }
            textureCopy.Apply();
        }
    }

    public List<(Vector2 position, float hardness)> DestroyGroundAtPosition(Vector2 worldPos, float radius)
    {
        Vector2 localPos = transform.InverseTransformPoint(worldPos);

        float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
        Vector2 pixelPos = new Vector2(
            localPos.x * pixelsPerUnit + textureCopy.width / 2,
            localPos.y * pixelsPerUnit + textureCopy.height / 2);

        int xCenter = Mathf.RoundToInt(pixelPos.x);
        int yCenter = Mathf.RoundToInt(pixelPos.y);

        int pixelRadius = Mathf.RoundToInt(radius * pixelsPerUnit);

        List<(Vector2 position, float hardness)> destroyedPixelData = new List<(Vector2, float)>();

        // Randomness parameters
        float destructionProbability = 0.8f; // Adjust as needed
        float noiseScale = 0.1f; // For Perlin noise (optional)

        // First pass: Determine which pixels to destroy and collect their positions and hardness
        bool[,] pixelsToDestroy = new bool[2 * pixelRadius + 1, 2 * pixelRadius + 1];

        for (int y = -pixelRadius; y <= pixelRadius; y++)
        {
            for (int x = -pixelRadius; x <= pixelRadius; x++)
            {
                int xi = xCenter + x;
                int yj = yCenter + y;

                if (xi >= 0 && xi < textureCopy.width && yj >= 0 && yj < textureCopy.height)
                {
                    if (x * x + y * y <= pixelRadius * pixelRadius)
                    {
                        Color pixelColor = textureCopy.GetPixel(xi, yj);
                        if (pixelColor.a > 0f)
                        {
                            // Get the hardness of the pixel
                            float hardness = pixelHardness[xi, yj];

                            // Adjust destruction probability based on hardness
                            float adjustedDestructionProbability = destructionProbability - hardness;
                            adjustedDestructionProbability = Mathf.Clamp01(adjustedDestructionProbability);

                            // Introduce randomness
                            float randomValue = Random.Range(0f, 1f);
                            if (randomValue < adjustedDestructionProbability)
                            {
                                // Mark pixel for destruction
                                pixelsToDestroy[x + pixelRadius, y + pixelRadius] = true;

                                // Convert pixel position back to world position
                                Vector2 pixelWorldPos = WorldPositionFromPixelCoordinates(xi, yj);
                                destroyedPixelData.Add((pixelWorldPos, hardness));
                            }
                        }
                    }
                }
            }
        }

        // Second pass: Destroy marked pixels
        if (destroyedPixelData.Count > 0)
        {
            for (int y = -pixelRadius; y <= pixelRadius; y++)
            {
                for (int x = -pixelRadius; x <= pixelRadius; x++)
                {
                    if (pixelsToDestroy[x + pixelRadius, y + pixelRadius])
                    {
                        int xi = xCenter + x;
                        int yj = yCenter + y;

                        textureCopy.SetPixel(xi, yj, new Color(0, 0, 0, 0));

                        // Optionally, reset hardness to zero since the pixel is destroyed
                        pixelHardness[xi, yj] = 0f;
                    }
                }
            }
            textureCopy.Apply();
        }

        return destroyedPixelData;
    }

    private Vector2 WorldPositionFromPixelCoordinates(int x, int y)
    {
        float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
        float worldX = (x - textureCopy.width / 2) / pixelsPerUnit;
        float worldY = (y - textureCopy.height / 2) / pixelsPerUnit;
        Vector2 localPos = new Vector2(worldX, worldY);
        Vector2 worldPos = transform.TransformPoint(localPos);
        return worldPos;
    }
}