using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Sources;

public class MyBot : IChessBot
{
    int baseMaxDepth = 4;
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool botIsWhite;

    public Move Think(Board board, Timer timer)
    {
        botIsWhite = IsOurTurn(board);

        Move[] allmoves = board.GetLegalMoves();
        Move bestmove = allmoves[0];
        int bestMoveAdvantage = int.MinValue;


        foreach (Move move in allmoves)
        {
            board.MakeMove(move);
            int moveAdvantage = -NegaMax(board, 0, int.MinValue, int.MaxValue, false);
            board.UndoMove(move);
            Console.WriteLine("Advantage: " + moveAdvantage);
            if (moveAdvantage > bestMoveAdvantage)
            {
                bestMoveAdvantage = moveAdvantage;
                bestmove = move;
            }
        }
        Console.WriteLine("Best Advantage: " + bestMoveAdvantage);
        return bestmove;
    }

    private bool IsOurTurn(Board board)
    {
        return board.IsWhiteToMove;
    }


    public int NegaMax(Board board, int currentDepth, int alpha, int beta, bool ourTurn)
    {
        Move[] moves = board.GetLegalMoves();

        if (board.IsInCheckmate())
        {
            return int.MinValue + 1; //bit scuffed, the +1 prevents overflow when it is multiplied by -1
        }
        if (board.IsDraw())
        {
            return 0;
        }
        if (currentDepth == baseMaxDepth)
        {
            return CalculateAdvantage(board);
        }

        int bestEval = int.MinValue;
        foreach (Move move in moves)
        {
            //things to note here is that use -NegaMax to get eval, and we dont figure out the value of beta (not sure if this one is intentional but wikipedia calls for it)
            board.MakeMove(move);
            bestEval = Math.Max(bestEval, -NegaMax(board, currentDepth + 1, -beta, -alpha, !ourTurn));
            board.UndoMove(move);
            alpha = Math.Max(alpha, bestEval);
            if (alpha >= beta)
            {
                break;
            }

        }

        return bestEval;
    }

    public int CalculateAdvantage(Board board)
    {
        int materialAdvantage = CalculateMaterialAdvantageOfCurrentPlayer(board);
        int positionalAdvantage = CalculatePositionalAdvantage(board);
        int boardValue = materialAdvantage + positionalAdvantage;

        return boardValue;
    }

    public int CalculateMaterialAdvantageOfCurrentPlayer(Board board)
    {
        PieceList[] pieceListList = board.GetAllPieceLists();
        int whiteMaterialValue = pieceListList.Take(6).Sum(list => list.Count * pieceValues[(int)list.TypeOfPieceInList]); //Sums values of first 6 lists (white pieces)
        int blackMaterialValue = pieceListList.Skip(6).Take(6).Sum(list => list.Count * pieceValues[(int)list.TypeOfPieceInList]); //Sums up next 6 lists (black pieces)

        int whiteMaterialAdvantage = whiteMaterialValue - blackMaterialValue;
        int blackMaterialAdvantage = blackMaterialValue - whiteMaterialValue;
        int materialAdvantage = board.IsWhiteToMove ? whiteMaterialAdvantage : blackMaterialAdvantage;

        return materialAdvantage;
    }

    private int CalculatePositionalAdvantage(Board board) //Refactor this and material advantage due to reused code
    {

        return 0;

    }

    private int CalculateDevelopmentIncrease(Move move)
    {
        int startCentreness = CalculateCentredness(move.StartSquare);
        int targetCentreness = CalculateCentredness(move.TargetSquare);

        int initialDevelopment = startCentreness;
        int newDevelopment = targetCentreness;
        //Currently, development is only calculated based on how "central" the pieces are, it does not take the piece into account

        return newDevelopment - initialDevelopment;
    }

    private int CalculateCentredness(Square square)
    {
        int rankMiddleness = (int)(3.5 - Math.Abs(square.Rank - 3.5)); //Closeness to the middle in terms of ranks
        int fileMiddleness = (int)(3.5 - Math.Abs(square.File - 3.5)); //Closeness to middle in terms of files

        int Centreness = rankMiddleness * fileMiddleness;
        /*Things closest to the centre have the highest centreness. Multiplying the values gives the highest difference in centreness when moving into the centre.
         This makes the calculations for development increase work better as the AI will choose to develop pieces into the centre as that gives the greatest development
         incresase. 
        Previously, using addition lead it to sometimes developing pieces on the edge of the board as moving two spaces forwards increases development by the same amount 
        as moving a piece in the middle to the centre*/
        //Issue? The AI will never develop pieces on the leftmost and rightmost side of the board, as the increase in development score is 0.

        return Centreness;
    }

}