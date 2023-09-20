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
    }
}