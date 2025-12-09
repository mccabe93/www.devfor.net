using System.Text.RegularExpressions;

namespace devfornet.onnx.nlp
{
    internal class ScoredSentence
    {
        public const double STOPWORD_LOW = 0.2;
        public const double STOPWORD_IDEAL = (STOPWORD_HIGH + STOPWORD_LOW) / 2;
        public const double STOPWORD_HIGH = 0.8;

        public const double CONTENT_LOW = 0.4;
        public const double CONTENT_IDEAL = (CONTENT_HIGH + CONTENT_LOW) / 2;
        public const double CONTENT_HIGH = 0.8;

        public const double WORDCOUNT_LOW = 5;

        //public const double WORDCOUNT_HIGH = 50;

        public readonly int Index;
        public readonly string Sentence;
        public readonly double Score;

#if DEBUG
        public readonly double ContentScore;
        public readonly double StopwordScore;
#endif

        public ScoredSentence(
            int index,
            string sentence,
            Dictionary<int, Dictionary<string, string>> properNouns,
            HashSet<string> nouns,
            HashSet<string> verbs,
            HashSet<string> stopwords
        )
        {
            Index = index;

            int properNounCount = 0;
            int nounCount = 0;
            int verbCount = 0;
            int stopwordsCount = 0;
            List<string> words = new();
            string[] wordsTmp = sentence.Split(new char[] { ' ' });
            if (wordsTmp.Length == 0 || wordsTmp.Length < WORDCOUNT_LOW
            //|| wordsTmp.Length > WORDCOUNT_HIGH
            )
            {
                Sentence = sentence;
                Score = 0;
                return;
            }
            double repitionScore = RepetitionScore(wordsTmp);
            if (repitionScore == -1)
            {
                Score = 0;
                Sentence = sentence;
                return;
            }
            for (int j = 0; j < wordsTmp.Length; j++)
            {
                if (wordsTmp[j] == null || wordsTmp[j].Length == 0)
                {
                    continue;
                }
                if (nouns.Contains(wordsTmp[j]))
                {
                    nounCount++;
                }
                else if (verbs.Contains(wordsTmp[j]))
                {
                    verbCount++;
                }
                if (stopwords.Contains(wordsTmp[j]))
                {
                    stopwordsCount++;
                }
                if (IsProperNoun(wordsTmp, j, properNouns))
                {
                    properNounCount++;
                }
                words.Add(wordsTmp[j]);
            }
            Sentence = string.Join(' ', words);

            double contentRatio =
                (double)(properNounCount + nounCount + verbCount)
                / sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            double stopwordRatio =
                (double)(stopwordsCount)
                / sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            double contentScore = 1 - Math.Abs(contentRatio - CONTENT_IDEAL) / CONTENT_IDEAL;
            double stopwordScore = 1 - Math.Abs(stopwordRatio - STOPWORD_IDEAL) / STOPWORD_IDEAL;

#if DEBUG
            ContentScore = contentScore;
            StopwordScore = stopwordScore;
#endif

            Score = (contentScore + stopwordScore) / 2;
        }

        private static bool IsProperNoun(
            string[] wordsTmp,
            int wordIndex,
            Dictionary<int, Dictionary<string, string>> properNouns
        )
        {
            foreach (var groupSize in properNouns)
            {
                if (wordIndex + groupSize.Key - 1 < wordsTmp.Length)
                {
                    string phrase = string.Join(' ', wordsTmp, wordIndex, groupSize.Key).ToLower();
                    if (groupSize.Value.TryGetValue(phrase, out string? proper) && proper != null)
                    {
                        for (int k = 0; k < groupSize.Key; k++)
                        {
                            wordsTmp[wordIndex + k] = null!;
                        }
                        wordsTmp[wordIndex] = proper;
                        return true;
                    }
                }
            }
            return false;
        }

        private static double RepetitionScore(string[] words)
        {
            for (int i = 1; i < words.Length; i++)
            {
                if (words[i] == words[i - 1])
                {
                    return -1;
                }
            }
            return words.Distinct().Count() / (double)words.Length;
        }
    }

    internal static class NLPHelper
    {
        public const double SENTENCE_SIMILARITY_MAX = 0.33;
        public const double SENTENCE_MINIMUM_SCORE = 0.5;

        /// <summary>
        /// [Number of words] -> [lowercase word] -> [proper cased word]
        /// </summary>
        private static Dictionary<int, Dictionary<string, string>> _properNouns
        {
            get => field ??= NLPHelper.LoadProperNounsList(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static HashSet<string> _nouns =>
            field ??= new HashSet<string>(
                File.ReadAllLines(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "nouns.txt")
                    )
                    .Select(line => line.Trim().ToLower())
            );

        private static HashSet<string> _verbs =>
            field ??= new HashSet<string>(
                File.ReadLines(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "verbs.txt")
                    )
                    .Select(line => line.Trim().ToLower())
            );

