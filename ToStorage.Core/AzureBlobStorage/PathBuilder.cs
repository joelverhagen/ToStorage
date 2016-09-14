using System;
using System.Text.RegularExpressions;

namespace Knapcode.ToStorage.Core.AzureBlobStorage
{
    public interface IPathBuilder
    {
        void Validate(string pathFormat);
        string GetLatest(string pathFormat);
        string GetDirect(string pathFormat, DateTimeOffset dateTimeOffset);
        string GetDirect(string pathFormat, int number);
    }

    public class PathBuilder : IPathBuilder
    {
        private static readonly Regex FormatParameterRegex = new Regex(@"\{(?<Index>\d+)\}", RegexOptions.Compiled);

        public void Validate(string pathFormat)
        {
            var matchCollection = FormatParameterRegex.Matches(pathFormat);
            if (matchCollection.Count != 1)
            {
                throw new ArgumentException($"There should be exactly one string format placeholder (i.e. '{{0}}'), not {matchCollection.Count}.");
            }

            if (matchCollection[0].Groups["Index"].Value != "0")
            {
                throw new ArgumentException($"The string format placeholder should be '{{0}}', not '{{{matchCollection[0].Groups["Index"]}}}'.");
            }
        }

        public string GetLatest(string pathFormat)
        {
            return string.Format(pathFormat, "latest");
        }

        public string GetDirect(string pathFormat, DateTimeOffset dateTimeOffset)
        {
            return string.Format(pathFormat, dateTimeOffset.ToString("yyyy.MM.dd.HH.mm.ss"));
        }

        public string GetDirect(string pathFormat, int number)
        {
            return string.Format(pathFormat, number);
        }
    }
}
