using System;

namespace dropdownloadcore
{
    public class Args
    {
        private const string RelavePathEnvironmentVariable = "relativepath";
        private const string VSTSPatEnvironmentVariable = "vstspat";
        private const string DropDestinationEnvironmentVariable = "dropdestination";
        private const string DefaultDropDestination = "/drop";
        private const string DropUrlEnvironmentVariable = "dropurl";

        public string VstsPat { get; set; }

        public string DropDestination { get; set; }

        public string DropUrl { get; set; }

        public string RelativePath { get; set; }

        public Args()
        {
            this.VstsPat = null;
            this.DropDestination = null;
            this.DropUrl = null;
            this.RelativePath = null;
        }

        public Args(string[] args)
            : this()
        {
            this.Parse(args);
        }

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

            this.CheckEnvironmentVariables();
            this.ValidatePat();
        }

        public void CheckEnvironmentVariables()
        {
            if (this.VstsPat == null)
            {
                this.VstsPat = System.Environment.GetEnvironmentVariable(VSTSPatEnvironmentVariable);
            }

            if (this.DropDestination == null)
            {
                this.DropDestination = System.Environment.GetEnvironmentVariable(DropDestinationEnvironmentVariable)
                                       ?? DefaultDropDestination;
            }

            if (this.DropUrl == null)
            {
                this.DropUrl = System.Environment.GetEnvironmentVariable(DropUrlEnvironmentVariable);
            }

            if (this.RelativePath == null)
            {
                this.RelativePath = System.Environment.GetEnvironmentVariable(RelavePathEnvironmentVariable) ?? "/";
            }
        }

        public void ValidatePat()
        {
            if (string.IsNullOrWhiteSpace(this.VstsPat) || this.VstsPat.Equals("$(System.AccessToken)"))
            {
                throw new ArgumentException("Invalid personal accestoken. Remember to set allow scripts to access oauth token in agent phase");
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
            bool valid = (index < args.Length - 1 && !args[index + 1].StartsWith("-"));
            if (!valid)
            {
                this.ThrowInvalidArgument(args, index);
            }

            return valid;
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
