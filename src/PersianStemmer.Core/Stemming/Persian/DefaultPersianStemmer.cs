using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace PersianStemmer.Core.Stemming.Persian
{
    public class DefaultPersianStemmer : IStemmer
    {
        private static readonly Trie<int> lexicon = new Trie<int>();
        private static readonly Trie<string> mokassarDic = new Trie<string>();
        private static readonly Trie<string> cache = new Trie<string>();
        private static readonly Trie<Verb> verbDic = new Trie<Verb>();
        private static readonly List<Rule> _ruleList = new List<Rule>();

        private static readonly string[] verbAffix = { "*ش", "*نده", "*ا", "*ار", "وا*", "اثر*", "فرو*", "پیش*", "گرو*", "*ه", "*گار", "*ن" };
        private static readonly string[] suffix = { "كار", "ناك", "وار", "آسا", "آگین", "بار", "بان", "دان", "زار", "سار", "سان", "لاخ", "مند", "دار", "مرد", "کننده", "گرا", "نما", "متر" };
        private static readonly string[] prefix = { "بی", "با", "پیش", "غیر", "فرو", "هم", "نا", "یک" };
        private static readonly string[] prefixException = { "غیر" };
        private static readonly string[] suffixZamir = { "م", "ت", "ش" };
        private static readonly string[] suffixException = { "ها", "تر", "ترین", "ام", "ات", "اش" };

        private static readonly string BASE_NAMESPACE = "PersianStemmer.Core.Stemming.Persian.data";
        private static readonly string PATTERN_FILE_NAME = "Patterns.fa";
        private static readonly string VERB_FILE_NAME = "VerbList.fa";
        private static readonly string DIC_FILE_NAME = "Dictionary.fa";
        private static readonly string MOKASSAR_FILE_NAME = "Mokassar.fa";

        private static int patternCount = 1;
        private static bool enableCache = true;
        private static bool enableVerb = true;

        public DefaultPersianStemmer()
        {
            LoadRule();
            LoadLexicon();
            LoadMokassarDic();
            if (enableVerb)
                LoadVerbDic();
        }

        private string[] LoadData(string resourceName)
        {
            var assembly = typeof(DefaultPersianStemmer).Assembly;
            var stringContent = "";

            using (var resource = assembly.GetManifestResourceStream($"{BASE_NAMESPACE}.{resourceName}"))
            {
                if(resource != null)
                {
                    var content = new byte[(int)resource.Length];
                    resource.Read(content, 0, content.Length);

                    stringContent = Encoding.UTF8.GetString(content);
                }
            }

            return stringContent
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }

        private void LoadVerbDic()
        {
            if (!verbDic.IsEmpty())
                return;

            string[] sLines = LoadData(VERB_FILE_NAME);
            foreach (string sLine in sLines)
            {
                string[] arr = sLine.Split('\t');
                try
                {
                    verbDic.Add(arr[0].Trim(), new Verb(arr[1].Trim(), arr[2].Trim()));
                }
                catch
                {
                    //log.Warn("Verb " + sLine + " cannot be added. Is it duplicated?");
                }
            }
        }

        private void LoadRule()
        {
            if (_ruleList.Count != 0)
                return;

            string[] sLines = LoadData(PATTERN_FILE_NAME);
            foreach (string sLine in sLines)
            {
                string[] arr = sLine.Split(',');
                _ruleList.Add(new Rule(arr[0], arr[1], arr[2][0], byte.Parse(arr[3]), bool.Parse(arr[4])));
            }
        }

        private void LoadLexicon()
        {
            if (!lexicon.IsEmpty())
                return;

            string[] sLines = LoadData(DIC_FILE_NAME);
            foreach (string sLine in sLines)
            {
                lexicon.Add(sLine.Trim(), 1);
            }
        }

        private void LoadMokassarDic()
        {
            if (!mokassarDic.IsEmpty())
                return;

            string[] sLines = LoadData(MOKASSAR_FILE_NAME);
            foreach (string sLine in sLines)
            {
                string[] arr = sLine.Split('\t');
                mokassarDic.Add(arr[0].Trim(), arr[1].Trim());
            }
        }

        private string Normalization(string s)
        {
            StringBuilder newString = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case 'ي':
                        newString.Append('ی');
                        break;
                    //case 'ة':
                    case 'ۀ':
                        newString.Append('ه');
                        break;
                    case '‌':
                        newString.Append(' ');
                        break;
                    case '‏':
                        newString.Append(' ');
                        break;
                    case 'ك':
                        newString.Append('ک');
                        break;
                    case 'ؤ':
                        newString.Append('و');
                        break;
                    case 'إ':
                    case 'أ':
                        newString.Append('ا');
                        break;
                    case '\u064B': //FATHATAN
                    case '\u064C': //DAMMATAN
                    case '\u064D': //KASRATAN
                    case '\u064E': //FATHA
                    case '\u064F': //DAMMA
                    case '\u0650': //KASRA
                    case '\u0651': //SHADDA
                    case '\u0652': //SUKUN
                        break;
                    default:
                        newString.Append(s[i]);
                        break;
                }
            }
            return newString.ToString();

        }

        private bool Validation(string? sWord)
        {
            return lexicon.Contains(sWord);
        }

        private string IsMokassar(string sInput, bool bState)
        {
            string sRule = "^(?<stem>.+?)((?<=(ا|و))ی)?(ها)?(ی)?((ات)?( تان|تان| مان|مان| شان|شان)|ی|م|ت|ش|ء)$";
            if (bState)
                sRule = "^(?<stem>.+?)((?<=(ا|و))ی)?(ها)?(ی)?(ات|ی|م|ت|ش| تان|تان| مان|مان| شان|شان|ء)$";

            return ExtractStem(sInput, sRule);
        }

        private string GetMokassarStem(string sWord)
        {
            var sTemp = mokassarDic.ContainsKey(sWord);
            if (String.IsNullOrEmpty(sTemp))
            {
                string sNewWord = IsMokassar(sWord, true);
                sTemp = mokassarDic.ContainsKey(sNewWord);
                if (String.IsNullOrEmpty(sTemp))
                {
                    sNewWord = IsMokassar(sWord, false);
                    sTemp = mokassarDic.ContainsKey(sNewWord);
                    if (!String.IsNullOrEmpty(sTemp))
                        return sTemp;
                }
                else
                {
                    return sTemp;
                }
            }
            else
            {
                return sTemp;
            }

            return "";
        }

        private string VerbValidation(string sWord)
        {
            if (sWord.IndexOf(' ') > -1)
                return "";

            for (int j = 0; j < verbAffix.Length; j++)
            {
                string sTemp = "";
                if (j == 0 && (sWord[sWord.Length - 1] == 'ا' || sWord[sWord.Length - 1] == 'و'))
                {
                    sTemp = verbAffix[j].Replace("*", sWord + "ی");
                }
                else
                {
                    sTemp = verbAffix[j].Replace("*", sWord);
                }

                if (NormalizeValidation(sTemp, true))
                    return verbAffix[j];
            }

            return "";
        }

        private bool InRange(int d, int from, int to)
        {
            return (d >= from && d <= to);
        }

        private string GetPrefix(string sWord)
        {
            foreach (string sPrefix in DefaultPersianStemmer.prefix)
            {
                if (sWord.StartsWith(sPrefix))
                    return sPrefix;
            }

            return "";
        }

        private string GetPrefixException(string sWord)
        {
            foreach (string sPrefix in DefaultPersianStemmer.prefixException)
            {
                if (sWord.StartsWith(sPrefix))
                    return sPrefix;
            }

            return "";
        }

        private string GetSuffix(string sWord)
        {
            foreach (string sSuffix in DefaultPersianStemmer.suffix)
            {
                if (sWord.EndsWith(sSuffix))
                    return sSuffix;
            }

            return "";
        }

        private bool NormalizeValidation(string sWord, bool bRemoveSpace)
        {
            int l = sWord.Trim().Length - 2;
            sWord = sWord.Trim();
            bool result = Validation(sWord);

            if (!result && sWord.IndexOf('ا') == 0)
            {
                result = Validation(ReplaceFirst(sWord, "ا", "آ"));
            }

            if (!result && InRange(sWord.IndexOf('ا'), 1, l))
            {
                result = Validation(sWord.Replace('ا', 'أ'));
            }

            if (!result && InRange(sWord.IndexOf('ا'), 1, l))
            {
                result = Validation(sWord.Replace('ا', 'إ'));
            }

            if (!result && InRange(sWord.IndexOf("ئو"), 1, l))
            {
                result = Validation(sWord.Replace("ئو", "ؤ"));
            }

            if (!result && sWord.EndsWith("ء"))
                result = Validation(sWord.Replace("ء", ""));

            if (!result && InRange(sWord.IndexOf("ئ"), 1, l))
                result = Validation(sWord.Replace("ئ", "ی"));

            if (bRemoveSpace)
            {
                if (!result && InRange(sWord.IndexOf(' '), 1, l))
                {
                    result = Validation(sWord.Replace(" ", ""));
                }
            }
            // دیندار
            // دین دار
            if (!result)
            {
                string sSuffix = GetSuffix(sWord);
                if (!string.IsNullOrEmpty(sSuffix))
                    result = Validation(sSuffix == ("مند") ? sWord.Replace(sSuffix, "ه " + sSuffix) : sWord.Replace(sSuffix, " " + sSuffix));
            }

            if (!result)
            {
                string sPrefix = GetPrefix(sWord);
                if (!string.IsNullOrEmpty(sPrefix))
                {
                    if (sWord.StartsWith(sPrefix + " "))
                        result = Validation(sWord.Replace(sPrefix + " ", sPrefix));
                    else
                        result = Validation(sWord.Replace(sPrefix, sPrefix + " "));
                }
            }

            if (!result)
            {
                string sPrefix = GetPrefixException(sWord);
                if (!string.IsNullOrEmpty(sPrefix))
                {
                    if (sWord.StartsWith(sPrefix + " "))
                        result = Validation(ReplaceFirst(sWord, sPrefix + " ", ""));
                    else
                        result = Validation(ReplaceFirst(sWord, sPrefix, ""));
                }
            }

            return result;
        }

        public string ReplaceFirst(string word, string oldValue, string newValue)
        {
            int i = word.IndexOf(oldValue);
            if (i >= 0)
            {
                return word.Substring(0, i) + newValue + word.Substring(i + oldValue.Length);
            }
            return word;
        }

        private bool IsMatch(string sInput, string sRule)
        {
            return Regex.IsMatch(sInput, sRule);
        }

        private string ExtractStem(string sInput, string sRule, string sReplacement)
        {
            return Regex.Replace(sInput, sRule, sReplacement).Trim();
        }

        private string ExtractStem(string sInput, string sRule)
        {
            return ExtractStem(sInput, sRule, "${stem}");
        }

        private string? GetVerb(string input)
        {
            var tmpNode = verbDic.FindNode(input);
            if (tmpNode != null)
            {
                Verb? vs = tmpNode.Value;
                if (vs == null) return "";
                if (Validation(vs.getPresent() )) return vs.getPresent();

                return vs.getPast();
            }

            return "";
        }

        private bool PatternMatching(string input, List<string> stemList)
        {
            bool terminate = false;
            string s = "";
            string sTemp = "";
            foreach (Rule rule in _ruleList)
            {
                if (terminate)
                    return terminate;

                string[] sReplace = rule.getSubstitution()?.Split(';') ?? Array.Empty<string>();
                string pattern = rule.getBody() ?? "";

                if (!IsMatch(input, pattern))
                    continue;

                int k = 0;
                foreach (string t in sReplace)
                {
                    if (k > 0)
                        break;

                    s = ExtractStem(input, pattern, t);
                    if (s.Length < rule.getMinLength())
                        continue;

                    switch (rule.getPoS())
                    {
                        case 'K': // Kasre Ezafe
                            if (stemList.Count == 0)
                            {
                                sTemp = GetMokassarStem(s);
                                if (!string.IsNullOrEmpty(sTemp))
                                {
                                    stemList.Add(sTemp);//, pattern + " [جمع مکسر]");
                                    k++;
                                }
                                else if (NormalizeValidation(s, true))
                                {
                                    stemList.Add(s);//, pattern);
                                    k++;
                                }
                                else
                                {
                                    //addToLog("", pattern + " : {" + s + "}");
                                }
                            }
                            break;
                        case 'V': // Verb

                            sTemp = VerbValidation(s);
                            if (!string.IsNullOrEmpty(sTemp))
                            {
                                stemList.Add(s/* pattern + " : [" + sTemp + "]"*/);
                                k++;
                            }
                            else
                            {
                                //addToLog("", pattern + " : {تمام وندها}");
                            }
                            break;
                        default:
                            if (NormalizeValidation(s, true))
                            {
                                stemList.Add(s/*, pattern*/);
                                if (rule.getState())
                                    terminate = true;
                                k++;
                            }
                            else
                            {
                                //addToLog("", pattern + " : {" + s + "}");
                            }
                            break;
                    }
                }
            }
            return terminate;
        }

        public string Run(string input)
        {
            input = Normalization(input).Trim();

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            //Integer or english 
            if (Utils.IsEnglish(input) || Utils.IsNumber(input) || (input.Length <= 2))
                return input;

            if (enableCache)
            {
                var stm = cache.ContainsKey(input);
                if (!String.IsNullOrEmpty(stm))
                    return stm;
            }

            string? s = GetMokassarStem(input);
            if (NormalizeValidation(input, false))
            {
                //stemList.add(input/*, "[فرهنگ لغت]"*/);
                if (enableCache)
                    cache.Add(input, input);
                return input;
            }
            else if (!string.IsNullOrEmpty(s))
            {
                //addToLog(s/*, "[جمع مکسر]"*/);
                //stemList.add(s);
                if (enableCache)
                    cache.Add(input, s);
                return s;
            }

            List<string> stemList = new List<string>();
            bool terminate = PatternMatching(input, stemList);

            if (enableVerb)
            {
                s = GetVerb(input);
                if (!string.IsNullOrEmpty(s))
                {
                    stemList.Clear();
                    stemList.Add(s);
                }
            }

            if (stemList.Count == 0)
            {
                if (NormalizeValidation(input, true))
                {
                    //stemList.add(input, "[فرهنگ لغت]");
                    if (enableCache)
                        cache.Add(input, input); //stemList.get(0));
                    return input;//stemList.get(0);
                }
                stemList.Add(input);//, "");            
            }

            if (terminate && stemList.Count > 1)
            {
                return NounValidation(stemList);
            }

            const int I = 0;
            if (patternCount != 0)
            {
                if (patternCount < 0)
                    stemList.Reverse();
                else
                    stemList.Sort();

                while (I < stemList.Count && (stemList.Count > Math.Abs(patternCount)))
                {
                    stemList.RemoveAt(I);
                    //patternList.remove(I);
                }
            }

            if (enableCache)
                cache.Add(input, stemList[0]);
            return stemList[0];
        }

        public int Stem(char[] s, int len) /*throws Exception*/
        {

            StringBuilder input = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                input.Append(s[i]);
            }
            string sOut = this.Run(input.ToString());

            if (sOut.Length > s.Length)
                s = new char[sOut.Length];
            for (int i = 0; i < sOut.Length; i++)
            {
                s[i] = sOut[i];
            }
            /*try {
                for (int i=0; i< Math.min(sOut.length(), s.length); i++) {
                    s[i] = sOut.charAt(i);
                }    
            }
            catch (Exception e) {
                throw new Exception("stem: "+sOut+" - input: "+ input.toString());
            }*/

            return sOut.Length;

        }

        private string NounValidation(List<string> stemList)
        {
            stemList.Sort();
            int lastIdx = stemList.Count - 1;
            string lastStem = stemList[lastIdx];

            if (lastStem.EndsWith("ان"))
            {
                return lastStem;
            }
            else
            {
                string firstStem = stemList[0];
                string secondStem = stemList[1].Replace(" ", "");

                /*if (secondStem.equals(firstStem.concat("م"))) {
                    return firstStem;
                }
                else if (secondStem.equals(firstStem.concat("ت"))) {
                    return firstStem;
                }
                else if (secondStem.equals(firstStem.concat("ش"))) {
                    return firstStem;
                }*/

                foreach (string sSuffix in DefaultPersianStemmer.suffixZamir)
                {
                    if (secondStem.Equals(firstStem + sSuffix))
                        return firstStem;
                }
            }
            return lastStem;
        }
    }
}