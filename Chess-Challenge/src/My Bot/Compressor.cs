using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;


public class Compressor
{

    sbyte[] pawnPSTable =
    {
        0,   0,   0,   0,   0,   0,  0,   0,
        -35,  -1, -20, -23, -15,  24, 38, -22,
        -26,  -4,  -4, -10,   3,   3, 33, -12,
        -27,  -2,  -5,  12,  17,   6, 10, -25,
        -14,  13,   6,  21,  23,  12, 17, -23,
        -6,   7,  26,  31,  65,  56, 25, -20,
        98, 127,  61,  95,  68, 126, 34, -11,
        0,   0,   0,   0,   0,   0,  0,   0,
    };

    sbyte[] knightPSTable =
    {
        -105, -21, -58, -33, -17, -28, -19,  -23,
        -29, -53, -12,  -3,  -1,  18, -14,  -19,
        -23,  -9,  12,  10,  19,  17,  25,  -16,
        -13,   4,  16,  13,  28,  19,  21,   -8,
        -9,  17,  19,  53,  37,  69,  18,   22,
        -47,  60,  37,  65,  84, 127,  73,   44,
        -73, -41,  72,  36,  23,  62,   7,  -17,
        -128, -89, -34, -49,  61, -97, -15, -107,
    };

    sbyte[] bishopPSTable =
    {
        -33,  -3, -14, -21, -13, -12, -39, -21,
        4,  15,  16,   0,   7,  21,  33,   1,
        0,  15,  15,  15,  14,  27,  18,  10,
        -6,  13,  13,  26,  34,  12,  10,   4,
        -4,   5,  19,  50,  37,  37,   7,  -2,
        -16,  37,  43,  40,  35,  50,  37,  -2,
        -26,  16, -18, -13,  30,  59,  18, -47,
        -29,   4, -82, -37, -25, -42,   7,  -8,
    };

    sbyte[] rookPSTable = 
    {
        -19, -13,   1,  17, 16,  7, -37, -26,
        -44, -16, -20,  -9, -1, 11,  -6, -71,
        -45, -25, -16, -17,  3,  0,  -5, -33,
        -36, -26, -12,  -1,  9, -7,   6, -23,
        -24, -11,   7,  26, 24, 35,  -8, -20,
        -5,  19,  26,  36, 17, 45,  61,  16,
        27,  32,  58,  62, 80, 67,  26,  44,
        32,  42,  32,  51, 63,  9,  31,  43,
    };

    sbyte[] queenPSTable = 
    {
        -1, -18,  -9,  10, -15, -25, -31, -50,
        -35,  -8,  11,   2,   8,  15,  -3,   1,
        -14,   2, -11,  -2,  -5,   2,  14,   5,
        -9, -26,  -9, -10,  -2,  -4,   3,  -3,
        -27, -27, -16, -16,  -1,  17,  -2,   1,
        -13, -17,   7,   8,  29,  56,  47,  57,
        -24, -39,  -5,   1, -16,  57,  28,  54,
        -28,   0,  29,  12,  59,  44,  43,  45,
    };

    sbyte[] kingPSTable = 
    {
        -15,  36,  12, -54,   8, -28,  24,  14,
        1,   7,  -8, -64, -43, -16,   9,   8,
        -14, -14, -22, -46, -44, -30, -15, -27,
        -49,  -1, -27, -39, -46, -44, -33, -51,
        -17, -20, -12, -27, -30, -25, -14, -36,
        -9,  24,   2, -16, -20,   6,  22, -22,
        29,  -1, -20,  -7,  -8,  -4, -38, -29,
        -65,  23,  16, -15, -56, -34,   2,  13,
    };
    
     public void PackScoreData()
     {
        //Add boards from "index" 0 upwards. Here, the pawn board is "index" 0.
        //That means it will occupy the least significant byte in the packed data.
        List<sbyte[]> allScores = new();

        allScores.Add(pawnPSTable);
        allScores.Add(knightPSTable);
        allScores.Add(bishopPSTable);
        allScores.Add(rookPSTable);
        allScores.Add(queenPSTable);
        allScores.Add(kingPSTable);

        ulong[] packedData = new ulong[64];
        for (int square  = 0; square < 64; square++)
        {
            for (int set = 0; set < 6; set++)
            {
                sbyte[] thisSet = allScores[set];
                //packedData[square] = ((ulong)thisSet[square]) << (8 * set);
                packedData[square] |= (ulong)(thisSet[square] & 0xFF) << (8 * set);
                // a |= b is the same as a = a|b. we are shifting the new data that will be added to the right, then doing bitwise or
            }
        }
        string output = "{";
        for (int square = 0; square < 64; square ++)
        {
            output = output + packedData[square].ToString();
            output = output + ", ";
            if ((square + 1) % 8 == 0)
            {
                output = output + "\n";
            }
            
        }
        output = output + "}";

        Console.WriteLine(output);

     }

}
