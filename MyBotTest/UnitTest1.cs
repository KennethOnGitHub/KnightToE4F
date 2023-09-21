using ChessChallenge;
using ChessChallenge.API;
using ChessChallenge.Application;
using System.ComponentModel;

namespace MyBotTest
{
    [TestClass]
    public class EvaluateTests
    { 
        [TestMethod]
        public void CalculateAdvantage_EmptyBoard_0()
        {
            var bot = new MyBot();
            string position = "8/8/8/8/8/8/8/8 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(0, advantage);
        }

        [TestMethod]
        public void CalculateAdvantage_2Queens_0()
        {
            var bot = new MyBot();
            string position = "8/8/1q6/8/8/1Q6/8/8 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(0, advantage);
        }

        [TestMethod]
        public void CalculateAdvantage_2Pawns_0()
        {
            var bot = new MyBot();
            string position = "8/p7/8/8/8/8/P7/8 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(0, advantage);
        }

        [TestMethod]
        public void CalulcateAdvantage_1Pawn_113()
        {
            var bot = new MyBot();
            string position = "8/8/8/8/8/8/P7/8 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(113, advantage);
        }

        [TestMethod]
        public void CalulcateAdvantage_QueenTopRight_945()
        {
            var bot = new MyBot();
            string position = "7Q/8/8/8/8/8/8/8 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(945, advantage);
        }

        [TestMethod]
        public void CalulcateAdvantage_PawnAndQueen_1167()
        {
            var bot = new MyBot();
            string position = "7Q/P7/8/8/8/8/8/8 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(1167, advantage);
        }

        [TestMethod]
        public void CalulcateAdvantage_BlackQueenBottomRight_neg945()
        {
            var bot = new MyBot();
            string position = "8/8/8/8/8/8/8/7q w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(-945, advantage);
        }
    }
}