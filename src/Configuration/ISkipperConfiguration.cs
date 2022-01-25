using System.Collections.Generic;

namespace Our.Umbraco.Skipper.Configuration
{
    public interface ISkipperConfiguration
    {
        public List<string> Aliases { get; }

        public bool SkipperWorkReturns404 { get; }

        public int WhileLoopMaxCount { get; }
    }
}