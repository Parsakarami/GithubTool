using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarCloudModel.Report
{
    public class ReportRecord
    {
        public string Name { get; set; }
        public int Bugs { get; set; }
        public int CodeSmell { get; set; }
        public string Debt { get; set; }
        public string Duplication { get; set; }
        public int DuplicatedLines { get; set; }
        public int DuplicatedBlocks { get; set; }
        public int DuplicatedFiles { get; set; }
        public int LinesOfCode { get; set; }
        public int TotalLines { get; set; }
        public int NumberOfStatements { get; set; }
        public int NumberOfFunctions { get; set; }
        public int NumberOfClasses { get; set; }
        public int NumberOfFiles { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int CognitiveComplexity { get; set; }
    }
}
