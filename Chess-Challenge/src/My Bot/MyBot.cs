using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        int psAdvantage = CalculatePieceSquareAdvantage(board);
        int boardValue = materialAdvantage + psAdvantage;

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

    public int CalculatePieceSquareAdvantage(Board board)  //shitass af fix later!!!
    {
        //the use of 2d arrays will have to be replaced in the future, in favour of string? ulong?
        int[,] pawnPSTable = new int[8, 8] //the 8s could be removed to reduce token count? 
        {
            {0,   0,   0,   0,   0,   0,  0,   0},
            {98, 134,  61,  95,  68, 126, 34, -11},
            {-6,   7,  26,  31,  65,  56, 25, -20},
            {-14,  13,   6,  21,  23,  12, 17, -23},
            {-27,  -2,  -5,  12,  17,   6, 10, -25},
            {-26,  -4,  -4, -10,   3,   3, 33, -12},
            {-35,  -1, -20, -23, -15,  24, 38, -22},
            { 0,   0,   0,   0,   0,   0,  0,   0},
        };

        int[,] knightPSTable = new int[8, 8]
        {
            {-167, -89, -34, -49,  61, -97, -15, -107},
            {-73, -41,  72,  36,  23,  62,   7,  -17},
            {-47,  60,  37,  65,  84, 129,  73,   44},
            {-9,  17,  19,  53,  37,  69,  18,   22},
            {-13,   4,  16,  13,  28,  19,  21,   -8},
            {-23,  -9,  12,  10,  19,  17,  25,  -16},
            {-29, -53, -12,  -3,  -1,  18, -14,  -19 },
            {-105, -21, -58, -33, -17, -28, -19,  -23 },
        };

        int[,] bishopPSTable = new int[8, 8]
        {
            {-29,   4, -82, -37, -25, -42,   7,  -8},
            {-26,  16, -18, -13,  30,  59,  18, -47 },
            {-16,  37,  43,  40,  35,  50,  37,  -2 },
            {-4,   5,  19,  50,  37,  37,   7,  -2 },
            {-6,  13,  13,  26,  34,  12,  10,   4 },
            {0,  15,  15,  15,  14,  27,  18,  10 },
            {4,  15,  16,   0,   7,  21,  33,   1 },
            {-33,  -3, -14, -21, -13, -12, -39, -21 }
        };

        int[,] rookPSTable = new int[8, 8]
        {
            {32,  42,  32,  51, 63,  9,  31,  43 },
            {27,  32,  58,  62, 80, 67,  26,  44},
            {-5,  19,  26,  36, 17, 45,  61,  16 },
            {-24, -11,   7,  26, 24, 35,  -8, -20 },
            { -36, -26, -12,  -1,  9, -7,   6, -23 },
            {-45, -25, -16, -17,  3,  0,  -5, -33 },
            {-44, -16, -20,  -9, -1, 11,  -6, -71 },
            {-19, -13,   1,  17, 16,  7, -37, -26 }
        };

        int[,] queenPSTable = new int[8, 8]
        {
            {-28,   0,  29,  12,  59,  44,  43,  45 },
            {-24, -39,  -5,   1, -16,  57,  28,  54 },
            {-13, -17,   7,   8,  29,  56,  47,  57 },
            {-27, -27, -16, -16,  -1,  17,  -2,   1 },
            { -9, -26,  -9, -10,  -2,  -4,   3,  -3 },
            {-14,   2, -11,  -2,  -5,   2,  14,   5 },
            { -35,  -8,  11,   2,   8,  15,  -3,   1 },
            {-1, -18,  -9,  10, -15, -25, -31, -50 }
        };

        int[,] kingPSTable = new int[8, 8]
        {
            {-65,  23,  16, -15, -56, -34,   2,  13 },
            {29,  -1, -20,  -7,  -8,  -4, -38, -29 },
            {-9,  24,   2, -16, -20,   6,  22, -22 },
            {-17, -20, -12, -27, -30, -25, -14, -36 },
            {-49,  -1, -27, -39, -46, -44, -33, -51 },
            { -14, -14, -22, -46, -44, -30, -15, -27 },
            {1,   7,  -8, -64, -43, -16,   9,   8 },
            {-15,  36,  12, -54,   8, -28,  24,  14 }
        };

        //yea u can optimise tf out of this uwu :3 (ill do it laterrr)
        int[][,] tableList = {
            pawnPSTable,
            knightPSTable,
            bishopPSTable,
            rookPSTable,
            queenPSTable,
            kingPSTable
        };

        PieceList[] pieceListList = board.GetAllPieceLists(); //shitass af

        int whiteAdvantage = 0;
        foreach (PieceList pieceList in pieceListList.Take(6))
        {
            int[,] currentTable = tableList[(int)pieceList.TypeOfPieceInList - 1];
            foreach (Piece piece in pieceList)
            {
                whiteAdvantage += currentTable[Math.Abs(piece.Square.Rank - 7), piece.Square.File];
            }
        }

        foreach (PieceList pieceList in pieceListList.Skip(6).Take(6))
        {
            int[,] currentTable = tableList[(int)pieceList.TypeOfPieceInList - 1];
            foreach (Piece piece in pieceList)
            {
                whiteAdvantage -= currentTable[piece.Square.Rank, piece.Square.File];
            }
        }



        return board.IsWhiteToMove ? whiteAdvantage : -whiteAdvantage;
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