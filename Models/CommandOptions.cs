using CommandLine.Attributes;

namespace extensions.Models
{
    class CommandOptions
    {
        [RequiredArgument(0, "dir", "Directory to seach")]
        public string Root { get; set; }

        [OptionalArgument("csv", "output", "Output type (csv, json")]
        public string Output { get; set; }

        [OptionalArgument("", "out", "output file")]
        public string OutputFile { get; set; }

        [OptionalArgument("extension", "sort", "Sort type (extension, count")]
        public string Sort { get; set; }

        [OptionalArgument("files", "count", "Count type (files, size")]
        public string Count { get; set; }

        [OptionalArgument("", "remove", "Remove comma separated list of extensions (bak,gif,jpg)")]
        public string Remove{ get; set; }

        public bool RemoveFiles { get; set; }
    }
}
