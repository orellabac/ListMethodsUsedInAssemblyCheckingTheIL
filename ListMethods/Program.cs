

namespace UsageAssessmentWithCecil
{
    using Mono.Cecil;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;

    partial class Program
    {
        /// <summary>
        /// Print Usage info
        /// </summary>
        public static void Usage()
        {
            Console.WriteLine("Tool to collect PMEs used by files in the give directory");
            Console.WriteLine("Usage:");
            Console.WriteLine("Tool <directory> [outputreportname default is pmecounts.csv");
        }

        static Dictionary<ReferenceInfo, int> PMEcounts = new Dictionary<ReferenceInfo, int>();
        static List<string> excludeInputFilesPrefixes = new List<string>();
        static List<string> interestingPMEPrefixes = new List<string>();
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                System.Environment.Exit(1);
            }
            string outputReportFileName = args.Length >= 2 ? args[1] : "pmecounts.csv";

            ReadConfigFromAppConfig();

            Console.WriteLine("Processing dir " + args[0]);


            Console.WriteLine("Returning type, assembly and method");

            var files = Directory.GetFiles(args[0], "*.dll", SearchOption.AllDirectories);
            foreach (var file in files)
            {

                var filename = Path.GetFileName(file);
                // Exclude file to analyze by file prefix
                if (excludeInputFilesPrefixes.Any(prefix => filename.StartsWith(prefix)))
                {
                    continue;
                }
                ProcessFile(file);

            }
            var textWriter = new StreamWriter("pmecounts.csv");
            foreach (var pme in PMEcounts)
            {
                var key = pme.Key;
                textWriter.WriteLine("{0}\t{1}\t{2}", key.ModuleFileName, key.MethodCall, pme.Value);
            }
            textWriter.Flush();
            textWriter.Close();
        }

        private static void ReadConfigFromAppConfig()
        {
            try
            {
                excludeInputFilesPrefixes.AddRange(ConfigurationManager.AppSettings[nameof(excludeInputFilesPrefixes)].Split(';'));
                interestingPMEPrefixes.AddRange(ConfigurationManager.AppSettings[nameof(interestingPMEPrefixes)].Split(';'));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error reading configuration " + ex.Message);
            }
        }

        private static bool isInterestingPME(MethodReference methodReference)
        {
            var ns = methodReference.DeclaringType.Namespace;
            var methodName = GetMethodName(methodReference);
            if (interestingPMEPrefixes.Count > 0)
            {
                if (interestingPMEPrefixes.Any(prefix => methodName.StartsWith(prefix)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else 
                return true;
        }

        private static void ProcessFile(string file)
        {
            Console.WriteLine($"Processing {file}");

            ModuleDefinition module = null;

            try
            {
                module = ModuleDefinition.ReadModule(file);

            }
            catch (BadImageFormatException)
            {
                return;
            }

            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        foreach (var instruction in method.Body.Instructions)
                        {
                            if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Call)
                            {

                                var methodCall = instruction.Operand as MethodReference;
                                if (methodCall != null && isInterestingPME(methodCall))
                                {
                                    var methodCallName = GetMethodName(methodCall);
                                    Count(Path.GetFileName(module.FileName), methodCallName);
                                    //Console.WriteLine("{0},{1}", type, methodCallName);
                                }
                            }
                            else if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Callvirt)
                            {
                                MethodReference methodCall = instruction.Operand as MethodReference;
                                if (methodCall != null && isInterestingPME(methodCall))
                                {
                                    var methodCallName = GetMethodName(methodCall);
                                    Count(Path.GetFileName(module.FileName), methodCallName);
                                    //Console.WriteLine("{0},{1}", type, methodCallName);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Count(string moduleFileName, string methodCall)
        {
            var key = new ReferenceInfo(moduleFileName, methodCall);
            if (PMEcounts.TryGetValue(key, out var count))
            {
                PMEcounts[key] = count + 1;
            }
            else
            {
                PMEcounts[key] =  1;
            }
        }

        private static string GetMethodName(MethodReference methodCall)
        {
            return methodCall.DeclaringType.FullName + "." + methodCall.Name;
        }
    }
}
