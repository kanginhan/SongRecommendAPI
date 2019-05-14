using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SongRecommendAPI.Infra
{
    using com.twitter.penguin.korean;
    using com.twitter.penguin.korean.tokenizer;
    using ikvm.extensions;

    using ScalaKoreanPhrase = com.twitter.penguin.korean.phrase_extractor.KoreanPhraseExtractor.KoreanPhrase;

    public static class TwitterKoreanProcessorCS
    {
        static TwitterKoreanProcessorCS()
        {
            // http://sourceforge.net/p/ikvm/mailman/message/31712591/
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Add("file.encoding", "UTF-8");
            ikvm.runtime.Startup.setProperties(props);
        }

        public static string Normalize(string text)
        {
            string result = TwitterKoreanProcessor.normalize(text).toString();
            return result;
        }

        public static IEnumerable<KoreanToken> Stem(this IEnumerable<KoreanToken> tokens)
        {
            var scalaTokenSeq = Utils.ScalaCSHelper.ReverseScalaSeqConverter<KoreanTokenizer.KoreanToken, KoreanToken>(tokens, t => t.ToScalaToken());
            var scalaResults = TwitterKoreanProcessor.stem(scalaTokenSeq);
            var results = Utils.ScalaCSHelper.ScalaSeqConverter<KoreanToken, KoreanTokenizer.KoreanToken>(scalaResults, t => new KoreanToken(t));
            return results;
        }

        public static IEnumerable<KoreanToken> Tokenize(this string text)
        {
            var scalaResults = TwitterKoreanProcessor.tokenize(text);
            List<KoreanToken> results = Utils.ScalaCSHelper.ScalaSeqConverter<KoreanToken, KoreanTokenizer.KoreanToken>(
                scalaResults, (scalaResult) => { return new KoreanToken(scalaResult); });

            return results;
        }

        public static IEnumerable<string> TokensToStrings(this IEnumerable<KoreanToken> tokens)
        {
            var scalaTokenSeq = Utils.ScalaCSHelper.ReverseScalaSeqConverter<KoreanTokenizer.KoreanToken, KoreanToken>(tokens, t => t.ToScalaToken());
            var scalaResults = TwitterKoreanProcessor.tokensToStrings(scalaTokenSeq);
            var results = Utils.ScalaCSHelper.ScalaSeqStringConverter(scalaResults);

            return results;
        }

        public static List<KoreanPhrase> ExtractPhrases(this IEnumerable<KoreanToken> tokens, bool filterSpam = false, bool enableHashTags = true)
        {
            var scalaTokenSeq = Utils.ScalaCSHelper.ReverseScalaSeqConverter<KoreanTokenizer.KoreanToken, KoreanToken>(tokens, t => t.ToScalaToken());

            // returns: Seq[KoreanPhrase]
            var scalaResults = TwitterKoreanProcessor.extractPhrases(scalaTokenSeq, filterSpam, enableHashTags);
            var results = Utils.ScalaCSHelper.ScalaSeqConverter<KoreanPhrase, ScalaKoreanPhrase>(scalaResults, p => new KoreanPhrase(p));


            return results;
        }
    }

    public class KoreanToken
    {
        public string Text { get; set; }
        public KoreanPos Pos { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public bool Unknown { get; set; }

        public KoreanToken(com.twitter.penguin.korean.tokenizer.KoreanTokenizer.KoreanToken scalaToken)
        {
            this.Text = scalaToken.text().toString();
            this.Pos = (KoreanPos)scalaToken.pos().id();
            this.Offset = scalaToken.offset();
            this.Length = scalaToken.length();
            this.Unknown = scalaToken.unknown();
        }

        public com.twitter.penguin.korean.tokenizer.KoreanTokenizer.KoreanToken ToScalaToken()
        {
            var scalaKoreanPos = com.twitter.penguin.korean.util.KoreanPos.withName(this.Pos.ToString());

            var scalaToken = new com.twitter.penguin.korean.tokenizer.KoreanTokenizer.KoreanToken(
                text: this.Text,
                pos: scalaKoreanPos,
                offset: this.Offset,
                length: this.Length,
                unknown: this.Unknown);

            return scalaToken;
        }
    }

    public class KoreanPhrase
    {
        public IEnumerable<KoreanToken> Tokens { get; set; }
        public KoreanPos Pos { get; set; }

        public KoreanPhrase(com.twitter.penguin.korean.phrase_extractor.KoreanPhraseExtractor.KoreanPhrase scalaPhrase)
        {
            var scalaTokens = scalaPhrase.tokens(); // Seq[KoreanToken]
            var tokens = Utils.ScalaCSHelper.ScalaSeqConverter<KoreanToken, KoreanTokenizer.KoreanToken>(scalaTokens, t => new KoreanToken(t));

            this.Tokens = tokens;
            this.Pos = (KoreanPos)scalaPhrase.pos().id();
        }
    }

    public enum KoreanPos
    {
        // Word leved POS
        Noun, Verb, Adjective,
        Adverb, Determiner, Exclamation,
        Josa, Eomi, PreEomi, Conjunction,
        NounPrefix, VerbPrefix, Suffix, Unknown,

        // Chunk level POS
        Korean, Foreign, Number, KoreanParticle, Alpha,
        Punctuation, Hashtag, ScreenName,
        Email, URL, CashTag,

        // Functional POS
        Space, Others,
        ProperNoun
    }

    namespace Utils
    {
        internal static class ScalaCSHelper
        {
            internal static List<T> ScalaSeqConverter<T, TScala>(scala.collection.Seq sequences, Func<TScala, T> convertFunc)
                where TScala : class
                where T : class
            {
                List<T> results = new List<T>();

                for (int i = 0; i < sequences.size(); i++) {
                    TScala scalaResult = sequences.apply(i) as TScala;
                    T result = convertFunc(scalaResult);
                    results.Add(result);
                }

                return results;
            }


            internal static List<string> ScalaSeqStringConverter(scala.collection.Seq sequences)
            {
                List<string> results = new List<string>();

                for (int i = 0; i < sequences.size(); i++) {
                    var scalaResult = sequences.apply(i);
                    string result = scalaResult.toString();
                    results.Add(result);
                }

                return results;
            }

            internal static scala.collection.Seq ReverseScalaSeqConverter<TScala, T>(IEnumerable<T> items, Func<T, TScala> convertFunc)
            {
                var scalaList = new scala.collection.mutable.MutableList();
                foreach (T item in items) {
                    TScala scalaItem = convertFunc(item);
                    scalaList.appendElem(scalaItem);
                }

                return scalaList;
            }
        }
    }
}
