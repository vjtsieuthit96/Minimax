using TMPro;
using UnityEngine;

public class ButtonCell : MonoBehaviour
{
    public int row;
    public int col;

    public TicTacToeManager ticTacToeManager;

    private void OnMouseDown()
    {
        ticTacToeManager.HandlePlayerMove(row,col);
    }

    public void SetSymbol(string symbol)
    {
        GetComponentInChildren<TextMeshProUGUI>().text = symbol;
    }

}
