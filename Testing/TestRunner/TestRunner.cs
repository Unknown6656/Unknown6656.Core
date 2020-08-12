#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System.IO;
using System;

public static class __module
{
    public const bool DEBUG__RETHROW_ON_FAILED_TEST = false;


    public static int Main(string[] argv) => Unknown6656.Testing.UnitTestRunner.RunTests(argv.Length > 0 ? Assembly.LoadFrom(argv[0]) : typeof(__module).Assembly);
}

namespace Unknown6656.Testing
{
    using static Console;


    public abstract class UnitTestRunner
    {
        public virtual void Test_StaticInit()
        {
        }

        public virtual void Test_StaticCleanup()
        {
        }

        [TestInitialize]
        public virtual void Test_Init()
        {
        }

        [TestCleanup]
        public virtual void Test_Cleanup()
        {
        }

        public static void Skip() => throw new SkippedException();

        private static void AddTime(ref long target, Stopwatch sw)
        {
            sw.Stop();
            target += sw.ElapsedTicks;
            sw.Restart();
        }

        private static void Print(string text, ConsoleColor color)
        {
            ForegroundColor = color;
            Write(text);
        }

        private static void PrintLine(string text, ConsoleColor color) => Print(text + '\n', color);

        private static void PrintHeader(string text, int width)
        {
            int rw = width - text.Length - 2;
            string ps = new string('=', rw / 2);

            WriteLine($"{ps} {text} {ps}{(rw % 2 == 0 ? "" : "=")}");
        }

        private static void PrintColorDescription(ConsoleColor col, string desc)
        {
            Print("       ### ", col);
            PrintLine(desc, ConsoleColor.White);
        }

        private static void PrintGraph(int padding, int width, string descr, params (double v, ConsoleColor c)[] values)
        {
            double sum = values.Sum(t => t.v);

            width -= 2;
            values = (from v in values
                      select (v.v / sum * width, v.c)).ToArray();

            double max = values.Max(t => t.v);
            int rem = width - values.Sum(t => (int)t.v);
            (double, ConsoleColor) elem = values.First(t => t.v == max);
            int ndx = Array.IndexOf(values, elem);

            // this is by value not by reference!
            elem = values[ndx];
            elem.Item1 += rem;
            values[ndx] = elem;

            Print($"{new string(' ', padding)}[", ConsoleColor.White);

            foreach ((double, ConsoleColor) v in values)
                Print(new string('#', (int)v.Item1), v.Item2);

            PrintLine($"] {descr ?? ""}", ConsoleColor.White);
        }

