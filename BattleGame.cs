using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Battle
{
    internal class BattleGame
    {
        private int playerHP = 100;
        private int enemyHP = 300;
        private int tick = 0;
        private bool gameRunning = true;
        private bool isCasting = false;
        private bool rageEvent = false;
        private Random random = new Random();

        private string logFile = "battle.log";
        private string saveFile = "GameSaveFile";

        private object logLock = new object();

        public async Task Start()
        {
            CheckFilesAtStart();

            Task clockTask = GameClock();
            Task eventsTask = RandomEvents();

            while (gameRunning)
            {
                Console.Write("\nВведите команду (attack, heal, cast, stats, log, exit): ");
                string command = Console.ReadLine().ToLower();

                switch (command)
                {
                    case "attack":
                        {
                            await Attack();
                            break;
                        }
                    case "heal":
                        {
                            await Heal();
                            break;
                        }
                    case "cast":
                        {
                            await CastSpell();
                            break;
                        }
                    case "stats":
                        {
                            ShowStats();
                            break;
                        }
                    case "log":
                        {
                            ShowLog();
                            break;
                        }
                    case "exit":
                        {
                            gameRunning = false;
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Неизвестная команда.");
                            break;
                        }
                }

                if (playerHP <= 0 || enemyHP <= 0)
                {
                    gameRunning = false;
                    EndGame();
                }
            }

            await Task.WhenAll(clockTask, eventsTask);
        }

        private void CheckFilesAtStart()
        {
            if (!File.Exists(logFile) || !File.Exists(saveFile))
            {
                File.Create(logFile).Close();
                File.Create(saveFile).Close();
                Console.WriteLine("Новая игра начата.");
            }
            else
            {
                Console.WriteLine("Обнаружены сохраненные данные.");
                Console.WriteLine("1 - Начать заново");
                Console.WriteLine("2 - Продолжить");
                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    File.Delete(logFile);
                    File.Delete(saveFile);
                    File.Create(logFile).Close();
                    File.Create(saveFile).Close();
                    Console.WriteLine("Новая игра начата.");
                }
                else if (choice == "2")
                {
                    string history = GetHistory();
                    Console.WriteLine("История: " + history);
                }
                else
                {
                    Console.WriteLine("Некорректный выбор. Начинаем новую игру.");
                    File.Delete(logFile);
                    File.Delete(saveFile);
                    File.Create(logFile).Close();
                    File.Create(saveFile).Close();
                }
            }
        }

        private async Task GameClock()
        {
            while (gameRunning)
            {
                tick++;
                await Task.Delay(1000);
            }
        }
        private async Task RandomEvents()
        {
            while (gameRunning)
            {
                int delay = random.Next(2, 6);
                await Task.Delay(delay * 1000);

                if (!gameRunning)
                {
                    break;
                }

                int eventType = random.Next(0, 3);

                if (eventType == 0) 
                {
                    int damage = random.Next(2, 7);
                    playerHP -= damage;
                    if (playerHP < 0)
                    {
                        playerHP = 0;
                    }
                    rageEvent = true;
                    WriteToLog("EVENT", "kind=RAGE | hpDelta=-" + damage + " | playerHP=" + playerHP);
                    Console.WriteLine("\nУдар врага -" + damage + " HP");
                }
                else if (eventType == 1)
                {
                    int heal = random.Next(1, 5);
                    playerHP += heal;
                    if (playerHP > 100)
                    {
                        playerHP = 100;
                    }
                    WriteToLog("EVENT", "kind=BANDAGE | hpDelta=" + heal + " | playerHP=" + playerHP);
                    Console.WriteLine("\nНайден бинт! +" + heal + " HP");
                }
                else
                {
                    WriteToLog("EVENT", "kind=CALM | hpDelta=0 | playerHP=" + playerHP);
                    Console.WriteLine("\nТишина");
                }

                if (isCasting && rageEvent)
                {
                    WriteToLog("CAST_CANCELLED", "reason=rage");
                    Console.WriteLine("Каст прерван!");
                    isCasting = false;
                    rageEvent = false;
                }
            }
        }

        private async Task Attack()
        {
            int damage = random.Next(8, 16);
            enemyHP -= damage;
            if (enemyHP < 0)
            {
                enemyHP = 0;
            }
            WriteToLog("ATTACK", "dmg=" + damage + " | enemyHP=" + enemyHP + " | playerHP=" + playerHP);
            Console.WriteLine("\nВы наносите " + damage + " урона! HP врага: " + enemyHP);
        }

        private async Task Heal()
        {
            int heal = random.Next(5, 13);
            playerHP += heal;
            if (playerHP > 100)
            {
                playerHP = 100;
            }
            WriteToLog("HEAL", "value=" + heal + " | playerHP=" + playerHP);
            Console.WriteLine("\nВы лечитесь на " + heal + " HP! Ваше HP: " + playerHP);
        }

        private async Task CastSpell()
        {
            if (isCasting)
            {
                Console.WriteLine("\nКаст уже идет!");
                return;
            }

            isCasting = true;
            WriteToLog("CAST_START", "durationMs=3000");
            Console.WriteLine("\nКаст начался");

            for (int i = 0; i < 3; i++)
            {
                if (!isCasting)
                {
                    break;
                }
                await Task.Delay(1000);
            }

            if (isCasting)
            {
                int bonusDamage = 25;
                enemyHP -= bonusDamage;
                if (enemyHP < 0)
                {
                    enemyHP = 0;
                }
                WriteToLog("CAST_SUCCESS", "bonusDmg=" + bonusDamage + " | enemyHP=" + enemyHP);
                Console.WriteLine("\nКаст завершен! +" + bonusDamage + " урона! HP врага: " + enemyHP);
                isCasting = false;
            }
        }

        private void ShowStats()
        {
            Console.WriteLine("\nСтатистика:");
            Console.WriteLine("Ваше HP: " + playerHP);
            Console.WriteLine("HP врага: " + enemyHP);
            Console.WriteLine("Тик: " + tick);
        }

        private void ShowLog()
        {
            if (!File.Exists(logFile))
            {
                Console.WriteLine("\nФайл лога не найден.");
                return;
            }

            string[] lines = File.ReadAllLines(logFile);
            Console.WriteLine("\n--- Последние 10 строк лога ---");

            int startIndex = lines.Length - 10;
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            for (int i = startIndex; i < lines.Length; i++)
            {
                Console.WriteLine(lines[i]);
            }
        }

        private void WriteToLog(string type, string details)
        {
            lock (logLock)
            {
                string time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string line = time + " | " + type + " | " + details;
                File.AppendAllText(logFile, line + Environment.NewLine);
            }
        }

        private string GetHistory()
        {
            if (!File.Exists(logFile))
                return "ATTACK=0, HEAL=0, EVENT=0, CAST_CANCELLED=0, CAST_SUCCESS=0";

            string[] lines = File.ReadAllLines(logFile);
            int attack = 0, heal = 0, events = 0, castCancelled = 0, castSuccess = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains("TYPE=ATTACK"))
                {
                    attack++;
                }
                else if (line.Contains("TYPE=HEAL"))
                {
                    heal++;
                }
                else if (line.Contains("TYPE=EVENT"))
                {
                    events++;
                }
                else if (line.Contains("TYPE=CAST_CANCELLED"))
                {
                    castCancelled++;
                }
                else if (line.Contains("TYPE=CAST_SUCCESS"))
                {
                    castSuccess++;
                }
            }

            return "ATTACK=" + attack + ", HEAL=" + heal + ", EVENT=" + events +
                   ", CAST_CANCELLED=" + castCancelled + ", CAST_SUCCESS=" + castSuccess;
        }

        private void EndGame()
        {
            string winner = (playerHP > enemyHP) ? "Игрок" : "Противник";
            Console.WriteLine("\nБой закончен. Победил: " + winner);

            string finalHistory = GetHistory();
            Console.WriteLine("История: " + finalHistory);

            File.Delete(logFile);
            File.Delete(saveFile);
        }
    }

}
