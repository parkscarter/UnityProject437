using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


 public enum Turn
    {
        Red,   // Human player
        Brown  // AI
    }

public class CheckersLogic : MonoBehaviour
{
    public GameObject RedPiece;
    public GameObject BrownPiece;

    public GameObject highlightPrefab;  //highlighted tile
    public Material selectedMaterial;   //highlighted material for piece
    
    public Material redMaterial;        //User piece color
    public Material brownMaterial;      //AI piece color

    public static int depth = 2;

   
    private List<GameObject> highlightedPieces = new List<GameObject>();                                                    // Highlighted pieces
    private Dictionary<GameObject, (Vector2Int target, List<Vector2Int> captured)> targetTileAndJumpedPieces = new();       // Highlighted Tiles, and list of captured pieces associated with each jump
    private HashSet<Piece> jumpablePieces = new();                                                                          //list of enemy jumpable pieces
    private Piece[,] board = new Piece[8, 8];                                                                               //logical representation of board

    private Piece selectedPiece;            

    private Turn currentTurn = Turn.Red;   


    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SpawnPieces();      //Function to spawn pieces
    }


    void Update()
    {
        //continually check for click
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
        CheckGameOver();    //see if the game is over
    }

    //Spawns all pieces in correct starting positions
    void SpawnPieces()
    {
        //loop through entire board
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if ((x + y) % 2 != 1)// Only spawn on black tiles
                {
                    // Calculate (x, z) position based on board coordinates
                    Vector3 position = new Vector3(x, 1.0f, y);
                    Vector2Int boardPos = new Vector2Int(x, y);

                    //Place red and brown pieces respectively
                    if (y < 3)
                    {
                        GameObject redPiece = Instantiate(RedPiece, position, Quaternion.identity);
                        board[x, y] = new Piece(PieceColor.Red, boardPos, redPiece);
                    }
                    else if (y > 4)
                    {
                        GameObject brownPiece = Instantiate(BrownPiece, position, Quaternion.identity);
                        board[x, y] = new Piece(PieceColor.Brown, boardPos, brownPiece);
                    }
                }
            }
        }
    }


    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //If mouse click hit, set hitObject
            GameObject hitObject = hit.collider.gameObject;

            // If clicked on a highlighted tile, move the piece
            if (targetTileAndJumpedPieces.ContainsKey(hitObject) && selectedPiece != null)
            {
                var (targetPos, capturedPieces) = targetTileAndJumpedPieces[hitObject];
                MovePiece(selectedPiece, targetPos, capturedPieces);
                return; 
            }

            // Only try to select piece if not a highlighted tile
            SelectPieceUnderMouse(hitObject);
        }
    }


    void MovePiece(Piece piece, Vector2Int targetPos, List<Vector2Int> captured)
    {
        board[piece.Position.x, piece.Position.y] = null;

        foreach (var cap in captured)
        {
            var capturedPiece = board[cap.x, cap.y];
            if (capturedPiece != null)
            {
                Destroy(capturedPiece.GameObjectRef);
                board[cap.x, cap.y] = null;
            }
        }

        piece.GameObjectRef.transform.position = new Vector3(targetPos.x, 1.0f, targetPos.y);
        piece.Position = targetPos;
        board[targetPos.x, targetPos.y] = piece;

        if (piece.Color == PieceColor.Red)
            SetMaterial(piece.GameObjectRef, redMaterial);
        else
            SetMaterial(piece.GameObjectRef, brownMaterial);

        selectedPiece = null;
        ClearHighlightedPieces();

        //if the piece isn't a king yet
        if (!piece.IsKing)
        {
            if ((piece.Color == PieceColor.Red && piece.Position.y == 7) ||         //and it's in the back row, 
                (piece.Color == PieceColor.Brown && piece.Position.y == 0))
            {
                piece.IsKing = true;                                                            //set king
                piece.GameObjectRef.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);      // make taller
            }
        }

        //Switch turn
        if (currentTurn == Turn.Red)
        {
            currentTurn = Turn.Brown;
            StartCoroutine(HandleAITurn());
        }
        else
        {
            currentTurn = Turn.Red;
        }
    }

    //Cast a ray out of the camera with the mouse position, 
    void SelectPieceUnderMouse(GameObject hitObject)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = board[x, y];
                if (piece != null && piece.GameObjectRef == hitObject)
                {
                    if (piece.Color == PieceColor.Red && currentTurn == Turn.Red) // if the user hits their own piece
                    {
                        if (jumpablePieces.Count > 0 && !jumpablePieces.Contains(piece))    //check if there are any valid jump moves, and if this piece has any valid jump moves
                            return;
                        if (selectedPiece != null)
                        {
                            SetMaterial(selectedPiece.GameObjectRef, redMaterial);  //deselect old piece
                        }

                        selectedPiece = piece;
                        SetMaterial(piece.GameObjectRef, selectedMaterial);     //change material of this piece
                        HighlightValidMoves(piece);                             //once selected, highlight the pieces valid moves, including jumps
                    }
                }
            }
        }
    }



    //This function checks tiles forward and at a diagonal to the piece passed in
    void HighlightValidMoves(Piece piece)
    {
        ClearHighlightedPieces();
        targetTileAndJumpedPieces.Clear();  //Remove old jump data

        //find and store jump results
        var jumpResults = new List<(Vector2Int, List<Vector2Int>)>();
        FindJumpMoves(piece, piece.Position, new List<Vector2Int>(), new HashSet<Vector2Int>(), jumpResults);

        //If there's an option to jump a piece, only allow jumps
        if (jumpResults.Count > 0)
        {
            // Show only jump options
            foreach (var jump in jumpResults)
            {
                Vector3 worldPos = new Vector3(jump.Item1.x, 0.0f, jump.Item1.y);
                GameObject highlight = Instantiate(highlightPrefab, worldPos, Quaternion.identity);
                highlightedPieces.Add(highlight);
                targetTileAndJumpedPieces[highlight] = jump;
            }
        }
        else    //if there's no jumps available, highlight basic moves
        {
            //Set diagonal translations based on piece color and king status
            List<Vector2Int> directions = new();

            int forward = (piece.Color == PieceColor.Red) ? 1 : -1;
            directions.Add(new Vector2Int(-1, forward));
            directions.Add(new Vector2Int(1, forward));

            if (piece.IsKing)
            {
                int backward = -forward;
                directions.Add(new Vector2Int(-1, backward));
                directions.Add(new Vector2Int(1, backward));
            }

            //Loop through valid tiles and see if they are free; create highlighted tile object over top
            foreach (var dir in directions)
            {
                Vector2Int targetPos = piece.Position + dir;
                if (IsWithinBounds(targetPos) && board[targetPos.x, targetPos.y] == null)
                {
                    Vector3 worldPos = new Vector3(targetPos.x, 0.0f, targetPos.y);
                    GameObject highlightedPiece = Instantiate(highlightPrefab, worldPos, Quaternion.identity);
                    highlightedPieces.Add(highlightedPiece);
                    targetTileAndJumpedPieces[highlightedPiece] = (targetPos, new List<Vector2Int>());      //add target position and empty piece list to targetTileAndJumpedPieces List
                }
            }
        }
    }

    //Recursively enter all jump moves into result, same format as targetTileAndJumpedPieces
    private void FindJumpMoves(Piece piece, Vector2Int currentPos, List<Vector2Int> capturedSoFar, HashSet<Vector2Int> visited, List<(Vector2Int, List<Vector2Int>)> result)
    {
        bool foundJump = false;

        //Set diagonal translations based on piece color and king status
        List<Vector2Int> directions = new();

        int forward = (piece.Color == PieceColor.Red) ? 1 : -1;
        directions.Add(new Vector2Int(-1, forward));
        directions.Add(new Vector2Int(1, forward));

        if (piece.IsKing)
        {
            int backward = -forward;
            directions.Add(new Vector2Int(-1, backward));
            directions.Add(new Vector2Int(1, backward));
        }
        //For each direction, check immediate tile and landing tile
        foreach (var dir in directions)
        {
            Vector2Int mid = currentPos + dir;
            Vector2Int landing = currentPos + dir * 2;

            if (IsWithinBounds(mid) && IsWithinBounds(landing) &&
                board[mid.x, mid.y] != null &&
                board[mid.x, mid.y].Color != piece.Color &&
                board[landing.x, landing.y] == null &&
                !visited.Contains(mid))
            {
                foundJump = true;
                var newCaptured = new List<Vector2Int>(capturedSoFar) { mid };
                var newVisited = new HashSet<Vector2Int>(visited) { mid };
                //if there's a valid jump, always check if theres a double jump
                FindJumpMoves(piece, landing, newCaptured, newVisited, result);
            }
        }

        // if we jumped at least once, add the target tile and list of captured pieces to result
        if (!foundJump && capturedSoFar.Count > 0)
        {
            result.Add((currentPos, capturedSoFar));
        }
    }

    //when it's the ai's turn, get their best move with minimax, 
    private IEnumerator HandleAITurn()
    {
        yield return new WaitForSeconds(1f);

        var move = CheckersAI.GetBestMove(board, PieceColor.Brown, depth);

        if (move.HasValue)
        {
            MovePiece(move.Value.piece, move.Value.move, move.Value.captured);
        }

        currentTurn = Turn.Red;
        jumpablePieces = GetAllJumpablePieces(PieceColor.Red);
    }

    // Given the current game state and a color, return 
    private HashSet<Piece> GetAllJumpablePieces(PieceColor color)
    {
        HashSet<Piece> jumpables = new();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = board[x, y];
                if (piece != null && piece.Color == color)
                {
                    var result = new List<(Vector2Int, List<Vector2Int>)>();
                    FindJumpMoves(piece, piece.Position, new List<Vector2Int>(), new HashSet<Vector2Int>(), result);

                    if (result.Count > 0)
                    {
                        jumpables.Add(piece);
                    }
                }
            }
        }

        return jumpables;
    }

    // destroy all highlighted pieces
    void ClearHighlightedPieces()
    {
        foreach (GameObject piece in highlightedPieces)
        {
            Destroy(piece);
        }
        highlightedPieces.Clear();
    }

    //Given a game object and a material, set that game object's material (used for highlighting)
    void SetMaterial(GameObject obj, Material mat)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = mat;
        }
    }

    bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }

    
    private void CheckGameOver()
    {
        int redCount = 0;
        int brownCount = 0;

        // Count remaining pieces
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = board[x, y];
                if (piece != null)
                {
                    if (piece.Color == PieceColor.Red)
                        redCount++;
                    else if (piece.Color == PieceColor.Brown)
                        brownCount++;
                }
            }
        }

        // If either side has no pieces left
        if (redCount == 0)
        {
            EndGame(false); // Player lost
        }
        else if (brownCount == 0)
        {
            EndGame(true);  // Player won
        }
    }

    private void EndGame(bool playerWon)
    {
        if (playerWon)
        {
            // Player Wins
            PlayerMovement.respawn = true;
            PlayerMovement.RespawnPosition = new Vector3(67f, 60f, 868f); // Above medium gate
            PlayerMovement.RespawnForce = new Vector3(0f, 100f, 0f);       // Big upward launch (doesn't actually work for some reason)
            PlayerMovement.playerWon = true;
        }
        else
        {
            // Player Loses
            PlayerMovement.respawn = true;
            PlayerMovement.RespawnPosition = new Vector3(67f, 60f, 868f); // Spawn above easy gate (for example)
            PlayerMovement.RespawnForce = new Vector3(-100f, 40f, -100f);       // Smaller upward launch
        }

        // Load Terrain Scene
        SceneManager.LoadScene("TerrainScene");
    }

}
