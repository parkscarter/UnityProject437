using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public enum PieceColor
{
    Red,
    Brown
}

public class Piece
{

    public PieceColor Color;
    public bool IsKing;
    public Vector2Int Position;
    public GameObject GameObjectRef;

    public Piece(PieceColor color, Vector2Int position, GameObject gameObject)
	{
        Color = color;
        IsKing = false;
        Position = position;
        GameObjectRef = gameObject;
    }
}

