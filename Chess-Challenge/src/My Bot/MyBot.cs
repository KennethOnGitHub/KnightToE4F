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
using System.Xml.Linq;


public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int debug_negaMaxCalledCount = 0;

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

    public const byte INVALID = 0, EXACT = 1, LOWERBOUND = 2, UPPERBOUND = 3; //this can be refactored to reduce tokens at the cost of readability
    //not sure why we have INVALID tbh
    struct Transposition
    {
        /*
        public Transposition(ulong zHash, int eval, byte d) //Constructor for a transposition
        {
            zobristHash = zHash;
            evaluation = eval;
            depth = d;
            flag = INVALID;
        }*/

        public ulong zobristHash;
        public int evaluation;
        public byte depth;
        public byte flag;
        public Move bestMove;
    }

    ulong transpositionTableMask = 0x7FFFFF; //011111111111111111111111 in binary we will bitwise AND the mask and the zobrist hash to lop off all the digits except for the last 23 (in binary), 

    //COMPRESSOR
    private sbyte[][] PSQT;

    Transposition[] transpositions;
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
                /*
                Console.Write("Type: " + pieceType);
                Console.Write(" |Square: " + square + " | ");
                Console.WriteLine(unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF)));*/

                PSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF));
            }
        }

        transpositions = new Transposition[transpositionTableMask + 1]; // transpositionTableMask + 1 is 100000000000000000000000 in binary

    }


    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine("STARTED THINK");
        debug_negaMaxCalledCount = 0;

        Move bestMove = IterativeDeepening(board, timer);

        Console.WriteLine("search count: " +  debug_negaMaxCalledCount);
        return bestMove;
    }


    bool timeout = false;

    public Move IterativeDeepening(Board board, Timer moveTimer) //tmrw, remove the told temp best move and add move ordering to here as well to resolve the early cutoff problem
    {
        Move[] allMoves = board.GetLegalMoves();
        Move bestMove = allMoves[0];

        int searchDepth = 1; //currently with our implementation we're technically doing a 2ply search since we are evaluating the move after the next move
        timeout = false;
        
        while (true)
        {
            Console.WriteLine("STARTING SEARCH DEPTH:" + searchDepth);
            int bestMoveAdvantage = int.MinValue;
            Move tempBestMove = allMoves[0];   //temp best move so far for this search, but this is may not be the true best move as we have not finished searching yet.
            
            foreach (Move move in allMoves)
            {
                if (timeout)
                {
                    return bestMove; 
                }
                board.MakeMove(move);
                int moveAdvantage = -NegaMax(board, moveTimer, searchDepth, int.MinValue, int.MaxValue - 1);
                board.UndoMove(move);
                if (moveAdvantage > bestMoveAdvantage)
                {
                    Console.WriteLine("FOUND NEW BEST MOVE");
                    bestMoveAdvantage = moveAdvantage;
                    tempBestMove = move;
                }
            }
            Console.WriteLine(bestMoveAdvantage);
            bestMove = tempBestMove; //only once we have finished searching a layer will we update the best move, as we can be sure it is actually better
            searchDepth++;

        }
    }

    public int NegaMax(Board board, Timer moveTimer, int currentDepth, int alpha, int beta)
    {
        debug_negaMaxCalledCount += 1;

        ref Transposition transposition = ref transpositions[board.ZobristKey & transpositionTableMask];
        
        if (transposition.zobristHash == board.ZobristKey  //checks 2 things, that is has been hashed already (zobristHash is initally set to 0 be default) and that the entry we are getting from the table is hopefully the right transposition
            && transposition.depth >= currentDepth) //a transposition with a greater depth means it got its eval from a deeper search, so its more accurate
        {
            if (transposition.flag == EXACT) return transposition.evaluation;

            //If the value stored is a lower bound, and we have found that it is greater than beta, cut off (or at least I think this is what we are doing)
            if (transposition.flag == LOWERBOUND && transposition.evaluation >= beta) return transposition.evaluation;

            //I dont fully understand this tbh
            if (transposition.flag == UPPERBOUND && transposition.evaluation <= alpha) return transposition.evaluation;
        }

        int moveTime = moveTimer.MillisecondsRemaining / 30; //arbitary value 
        if (moveTimer.MillisecondsElapsedThisTurn > moveTime)
        {
            timeout = true;
            return alpha; //is this correct?
        }

        if (board.IsInCheckmate())
        {
            return int.MinValue + 1;
        }
        if (board.IsDraw())
        {
            return 0;
        }

        if (currentDepth == 0) //not perfect, this means a search depth of 1 leads to 2ply search
        {
            return CalculateAdvantage(board);
        }

        Move[] allMoves = board.GetLegalMoves();

        allMoves.OrderByDescending(move => CalculatePriorityOfMove(move, board)); //seen array.sort also used, need to see if that is better
        //also look into not passing board as arg, am too tired to look at the alternative

        Move bestMove = allMoves[0];
        int bestEval = int.MinValue;

        transposition.flag = UPPERBOUND;

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            //bestEval = Math.Max(bestEval, -NegaMax(board, moveTimer, currentDepth - 1, -beta, -alpha));
            int eval = -NegaMax(board, moveTimer, currentDepth - 1, -beta, -alpha);
            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            } 
            board.UndoMove(move);

            if (bestEval > alpha) //look into reworking this to use Max(), but will have to change flags
            {
                alpha = bestEval;
                transposition.flag = EXACT;
            }

            if (alpha >= beta)
            {
                //Console.WriteLine(String.Format("PRUNING | ALPHA: {0} BETA {1}", alpha,beta));
                transposition.flag = LOWERBOUND;
                break; //seen some return beta here, idk why though
            }
        }

        transposition.evaluation = bestEval;
        transposition.zobristHash = board.ZobristKey;
        transposition.depth = (byte)currentDepth;
        transposition.bestMove = bestMove;

        return bestEval;
    }

    public int CalculatePriorityOfMove(Move move, Board board) //We want probably good moves to be checked first for better pruning
    {
        board.MakeMove(move);

        int priority = 0;

        Transposition transP = transpositions[board.ZobristKey & transpositionTableMask];

        if (transP.bestMove == move && board.ZobristKey == transP.zobristHash)
            priority = 10000; //rando big number
        if (move.IsCapture) priority += pieceValues[(int)move.CapturePieceType - 1] - pieceValues[(int)move.MovePieceType - 1]; //seen elseif used instead, not sure

        //could transposition depth also be used? mayb

        board.UndoMove(move);
        return priority;

        //optimise this in terms of tokens later
    }

    public int CalculateAdvantage(Board board)
    {
        int whiteAdvantage = 0;
        ulong bitboard = board.AllPiecesBitboard;
        while (bitboard != 0) //learnt this trick from tyrant <3
        {
            int pieceIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
            Piece piece = board.GetPiece(new Square(pieceIndex));

            whiteAdvantage +=
                (
                PSQT[(int)piece.PieceType - 1] //gets the piece square table of the current piece
                [piece.IsWhite ? pieceIndex : 56 - ((pieceIndex / 8) * 8) + pieceIndex % 8] //gets the square of that piece, flips rank if black
                + pieceValues[(int)piece.PieceType - 1]
                )
                * (piece.IsWhite ? 1 : -1); //negates if black
            //ISSUE: [piece.IsWhite ? pieceIndex : 63 - pieceIndex] is incorrect, we don't want to just do 63-piece index as this flips both rank and file!!!
        };

        return board.IsWhiteToMove ? whiteAdvantage : -whiteAdvantage;
    }
    /*
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
    }*/


}