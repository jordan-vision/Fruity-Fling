using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class RefillBoardStrategy
{
    // Remove all repeated fruits from the match list
    public void IdentifyReplacements(List<Match> matches)
    {
        // Shuffle matches
        var shuffledMatches = matches.OrderBy(e => Random.Range(0.0f, 1.0f));
        var fruitsChecked = new List<(int, int)>();

        foreach (var match in shuffledMatches)
        {
            var markedForDeletion = new List<(int, int)>();
            foreach (var ij in match.Coordinates)
            {
                // If this fruit is part of another match, ignore it
                if (fruitsChecked.Contains(ij))
                {
                    markedForDeletion.Add(ij);
                } else
                {
                    fruitsChecked.Add(ij);
                }
            }

            foreach (var ij in markedForDeletion)
            {
                match.Coordinates.Remove(ij);
            }
        }

        // Find the lowest empty space in each column
        var firstEmptySpacePerColumn = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };

        for (int j = 0; j < 8; j++)
        {
            for (int i = 7; i >= 0; i--)
            {
                if (GameManager.Instance.LevelGrid.Board(i, j) == FruitType.None)
                {
                    firstEmptySpacePerColumn[j] = i;
                    break;
                }
            }
        }

        RefillBoard(shuffledMatches, firstEmptySpacePerColumn);
    }

    // Use weights to randomize fruit
    protected FruitType FruitRandomizer(float[] weights)
    {
        var rand = Random.Range(0, weights.Sum());
        var weightIndex = 0;
        var total = weights[0];

        while (total < rand)
        {
            weightIndex++;
            total += weights[weightIndex];
        }

        return (FruitType)(weightIndex + 1);
    }
    
    // Perform the board refilling algorithm unique to each level
    protected abstract void RefillBoard(IEnumerable<Match> matches, int[] firstEmptySpacePerColumn);
}

public class Level1Strategy : RefillBoardStrategy
{
    protected override void RefillBoard(IEnumerable<Match> matches, int[] firstEmptyRowPerColumn)
    {
        foreach (var match in matches)
        {
            var iterator = 0;

            // For vertical matches, the weights for the randomizer of the lowest fruit are different
            if (match.Direction == Match.MatchDirection.Vertical)
            {
                // Go from lowest to highest
                var newCoordinates = match.Coordinates.OrderByDescending(e => e.Item1);

                // Lowest fruit in match
                var j = newCoordinates.FirstOrDefault().Item2;
                var i = firstEmptyRowPerColumn[j];

                if (i == -1)
                {
                    break;
                }
                else if (i == 7)
                {
                    GameManager.Instance.StartCoroutine(GameManager.Instance.LevelGrid.MakeFruitFall(i, j, () => (FruitType)Random.Range(1, 6))); // 20% chance for each fruit
                } 
                else
                {
                    var bigWeightIndex = (int)GameManager.Instance.LevelGrid.Board(i + 1, j) - 1; // Fruit right below empty space;
                    var weights = WeightManager(bigWeightIndex, 40);

                    GameManager.Instance.StartCoroutine(GameManager.Instance.LevelGrid.MakeFruitFall(i, j, () =>
                    {
                        return FruitRandomizer(weights);
                    }));
                }

                iterator = 1;
                firstEmptyRowPerColumn[j]--;
            }

            // Horizontal matches and vertical matches after the first one
            for (; iterator < match.Coordinates.Count; iterator++)
            {
                var j = match.Coordinates[iterator].Item2;
                var i = firstEmptyRowPerColumn[j];

                if (i == -1)
                {
                    break;
                }
                if (i == 7)
                {
                    GameManager.Instance.StartCoroutine(GameManager.Instance.LevelGrid.MakeFruitFall(i, j, () => (FruitType)Random.Range(1, 6)));
                }
                else
                {
                    var bigWeightIndex = (int)GameManager.Instance.LevelGrid.Board(i + 1, j) - 1; // Fruit right below empty space;
                    var weights = WeightManager(bigWeightIndex, 60);

                    GameManager.Instance.StartCoroutine(GameManager.Instance.LevelGrid.MakeFruitFall(i, j, () =>
                    {
                        return FruitRandomizer(weights);
                    }));
                }

                firstEmptyRowPerColumn[j]--;
            }
        }
    }

    // Randomizer weighing
    protected float[] WeightManager(int bigWeightIndex, int bigWeightValue)
    {
        var otherWeights = (100.0f - bigWeightValue) / 4.0f;
        var weights = new float[] { otherWeights, otherWeights, otherWeights, otherWeights, otherWeights };
        weights[bigWeightIndex] = bigWeightValue;
        return weights;
    }
}

public class Level2Strategy : RefillBoardStrategy
{
    protected override void RefillBoard(IEnumerable<Match> matches, int[] firstEmptySpacePerColumn)
    {
        foreach (Match match in matches)
        {
            var newCoordinates = match.Coordinates.OrderBy(e => Random.Range(0.0f, 1.0f));

            foreach (var (_, j) in newCoordinates)
            {
                var i = firstEmptySpacePerColumn[j];
                var weights = new float[] { 1, 1, 1, 1, 1 };

                var neighbors = new List<(int, int)>();

                if (i >= 1) 
                {
                    // Up neighbor
                    neighbors.Add((i - 1, j));

                    // Up left neighbor
                    if (j >= 1)
                    {
                        neighbors.Add((i - 1, j - 1));
                    }

                    // Up right neigbor
                    if (j <= 6)
                    {
                        neighbors.Add((i - 1, j + 1));
                    }
                }

                if (i <= 6)
                {
                    // Down neighbor
                    neighbors.Add((i + 1, j));

                    // Up left neighbor
                    if (j >= 1)
                    {
                        neighbors.Add((i + 1, j - 1));
                    }

                    // Up right neigbor
                    if (j <= 6)
                    {
                        neighbors.Add((i + 1, j + 1));
                    }
                }

                if (j >= 1)
                {
                    // Left neighbor
                    neighbors.Add((i, j - 1));
                }

                if (j <= 6)
                {
                    // Right neighbor
                    neighbors.Add((i, j + 1));
                }

                foreach (var (x, y) in neighbors)
                {
                    if (GameManager.Instance.LevelGrid.Board(x, y) != FruitType.None)
                    {
                        weights[(int)GameManager.Instance.LevelGrid.Board(x, y) - 1]++;
                    }
                }

                GameManager.Instance.StartCoroutine(GameManager.Instance.LevelGrid.MakeFruitFall(i, j, () =>
                {
                    return FruitRandomizer(weights);
                }));

                firstEmptySpacePerColumn[j]--;
            }
        }
    }
}
