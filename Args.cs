using System;

namespace dropdownloadcore
{
    public class Args
    {
        public string VstsPat { get; set; }

        public string DropDestination { get; set; }

        public string DropUrl { get; set; }

        public string RelativePath { get; set; }

        public void Parse(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (this.ArgEquals(args[i], "VstsPat", "t"))
                {
                    if (this.IsValidArg(args, i) && !this.IsValidArg(args, i + 1))
                    {
                        this.VstsPat = args[i + 1];
                        ++i;
                        continue;
                    }
                }

                if (this.ArgEquals(args[i], "DropDestination", "d"))
                {
                    if (this.IsValidArg(args, i) && !this.IsValidArg(args, i + 1))
                    {
                        this.DropDestination = args[i + 1];
                        ++i;
                        continue;
                    }
                }

                if (this.ArgEquals(args[i], "DropUrl", "u"))
                {
                    if (this.IsValidArg(args, i) && !this.IsValidArg(args, i + 1))
                    {
                        this.DropUrl = args[i + 1];
                        ++i;
                        continue;
                    }
                }

                if (this.ArgEquals(args[i], "RelativePath", "p"))
                {
                    if (this.IsValidArg(args, i) && !this.IsValidArg(args, i + 1))
                    {
                        this.RelativePath = args[i + 1];
                        ++i;
                        continue;
                    }
                }
            }
        }

        private bool ArgEquals(string givenArgument, string expected, string shorthandArg = null)
        {
            char[] trimChars = { '-' };
            expected = expected.Trim(trimChars);
            shorthandArg = shorthandArg.Trim(trimChars);

            bool shortEq = false;
            if (!string.IsNullOrWhiteSpace(shorthandArg))
            {
                shortEq = givenArgument.Equals(string.Format("-{0}", shorthandArg));
            }

            return shortEq || givenArgument.Equals(string.Format("--{0}", expected), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsValidArg(string[] args, int index)
        {
            return (index < args.Length - 1 && !args[index + 1].StartsWith("-"));
        }

        private void ThrowInvalidArgument(string[] arguments, int index)
        {
            string exceptionMessage = string.Format(
                        "The argument, {0}, was invalid\n{1}",
                        arguments[index],
                        string.Join(" ", arguments));

            throw new ArgumentException(exceptionMessage);
        }
    }
}
