/*
 * MiniJSON - A simple JSON parser for Unity
 * Based on the public domain MiniJSON library, commonly used in Unity SDKs.
 * 
 * Usage:
 *   string json = AdshiftMiniJSON.Serialize(dictionary);
 *   Dictionary<string, object> dict = AdshiftMiniJSON.Deserialize(json);
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Adshift.Utils
{
    /// <summary>
    /// Simple JSON serializer/deserializer for Unity.
    /// Handles Dictionary, List, primitives (string, int, double, bool), and null.
    /// </summary>
    public static class AdshiftMiniJSON
    {
        /// <summary>
        /// Deserializes a JSON string into a Dictionary or List.
        /// </summary>
        /// <param name="json">JSON string to parse.</param>
        /// <returns>Dictionary&lt;string, object&gt; for objects, List&lt;object&gt; for arrays, or null on error.</returns>
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }

        /// <summary>
        /// Serializes an object to JSON string.
        /// </summary>
        /// <param name="obj">Object to serialize (Dictionary, List, or primitive).</param>
        /// <returns>JSON string representation.</returns>
        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        // ============ Parser ============

        private sealed class Parser : IDisposable
        {
            private const string WORD_BREAK = "{}[],:\"";

            private StringReader _json;

            private Parser(string jsonString)
            {
                _json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                _json.Dispose();
                _json = null;
            }

            private char PeekChar => Convert.ToChar(_json.Peek());

            private char NextChar => Convert.ToChar(_json.Read());

            private string NextWord
            {
                get
                {
                    var word = new StringBuilder();
                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);
                        if (_json.Peek() == -1) break;
                    }
                    return word.ToString();
                }
            }

            private TOKEN NextToken
            {
                get
                {
                    EatWhitespace();
                    if (_json.Peek() == -1) return TOKEN.NONE;

                    switch (PeekChar)
                    {
                        case '{': return TOKEN.CURLY_OPEN;
                        case '}': _json.Read(); return TOKEN.CURLY_CLOSE;
                        case '[': return TOKEN.SQUARED_OPEN;
                        case ']': _json.Read(); return TOKEN.SQUARED_CLOSE;
                        case ',': _json.Read(); return TOKEN.COMMA;
                        case '"': return TOKEN.STRING;
                        case ':': return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-': return TOKEN.NUMBER;
                    }

                    switch (NextWord)
                    {
                        case "false": return TOKEN.FALSE;
                        case "true": return TOKEN.TRUE;
                        case "null": return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }

            private static bool IsWordBreak(char c)
            {
                return char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }

            private void EatWhitespace()
            {
                while (char.IsWhiteSpace(PeekChar))
                {
                    _json.Read();
                    if (_json.Peek() == -1) break;
                }
            }

            private object ParseValue()
            {
                var nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            private object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING: return ParseString();
                    case TOKEN.NUMBER: return ParseNumber();
                    case TOKEN.CURLY_OPEN: return ParseObject();
                    case TOKEN.SQUARED_OPEN: return ParseArray();
                    case TOKEN.TRUE: return true;
                    case TOKEN.FALSE: return false;
                    case TOKEN.NULL: return null;
                    default: return null;
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();
                _json.Read(); // skip '{'

                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE: return null;
                        case TOKEN.CURLY_CLOSE: return table;
                        case TOKEN.COMMA: continue;
                        default:
                            var name = ParseString();
                            if (name == null) return null;
                            if (NextToken != TOKEN.COLON) return null;
                            _json.Read(); // skip ':'
                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();
                _json.Read(); // skip '['

                var parsing = true;
                while (parsing)
                {
                    var nextToken = NextToken;
                    switch (nextToken)
                    {
                        case TOKEN.NONE: return null;
                        case TOKEN.SQUARED_CLOSE: parsing = false; break;
                        case TOKEN.COMMA: continue;
                        default: array.Add(ParseByToken(nextToken)); break;
                    }
                }

                return array;
            }

            private string ParseString()
            {
                var s = new StringBuilder();
                _json.Read(); // skip '"'

                var parsing = true;
                while (parsing)
                {
                    if (_json.Peek() == -1)
                    {
                        parsing = false;
                        break;
                    }

                    var c = NextChar;
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (_json.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }

                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    s.Append(c);
                                    break;
                                case 'b': s.Append('\b'); break;
                                case 'f': s.Append('\f'); break;
                                case 'n': s.Append('\n'); break;
                                case 'r': s.Append('\r'); break;
                                case 't': s.Append('\t'); break;
                                case 'u':
                                    var hex = new char[4];
                                    for (var i = 0; i < 4; i++)
                                    {
                                        hex[i] = NextChar;
                                    }
                                    s.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            private object ParseNumber()
            {
                var number = NextWord;

                if (number.Contains("."))
                {
                    double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDouble);
                    return parsedDouble;
                }

                long.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedLong);
                return parsedLong;
            }

            private enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }
        }

        // ============ Serializer ============

        private sealed class Serializer
        {
            private readonly StringBuilder _builder;

            private Serializer()
            {
                _builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();
                instance.SerializeValue(obj);
                return instance._builder.ToString();
            }

            private void SerializeValue(object value)
            {
                if (value == null)
                {
                    _builder.Append("null");
                }
                else if (value is string str)
                {
                    SerializeString(str);
                }
                else if (value is bool b)
                {
                    _builder.Append(b ? "true" : "false");
                }
                else if (value is IList list)
                {
                    SerializeArray(list);
                }
                else if (value is IDictionary dict)
                {
                    SerializeObject(dict);
                }
                else if (value is char c)
                {
                    SerializeString(new string(c, 1));
                }
                else
                {
                    SerializeOther(value);
                }
            }

            private void SerializeObject(IDictionary obj)
            {
                var first = true;
                _builder.Append('{');

                foreach (var e in obj.Keys)
                {
                    if (!first) _builder.Append(',');
                    SerializeString(e.ToString());
                    _builder.Append(':');
                    SerializeValue(obj[e]);
                    first = false;
                }

                _builder.Append('}');
            }

            private void SerializeArray(IList array)
            {
                _builder.Append('[');
                var first = true;

                foreach (var obj in array)
                {
                    if (!first) _builder.Append(',');
                    SerializeValue(obj);
                    first = false;
                }

                _builder.Append(']');
            }

            private void SerializeString(string str)
            {
                _builder.Append('\"');

                foreach (var c in str)
                {
                    switch (c)
                    {
                        case '"': _builder.Append("\\\""); break;
                        case '\\': _builder.Append("\\\\"); break;
                        case '\b': _builder.Append("\\b"); break;
                        case '\f': _builder.Append("\\f"); break;
                        case '\n': _builder.Append("\\n"); break;
                        case '\r': _builder.Append("\\r"); break;
                        case '\t': _builder.Append("\\t"); break;
                        default:
                            var codepoint = Convert.ToInt32(c);
                            if (codepoint >= 32 && codepoint <= 126)
                            {
                                _builder.Append(c);
                            }
                            else
                            {
                                _builder.Append("\\u");
                                _builder.Append(codepoint.ToString("x4"));
                            }
                            break;
                    }
                }

                _builder.Append('\"');
            }

            private void SerializeOther(object value)
            {
                if (value is float f)
                {
                    _builder.Append(f.ToString("R", CultureInfo.InvariantCulture));
                }
                else if (value is double d)
                {
                    _builder.Append(d.ToString("R", CultureInfo.InvariantCulture));
                }
                else if (value is int || value is uint
                    || value is long || value is ulong
                    || value is sbyte || value is byte
                    || value is short || value is ushort)
                {
                    _builder.Append(value);
                }
                else if (value is decimal dec)
                {
                    _builder.Append(dec.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    SerializeString(value.ToString());
                }
            }
        }
    }
}

