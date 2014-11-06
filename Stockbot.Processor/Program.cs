﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stockbot.Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Enter username:");
            //string username = Console.ReadLine();
           // Console.WriteLine("Enter username:");
            //string password = ReadPassword();
            string username = "z";
            string password = "x";
            ScottradeConnector sc = new ScottradeConnector( );
            bool result = sc.TryConnectToService("", username, password);
            Console.WriteLine("{0}", result ? "success!" : "Failure! :(");

            Console.WriteLine("press any key to end");
            Console.ReadKey();
        }

        //utility function for masked command-line input
        static string ReadPassword(char mask)
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr = (char)0;

            while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push((char)chr);
                    System.Console.Write(mask);
                }
            }

            System.Console.WriteLine();

            return new string(pass.Reverse().ToArray());
        }

        /// <summary>
        /// Like System.Console.ReadLine(), only with a mask.
        /// </summary>
        /// <returns>the string the user typed in </returns>
        public static string ReadPassword()
        {
            return ReadPassword('*');
        }
    }
}
