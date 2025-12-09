using System.Text;
using System.Text.RegularExpressions;
using devfornet.onnx.nlp;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Tokenizers.HuggingFace.Tokenizer;
using static System.Net.Mime.MediaTypeNames;

namespace devfornet.onnx
{
    public sealed class OnnxT5Summarizer
    {
        public const long PAD_TOKEN_ID = 0; // <pad>
        public const long EOS_TOKEN_ID = 1; // </s>
        public const double TOKEN_SCORE_MINIMUM = -1;
        public const long INPUT_MAX_SIZE = 512;
        public const double SENTENCE_SCORE_MINIMUM = 0.70;

        private readonly Tokenizer _tokenizer;
        private readonly InferenceSession _encoderSession;
        private readonly InferenceSession _decoderSession;

        public OnnxT5Summarizer()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string t5Directory = Path.Combine(currentDirectory, "models", "t5-small-onnx");
            _tokenizer = Tokenizer.FromFile(Path.Combine(t5Directory, "tokenizer.json"));
            _encoderSession = new InferenceSession(Path.Combine(t5Directory, "t5-encoder.onnx"));
            _decoderSession = new InferenceSession(Path.Combine(t5Directory, "t5-decoder.onnx"));
        }

        public string GetSummary(string title, string inputText, int maxLength)
        {
            string input = $"summarize: Title: {title}, Text: ";
            if (GetSentenceTokenCount(inputText) > INPUT_MAX_SIZE)
            {
                inputText = GetCleanInput(inputText);
                while (GetSentenceTokenCount(inputText) > INPUT_MAX_SIZE)
                {
                    List<string> subsegments = SegmentInputText(inputText);
                    List<string> summaries = SummarizeSubsegments(title, subsegments, maxLength);
                    inputText = GetCleanInput(string.Join(". ", summaries));
                }
                input += inputText;
            }
            else
            {
                input += GetCleanInput(inputText);
            }
            var encoding = _tokenizer.Encode(input, true);
            long[] inputIds = encoding.SelectMany(ids => ids.Ids.Select(i => (long)i)).ToArray();
            var inputTensor = new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length });
            var encoderInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
            };
            var encoderOutputs = _encoderSession.Run(encoderInputs);
            var encoderHiddenStates = encoderOutputs
                .First(x => x.Name == "hidden_states")
                .AsTensor<float>();
            List<long> decoderInputIds = new List<long> { PAD_TOKEN_ID }; // T5 uses <pad> as start token
            for (int step = 0; step < maxLength; step++)
            {
                var decoderInputTensor = new DenseTensor<long>(
                    decoderInputIds.ToArray(),
                    new[] { 1, decoderInputIds.Count }
                );
                var decoderInputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", decoderInputTensor),
                    NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHiddenStates),
                };
                var decoderOutputs = _decoderSession.Run(decoderInputs);
                var logits = decoderOutputs.First(x => x.Name == "hidden_states").AsTensor<float>();
                int nextTokenId = ArgMaxLastToken(logits, decoderInputIds.Count - 1);
                decoderInputIds.Add(nextTokenId);
                if (nextTokenId == EOS_TOKEN_ID)
                {
                    break;
                }
            }
            var summaryIds = decoderInputIds.Skip(1).ToArray();
            string summary = _tokenizer.Decode(
                summaryIds.Select(i => (uint)i).ToArray(),
                skipSpecialTokens: true
            );

            summary = NLPHelper.Normalize(summary);
            Console.WriteLine("Summary: " + summary);
            return summary;
        }

        private static int ArgMaxLastToken(Tensor<float> logits, int lastIndex, int selection = 1)
        {
            // logits shape: [1, seq_len, vocab_size]
            // We want the last position (seq_len-1 or lastIndex)
            int vocabSize = logits.Dimensions[2];
            float[] maxes = new float[selection];
            for (int i = 0; i < selection; i++)
            {
                maxes[i] = float.NegativeInfinity;
            }
            int[] maxIdxs = new int[selection];
            for (int i = 0; i < vocabSize; i++)
            {
                float val = logits[0, lastIndex, i];
                for (int j = 0; j < selection; j++)
                {
                    if (val > maxes[j])
                    {
                        int prevMaxIdx = maxIdxs[j];
                        float prevMax = maxes[j];
                        maxes[j] = val;
                        maxIdxs[j] = i;
                        for (int k = j + 1; k < selection; k++)
                        {
                            float prevSubMax = maxes[k];
                            int prevSubMaxIdx = maxIdxs[k];
                            maxIdxs[k] = prevMaxIdx;
                            maxes[k] = prevMax;
                            prevMaxIdx = prevSubMaxIdx;
                            prevMax = prevSubMax;
                        }
                        break;
                    }
                }
            }
            for (int i = 1; i < maxes.Length; i++)
            {
                if (maxes[i] < TOKEN_SCORE_MINIMUM)
                {
                    maxIdxs[i] = maxIdxs[0];
                }
            }
            Random.Shared.Shuffle(maxIdxs);
            return maxIdxs[0];
        }

        // Break entire article up into subsegments equal to input max size
        private List<string> SegmentInputText(string inputText)
        {
            List<string> segments = new List<string>();
            List<string> sentences = inputText.Split(". ").ToList();
            Console.WriteLine("------------------");
            Console.WriteLine("New input segmentation . . .");
            while (sentences.Count > 0)
            {
                string segment = string.Empty;
                int segmentTokens = 0;
                while (segmentTokens < INPUT_MAX_SIZE)
                {
                    if (sentences.Count == 0)
                    {
                        break;
                    }
                    string sentence = sentences[0];
                    char? firstLetter = sentence.TrimStart().FirstOrDefault(c => char.IsLetter(c));
                    bool isFirstLetterCapital =
                        firstLetter.HasValue && char.IsUpper(firstLetter.Value);
                    if (!isFirstLetterCapital)
                    {
                        sentences.RemoveAt(0);
                    }
                    else
                    {
                        int sentenceTokens = GetSentenceTokenCount(sentences[0]);
                        if (sentenceTokens > INPUT_MAX_SIZE)
                        {
                            sentences.RemoveAt(0);
                        }
                        else if (segmentTokens + sentenceTokens < INPUT_MAX_SIZE)
                        {
                            segment += sentences[0] + ". ";
                            segmentTokens += sentenceTokens;
                            sentences.RemoveAt(0);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                segments.Add(segment);
            }
            return segments;
        }

        // Summarize subsegments
        private List<string> SummarizeSubsegments(
            string title,
            List<string> segments,
            int maxLength
        )
        {
            List<string> summaries = new List<string>();
            foreach (var segment in segments)
            {
                var summary = GetSummary(title, segment, maxLength);
                summaries.Add(summary);
            }
            return summaries;
        }

        private string GetOptimalSentencesFromInput(string input)
        {
            List<string> sentences = input.Split(". ").ToList();
            List<ScoredSentence> scoredSentences = NLPHelper.ScoreSentences(sentences);
            int quartileIndices = scoredSentences.Count / 4;
            List<ScoredSentence> firstQuartile = GetScoredSentenceSegment(
                    scoredSentences,
                    0,
                    quartileIndices
                )
                .OrderByDescending(t => t.Score)
                .ToList();
            List<ScoredSentence> secondQuartile = GetScoredSentenceSegment(
                    scoredSentences,
                    quartileIndices,
                    quartileIndices
                )
                .OrderByDescending(t => t.Score)
                .ToList();
            List<ScoredSentence> thirdQuartile = GetScoredSentenceSegment(
                    scoredSentences,
                    quartileIndices * 2,
                    quartileIndices
                )
                .OrderByDescending(t => t.Score)
                .ToList();
            List<ScoredSentence> fourthQuartile = GetScoredSentenceSegment(
                    scoredSentences,
                    quartileIndices * 3,
                    scoredSentences.Count - (quartileIndices * 3)
                )
                .OrderByDescending(t => t.Score)
                .ToList();
            int tokenCount = 0;
            StringBuilder summary = new StringBuilder();
            List<ScoredSentence> finalSentences = new List<ScoredSentence>();
            while (tokenCount < INPUT_MAX_SIZE)
            {
                if (
                    !TryAddBestSentence(firstQuartile, finalSentences, ref tokenCount)
                    || !TryAddBestSentence(secondQuartile, finalSentences, ref tokenCount)
                    || !TryAddBestSentence(thirdQuartile, finalSentences, ref tokenCount)
                    || !TryAddBestSentence(fourthQuartile, finalSentences, ref tokenCount)
                )
                {
                    break;
                }
            }
            finalSentences = finalSentences.OrderBy(s => sentences.IndexOf(s.Sentence)).ToList();
            foreach (var scoredSentence in finalSentences)
            {
                summary.Append(scoredSentence.Sentence + ". ");
            }
            return summary.ToString();
        }

        private bool TryAddBestSentence(
            List<ScoredSentence> source,
            List<ScoredSentence> desintation,
            ref int currentTokenCount
        )
        {
            if (source.Count == 0)
            {
                return false;
            }
            int sentenceTokenCount = GetSentenceTokenCount(source[0].Sentence);
            if (currentTokenCount + sentenceTokenCount <= INPUT_MAX_SIZE)
            {
                currentTokenCount += sentenceTokenCount;
                desintation.Add(source[0]);
                source.RemoveAt(0);
                return true;
            }
            return false;
        }

        private int GetSentenceTokenCount(string sentence)
        {
            return _tokenizer.Encode(sentence, true).SelectMany(ids => ids.Ids).Count();
        }

        private static string GetCleanInput(string input)
        {
            string cleanedInput = Regex.Replace(input, @"\t|\n|\r", " "); // Removes tabs and new lines
            cleanedInput = Regex.Replace(cleanedInput, @"[-;]", " "); // Replaces - and ; with space
            cleanedInput = Regex.Replace(cleanedInput, @"([!?])\1+", "$1"); // Replaces all repeat ?/! to single ?/!
            cleanedInput = Regex.Replace(cleanedInput, @"\s+", " "); // Remove all repeated whitespace
            return cleanedInput;
        }

        private static List<ScoredSentence> GetScoredSentenceSegment(
            List<ScoredSentence> sentences,
            int start,
            int count
        )
        {
            List<ScoredSentence> segment = new List<ScoredSentence>();
            for (int i = start; i < start + count && i < sentences.Count; i++)
            {
                segment.Add(sentences[i]);
            }
            return segment;
        }
    }
}
