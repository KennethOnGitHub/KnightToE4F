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

    /*
    public ulong[] PackedTables =
    {
    0xFFFFBEE41FE28000, 0x17002A03A700, 0x101D1FADDE00, 0xFFFFF10C32DACF00, 0xFFFFC83B3EE73D00, 0xFFFFDE2C08D59F00, 0x22B1F06F100, 0xD2D2AF79500,
    0x1CE81AE5B762, 0xFFFFFED9200FD77F, 0xFFFFEBFB39EE483D, 0xFFFFF9013DF3245F, 0xFFFFF7F0501E1744, 0xFFFFFC39433B3E7E, 0xFFFFDA1C1A120722, 0xFFFFE3362BD0EEF5,
    0xFFFFF6F2FAEFD0FA, 0x17EF13253C07, 0x2071A2B251A, 0xFFFFF0082428411F, 0xFFFFEC1D11235441, 0x6382D327F38, 0x162F3D254919, 0xFFFFEA390FFE2BEC,
    0xFFFFEEE4E7FBF6F2, 0xFFFFEBE4F505110D, 0xFFFFF3F007131306, 0xFFFFE4F01A323515, 0xFFFFE1FF18252517, 0xFFFFE7112325450C, 0xFFFFF1FDF8071211, 0xFFFFDC00EBFE15E9,
    0xFFFFCEF6DBF9F2E5, 0xFFFFFEE5E60D03FE, 0xFFFFE4F6F40D0FFB, 0xFFFFD8F5FF1A0D0C, 0xFFFFD1FE09221C11, 0xFFFFD3FBF90C1306, 0xFFFFDF03060A150A, 0xFFFFCCFCE903F7E7,
    0xFFFFF1F1D2FFE8E6, 0xFFFFF201E70EF6FC, 0xFFFFE9F4F00F0BFC, 0xFFFFD1FDEF0F09F6, 0xFFFFD3FB030E1303, 0xFFFFE202001B1103, 0xFFFFF10DFB121921, 0xFFFFE504DF09EFF4,
    0xDCD403E2DD, 0x6F7F00ECAFF, 0xFFFFF80AEC0FF3EC, 0xFFFFC001F6FFFCE9, 0xFFFFD507FF06FEF1, 0xFFFFF00F0B151218, 0x8FCFA20F226, 0x800B900ECEA,
    0xFFFFF0FEECDE9700, 0x23EDF2FCEB00, 0xBF700F1C600, 0xFFFFCA0A10EADF00, 0x7F10FF2EF00, 0xFFFFE3E706F3E400, 0x17E0DAD8ED00, 0xDCDE5EAE900
    }; */

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

    public int CalculatePieceSquareAdvantage(Board board)  //shitass af fix later!!!
    {
        /*
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
        }*/

        int whiteAdvantage = 0;
        ulong bitboard = board.AllPiecesBitboard;
        while (bitboard != 0) //learnt this trick from tyrant <3
        {
            int pieceIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
            Piece piece = board.GetPiece(new Square(pieceIndex));

            whiteAdvantage += PSQT[(int)piece.PieceType - 1][piece.IsWhite ? pieceIndex : 63 - pieceIndex] * (piece.IsWhite ? 1:-1);
            //ISSUE: [piece.IsWhite ? pieceIndex : 63 - pieceIndex] is incorrect, we don't want to just do 63-piece index as this flips both rank and file!!!
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