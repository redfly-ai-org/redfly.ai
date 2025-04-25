using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyCoreFramework;

public class RedflyConsole
{

    public static StringBuilder GetPasswordFromUser()
    {
        var password = new StringBuilder();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(intercept: true);
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }

    public static async Task ShowWaitAnimation(CancellationToken token)
    {
        int dotCount = 0;
        bool useDots = true;

        try
        {
            Console.CursorVisible = false; // Hide the cursor

            while (!token.IsCancellationRequested)
            {
                if (useDots)
                {
                    Console.Write(".");
                    dotCount++;
                    if (dotCount == 20)
                    {
                        useDots = false;
                        dotCount = 0;
                        Console.Write("\r");
                    }
                }
                else
                {
                    Console.Write("_");
                    dotCount++;
                    if (dotCount == 20)
                    {
                        useDots = true;
                        dotCount = 0;
                        Console.Write("\r");
                    }
                }

                await Task.Delay(700);
            }

            Console.WriteLine();
        }
        finally
        {
            Console.CursorVisible = true; // Ensure the cursor is visible again
        }
    }

}