        public static int RunTests(Assembly asm)
        {
            #region REFLECTION + INVOCATION

            ForegroundColor = ConsoleColor.White;
            OutputEncoding = Encoding.Default;

            List<(string Name, int Passed, int Failed, int Skipped, long TimeCtor, long TimeInit, long TimeMethod)> partial_results = new List<(string, int, int, int, long, long, long)>();
            int passed = 0, failed = 0, skipped = 0;
            Stopwatch sw = new Stopwatch();
            long swc, swi, swm;
            Type[] types = (from t in asm.GetTypes()
                            let attr = t.GetCustomAttributes<TestClassAttribute>(true).FirstOrDefault()
                            where attr is { }
                            orderby t.Name ascending
                            orderby t.GetCustomAttributes<PriorityAttribute>(true).FirstOrDefault()?.Priority ?? 0 descending
                            select t).ToArray();

            WriteLine($@"
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ UNIT TESTS ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Testing {types.Length} type(s):
{string.Concat(types.Select(t => $"    [{new FileInfo(t.Assembly.Location).Name}] {t.FullName}\n"))}".TrimStart());

            foreach (Type t in types)
            {
                sw.Restart();
                swc = swi = swm = 0;

                bool skipclass = t.GetCustomAttributes<SkipAttribute>(true).FirstOrDefault() != null;
                dynamic? container = skipclass ? null : Activator.CreateInstance(t);
                MethodInfo? sinit = t.GetMethod(nameof(Test_StaticInit));
                MethodInfo? scleanup = t.GetMethod(nameof(Test_StaticInit));
                MethodInfo? init = t.GetMethod(nameof(Test_Init));
                MethodInfo? cleanup = t.GetMethod(nameof(Test_Cleanup));
                int tp = 0, tf = 0, ts = 0, pleft, ptop, rptop;

                WriteLine($"Testing class '{t.FullName}'");

                sinit?.Invoke(container, new object[0]);

                AddTime(ref swc, sw);

                IEnumerable<(MethodInfo method, object[] args)> get_methods()
                {
                    foreach (MethodInfo nfo in t.GetMethods().OrderBy(m => m.Name))
                        if (nfo.GetCustomAttributes<TestMethodAttribute>()?.FirstOrDefault() is { })
                        {
                            TestWithAttribute[] attr = nfo.GetCustomAttributes<TestWithAttribute>()?.ToArray() ?? new TestWithAttribute[0];

                            if (attr.Length > 0)
                                foreach (TestWithAttribute tw in attr)
                                    if (nfo.ContainsGenericParameters)
                                    {
                                        ParameterInfo[] pars = nfo.GetParameters();
                                        List<Type> types = new List<Type>();

                                        for (int i = 0; i < pars.Length; ++i)
                                            if (pars[i].ParameterType.IsGenericParameter)
                                                types.Add(tw.Arguments[i].GetType());

                                        MethodInfo concrete = nfo.MakeGenericMethod(types.ToArray());

                                        yield return (concrete, tw.Arguments);
                                    }
                                    else
                                        yield return (nfo, tw.Arguments);
                            else
                                yield return (nfo, new object[0]);
                        }
                }

                foreach ((MethodInfo nfo, object[] args) in get_methods())
                {
                    Write("\t[");
                    ptop = CursorTop;
                    pleft = CursorLeft;
                    Write($"    ] Testing '{nfo.Name}({string.Join(", ", nfo.GetParameters().Select(p => p.ParameterType.FullName))})' with ({string.Join(", ", args)})");
                    rptop = CursorTop;

                    void WriteResult(ConsoleColor clr, string text)
                    {
                        int ttop = CursorTop;

                        ForegroundColor = clr;
                        CursorLeft = pleft;
                        CursorTop = ptop;

                        WriteLine(text);

                        ForegroundColor = ConsoleColor.White;
                        CursorTop = rptop + 1;
                    }

                    try
                    {
                        if ((nfo.GetCustomAttributes<SkipAttribute>().FirstOrDefault() != null) || skipclass)
                            Skip();

                        init?.Invoke(container, new object[0]);

                        AddTime(ref swi, sw);

                        nfo.Invoke(container, args);

                        AddTime(ref swm, sw);

                        cleanup?.Invoke(container, new object[0]);

                        AddTime(ref swi, sw);

                        WriteResult(ConsoleColor.Green, "PASS");

                        ++passed;
                        ++tp;
                    }
                    catch (Exception ex)
                    when ((ex is SkippedException) || (ex?.InnerException is SkippedException))
                    {
                        WriteResult(ConsoleColor.Yellow, "SKIP");

                        ++skipped;
                        ++ts;
                    }
                    catch (Exception ex)
                    {
                        if (ex is TargetInvocationException { InnerException: { } ie } && __module.DEBUG__RETHROW_ON_FAILED_TEST)
                        {
                            ExceptionDispatchInfo.Capture(ie).Throw();

                            throw;
                        }

                        WriteResult(ConsoleColor.Red, "FAIL");

                        ++failed;
                        ++tf;

                        ForegroundColor = ConsoleColor.Red;

                        while (ex?.InnerException is { })
                        {
                            ex = ex.InnerException;

                            WriteLine($"\t\t  [{ex.GetType()}] {ex.Message}\n{string.Join("\n", ex.StackTrace?.Split('\n').Select(x => $"\t\t{x}") ?? new string[0])}");
                        }

                        ForegroundColor = ConsoleColor.White;
                    }

                    AddTime(ref swm, sw);
                }

                scleanup?.Invoke(container, new object[0]);

                AddTime(ref swc, sw);

                partial_results.Add((t.FullName!, tp, ts, tf, swc, swi, swm));
            }

            #endregion
            #region PRINT RESULTS

            const int wdh = 110;
            int total = passed + failed + skipped;
            double time = partial_results.Select(r => r.TimeCtor + r.TimeInit + r.TimeMethod).Sum();
            double pr = passed / (double)total;
            double sr = skipped / (double)total;
            double tr;
            const int i_wdh = wdh - 35;

            WriteLine();
            PrintHeader("TEST RESULTS", wdh);

            PrintGraph(0, wdh, "", (pr, ConsoleColor.Green),
                                   (sr, ConsoleColor.Yellow),
                                   (1 - pr - sr, ConsoleColor.Red));
            Print($@"
    MODULES: {partial_results.Count,3}
    TOTAL:   {passed + failed + skipped,3}
    PASSED:  {passed,3} ({pr * 100,7:F3} %)
    SKIPPED: {skipped,3} ({sr * 100,7:F3} %)
    FAILED:  {failed,3} ({(1 - pr - sr) * 100,7:F3} %)
    TIME:    {time * 1000d / Stopwatch.Frequency,9:F3} ms
    DETAILS:", ConsoleColor.White);

            foreach (var res in partial_results)
            {
                double mtime = res.TimeCtor + res.TimeInit + res.TimeMethod;
                double tot = res.Passed + res.Failed + res.Skipped;

                pr = res.Passed / tot;
                sr = res.Failed / tot;
                tr = mtime / time;

                double tdt_ct = res.TimeCtor / mtime;
                double tdt_in = res.TimeInit / mtime;
                double tdt_tt = res.TimeMethod / mtime;

                WriteLine($@"
        MODULE:  {res.Name}
        PASSED:  {res.Passed,3} ({pr * 100,7:F3} %)
        SKIPPED: {res.Failed,3} ({sr * 100,7:F3} %)
        FAILED:  {res.Skipped,3} ({(1 - pr - sr) * 100,7:F3} %)
        TIME:    {mtime * 1000d / Stopwatch.Frequency,9:F3} ms ({tr * 100d,7:F3} %)
            CONSTRUCTORS AND DESTRUCTORS: {res.TimeCtor * 1000d / Stopwatch.Frequency,9:F3} ms ({tdt_ct * 100d,7:F3} %)
            INITIALIZATION AND CLEANUP:   {res.TimeInit * 1000d / Stopwatch.Frequency,9:F3} ms ({tdt_in * 100d,7:F3} %)
            METHOD TEST RUNS:             {res.TimeMethod * 1000d / Stopwatch.Frequency,9:F3} ms ({tdt_tt * 100d,7:F3} %)");
                PrintGraph(8, i_wdh, "TIME/TOTAL", (tr, ConsoleColor.Magenta),
                                                   (1 - tr, ConsoleColor.Black));
                PrintGraph(8, i_wdh, "TIME DISTR", (tdt_ct, ConsoleColor.DarkBlue),
                                                   (tdt_in, ConsoleColor.Blue),
                                                   (tdt_tt, ConsoleColor.Cyan));
                PrintGraph(8, i_wdh, "PASS/SKIP/FAIL", (res.Passed, ConsoleColor.Green),
                                                       (res.Failed, ConsoleColor.Yellow),
                                                       (res.Skipped, ConsoleColor.Red));
            }

            WriteLine("\n    GRAPH COLORS:");
            PrintColorDescription(ConsoleColor.Green, "Passed test methods");
            PrintColorDescription(ConsoleColor.Yellow, "Skipped test methods");
            PrintColorDescription(ConsoleColor.Red, "Failed test methods");
            PrintColorDescription(ConsoleColor.Magenta, "Time used for testing (relative to the total time)");
            PrintColorDescription(ConsoleColor.DarkBlue, "Time used for the module's static and instance constructors/destructors (.cctor, .ctor and .dtor)");
            PrintColorDescription(ConsoleColor.Blue, "Time used for the test initialization and cleanup method (@before and @after)");
            PrintColorDescription(ConsoleColor.Cyan, "Time used for the test method (@test)");
            WriteLine();
            //PrintHeader("DETAILED TEST METHOD RESULTS", wdh);
            //WriteLine();
            WriteLine(new string('=', wdh));

            //if (Debugger.IsAttached)
            //{
            //    WriteLine("\nPress any key to exit ....");
            //    ReadKey(true);
            //}

            return failed; // NO FAILED TEST --> EXITCODE = 0

            #endregion
        }
    }

    public static class AssertExtensions
    {
        public static void AreSequentialEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual) => Assert.IsTrue(expected.SequenceEqual(actual));

        public static void AreSetEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            T[] a1 = expected.ToArray();
            T[] a2 = actual.ToArray();

            Assert.AreEqual(a1.Length, a2.Length);

            AreSequentialEqual(a1.Except(a2), new T[0]);
            AreSequentialEqual(a2.Except(a1), new T[0]);
        }
    }

    public sealed class SkippedException
        : Exception
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestWithAttribute
        : Attribute
    {
        public object[] Arguments { get; }


        public TestWithAttribute(params object[] args) => Arguments = args;
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SkipAttribute
        : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TestingPriorityAttribute
            : Attribute
        {
            public uint Priority { get; }


            public TestingPriorityAttribute(uint p = 0) => Priority = p;
        }
}
