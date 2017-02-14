﻿using System.Collections.Generic;
using System.Text;

namespace EngineLayer.Indexing
{
    public class IndexingResults : MyResults
    {
        #region Public Constructors

        public IndexingResults(List<CompactPeptide> peptideIndex, Dictionary<float, List<int>> fragmentIndexDict, IndexingEngine indexParams) : base(indexParams)
        {
            this.PeptideIndex = peptideIndex;
            this.FragmentIndexDict = fragmentIndexDict;
        }

        #endregion Public Constructors

        #region Public Properties

        public Dictionary<float, List<int>> FragmentIndexDict { get; private set; }
        public List<CompactPeptide> PeptideIndex { get; private set; }

        #endregion Public Properties

        #region Protected Properties

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.ToString());
            sb.AppendLine("\t\tfragmentIndexDict.Count: " + FragmentIndexDict.Count);
            sb.Append("\t\tpeptideIndex.Count: " + PeptideIndex.Count);
            return sb.ToString();
        }

        #endregion Protected Properties
    }
}