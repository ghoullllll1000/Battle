using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battle
{
    internal class Program
    {
        static async Task Main()
        {
            BattleGame game = new BattleGame();
            await game.Start();
            Console.WriteLine("\nИгра завершена.");
        }
    }
}
