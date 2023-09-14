using ChessChallenge;
using ChessChallenge.API;
using ChessChallenge.Application;
using System.ComponentModel;

namespace MyBotTest
{
    [TestClass]
    public class EvaluateTestss
    {
        //TODO:
        //CLEAN UP TESTS SO NEW TESTS ARE EASIER TO MAKE AND IT LOOKS A LITTLE NICE

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
        public void CalculateAdvantage_QueenAtStart_910()
        {
            var bot = new MyBot();
            string position = "8/8/8/8/8/8/8/3Q4 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(910, advantage);
        }

        [TestMethod]
        public void CalculateAdvantage_EqualQueenAtStart_0()
        {
            var bot = new MyBot();
            string position = "3q4/8/8/8/8/8/8/3Q4 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(0, advantage);
        }

        [TestMethod]
        public void CalculateAdvantage_BlackQueenOnly_Neg910()
        {
            var bot = new MyBot();
            string position = "3q4/8/8/8/8/8/8/8 w - - 0 1";
            Board board = Board.CreateBoardFromFEN(position);

            int advantage = bot.CalculateAdvantage(board);

            Assert.AreEqual(-910, advantage);
        }
    }
}