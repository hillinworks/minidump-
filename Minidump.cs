using System;
using System.Diagnostics;
using System.IO;
using CommandLine;

namespace Hillinworks
{
    public class Minidump
    {
        public static void Create(string path = null, DumpLevel level = DumpLevel.Minimal)
        {
            var executable = typeof(Minidump).Assembly.GetName().Name + ".exe";
            var arguments = $"\"{path}\" --level {level}";
            var process = Process.Start(executable, arguments);
            Debug.Assert(process != null, nameof(process) + " != null");
            process.WaitForExit();
        }

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Execute);
            Console.ReadKey();
        }

        private static void Execute(Options options)
        {
            var parentProcessId = Win32.GetParentProcessId(Process.GetCurrentProcess().Handle);

            if (parentProcessId == 0)
            {
                throw new InvalidOperationException();
            }

            var parentProcess = Process.GetProcessById(parentProcessId);

            var path = options.Path;
            if (string.IsNullOrEmpty(path))
            {
                var time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                path = $"{time}.dmp";
            }
            else
            {
                var directory = Path.GetDirectoryName(path);
                Debug.Assert(directory != null);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            Win32.MINIDUMP_TYPE dumpType;
            switch (options.Level)
            {
                case DumpLevel.Minimal:
                    dumpType = Win32.MINIDUMP_TYPE.MiniDumpNormal;
                    break;
                case DumpLevel.WithDataSeg:
                    dumpType = Win32.MINIDUMP_TYPE.MiniDumpWithDataSegs;
                    break;
                case DumpLevel.Full:
                    dumpType = Win32.MINIDUMP_TYPE.MiniDumpWithFullMemory;
                    break;
                default:
                    throw new NotSupportedException();
            }

            using (var file = File.Create(path))
            {
                Debug.Assert(file.SafeFileHandle != null);

                Win32.MiniDumpWriteDump(
                    parentProcess.Handle,
                    parentProcessId,
                    file.SafeFileHandle.DangerousGetHandle(),
                    dumpType,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }

    internal class Options
    {
        [Value(0)]
        public string Path { get; set; }

        [Option('l', "level", Default = DumpLevel.Minimal)]
        public DumpLevel Level { get; set; }
    }
}