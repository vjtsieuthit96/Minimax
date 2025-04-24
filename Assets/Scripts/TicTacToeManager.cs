using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TicTacToeManager : MonoBehaviour
{
    #region Variable
    public GameObject cellPrefabs; // prefab ô cờ

    public Transform boardParent; // panel bàn cờ

    public int boardSize; // Size bàn cờ

    private string [,] board; // Mảng 2 chiều lưu trạng thái ô cờ

    public List<ButtonCell> cells = new List<ButtonCell>();// Danh sách ô cờ;

    private bool isPlayerTurn = true; // biến cờ lượt chơi 

    public int winLength = 5; // Chiều dài chuỗi để chiến thắng

    private readonly int[] dx = { 1, 0, 1, 1 };
    private readonly int[] dy = { 0, 1, 1, -1 };

    private void Start()
    {
        CreateBoard();
    }
    #endregion

    void CreateBoard()
    {
        board = new string[boardSize,boardSize];
        for (int i = 0; i < boardSize; i++) //row
        {
            for (int j = 0; j < boardSize; j++) // column
            {
                // tạo ô cờ và gán giá trị
                var go = Instantiate(cellPrefabs, new Vector3(j,-i,0), Quaternion.identity, boardParent);
                var cell = go.GetComponent<ButtonCell>();
                cell.row = i;
                cell.col = j;
                cell.ticTacToeManager = this;
                int row = i;
                int col = j;
                // Tạo event khi click
                cell.GetComponent<Button>().onClick.AddListener(()=>HandlePlayerMove(row, col));
                cells.Add(cell);
            }
        }
    }  

    public void HandlePlayerMove(int row, int column)
    {
        if (!isPlayerTurn || board[row, column] != null) return;
        // Cập nhật trang thái bàn cờ
        board[row, column] = "O";
        UpdateCellUI(row, column, "O");
        //Kiểm tra chiến thắng và bàn cờ full 
        var isWin = CheckWin("O");
        if (isWin)
        {
            Debug.Log("Player Wins!");
            return;
        }

        var isFull = IsBoardFull(board);
        if (isFull)
        {
            Debug.Log("Deuce!");
            return;
        }
        else
        {
            isPlayerTurn = false; // Chuyển turn qua AI
            Invoke(nameof(PlayerAIMove), 0.3f);
        }
    }
    void PlayerAIMove()
    {
        int stoneCount = CountStones(board);
        int depth = stoneCount < 10 ? 4 : 3;

        // Kiểm tra có thắng ngay hay ko
        var immediateMove = FindImmediateMove();
        Vector2Int bestMove;
        if (immediateMove != Vector2Int.one * -1)
        {
            bestMove = immediateMove;
        }
        else
        {
            var (move, _) = MiniMax(board, depth, true, int.MinValue, int.MaxValue);
            bestMove = move;
        }
        board[bestMove.x, bestMove.y] = "X";
        UpdateCellUI(bestMove.x,bestMove.y, "X");
        if (CheckWin("X"))
        {
            Debug.Log("AI Win");
        }
        else if (IsBoardFull(board))
        {
            Debug.Log("Deuce!");
        }
        else
        {
            isPlayerTurn = true;
        }
    }

    //Thuật toán MiniMax
    (Vector2Int move , int score) MiniMax(string[,] b, int depth, bool isMax, int alpha, int beta)
    {
        // kiểm tra thắng thua
        //AI win
        if (CheckWin("X", b)) return (Vector2Int.zero, 10000 + depth);
        //Player Win
        if (CheckWin("O", b)) return (Vector2Int.zero, -10000 - depth);
        // Nếu bàn cờ full hoặc đạt độ sau tối đa
        if (IsBoardFull(b) || depth == 0)
            return (Vector2Int.zero,EvaluateBoard(b));
        //khởi tạo biến để lưu điểm
        List<Vector2Int> moves = GetSmartCandidateMoves(b);
        Vector2Int bestMove = moves.Count > 0 ? moves[0] : Vector2Int.zero;
        int bestScore = isMax ? int.MinValue : int.MaxValue;
        //Duyệt qua các nước đi
        foreach (var move in moves)
        {
            b[move.x, move.y] = isMax ? "X" : "O";// giả lập nước đi
            //Gọi đệ quy MiniMax
            var score = MiniMax(b,depth-1,!isMax,alpha,beta).score;
            b[move.x, move.y] = null;
            if (isMax && score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                alpha = Mathf.Max(alpha, bestScore); // cập nhật alpha

            }
            else if (!isMax && score < bestScore)
            {
                bestScore = score;
                bestMove = move;
                beta = Mathf.Min(beta, score);
            }
            if (beta <= alpha) break;
        }
        return (bestMove,bestScore);
    }

    //Hàm lấy nước đi thông miinh
    List<Vector2Int> GetSmartCandidateMoves(string[,] b)
    {
        // Danh sách nước đi
        List<Vector2Int> candidates = new List<Vector2Int>();
        // Tìm nước đi đã được xem xét
        HashSet<Vector2Int> consideredCells = new HashSet<Vector2Int>();

        //Phạm vi xem xét xung quanh các quân đã đặt
        int searchRange = 2;
        //Tìm các ô trống xung quanh các quân đã đặt
        for (var row = 0; row < boardSize; row++)
        {
            for (var col = 0; col < boardSize; col++)
            {
                if (b[row, col] !=null) // Tìm các ô đã có quân
                {
                    for (int dr = -searchRange; dr <= searchRange; dr++)
                    {
                        for (int dc = -searchRange; dc <= searchRange; dc++)
                        {
                            int newRow = row + dr;
                            int newCol = col + dc;
                            // Kiểm tra ô mới có nằm trong board ko 
                            if (newRow >= 0 && newRow < boardSize && newCol >= 0 && newCol < boardSize &&
                                b[newRow, newCol] == null &&
                                !consideredCells.Contains(new Vector2Int(newRow, newCol)))
                            {
                                consideredCells.Add(new Vector2Int(newRow, newCol));
                                candidates.Add(new Vector2Int(newRow, newCol));
                            }
                        }
                    }
                }
            }
        }
        //Nếu ko có nước đi nào thì sẽ chọn vị trí trung tâm
        if (candidates.Count == 0)
        {
            int center = boardSize / 2;
            if (b[center,center] == null)
                candidates.Add((new Vector2Int(center, center)));
            else
            {
                //Tìm ô trống đầu tiên
                for (int row = 0; row < boardSize; row++)
                {
                    for (int col = 0; col < boardSize; col++)
                    {
                        if (b[row, col] == null)
                        {
                            candidates.Add((new Vector2Int(row, col)));
                            break;
                        }
                    }
                    if (candidates.Count > 0) break;
                }
            }
        }
        // sắp xếp thứ tự ưu tiên
        candidates = candidates.OrderByDescending(pos => EvaluateMove(b,pos.x,pos.y)).ToList();
        return candidates;
    }

    void UpdateCellUI(int row, int col,string symbol)
    {
        var cell = cells.First(x=>x.row == row && x.col == col); 
        cell.SetSymbol(symbol);
        var image = cell.GetComponent<Image>();      
        if (symbol == "X")
        {
            image.color = Color.red;
        }
        else if (symbol == "O")
        {
            image.color = Color.green;
        }
    }

    private bool CheckWin(string symbol, string[,] b = null)
    {
        b ??= board;
        for (var row = 0; row < boardSize; row++)
            for (var col = 0; col < boardSize; col++)
            {
                if (b[row, col] != symbol) continue;

                //kiểm tra 4 hướng
                for (int d = 0; d < 4; d++)
                {
                    int count = 1;
                    for (int k = 1; k < winLength; k++)
                    {
                        int newRow = row + dx[d] * k;
                        int newCol = col + dy[d] * k;
                        // Kiểm tra ô mới có nằm trong bàn cờ
                        if (newRow < 0 || newRow >= boardSize || 
                            newCol < 0 || newCol >= boardSize || 
                            b[newRow,newCol] != symbol)
                            break;
                        count++;
                    }
                    if (count >= winLength)
                    {                       
                        return true;
                    }
                }

            }
        return false;
    }

    private bool IsBoardFull(string[,] board)
    {
        for (var i = 0; i < boardSize; i++)
        {
            for (var j = 0; j < boardSize; j++)
            {
                if (board[i, j] == null)
                {
                    return false;
                }
            }
        }
        return true;
    }
    // Đánh giá giá trị bàn cờ 
    int EvaluateBoard(string[,] b)
    {
        int score = 0;
        // Mảng lưu các giá trị của chuỗi
        int[] aiValues = { 0, 1, 10, 100, 1000 };
        // Giá trị phòng thủ cao hơn tấn công
        int[] playerValues = { 0, -1, -15, -150, -2000 };
        //Tính điểm theo dòng cột và đường chéo
        for (var row = 0; row < boardSize; row++)
        {
            for (var col = 0; col < boardSize; col++)
            {
                for (int d = 0; d < 4; d++)
                {
                    // kiểm tra xem có thể tạo được chuỗi chiến thắng ko
                    if (row + dx[d] * (winLength - 1) >= boardSize ||
                        row + dx[d] * (winLength - 1) < 0 ||
                        col + dy[d] * (winLength - 1) >= boardSize ||
                        col + dy[d] * (winLength - 1) < 0) continue;
                    int aiCount = 0, playerCount = 0, emptyCount = 0;
                    for (int k = 0; k < winLength; k++)
                    {
                        int newRow = row + dx[d] * k;
                        int newCol = col + dy[d] * k;
                        if (b[newRow, newCol] == "X")
                            aiCount++;
                        else if (b[newRow, newCol] == "O")
                            playerCount++;
                        else
                            emptyCount++;
                    }
                    //Chỉ tính điểm nếu chuỗi ko bị chặn
                    if (playerCount == 0 && aiCount > 0)
                        score += aiValues[aiCount];
                    else if (aiCount == 0 && playerCount > 0)
                        score += playerValues[playerCount];
                }
            }
        }

        // Thêm yếu tố vị trí - ưu tiên ô giữa bàn cờ
        for (var row = 0; row <boardSize; row++)
            for (var col = 0; col <boardSize; col++)
            {
                if (board[row, col] == "X")
                {
                    // Điểm vị trí - khoảng cách đến ô trung tâm
                    int centerRow = boardSize / 2;
                    int centerCol = boardSize / 2;
                    float distance = Mathf.Sqrt(Mathf.Pow(row-centerRow,2) + Mathf.Pow(col-centerCol,2));
                    score += Mathf.Max(5 - (int)distance, 0);
                }
            }
        return score;
    }
    //Đánh giá nước đi tại vị trí cụ thể 
    int EvaluateMove(string[,] b, int row, int col)
    {
        int value = 0;
        //Giả lập nước đi
        b[row, col] = "X";
        // Tính điểm nước đi
        value += CalculateStrength(b, row, col, "X");
        b[row, col] = null;
        //Giả lấp nước đi của đối thủ
        b[row, col] = "O";
        //Tính điểm nước đi
        value -= CalculateStrength(b, row, col, "O");
        b[row, col] = null;

        //Ưu tiên ô ở giữa
        int centerRow = boardSize / 2;
        int centerCol = boardSize / 2;
        int distance = Mathf.Abs(row- centerRow) + Mathf.Abs(col- centerCol);
        value += Mathf.Max(5 - distance, 0);
        return value;
    }
    //Tính điểm nước đii
    int CalculateStrength(string[,] b, int row, int col, string symbol)
    {
        int strength = 0;
        // Kiểm tra 4 hướng
        for (int d = 0; d < 4; d++)
        {
            int count = 0; // Đếm số quân liên tiếp
            int emptyBefore = 0; // Đếm số ô trống trước chuỗi;
            int emptyAfter = 0; //Đếm số ô trống sau chuỗi

            //Đếm về trước
            for (int k = 0; k< winLength;k++)
            {
                int newRow = row + dx[d] * k;
                int newCol = row + dy[d] * k;
                if (newRow < 0 || newRow >= boardSize || newCol < 0 || newCol >= boardSize) break;
                if (b[newRow, newCol] == symbol)
                    count++;
                else if (b[newRow, newCol] == null)
                {
                    emptyAfter++;
                    break;
                }
                else
                    break;
            }
            //Đếm về phía sau
            for (int k = 0; k < winLength; k++)
            {
                int newRow = row - dx[d] * k;
                int newCol = row - dy[d] * k;
                if (newRow < 0 || newRow >= boardSize || newCol < 0 || newCol >= boardSize) break;
                if (b[newRow, newCol] == symbol)
                    count++;
                else if (b[newRow, newCol] == null)
                {
                    emptyBefore++;
                    break;
                }
                else
                    break;
            }
            //Đánh giá sức mạnh dựa trên số quân liên tiếp và ô trống
            if (count + emptyBefore + emptyAfter >= winLength)
            {
                if (count == 4) strength += 1000;
                else if (count == 3) strength += 100;
                else if (count == 2) strength += 10;

                //thêm giá trị dựa trên không gian có sẵn
                strength += (emptyBefore + emptyAfter) * 2;
            }

        }
        return strength;
    }
            
    // Tìm nước đi - win or block
    Vector2Int FindImmediateMove()
    {
        // Kiểm tra xem AI có thể win ko
        for (var row = 0; row < boardSize; row++)
            for (var col = 0; col < boardSize; col++)
            {
                if (board[row, col] != null) continue;
                board[row, col] = "X"; // Giả lập nước đi của AI
                if (CheckWin("X"))
                {
                    board[row, col] = null; // Khôi phục ban đầu
                    return new Vector2Int(row, col); // trả về nước đi thắng
                }
                board [row, col] = null; // Khôi phục lại ban đầu
            }

        // Kiểm tra AI cần chặn Player ko
        for (var row = 0; row <boardSize; row++)
            for (var col = 0; col<boardSize; col++)
            {
                if (board [row, col] != null) continue;
                board[row, col] = "O"; //Giả lập nc đi
                if (CheckWin ("O"))
                {
                    board[row, col] = null;
                    return new Vector2Int(row, col);
                }
                board[row,col] = null;              
            }
        return new Vector2Int(-1, -1); // không tìm thấy nước đi
    }
    // Hàm đếm số quân trên bàn cờ
    int CountStones(string[,] b)
    {
        int count = 0;
        foreach (var s in b)        
            if (s!= null)
                count++;        
        return count;
    }    
}
