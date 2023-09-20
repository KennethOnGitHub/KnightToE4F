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
    int baseMaxDepth = 5;
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool botIsWhite;

    public ulong[] compressedTables =
    {
        266081509807872, 40608714320640, 14255029143040, 222144599154432, 9831464562432, 251680922067968, 27358335200512, 16281800272128,
        2052256490461, 8765760850943, 272730088862956, 211118966504937, 234234615169009, 263947401105944, 10986427904550, 8803491900906,
        267124736059878, 266094280439804, 258342015405308, 231992374266614, 234174553133827, 248498219585795, 265046644103457, 251813379633396,
        228663471305701, 281367167173886, 252853114179835, 239654865079564, 231988516756497, 234182975165190, 245204079219978, 226490419837159,
        263770735441906, 260472402415885, 269311748018950, 252819394409749, 249585249625367, 254060790105356, 267176896827921, 241900829153001,
        272627275977210, 27415097457671, 2229527061786, 263917757022495, 259609585734721, 6838346219320, 24392145127705, 257530819128556,
        32882737723234, 281308010960767, 260563769641021, 273783746405471, 273711019988804, 277322871291518, 239814231328546, 249821819695093,
        210986525229056, 25289472386816, 17717288427008, 265034711944960, 220156800744704, 244280724987648, 2384227463424, 14487662400768,
    };

    //COMPRESSOR
    private sbyte[][] PSQT;
    public MyBot()
    {
        var compressor = new Compressor();
        compressor.PackScoreData();

        PSQT = new sbyte[6][];//this can be changed in the future, we don't have to stick to a jagged array of 1d arrays
        for (int pieceType = 0; pieceType < 6; pieceType++)
        {
            PSQT[pieceType] = new sbyte[64]; //don't like this but this is how you do jagged arrays :/
            for (int square = 0; square < 64; square++)
            {
                Console.Write("Type: " + pieceType);
                Console.Write(" |Square: " + square + " | ");
                Console.WriteLine(unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF)));

                PSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF));
            }
        }
    }

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

    public int CalculatePieceSquareAdvantage(Board board)
    {
        int whiteAdvantage = 0;
        ulong bitboard = board.AllPiecesBitboard;
        while (bitboard != 0) //learnt this trick from tyrant <3
        {
            int pieceIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
            Piece piece = board.GetPiece(new Square(pieceIndex));

            whiteAdvantage +=
                PSQT[(int)piece.PieceType - 1] //gets the piece square table of the current piece
                [piece.IsWhite ? pieceIndex : 56 - ((pieceIndex/8) * 8) + pieceIndex % 8] //gets the square of that piece, flips rank if black
                * (piece.IsWhite ? 1:-1); //negates when black
            //ISSUE: [piece.IsWhite ? pieceIndex : 63 - pieceIndex] is incorrect, we don't want to just do 63-piece index as this flips both rank and file!!!
        }

        return board.IsWhiteToMove ? whiteAdvantage : -whiteAdvantage;
    }
}