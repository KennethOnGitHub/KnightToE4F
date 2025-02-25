﻿using ChessChallenge.API;
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


public class EvilBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 }; //do we need that first 0? could be optimised out

    int debug_negaMaxCalledCount = 0;

    public ulong[] compressedTables =
    {
    17366146244650440448, 2594113994079726336, 864705383484278272, 14555856140260597504, 576470583767985920, 16429383121569637376, 1729409615245470976, 1008822598331263232,
    75718820991656925, 506663723840031743, 17872807851308676332, 13838083924015775209, 15352160939391057905, 17294086516503810584, 649092282722677286, 646556824996277738,
    17439330781821463014, 17440174176295974908, 16932104091068206332, 15132608215315843830, 15276444110593856259, 16355914969945673987, 17437921328845953313, 16571246642289373428,
    14919809804019626981, 18377501121629193470, 16572655056907473147, 15706824830296395020, 15202414005682576401, 15346249913240130310, 16069933099467281674, 14843809387256281319,
    17231035945054959602, 17012608064794464525, 17585981431699673862, 16502849228963460373, 16356760481905845527, 16646684183458301196, 17442990008679469585, 15857697663777380073,
    17824957002454979066, 1757557169678793735, 168042790623323418, 17312945310299341087, 17021614401232524353, 447270576339451704, 1608372409069816089, 16885378433737960684,
    2125450431879886690, 18410715109724837759, 17041600078762887229, 17978362021232714847, 17906304354468370244, 18194538342471384702, 15744542636541871906, 16393070990471589877,
    13763211447769464832, 1657349952344729344, 1152939221895273984, 17366145197852577536, 14411738964386331904, 15997030157144989440, 144117572303319296, 936763210155463936,
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
    private sbyte[][] mgPSQT;
    private sbyte[][] egPSQT;

    Transposition[] transpositions;
    public EvilBot()
    {
        var compressor = new Compressor();
        compressor.PackScoreData();

        mgPSQT = new sbyte[6][];//this can be changed in the future, we don't have to stick to a jagged array of 1d arrays
        egPSQT = new sbyte[6][]; //we could instead have the reading process be different, but this might be faster
        for (int pieceType = 0; pieceType < 6; pieceType++)
        {
            mgPSQT[pieceType] = new sbyte[64]; //don't like this but this is how you do jagged arrays :/
            egPSQT[pieceType] = new sbyte[64];
            for (int square = 0; square < 64; square++)
            {
                /*
                Console.Write("Type: " + pieceType);
                Console.Write(" |Square: " + square + " | ");
                Console.WriteLine(unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF)));*/

                mgPSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF));
                if (pieceType == 0) //if it's a pawn
                {
                    egPSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * 6)) & 0xFF));
                }
                else if (pieceType == 5) //if its a king
                {
                    egPSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * 7)) & 0xFF));
                }
                else
                {
                    egPSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF));
                }

            }
        }

        for (int pieceType = 0; pieceType < 6; pieceType++)
        {
            Console.WriteLine("PIECETYPE:" + pieceType);
            foreach (sbyte score in egPSQT[pieceType])
            {
                Console.WriteLine(score);
            }


        }


        transpositions = new Transposition[transpositionTableMask + 1]; // transpositionTableMask + 1 is 100000000000000000000000 in binary

    }

    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine("STARTED THINK");
        debug_negaMaxCalledCount = 0;

        Move bestMove = IterativeDeepening(board, timer);

        Console.WriteLine("search count: " + debug_negaMaxCalledCount);
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
                mgPSQT[(int)piece.PieceType - 1] //gets the piece square table of the current piece
                [piece.IsWhite ? pieceIndex : 56 - ((pieceIndex / 8) * 8) + pieceIndex % 8] //gets the square of that piece, flips rank if black
                + pieceValues[(int)piece.PieceType]
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