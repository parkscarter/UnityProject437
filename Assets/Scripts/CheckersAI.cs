using System.Collections.Generic;
using UnityEngine;

public static class CheckersAI
{
   
    // Return the AIs chosen move
    public static (Piece piece, Vector2Int move, List<Vector2Int> captured)? GetBestMove(Piece[,] board, PieceColor aiColor, int depth)
    {
        float bestScore = float.NegativeInfinity;
        (Piece, Vector2Int, List<Vector2Int>)? bestMove = null;

        //check all possible moves
        // A move comes in the form of (pieceToMove, targetTile, jumpedPieces)
        foreach (var move in GetAllPossibleMoves(board, aiColor))
        {
            //Apply each possible move on a cloned board, run minimax to get a score
            Piece[,] boardCopy = CloneBoard(board, out var pieceMapping);
            var mappedMove = (
                pieceMapping[move.Item1],
                move.Item2,
                move.Item3
            );
            ApplyMove(boardCopy, mappedMove);

            float score = Minimax(boardCopy, depth - 1, false, aiColor);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        //pick move with the best score
        return bestMove;
    }

    //Recursively test all possible board states that can be led to by an inputed board state, and return a score used to evaluate the given state
    private static float Minimax(Piece[,] board, int depth, bool isMaximizing, PieceColor aiColor)
    {
        //return board evaluation if depth reaches 0
        if (depth == 0)
            return EvaluateBoard(board, aiColor);

        PieceColor currentPlayer = isMaximizing ? aiColor : (aiColor == PieceColor.Red ? PieceColor.Brown : PieceColor.Red);

        //get all possible moves for this player
        var moves = GetAllPossibleMoves(board, currentPlayer);

        //if no moves, return
        if (moves.Count == 0)
        {
            return isMaximizing ? float.NegativeInfinity : float.PositiveInfinity;
        }

        if (isMaximizing)
        {
            float maxEval = float.NegativeInfinity;
            //Apply each possible move to a copied board
            foreach (var move in moves)
            {
                Piece[,] boardCopy = CloneBoard(board, out var pieceMapping);
                var mappedMove = (
                    pieceMapping[move.Item1],
                    move.Item2,
                    move.Item3
                );

                //Apply each possible move to a copied board
                ApplyMove(boardCopy, mappedMove);

                //call minimax with depth-1 to eventually find best board state
                float eval = Minimax(boardCopy, depth - 1, false, aiColor);
                maxEval = Mathf.Max(maxEval, eval);
            }
            return maxEval;
        }
        else
        {
            float minEval = float.PositiveInfinity;
            foreach (var move in moves)
            {
                Piece[,] boardCopy = CloneBoard(board, out var pieceMapping);
                var mappedMove = (
                    pieceMapping[move.Item1],
                    move.Item2,
                    move.Item3
                );

                ApplyMove(boardCopy, mappedMove);

                //call minimax with depth-1 to eventually find best board state
                float eval = Minimax(boardCopy, depth - 1, true, aiColor);
                minEval = Mathf.Min(minEval, eval);
            }
            return minEval;
        }
    }


    //Return all possible moves for a color
    private static List<(Piece, Vector2Int, List<Vector2Int>)> GetAllPossibleMoves(Piece[,] board, PieceColor color)
    {
        List<(Piece, Vector2Int, List<Vector2Int>)> allJumps = new();
        List<(Piece, Vector2Int, List<Vector2Int>)> allNormalMoves = new();

        //for each location on the board...
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = board[x, y];
                //if there's a piece that's our color
                if (piece != null && piece.Color == color)
                {
                    //find jump moves
                    var jumpResults = new List<(Vector2Int, List<Vector2Int>)>();
                    FindJumpMoves(piece, board, piece.Position, new List<Vector2Int>(), new HashSet<Vector2Int>(), jumpResults);

                    foreach (var jump in jumpResults)
                    {
                        allJumps.Add((piece, jump.Item1, jump.Item2));
                    }

                    // only find non-jump moves if there's no jump available
                    if (jumpResults.Count == 0)
                    {
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

                        foreach (var dir in directions)
                        {
                            Vector2Int target = piece.Position + dir;
                            if (IsWithinBounds(target) && board[target.x, target.y] == null)
                            {
                                allNormalMoves.Add((piece, target, new List<Vector2Int>()));
                            }
                        }
                    }
                }
            }
        }

        // If we can jump, only return jump moves
        if (allJumps.Count > 0)
        {
            return allJumps;
        }
        else
        {
            return allNormalMoves;
        }
    }

    //Given a board and move, apply the move
    private static void ApplyMove(Piece[,] board, (Piece piece, Vector2Int tile, List<Vector2Int> captured) move)
    {
        Piece piece = move.piece;
        Vector2Int from = piece.Position;
        Vector2Int to = move.tile;

        //remove captured pieces
        board[from.x, from.y] = null;
        foreach (var p in move.captured)
        {
            board[p.x, p.y] = null;
        }

        piece.Position = to;

        //move this piece to target location
        board[to.x, to.y] = piece;

        // Promote if reaching the back row
        if (!piece.IsKing)
        {
            if ((piece.Color == PieceColor.Red && piece.Position.y == 7) ||
                (piece.Color == PieceColor.Brown && piece.Position.y == 0))
            {
                piece.IsKing = true;
            }
        }
    }

    //Return a copy of the given board
    private static Piece[,] CloneBoard(Piece[,] board, out Dictionary<Piece, Piece> pieceMapping)
    {
        Piece[,] copy = new Piece[8, 8];
        pieceMapping = new Dictionary<Piece, Piece>();

        //iterate through board and copy pieces into new board
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = board[x, y];
                if (piece != null)
                {
                    Piece newPiece = new Piece(piece.Color, new Vector2Int(x, y), null)
                    {
                        IsKing = piece.IsKing
                    };
                    copy[x, y] = newPiece;
                    pieceMapping[piece] = newPiece;
                }
            }
        }
        return copy;
    }

    // Gives an evaluation for a given boardstate and who's turn it is
    private static float EvaluateBoard(Piece[,] board, PieceColor currentPlayerColor)
    {
        float score = 0f;
        int opponentPieceCount = 0;

        //Count how many of each piece is on the board, add for current player and subtract if its the other player's piece
        //King are worth double
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = board[x, y];
                if (piece != null)
                {
                    float value = piece.IsKing ? 2f : 1f;
                    if (piece.Color == currentPlayerColor)
                        score += value;
                    else
                    {
                        score -= value;
                        opponentPieceCount++;
                    }
                }
            }
        }

        // Boost score if opponent has no pieces left
        if (opponentPieceCount == 0)
        {
            score += 1000f;
        }

        return score;
    }

    //Copied from checkersLogic
    private static void FindJumpMoves(Piece piece, Piece[,] board, Vector2Int currentPos, List<Vector2Int> capturedSoFar, HashSet<Vector2Int> visited, List<(Vector2Int, List<Vector2Int>)> result)
    {
        bool foundJump = false;
        int forward = (piece.Color == PieceColor.Red) ? 1 : -1;
        List<Vector2Int> directions = new();
        directions.Add(new Vector2Int(-1, forward));
        directions.Add(new Vector2Int(1, forward));

        if (piece.IsKing)
        {
            int backward = -forward;
            directions.Add(new Vector2Int(-1, backward));
            directions.Add(new Vector2Int(1, backward));
        }

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

                FindJumpMoves(piece, board, landing, newCaptured, newVisited, result);
            }
        }

        if (!foundJump && capturedSoFar.Count > 0)
        {
            result.Add((currentPos, capturedSoFar));
        }
    }

    private static bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }
}
