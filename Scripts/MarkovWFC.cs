using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MarkovWFC : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSize = 10; // Size of the grid (dimension of the map)
    public int tilesCount; // Number of different tiles

    [Header("Prefabs and Tilemaps")]
    public GameObject textPrefab;
    public RectTransform canvasTransform;
    public Tilemap tilemap;
    public TileBase[] tiles;
    public GameObject[] prefabTiles;

    private TextMeshProUGUI[,] textElements;
    private Cell[,] grid;

    [Header("Transition Matrices")]
    public float[,] transitionMatrixTop;
    public float[,] transitionMatrixBottom;
    public float[,] transitionMatrixLeft;
    public float[,] transitionMatrixRight;

    private void Start()
    {
        Initialize();
    }

    private IEnumerator CollapseCell()
    {
        EvaluateCellEntropy();
        bool continueCollapsing = CollapseCellWithLowestEntropy();
        UpdateTilemap();

        // Wait for a short period before collapsing the next cell to prevent frame dropping
        yield return new WaitForSeconds(0.001f);

        if (continueCollapsing)
            StartCoroutine(CollapseCell());
        else
            PlacePrefabs();
    }

    private void EvaluateCellEntropy()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                grid[x, y].entropy = (int)GetCellEntropy(x, y, 1);
                if (grid[x, y].entropy == 0)
                    TriggerCellExplosion(x, y);
            }
        }

        UpdateTilemap();
    }

    public void UpdateTilemap()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                textElements[x, y].text = string.Empty;

                if (grid[x, y].tileIndex != -1)
                    tilemap.SetTile(new Vector3Int(-x, -y, 0), tiles[grid[x, y].tileIndex]);
            }
        }
    }

    private object GetCellEntropy(int x, int y, int type)
    {
        if (grid[x, y].tileIndex != -1 && type == 1) return 1;

        float[] probabilities = new float[tilesCount];
        for (int i = 0; i < probabilities.Length; i++) probabilities[i] = 1;

        // Top
        if (y < gridSize - 1 && grid[x, y + 1].tileIndex != -1)
            for (int i = 0; i < probabilities.Length; i++) probabilities[i] *= transitionMatrixTop[grid[x, y + 1].tileIndex, i];

        // Bottom
        if (y > 0 && grid[x, y - 1].tileIndex != -1)
            for (int i = 0; i < probabilities.Length; i++) probabilities[i] *= transitionMatrixBottom[grid[x, y - 1].tileIndex, i];

        // Left
        if (x > 0 && grid[x - 1, y].tileIndex != -1)
            for (int i = 0; i < probabilities.Length; i++) probabilities[i] *= transitionMatrixLeft[grid[x - 1, y].tileIndex, i];

        // Right
        if (x < gridSize - 1 && grid[x + 1, y].tileIndex != -1)
            for (int i = 0; i < probabilities.Length; i++) probabilities[i] *= transitionMatrixRight[grid[x + 1, y].tileIndex, i];

        switch (type)
        {
            // Return entropy as integer
            case 1:
                int count = 0;
                for (int i = 0; i < probabilities.Length; i++) if (probabilities[i] > 0) count++;
                return count;

            // Return probabilities as float array
            case 2:
                return probabilities;
        }

        return 0;
    }

    private bool CollapseCellWithLowestEntropy()
    {
        Vector2Int position = new Vector2Int(-1, -1);
        int minEntropy = int.MaxValue;

        // Find the cell with the lowest entropy
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y].entropy < minEntropy && (Random.Range(0, 2) == 0 || minEntropy == int.MaxValue) && grid[x, y].tileIndex == -1)
                {
                    minEntropy = grid[x, y].entropy;
                    position = new Vector2Int(x, y);
                }
            }
        }

        if (minEntropy == int.MaxValue) return false;

        // Get the normalized probabilities for the tile placement
        float[] probabilities = (float[])GetCellEntropy(position.x, position.y, 2);
        float[] normalizedProbabilities = NormalizeProbabilities(probabilities);

        // Select the tile based on the probabilities
        float randomValue = Random.Range(0f, 1f);
        float cumulativeProbability = 0;

        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulativeProbability += normalizedProbabilities[i];
            if (randomValue <= cumulativeProbability)
            {
                grid[position.x, position.y].tileIndex = i;
                break;
            }
        }

        return true;
    }

    private float[] NormalizeProbabilities(float[] probabilities)
    {
        float sum = 0;
        float[] normalizedProbabilities = new float[tilesCount];

        for (int i = 0; i < probabilities.Length; i++)
            sum += probabilities[i];

        for (int i = 0; i < probabilities.Length; i++)
            normalizedProbabilities[i] = probabilities[i] / sum;

        return normalizedProbabilities;
    }

    private void TriggerCellExplosion(int x, int y)
    {
        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                if (i >= 0 && i < gridSize && j >= 0 && j < gridSize)
                    grid[i, j].tileIndex = -1; // Reset the affected cells
            }
        }
    }

    private void PlacePrefabs()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (tilemap.GetTile(new Vector3Int(-x, -y, 0)) == tiles[4]) // Check for a specific tile
                {
                    if (y != gridSize - 1 && y != 0 && x != gridSize - 1 && x != 0) // Avoid corners
                    {
                        int randomIndex = Random.Range(0, prefabTiles.Length);
                        Instantiate(prefabTiles[randomIndex], new Vector3(-x + tilemap.transform.position.x, -y + tilemap.transform.position.y, gridSize - y), Quaternion.identity);
                    }
                }
            }
        }
    }

    private void Initialize()
    {
        // Initialize text elements for debugging/visualization
        textElements = new TextMeshProUGUI[gridSize, gridSize];

        // Initialize the grid and matrix
        grid = new Cell[gridSize, gridSize];

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                grid[x, y] = new Cell(-1, 0);

                GameObject textObj = Instantiate(textPrefab, canvasTransform);
                textElements[x, y] = textObj.GetComponent<TextMeshProUGUI>();
                textElements[x, y].text = grid[x, y].entropy.ToString();

                textObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(y * 100, -x * 100);
            }
        }

        // Initialize transition matrices for each direction
        transitionMatrixTop = new float[tilesCount, tilesCount];
        transitionMatrixBottom = new float[tilesCount, tilesCount];
        transitionMatrixLeft = new float[tilesCount, tilesCount];
        transitionMatrixRight = new float[tilesCount, tilesCount];

        GetComponent<PopulateMatrix>().PopulateTransitionMatrices();

        // Start collapsing process
        StartCoroutine(CollapseCell());
    }

    [System.Serializable]
    public class Cell
    {
        public int tileIndex; // Index of the tile selected for this cell
        public int entropy;   // Entropy (uncertainty) of the cell

        public Cell(int tileIndex, int entropy)
        {
            this.tileIndex = tileIndex;
            this.entropy = entropy;
        }
    }
}
