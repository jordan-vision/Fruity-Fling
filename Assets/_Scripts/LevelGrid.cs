using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class LevelGrid : MonoBehaviour
{
    FruitType[,] board = new FruitType[8, 8];
    GameObject[,] fruits = new GameObject[8, 8];
    int numberOfFruitsMoving = 0, scoreMultiplier = 1;
    (int, int) firstSelection = (-1, -1);
    RefillBoardStrategy refillBoardStrategy;
    bool gameWon, resolvingMove, settingUpFall;

    [SerializeField] GameObject applePrefab, pineapplePrefab, peachPrefab, watermelonPrefab, grapePrefab;

    public bool ResolvingMove { get { return resolvingMove; } }
    public bool GameWon { get { return gameWon; } set { gameWon = value; } }

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            refillBoardStrategy = new Level1Strategy();
        } else
        {
            refillBoardStrategy = new Level2Strategy();
        }

        // Fill out board
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                InstantiateFruit(i, j, () => (FruitType)Random.Range(1, 6));
            }
        }

        // Check if there are matches on the board
        List<Match> matches;
        do
        {
            matches = CheckForExistingMatches();
            foreach (var match in matches)
            {
                // Check if this is still a match
                if (!IsMatchValid(match))
                {
                    continue;
                }

                // Replace a random fruit in the match
                var randIndex = Random.Range(0, match.Coordinates.Count);
                var (i, j) = match.Coordinates[randIndex];

                // Identify possibilities for replacement
                var possibleSwitches = new List<FruitType>() { FruitType.Apple, FruitType.Pineapple, FruitType.Peach, FruitType.Watermelon, FruitType.Grape };
                possibleSwitches.Remove(match.Type);

                // Down neighbor
                if (i >= 1)
                {
                    possibleSwitches.Remove(board[i - 1, j]);
                }

                // Up neighbor
                if (i <= 6)
                {
                    possibleSwitches.Remove(board[i + 1, j]);
                }

                // Left neighbor
                if (j >= 1)
                {
                    possibleSwitches.Remove(board[i, j - 1]);
                }

                // Right neighbor
                if (j <= 6)
                {
                    possibleSwitches.Remove(board[i, j + 1]);
                }

                Destroy(fruits[i, j]);
                InstantiateFruit(i, j, () =>
                {
                    var randIndex = Random.Range(0, possibleSwitches.Count);
                    return possibleSwitches[randIndex];
                });
            }
        } while (matches.Count != 0);
    }

    // Returns fruit at position (i, j)
    public FruitType Board(int i, int j)
    {
        return board[i, j];
    }

    private List<Match> CheckForExistingMatches()
    {
        var matches = new List<Match>();

        // Find horizontal matches
        matches.AddRange(TraverseBoard((i, j) =>
            {
                return (i + 1, 0);
            }, 
            (i, j) =>
            {
                return(i, j + 1);
            },
            Match.MatchDirection.Horizontal));

        // Find vertical matches
        matches.AddRange(TraverseBoard((i, j) =>
        {
            return (0, j + 1);
        },
        (i, j) =>
        {
            return (i + 1, j);
        },
        Match.MatchDirection.Vertical
        ));

        return matches;
    }

    private bool IsMatchValid(Match match)
    {
        foreach (var ij in match.Coordinates)
        {
            var (i, j) = ij;
            if (board[i, j] != match.Type)
            {
                return false;
            }
        }

        return true;
    }

    private void DestroyFruit(int i, int j)
    {
        board[i, j] = FruitType.None;

        if (fruits[i, j] != null)
        {
            Destroy(fruits[i, j]);
            fruits[i, j] = null;
        }
    }

    // Set fruit value and instantiate object
    public void InstantiateFruit(int i, int j, Func<FruitType> randomizer)
    {
        var position = transform.TransformPoint(new Vector2(-4.55f + 1.3f * j, 4.2f - 1.2f * i));
        var fruitId = randomizer();
        board[i, j] = fruitId;

        switch (fruitId)
        {
            case FruitType.Apple:
                fruits[i, j] = Instantiate(applePrefab, position, Quaternion.identity, transform);
                break;

            case FruitType.Pineapple:
                fruits[i, j] = Instantiate(pineapplePrefab, position, Quaternion.identity, transform);
                break;

            case FruitType.Peach:
                fruits[i, j] = Instantiate(peachPrefab, position, Quaternion.identity, transform);
                break;

            case FruitType.Watermelon:
                fruits[i, j] = Instantiate(watermelonPrefab, position, Quaternion.identity, transform);
                break;

            case FruitType.Grape:
                fruits[i, j] = Instantiate(grapePrefab, position, Quaternion.identity, transform);
                break;

            default:
                break;
        }
    }

    // Destroy all matching fruits and fill in the gaos
    private IEnumerator MatchAndCascade(List<Match> matches)
    {
        resolvingMove = true;

        do
        {
            foreach (var match in matches)
            {
                foreach (var (i, j) in match.Coordinates)
                {
                    DestroyFruit(i, j);
                }

                GameManager.Instance.LevelUI.UpdateObjectives(match.Type);
                GameManager.Instance.LevelUI.GetPoints(match.Coordinates.Count * scoreMultiplier);

                if (!gameWon)
                {
                    scoreMultiplier++;
                }

                else
                {
                    scoreMultiplier += 2;
                }
            }

            // Make everything cascade
            for (int j = 0; j < 8; j++)
            {
                // Find empty spaces from bottom to top
                var lowestEmpty = -1;

                for (int i = 7; i >= 0; i--)
                {
                    var type = board[i, j];
                    if (type == FruitType.None && lowestEmpty == -1)
                    {
                        lowestEmpty = i;
                    }
                    else if (type != FruitType.None && lowestEmpty != -1)
                    {
                        StartCoroutine(MakeFruitFall((i, j), (lowestEmpty, j)));
                        lowestEmpty--;
                    }
                }
            }

            while (numberOfFruitsMoving > 0)
            {
                yield return null;
            }

            refillBoardStrategy.IdentifyReplacements(matches);
            //yield return new WaitForSeconds(0.125f);

            while (numberOfFruitsMoving > 0)
            {
                yield return null;
            }

            matches = CheckForExistingMatches();

        } while (matches.Count > 0);
        
        resolvingMove = false;
        scoreMultiplier = 1;
    }

    // Make fruit fall from position to another position
    private IEnumerator MakeFruitFall((int, int) from, (int, int) to)
    {
        numberOfFruitsMoving++;

        while (settingUpFall)
        {
            yield return null;
        }
        settingUpFall = true;

        var (i1, j1) = from;
        var (i2, j2) = to;

        board[i2, j2] = board[i1, j1];
        board[i1, j1] = FruitType.None;

        var fruit = fruits[i1, j1];
        fruits[i2, j2] = fruit;
        fruits[i1, j1] = null;

        settingUpFall = false;

        var position = transform.TransformPoint(new Vector2(-4.55f + 1.3f * j2, 4.2f - 1.2f * i2));

        var speed = 0.0f;
        while (fruit.transform.position != position)
        {
            fruit.transform.position = Vector3.MoveTowards(fruit.transform.position, position, 0.5f);
            yield return new WaitForSeconds(1.0f / 16.0f);
            speed += 0.1f;
        }

        numberOfFruitsMoving--;
    }

    // Make fruit fall from top of screen to position
    public IEnumerator MakeFruitFall(int i, int j, Func<FruitType> randomizer)
    {
        numberOfFruitsMoving++;

        while (settingUpFall)
        {
            yield return null;
        }
        settingUpFall = true;

        var fromPosition = transform.TransformPoint(new Vector2(-4.55f + 1.3f * j, 5.4f));
        var toPosition = transform.TransformPoint(new Vector2(-4.55f + 1.3f * j, 4.2f - 1.2f * i));

        var type = randomizer();
        GameObject fruit = null;

        switch (type)
        {
            case FruitType.Apple:
                fruit = Instantiate(applePrefab, fromPosition, Quaternion.identity, transform);
                break;

            case FruitType.Pineapple:
                fruit = Instantiate(pineapplePrefab, fromPosition, Quaternion.identity, transform);
                break;

            case FruitType.Peach:
                fruit = Instantiate(peachPrefab, fromPosition, Quaternion.identity, transform);
                break;

            case FruitType.Watermelon:
                fruit = Instantiate(watermelonPrefab, fromPosition, Quaternion.identity, transform);
                break;

            case FruitType.Grape:
                fruit = Instantiate(grapePrefab, fromPosition, Quaternion.identity, transform);
                break;
        }
        
        board[i, j] = type;
        fruits[i, j] = fruit;

        settingUpFall = false;
        var speed = 0.0f;

        while (fruit.transform.position != toPosition)
        {
            fruit.transform.position = Vector3.MoveTowards(fruit.transform.position, toPosition, speed);
            yield return new WaitForSeconds(1.0f / 16.0f);
            speed += 0.1f;
        }

        numberOfFruitsMoving--;
    }

    public void TryToSelectFruit(GameObject fruit)
    {
        if (resolvingMove)
        {
            return;
        }

        (int, int) fruitFound = (-1, -1);
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (fruits[i, j] == fruit)
                {
                    fruitFound = (i, j);
                    break;
                }
            }
        }

        if (fruitFound == (-1, -1))
        {
            Debug.LogError("ERROR: Fruit not found?");
            return;
        }

        if (firstSelection == (-1, -1))
        {
            firstSelection = fruitFound;
            fruit.GetComponent<Animator>().SetTrigger("Enlarge");
        }

        // Unselect on the second click
        else if (firstSelection == fruitFound)
        {
            firstSelection = (-1, -1);
            fruit.GetComponent<Animator>().SetTrigger("Shrink");
        }

        else
        {
            // Get coordinates
            var (x1, y1) = firstSelection;
            var (x2, y2) = fruitFound;

            // Check if newly clicked is a neighbor of previously clicked
            if (!(Math.Abs(x2 - x1) == 0 && Math.Abs(y2 - y1) == 1
                || Math.Abs(x2 - x1) == 1 && Math.Abs(y2 - y1) == 0))
            {
                return;
            }

            TryToSwapFruits(x1, y1, x2, y2);

            // Unselect selected fruit
            var firstSelectionObject = fruits[x1, y1];
            if (firstSelectionObject != null)
            {
                firstSelectionObject.GetComponent<Animator>().SetTrigger("Shrink");
            }
            firstSelection = (-1, -1);
        }
    }

    public void TryToSwapFruits(int x1, int y1, int x2, int y2)
    {
        // Swap fruits on board
        (board[x2, y2], board[x1, y1]) = (board[x1, y1], board[x2, y2]);
        var matches = CheckForExistingMatches();
        
        // Illegal move, swap back
        if (matches.Count == 0)
        {
            Debug.Log("ILLEGAL");
            (board[x2, y2], board[x1, y1]) = (board[x1, y1], board[x2, y2]);
            return;
        }

        // Swao fruit gameobjects
        Destroy(fruits[x1, y1]);
        Destroy(fruits[x2, y2]);
        InstantiateFruit(x1, y1, () => board[x1, y1]);
        InstantiateFruit(x2, y2, () => board[x2, y2]);

        StartCoroutine(MatchAndCascade(matches));
        GameManager.Instance.LevelUI.UpdateMoveCount();
    }

    // Traverse the board to find matches in any configuration. Traversal parameters determine that configuration.
    private List<Match> TraverseBoard(Func<int, int, (int, int)> outerTraversal, Func<int, int, (int, int)> innerTraversal, Match.MatchDirection direction)
    {
        var matches = new List<Match>();
        var (i, j) = (0, 0);
        
        while (i < 8 && j < 8)
        {
            var matching = new List<(int, int)>();
            var previousType = FruitType.None;

            while (i < 8 && j < 8)
            {
                if (board[i, j] != previousType)
                {
                    if (matching.Count >= 3)
                    {
                        matches.Add(new Match(matching, previousType, direction));
                    }

                    matching = new();
                }

                matching.Add((i, j));
                previousType = board[i, j];

                // TEMP: prevent none mathches
                if (board[i, j] == FruitType.None)
                {
                    matching = new();
                }

                var newCoordinates = innerTraversal(i, j);
                (i, j) = newCoordinates;
            }

            if (matching.Count >= 3)
            {
                matches.Add(new Match(matching, previousType, direction));
            }

            var newIJ = outerTraversal(i, j);
            (i, j) = newIJ;
        }

        return matches;
    }
}

public class Match
{
    List<(int, int)> coordinates = new();
    FruitType type;
    MatchDirection direction;

    public List<(int, int)> Coordinates { get { return coordinates; } }
    public FruitType Type { get { return type; } }
    public MatchDirection Direction { get { return direction; } }

    public Match(List<(int, int)> coordinates, FruitType type, MatchDirection direction)
    {
        this.coordinates = coordinates;
        this.type = type;
        this.direction = direction;
    }

    public enum MatchDirection
    {
        Vertical, 
        Horizontal
    }
}

public enum FruitType
{
    None,
    Apple,
    Pineapple,
    Peach,
    Watermelon,
    Grape
}