using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RogueSquadLib.BaseServices
{
    internal class Instrument
    {
        public static bool Enabled { get; set; } = true;
        private static Dictionary<string, double> StartPoints { get; set; } = new Dictionary<string,double>();
        private static Dictionary<string, DateTime> StartTimes { get; set; } = new Dictionary<string, DateTime>();
        private static Stopwatch st;

        public static void Start(string token, string msg = "")
        {
            if (Enabled)
            {
                if (st == null)
                {
                    st = new Stopwatch();
                    st.Start();
                }

                var stamp = st.ElapsedMilliseconds;
                StartPoints.Add(token, st.ElapsedMilliseconds);
                Debug.WriteLine($"Start:{msg}");
            }
        }

        public static void Stop(string token, string msg = "")
        {
            if (Enabled)
            {
                var start = StartPoints[token];
                Debug.WriteLine($"Stop: [{st.ElapsedMilliseconds-start}ms] :{msg}");
            }
        }

        public static void StartTiming(string token)
        {
            StartTimes.Add(token, DateTime.Now);
        }

        public static void StopTiming(string token)
        {
            var start = StartTimes[token];
            var msg =  GetElapsedTimeString(start, DateTime.Now);
            Debug.WriteLine(msg);
        }

        public static string GetElapsedTimeString(DateTime Start, DateTime Stop)
        {
            var ms = (Stop - Start).TotalMilliseconds;
            var micro = (Stop - Start).TotalMicroseconds();
            var ns = (Stop - Start).TotalNanoseconds();

            bool useMs = ms > 1;
            bool useMicro = !useMs && micro > 1;
            bool useNano = !useMicro;

            string retval = useMs ? $"{Math.Round(ms, 2)}ms" : useMicro ? $"{micro}μs" : $"{ns}ns";
            return retval;

        }

    }

    public static class DateTimeExtensions
    {
        /// <summary>
        /// The number of ticks per microsecond.
        /// </summary>
        public const int TicksPerMicrosecond = 10;
        /// <summary>
        /// The number of ticks per Nanosecond.
        /// </summary>
        public const int NanosecondsPerTick = 100;

        /// <summary>
        /// Gets the microsecond fraction of a DateTime.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int TotalMicroseconds(this TimeSpan self)
        {
            return (int)Math.Floor(
               (self.Ticks
               % TimeSpan.TicksPerMillisecond)
               / (double)TicksPerMicrosecond);
        }
        /// <summary>
        /// Gets the Nanosecond fraction of a DateTime.  Note that the DateTime
        /// object can only store nanoseconds at resolution of 100 nanoseconds.
        /// </summary>
        /// <param name="self">The DateTime object.</param>
        /// <returns>the number of Nanoseconds.</returns>
        public static int TotalNanoseconds(this TimeSpan self)
        {
            return (int)(self.Ticks % TimeSpan.TicksPerMillisecond % TicksPerMicrosecond)
               * NanosecondsPerTick;
        }
    }
}