        private static HashSet<string> _stopwords =>
            field ??= new HashSet<string>(
                File.ReadLines(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "stopwords.txt")
                    )
                    .Select(line => line.Trim().ToLower())
            );

        public static List<ScoredSentence> ScoreSentences(List<string> sentences)
        {
            List<ScoredSentence> scoredSentences = new();
            for (int i = 0; i < sentences.Count; i++)
            {
                scoredSentences.Add(
                    new ScoredSentence(i, sentences[i], _properNouns, _nouns, _verbs, _stopwords)
                );
            }
            return scoredSentences;
        }

        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            string text = input.Trim();

            // Split sentences
            List<string> sentences = Regex.Split(text, @"(?<=[.!?]) (?=)").ToList();
            List<ScoredSentence> sentenceScores = new List<ScoredSentence>();
            bool[] removals = new bool[sentences.Count];
            int removalCount = 0;

            for (int i = 0; i < sentences.Count; i++)
            {
                /*
                ScoredSentence scoredSummarySentence = new ScoredSentence(
                    i,
                    sentences[i],
                    _properNouns,
                    _nouns,
                    _verbs,
                    _stopwords
                );
                sentenceScores.Add(scoredSummarySentence);
                double score = scoredSummarySentence.Score;
                if (score < SENTENCE_MINIMUM_SCORE)
                {
                    removals[i] = true;
                    removalCount++;
                    continue;
                }
                */
                var s = sentences[i].Trim();

                if (s.Length == 0)
                    continue;

                // Capitalize first letter
                s = char.ToUpper(s[0]) + s.Substring(1);

                // For some reason sentences consistently end with ' .'. Probably my own doing somewhere...
                if (s.EndsWith(" ."))
                {
                    s = s.Substring(0, s.Length - 2) + ".";
                }

                sentences[i] = s;
            }

            /*
            if (sentences.Count > 1)
            {
                for (int i = 0; i < sentences.Count; i++)
                {
                    // Order agnostic. j < i is already checked.
                    for (int j = i + 1; j < sentences.Count; j++)
                    {
                        double similarity = GetSentenceSimilarity(sentences[i], sentences[j]);
                        Console.WriteLine(
                            $"Similarity: ({sentences[i]}, {sentences[j]}) = {similarity}"
                        );
                        if (similarity >= SENTENCE_SIMILARITY_MAX)
                        {
                            // Keep the longer sentence.
                            if (sentences[i].Length < sentences[j].Length)
                            {
                                if (!removals[j])
                                {
                                    removals[j] = true;
                                    removalCount++;
                                }
                            }
                            else if (!removals[i])
                            {
                                removals[i] = true;
                                removalCount++;
                            }
                            // Duplicates are removed below.
                        }
                    }
                }

                // If all the sentences suck, keep the best one.
                if (removalCount == removals.Length)
                {
                    ScoredSentence bestSentence = sentenceScores.MaxBy(t => t.Score)!;
                    sentences.Clear();
                    sentences.Add(bestSentence.Sentence);
                }
                else
                {
                    for (int i = removals.Length - 1; i > 0; i--)
                    {
                        if (removals[i])
                            sentences.RemoveAt(i);
                    }
                }
            }
            */

            return string.Join(" ", sentences);
        }

        public static double GetSentenceSimilarity(string sentence1, string sentence2)
        {
            // If we take the number of intersecting words and divide it over the word count of the shorter string, we can estimate similarity.
            // e.g "My String 1".Intersect("My String 2") => 2
            // 2 / 3 similarity. Pretty high. Anything over ScoredSentence.SENTENCE_SIMILARITY_MAX we treat as too similar.
            string[] sentence1Words = GetSentenceWords(sentence1);
            string[] sentence2Words = GetSentenceWords(sentence2);
            return (sentence1Words.Intersect(sentence2Words).Count())
                / (double)Math.Min(sentence1Words.Length, sentence2Words.Length);
        }

        public static string[] GetSentenceWords(string sentence)
        {
            return sentence.Split(" ");
        }

        private static Dictionary<int, Dictionary<string, string>> LoadProperNounsList(
            string currentDirectory
        )
        {
            string[] properNounsLines = File.ReadAllLines(
                Path.Combine(currentDirectory, "data", "proper-nouns.txt")
            );
            Dictionary<int, Dictionary<string, string>> properNouns = new();
            foreach (var properNoun in properNounsLines)
            {
                int parts = properNoun.Split(' ').Length;
                if (!properNouns.ContainsKey(parts))
                {
                    properNouns[parts] = new Dictionary<string, string>();
                }
                properNouns[parts][properNoun.Trim().ToLower()] = properNoun.Trim();
            }
            return properNouns;
        }
    }
}
