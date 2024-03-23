using System;
using System.Collections;

using Lucene.Net.Util;
using Lucene.Net.Store;
using Lucene.Net.Index;

namespace Lucene.Net.LukeNet
{
	/// <summary>
	/// HighFreqTerms class extracts terms and their frequencies 
	/// out of an existing Lucene index.
	/// </summary>
	public class HighFreqTerms
	{
		private static int defaultNumTerms = 100;

		public static int DefaultNumTerms
		{
			get
			{ return defaultNumTerms; }
			set
			{ defaultNumTerms = value; }
		}
		
		public static TermInfo[] GetHighFreqTerms(Directory dir, 
												  Hashtable junkWords, 
												  String[] fields)
		{
			return GetHighFreqTerms(dir, junkWords, defaultNumTerms, fields);
		}

        public static TermInfo[] GetHighFreqTerms(Directory dir,
                                           Hashtable junkWords,
                                           int numTerms,
                                           string[] fields)
        {
            if (dir == null || fields == null) return new TermInfo[0];

            IndexReader reader = DirectoryReader.Open(dir);
            TermInfoQueue tiq = new TermInfoQueue(numTerms);

            foreach (var field in fields)
            {
                Terms terms = MultiFields.GetTerms(reader, field);
                TermsEnum termsEnum = terms?.GetEnumerator();
                if (termsEnum == null) continue;

                BytesRef term;
                while ((term = termsEnum.Next()) != null)
                {
                    string termText = term.Utf8ToString();

                    // Skip if term is in junkWords
                    if (junkWords != null && junkWords.ContainsKey(termText)) continue;

                    int docFreq = termsEnum.DocFreq;

                    if (tiq.Count < numTerms || docFreq > tiq.Top.DocFreq)
                    {
                        tiq.Insert(new TermInfo(new Term(field, termText), docFreq));
                        if (tiq.Count > numTerms)
                        {
                            tiq.Pop();
                        }
                    }
                }
            }

            // Calculate the minimum frequency
            int minFreq = tiq.Count > 0 ? tiq.Top.DocFreq : 0;

            TermInfo[] res = new TermInfo[tiq.Count];
            for (int i = 0; i < res.Length; i++)
            {
                res[res.Length - i - 1] = (TermInfo)tiq.Pop();
            }

            reader.Dispose();

            return res;
        }
    }

	sealed class TermInfoQueue : PriorityQueue<TermInfo> 
	{
        public TermInfoQueue(int maxSize) : base(maxSize)
        {
        }

        protected override bool LessThan(TermInfo a, TermInfo b)
        {
            TermInfo termInfoA = a as TermInfo;
            TermInfo termInfoB = b as TermInfo;

            if (null == termInfoA || null == termInfoB)
                return false;

            return termInfoA.DocFreq < termInfoB.DocFreq;
        }
    }
}
