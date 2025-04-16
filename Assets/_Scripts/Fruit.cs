using UnityEngine;

public class Fruit : MonoBehaviour
{
    private void OnMouseDown()
    {
        GameManager.Instance.LevelGrid.TryToSelectFruit(gameObject);
    }
}
