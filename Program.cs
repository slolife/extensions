using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using extensions.Models;

namespace extensions
{
    class Program
    {
        private static readonly Dictionary<string, ExtensionData> _extensions = new Dictionary<string, ExtensionData>();
        private static CommandOptions _options;
        private static List<string> _removeExtensions = new List<string>();
        private static long _filesDeleted = 0;

        static void Main(string[] args)
        {
            if (!Parser.TryParse(args, out _options))
            {
                DisplayHelp();
                return;
            }

            if (_options.Root == ".")
            {
                _options.Root = Directory.GetCurrentDirectory();
            }

            if (OptionsAreInvalid())
            {
                DisplayHelp();
                return;
            }

            if (!ConfirmOverwriteOutputFile())
            {
                return;
            }

            _options.RemoveFiles = !string.IsNullOrEmpty(_options.Remove);

            if (_options.RemoveFiles)
            {
                var list = _options.Remove.Split(',').ToList();
                foreach (var item in list)
                {
                    var value = item.Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!value.StartsWith("."))
                        {
                            value = "." + value;
                        }

                        _removeExtensions.Add(value);
                    }
                }
            }

            TallyExtensions(_options.Root);
            IOrderedEnumerable<KeyValuePair<string, ExtensionData>> sorted;
            switch (_options.Sort.ToLower())
            {
                case "extension":
                    sorted = from entry in _extensions orderby entry.Key ascending select entry;
                    break;
                case "count":
                    sorted = from entry in _extensions orderby entry.Value.Count ascending select entry;
                    break;
                case "size":
                    sorted = from entry in _extensions orderby entry.Value.Size ascending select entry;
                    break;
                default:
                    // This is handled by call to OptionsAreInvalid()
                    throw new InvalidOperationException("Unknown sort type");
            }

            string resultText;
            switch (_options.Output.ToLower())
            {
                case "json":
                    resultText = Newtonsoft.Json.JsonConvert.SerializeObject(sorted.ToArray(), Newtonsoft.Json.Formatting.Indented);
                    break;
                case "csv":
                    var result = new StringBuilder();
                    result.AppendLine("Extension,Count,Size");
                    foreach (var item in sorted)
                    {
                        result.AppendFormat("{0},{1},{2}", item.Key, item.Value.Count, item.Value.Size).AppendLine();
                    }

                    resultText = result.ToString();
                    break;
                default:
                    // This is handled by call to OptionsAreInvalid()
                    throw new InvalidOperationException("Unknown output type");
            }

            if (_filesDeleted > 0)
            {
                Console.Error.WriteLine($"{_filesDeleted} files deleted matching remove extensions.");
            }

            if (string.IsNullOrEmpty(_options.OutputFile))
            {
                Console.WriteLine(resultText);
            }
            else
            {
                // File.WriteAllText() will overwrite an existing file
                File.WriteAllText(_options.OutputFile, resultText);
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("extensions utility counts all file extensions used in a folder tree. It can also remove specified extensions.");
            Parser.DisplayHelp<CommandOptions>(HelpFormat.Full);
        }

        static bool ConfirmOverwriteOutputFile()
        {
            if (string.IsNullOrEmpty(_options.OutputFile))
            {
                return true;
            }

            if (!File.Exists(_options.OutputFile))
            {
                return true;
            }

            return false;
        }

        static bool OptionsAreInvalid()
        {
            bool invalid = false;
            if (!Directory.Exists(_options.Root))
            {
                Console.WriteLine("Specified root directory does not exist.");
                invalid = true;
            }

            var sort = _options.Sort.ToLower();
            if (sort != "extension" && sort != "count" && sort != "size")
            {
                Console.WriteLine("Unknown sort type");
                invalid = true;
            }

            var output = _options.Output.ToLower();
            if (output != "csv" && output != "json")
            {
                Console.WriteLine("Unknown output type");
                invalid = true;
            }

            return invalid;
        }

        static void TallyExtensions(string root)
        {
            var directories = Directory.GetDirectories(root);
            foreach (var directory in directories)
            {
                TallyExtensions(directory);
            }

            foreach (var file in Directory.GetFiles(root))
            {
                var fileInfo = new FileInfo(file);
                var extension = fileInfo.Extension;

                if (_removeExtensions.Contains(extension))
                {
                    fileInfo.Delete();
                    _filesDeleted++;
                }
                else
                {
                    IncrementExtension(fileInfo, extension);
                }
            }
        }

        static void IncrementExtension(FileInfo fileInfo, string extension)
        {
            if (_extensions.ContainsKey(extension))
            {
                _extensions[extension].Count += 1;
                _extensions[extension].Size += fileInfo.Length;
            }
            else
            {
                var data = new ExtensionData { Count = 1, Size = fileInfo.Length };
                _extensions.Add(extension, data);
            }
        }
    }
}
