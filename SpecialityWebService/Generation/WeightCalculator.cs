using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpecialityWebService.MathObjects;
using static SpecialityWebService.Generation.Lexer;
using System.Globalization;
using static SpecialityWebService.Generation.Parser;

namespace SpecialityWebService.Generation
{
    public class WeightCalculator
    {
        public static List<KeyValuePair<string, double>> ComputeWeight(IEnumerable<Point> edgepoints, Path path, List<KeyValuePair<string, string>> formulas, Dictionary<string, ColumnData> environmentvariables)
        {
            if (edgepoints.Count() < 2)
                throw new ArgumentException("Edge is expected to have 2 or more points as part of its rendered path");

            double distance = 0.0;
            bool firstPoint = true;
            Point prevp = new Point(0.0,0.0);
            foreach (Point p in edgepoints)
            {
                if (firstPoint)
                    distance += prevp.Distance(p);
                firstPoint = false;
                prevp = p;
            }

            path.ColumnValues.Add("distance", new ColumnData(distance.ToString(CultureInfo.InvariantCulture), "infty"));

            return formulas.Select(formula => new KeyValuePair<string, double>(formula.Key, Convert.ToDouble(Parser.ExecuteExpression(formula.Value, ref environmentvariables).Value))).ToList();
        }
    }

    public class ParseException : Exception
    {
        private string _msg, _surrounding;
        private int _row, _col, _linepos;
        public ParseException(string msg, string surrounding, int linepos, int row, int col)
        {
            this._msg = msg;
            this._surrounding = surrounding;
            this._linepos = linepos;
            this._row = row;
            this._col = col;
        }

        public override string ToString()
        {
            return $"{_msg}({_row}:{_col}):\n{_surrounding}\n{Enumerable.Range(0, _linepos).Aggregate("^", (acc, _) => " " + acc)}";
        }
    }

    public class Symbols
    {
        public static List<char> PrintableChars = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => !char.IsControl(c)).ToList();
        public static List<char> Letters = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => char.IsLetterOrDigit(c)).ToList();
        public static List<char> Digits = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => char.IsDigit(c)).ToList();
        public static List<char> Lowercase = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => char.IsLower(c)).ToList();
        public static List<char> Uppercase = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => char.IsUpper(c)).ToList();
        public static List<char> Whitespace = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => char.IsWhiteSpace(c)).ToList();
        public static List<char> StringChars = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => !char.IsControl(c) && c != '\"' && c != '\'').ToList();
        public static List<char> VariableChars = Enumerable.Range(0, char.MaxValue + 1).Select(i => (char)i).Where(c => char.IsLetterOrDigit(c) || c == '_').ToList();
    }

    public class Lexer
    {
        public enum Primitive
        {
            String = 1,
            Boolean = 2,
            Date = 4,
            Time = 8,
            Integer = 16,
            Float = 32,
            Variable = 64
        }

        public enum Operator
        {
            LogicEqual = 0,
            LogicNotEqual = 1,
            LogicLessThan = 2,
            LogicLessThanOrEqual = 3,
            LogicGreaterThan = 4,
            LogicGreaterThanOrEqual = 5,
            LogicNegate = 6,
            LogicAnd = 7,
            LogicOr = 8,

            ArithmeticPlus = 100,
            ArithmeticMinus = 101,
            ArithmeticSign = 102,
            ArithmeticMultiply = 103,
            ArithmeticDivide = 104,
            ArithmeticModulus = 105,
            ArithmeticPower = 106,

            FunctionSquareRoot = 200,
            FunctionPower = 201,
            FunctionFloor = 202,
            FunctionCeil = 203,
            FunctionCosine = 204,
            FunctionSine = 205,
            FunctionTangent = 206,
            FunctionInverseCosine = 207,
            FunctionInverseSine = 208,
            FunctionInverseTangent = 209
        }

        public static List<Token> ExtractPrimitiveTokens(Primitive primitive, string text) => ExtractPrimitiveTokens(primitive, Lexer.GetTokenExpression(text));

        public static List<Token> ExtractPrimitiveTokens(Primitive primitive, Token token)
        {
            if (token.Type == Token.Kind.Primitive)
                return token.Primitive == primitive ? new List<Token>() { token } : new List<Token>();
            else
                return token.Tokens.Select(t => ExtractPrimitiveTokens(primitive, t)).SelectMany(i => i).ToList();
        }

        private static Dictionary<Operator, string> _operatorMap = new Dictionary<Operator, string>() {
            { Operator.LogicEqual, "==" },
            { Operator.LogicNotEqual, "!=" },
            { Operator.LogicLessThan, "<" },
            { Operator.LogicLessThanOrEqual, "<=" },
            { Operator.LogicGreaterThan, ">" },
            { Operator.LogicGreaterThanOrEqual, ">=" },
            { Operator.LogicNegate, "not" },
            { Operator.LogicAnd, "and" },
            { Operator.LogicOr, "or" },

            { Operator.ArithmeticPlus, "+" },
            { Operator.ArithmeticMinus, "-" },
            { Operator.ArithmeticSign, "-" },
            { Operator.ArithmeticMultiply, "*" },
            { Operator.ArithmeticDivide, "/" },
            { Operator.ArithmeticModulus, "%" },
            { Operator.ArithmeticPower, "**" },

            { Operator.FunctionSquareRoot, "sqrt" },
            { Operator.FunctionPower, "pow" },
            { Operator.FunctionFloor, "floor" },
            { Operator.FunctionCeil, "ceil" },
            { Operator.FunctionCosine, "cos" },
            { Operator.FunctionSine, "sin" },
            { Operator.FunctionTangent, "tan" },
            { Operator.FunctionInverseCosine, "acos" },
            { Operator.FunctionInverseSine, "asin" },
            { Operator.FunctionInverseTangent, "atan" }
        };

        private static Dictionary<string, Token> _reservedKeywordsMap = new Dictionary<string, Token>()
        {
            { "true", new Token(true) },
            { "false", new Token(false) },
            { "infty", new Token(double.PositiveInfinity) },
            { "infinite", new Token(double.PositiveInfinity) },
            { "infinity", new Token(double.PositiveInfinity) },
            { "pi", new Token(Math.PI) }
        };

        public class Token
        {
            public enum Kind
            {
                Operation,
                Primitive
            }

            public Kind Type;
            public Operator Operation;
            public Primitive Primitive;
            public List<Token> Tokens;

            public TypeCode TypeCode = TypeCode.Empty;
            public dynamic Value = null;

            public Token(Operator op, List<Token> tokens)
            {
                Type = Kind.Operation;
                Operation = op;
                Tokens = tokens;
            }

            public Token(Operator op, Token token)
            {
                Type = Kind.Operation;
                Operation = op;
                Tokens = new List<Token>() { token };
            }

            public Token(double val)
            {
                Type = Kind.Primitive;
                Primitive = Primitive.Float;
                TypeCode = val.GetTypeCode();
                Value = val;
            }

            public Token(long val)
            {
                Type = Kind.Primitive;
                Primitive = Primitive.Integer;
                TypeCode = val.GetTypeCode();
                Value = val;
            }

            public Token(bool val)
            {
                Type = Kind.Primitive;
                Primitive = Primitive.Boolean;
                TypeCode = val.GetTypeCode();
                Value = val;
            }

            public Token(Primitive prim, string val)
            {
                Type = Kind.Primitive;
                Primitive = prim;
                TypeCode = val.GetTypeCode();
                Value = val;
            }

            public Token(Primitive prim, DateTime dt)
            {
                Type = Kind.Primitive;
                Primitive = prim;
                TypeCode = TypeCode.DateTime;
                Value = dt;
            }

            public override string ToString()
            {
                if (Type == Kind.Primitive)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<");
                    switch (Primitive)
                    {
                        case Primitive.Boolean:
                        case Primitive.Integer:
                        case Primitive.Variable:
                            sb.Append(Value);
                            break;
                        case Primitive.Float:
                            sb.Append(Value).Append('f');
                            break;
                        case Primitive.String:
                            sb.Append('\"').Append(Value).Append('\"');
                            break;
                        case Primitive.Date:
                            DateTime d = (DateTime)Value;
                            sb.Append($"{d.Day}/{d.Month}/{d.Year}");
                            break;
                        case Primitive.Time:
                            DateTime t = (DateTime)Value;
                            sb.Append($"{t.Hour}:{t.Minute}{(t.Second > 0 ? ":" + t.Second : "")}");
                            break;
                    }
                    return sb.Append(">").ToString();
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("(");
                    switch (Operation)
                    {
                        case Operator.LogicEqual:
                            sb.Append(Tokens[0]).Append(" == ").Append(Tokens[1]);
                            break;
                        case Operator.LogicNotEqual:
                            sb.Append(Tokens[0]).Append(" != ").Append(Tokens[1]);
                            break;
                        case Operator.LogicLessThan:
                            sb.Append(Tokens[0]).Append(" < ").Append(Tokens[1]);
                            break;
                        case Operator.LogicLessThanOrEqual:
                            sb.Append(Tokens[0]).Append(" <= ").Append(Tokens[1]);
                            break;
                        case Operator.LogicGreaterThan:
                            sb.Append(Tokens[0]).Append(" > ").Append(Tokens[1]);
                            break;
                        case Operator.LogicGreaterThanOrEqual:
                            sb.Append(Tokens[0]).Append(" >= ").Append(Tokens[1]);
                            break;
                        case Operator.LogicNegate:
                            sb.Append("not ").Append(Tokens[0]);
                            break;
                        case Operator.LogicAnd:
                            sb.Append(Tokens[0]).Append("and").Append(Tokens[1]);
                            break;
                        case Operator.LogicOr:
                            sb.Append(Tokens[0]).Append("or").Append(Tokens[1]);
                            break;
                        case Operator.ArithmeticPlus:
                            sb.Append(Tokens[0]).Append(" + ").Append(Tokens[1]);
                            break;
                        case Operator.ArithmeticMinus:
                            sb.Append(Tokens[0]).Append(" - ").Append(Tokens[1]);
                            break;
                        case Operator.ArithmeticSign:
                            sb.Append("-").Append(Tokens[0]);
                            break;
                        case Operator.ArithmeticMultiply:
                            sb.Append(Tokens[0]).Append(" * ").Append(Tokens[1]);
                            break;
                        case Operator.ArithmeticDivide:
                            sb.Append(Tokens[0]).Append(" / ").Append(Tokens[1]);
                            break;
                        case Operator.ArithmeticModulus:
                            sb.Append(Tokens[0]).Append(" % ").Append(Tokens[1]);
                            break;
                        case Operator.ArithmeticPower:
                            sb.Append(Tokens[0]).Append(" ** ").Append(Tokens[1]);
                            break;
                        case Operator.FunctionSquareRoot:
                            sb.Append("sqrt(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionFloor:
                            sb.Append("floor(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionCeil:
                            sb.Append("ceil(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionCosine:
                            sb.Append("cos(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionSine:
                            sb.Append("sin(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionTangent:
                            sb.Append("tan(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionInverseCosine:
                            sb.Append("acos(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionInverseSine:
                            sb.Append("asin(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                        case Operator.FunctionInverseTangent:
                            sb.Append("atan(").Append(Tokens.Count > 0 ? Tokens[0].ToString() : "").Append(')');
                            break;
                    }
                    return sb.Append(")").ToString();
                }
            }
        }

        private Lexer() { }

        public static Token GetTokenExpression(string input)
        {
            int pointer = 0;
            Token token = E1(ref input, ref pointer, 0, input.Length);
            if (pointer < input.Length)
            {
                Tuple<string, int, int, int> tuple = GetRowCol(ref input, 0, pointer);
                throw new ParseException("Failed to parse expression, likely syntax error", tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
            }
            return token;
        }

        private static Tuple<string, int, int, int> GetRowCol(ref string input, int offset, int pointer)
        {
            int counter = 0;
            int lastnewline = 0;
            int length = input.Length;
            int row = input.Substring(0, Math.Min(offset + pointer, length)).Aggregate(0, (acc, c) => {
                if (c == '\n')
                {
                    acc++;
                    lastnewline = counter + 1 < length ? counter + 1 : counter;
                }
                counter++;
                return acc;
            });
            int nextnewline = input.Substring(lastnewline).ToList().FindIndex(c => c == '\n');
            nextnewline = nextnewline > -1 ? nextnewline - lastnewline : length - lastnewline;

            int col = input.Substring(0, Math.Min(offset + pointer, length)).Aggregate(0, (acc, c) => acc = (c == '\n') ? 0 : acc + 1);
            return Tuple.Create(input.Substring(lastnewline, nextnewline < 0 ? 0 : nextnewline), pointer - lastnewline, row, col);
        }

        private static bool IsWhitespace(ref string input, int pointer, int offset, int length)
        {
            return pointer == length || pointer < length && Symbols.Whitespace.Contains(input[offset + pointer]);
        }

        private static void SkipWhitespace(ref string input, ref int pointer, int offset, int length)
        {
            while (IsWhitespace(ref input, pointer, offset, length))
                pointer++;
        }

        private static string ConsumeWhile(ref string input, ref int pointer, int offset, int length, Func<char, bool> f, bool requireWhitespace = false)
        {
            StringBuilder sb = new StringBuilder();
            while (pointer < length && f(input[offset + pointer]))
            {
                sb.Append(input[offset + pointer]);
                pointer++;
            }
            if (pointer == length && !f(input[offset + pointer - 1]))
            {
                Tuple<string, int, int, int> tuple = GetRowCol(ref input, offset, pointer);
                throw new ParseException("Error while consuming input", tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
            }
            if (requireWhitespace && !IsWhitespace(ref input, pointer, offset, length))
            {
                Tuple<string, int, int, int> tuple = GetRowCol(ref input, offset, pointer);
                throw new ParseException("Expected whitespace after consumer", tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
            }
            return sb.ToString();
        }

        private static bool ConsumeFloat(ref string input, ref int pointer, int offset, int length, out double value, bool requireWhitespace = false)
        {
            value = -1;
            int oldpointer = pointer;

            string value1, value2;
            value1 = ConsumeWhile(ref input, ref pointer, offset, length, (c) => Symbols.Digits.Contains(c), false);

            //Also allow for .02 to be legal
            if (ConsumeKeyword(ref input, ref pointer, offset, length, ".", false))
                value2 = ConsumeWhile(ref input, ref pointer, offset, length, (c) => Symbols.Digits.Contains(c), false);
            else
            {
                pointer = oldpointer;
                return false;
            }

            //Second part after decimal should ALWAYS be non-empty
            if (value2 == "" || requireWhitespace && !IsWhitespace(ref input, pointer, offset, length))
            {
                pointer = oldpointer;
                return false;
            }

            if (double.TryParse($"{value1}.{value2}", out value))
                return true;
            else
            {
                pointer = oldpointer;
                return false;
            }
        }

        private static bool ConsumeTime(ref string input, ref int pointer, int offset, int length, out DateTime value, bool requireWhitespace = false)
        {
            value = DateTime.MinValue;
            int oldpointer = pointer;

            string hour, minute, second = "";

            //Parse hour
            int counter = 0;
            hour = ConsumeWhile(ref input, ref pointer, offset, length, (c) => {
                counter += counter < 2 ? 1 : 0;
                return counter <= 2 && Symbols.Digits.Contains(c);
            }, false);

            if (hour.Length > 0 && ConsumeKeyword(ref input, ref pointer, offset, length, ":", false))
            {
                //Parse minute
                counter = 0;
                minute = ConsumeWhile(ref input, ref pointer, offset, length, (c) => {
                    counter += counter < 2 ? 1 : 0;
                    return counter <= 2 && Symbols.Digits.Contains(c);
                }, false);
            }
            else
            {
                pointer = oldpointer;
                return false;
            }

            if (minute.Length == 0)
            {
                pointer = oldpointer;
                return false;
            }

            if (ConsumeKeyword(ref input, ref pointer, offset, length, ":", false))
            {
                //Parse seconds
                counter = 0;
                second = ConsumeWhile(ref input, ref pointer, offset, length, (c) => {
                    counter += counter < 2 ? 1 : 0;
                    return counter <= 2 && Symbols.Digits.Contains(c);
                }, false);
            }

            if (DateTime.TryParse($"{hour}:{minute}{(second.Length > 0 ? ":" + second : "")}", out value))
                return true;
            else
            {
                pointer = oldpointer;
                return false;
            }
        }

        private static bool ConsumeDate(ref string input, ref int pointer, int offset, int length, out DateTime value, bool requireWhitespace = false)
        {
            value = DateTime.MinValue;
            int oldpointer = pointer;

            string day, month, year = "";

            //Parse day
            int counter = 0;
            day = ConsumeWhile(ref input, ref pointer, offset, length, (c) => {
                counter += counter < 2 ? 1 : 0;
                return counter <= 2 && Symbols.Digits.Contains(c);
            }, false);

            if (day.Length > 0 && ConsumeKeyword(ref input, ref pointer, offset, length, "/", false))
            {
                //Parse month
                counter = 0;
                month = ConsumeWhile(ref input, ref pointer, offset, length, (c) => {
                    counter += counter < 2 ? 1 : 0;
                    return counter <= 2 && Symbols.Digits.Contains(c);
                }, false);
            }
            else
            {
                pointer = oldpointer;
                return false;
            }

            if (month.Length == 0)
            {
                pointer = oldpointer;
                return false;
            }

            if (ConsumeKeyword(ref input, ref pointer, offset, length, "/", false))
            {
                //Parse year
                counter = 0;
                year = ConsumeWhile(ref input, ref pointer, offset, length, (c) => {
                    counter += counter < 4 ? 1 : 0;
                    return counter <= 4 && Symbols.Digits.Contains(c);
                }, false);
            }

            if (requireWhitespace && IsWhitespace(ref input, pointer, offset, length) || !requireWhitespace)
            {
                if (DateTime.TryParse($"{month}/{day}{(year.Length > 0 ? "/" + year : "")} 12:00 AM", out value))
                    return true;
                else
                {
                    pointer = oldpointer;
                    return false;
                }
            }
            else
            {
                pointer = oldpointer;
                return false;
            }
        }

        private static bool ConsumeInteger(ref string input, ref int pointer, int offset, int length, out long value, bool requireWhitespace = false)
        {
            value = -1;
            int oldpointer = pointer;

            string value1 = ConsumeWhile(ref input, ref pointer, offset, length, (c) => Symbols.Digits.Contains(c), false);

            //Second part after decimal should ALWAYS be non-empty
            if (value1 == "" || requireWhitespace && !IsWhitespace(ref input, pointer, offset, length))
            {
                pointer = oldpointer;
                return false;
            }

            if (long.TryParse($"{value1}", out value))
                return true;
            else
            {
                pointer = oldpointer;
                return false;
            }
        }

        private static bool ConsumeString(ref string input, ref int pointer, int offset, int length, out string value, bool requireWhitespace = false)
        {
            value = null;
            int oldpointer = pointer;

            if (pointer + 1 >= length)
                return false;

            if ("\"'".Contains(input[offset + pointer]))
            {
                char symbol = input[offset + pointer];
                pointer++;
                string value1 = ConsumeWhile(ref input, ref pointer, offset, length, (c) => Symbols.StringChars.Contains(c));
                if (symbol == input[offset + pointer])
                {
                    pointer++;
                    if (requireWhitespace && IsWhitespace(ref input, pointer, offset, length) || !requireWhitespace)
                    {
                        value = value1;
                        return true;
                    }
                    else
                    {
                        pointer = oldpointer;
                        return false;
                    }
                }
                else
                {
                    pointer = oldpointer;
                    return false;
                }
            }
            else
                return false;
        }

        private static bool ConsumeVariable(ref string input, ref int pointer, int offset, int length, out string value, bool requireWhitespace = false)
        {
            value = null;
            int oldpointer = pointer;

            if (pointer >= length)
                return false;

            if (Symbols.VariableChars.Contains(input[offset + pointer]) && !Symbols.Digits.Contains(input[offset + pointer]))
            {
                string value1 = ConsumeWhile(ref input, ref pointer, offset, length, (c) => Symbols.VariableChars.Contains(c));
                if (requireWhitespace && IsWhitespace(ref input, pointer, offset, length) || !requireWhitespace)
                {
                    value = value1;
                    return true;
                }
                else
                {
                    pointer = oldpointer;
                    return false;
                }
            }
            else
                return false;
        }

        public static bool BetweenSymbols(string input, string left, string right, out string output)
        {
            int pointer = 0;
            return BetweenSymbols(ref input, ref pointer, 0, input.Length, out output, left, right);
        }

        private static bool BetweenSymbols(ref string input, ref int pointer, int offset, int length, out string value, string left, string right, bool requireWhitespace = false)
        {
            value = null;

            if (pointer + left.Length >= length)
                return false;

            int oldpointer = pointer;

            if (input.Substring(offset + pointer, left.Length) == left)
                pointer += left.Length;
            else
                return false;


            string lefttmp = "", righttmp = "";
            int leftpos = 0, rightpos = 0;
            int leftcounter = 0;
            string value1 = ConsumeWhile(ref input, ref pointer, offset, length, (c) => {
                if (c == right[rightpos])
                {
                    righttmp += right[rightpos];
                    if (righttmp == right && leftcounter == 0)
                        return false;
                    else if (righttmp == right && leftcounter > 0)
                    {
                        leftcounter--;
                        rightpos = 0;
                        righttmp = "";
                    }
                    else
                        rightpos++;
                }
                else
                {
                    rightpos = 0;
                    righttmp = "";
                }

                if (c == left[leftpos])
                {
                    lefttmp += left[leftpos];
                    if (lefttmp == left)
                    {
                        leftcounter++;
                        leftpos = 0;
                        lefttmp = "";
                    }
                    else
                        leftpos++;
                }
                else
                {
                    leftpos = 0;
                    lefttmp = "";
                }
                return true;
            });

            if (input.Substring(offset + pointer - right.Length + 1, right.Length) != right)
            {
                pointer = oldpointer;
                return false;
            }

            if (requireWhitespace && IsWhitespace(ref input, pointer, offset, length) || !requireWhitespace)
            {
                pointer++;
                value = value1.Substring(0, value1.Length - right.Length + 1);
                return true;
            }
            else
            {
                pointer = oldpointer;
                return false;
            }
        }

        private static bool ConsumeKeyword(ref string input, ref int pointer, int offset, int length, string keyword, bool requireWhitespace)
        {
            if (pointer + keyword.Length >= length)
                return false;

            int oldpointer = pointer;
            pointer += keyword.Length;
            if (input.Substring(offset + oldpointer, keyword.Length) == keyword && (requireWhitespace && IsWhitespace(ref input, pointer, offset, length) || !requireWhitespace))
                return true;
            else
            {
                pointer = oldpointer;
                return false;
            }
        }

        private static bool ConsumeArguments(ref string input, ref int pointer, int offset, int length, string separator, out List<string> arguments)
        {
            arguments = input.Substring(offset + pointer, length).Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return arguments.Count > 0;
        }

        private static Token E1(ref string input, ref int pointer, int offset, int length)
        {
            Token left = E2(ref input, ref pointer, offset, length);
            SkipWhitespace(ref input, ref pointer, offset, length);
            return T1(ref input, ref pointer, offset, length)(left);
        }

        private static Func<Token, Token> T1(ref string input, ref int pointer, int offset, int length)
        {
            SkipWhitespace(ref input, ref pointer, offset, length);
            if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicOr], false))
            {
                Token right = E2(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T1(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.LogicOr, new List<Token>() { left, right }));
            }
            return (Token t) => t;
        }

        private static Token E2(ref string input, ref int pointer, int offset, int length)
        {
            Token left = E3(ref input, ref pointer, offset, length);
            SkipWhitespace(ref input, ref pointer, offset, length);
            return T2(ref input, ref pointer, offset, length)(left);
        }

        private static Func<Token, Token> T2(ref string input, ref int pointer, int offset, int length)
        {
            SkipWhitespace(ref input, ref pointer, offset, length);
            if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicAnd], false))
            {
                Token right = E3(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T2(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.LogicAnd, new List<Token>() { left, right }));
            }
            return (Token t) => t;
        }

        private static Token E3(ref string input, ref int pointer, int offset, int length)
        {
            Token left = E4(ref input, ref pointer, offset, length);
            SkipWhitespace(ref input, ref pointer, offset, length);
            if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicEqual], false))
                return new Token(Operator.LogicEqual, new List<Token>() { left, E3(ref input, ref pointer, offset, length) });
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicNotEqual], false))
                return new Token(Operator.LogicNotEqual, new List<Token>() { left, E3(ref input, ref pointer, offset, length) });
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicLessThan], false))
                return new Token(Operator.LogicLessThan, new List<Token>() { left, E3(ref input, ref pointer, offset, length) });
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicLessThanOrEqual], false))
                return new Token(Operator.LogicLessThanOrEqual, new List<Token>() { left, E3(ref input, ref pointer, offset, length) });
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicGreaterThan], false))
                return new Token(Operator.LogicGreaterThan, new List<Token>() { left, E3(ref input, ref pointer, offset, length) });
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.LogicGreaterThanOrEqual], false))
                return new Token(Operator.LogicGreaterThanOrEqual, new List<Token>() { left, E3(ref input, ref pointer, offset, length) });
            return left;
        }

        private static Token E4(ref string input, ref int pointer, int offset, int length)
        {
            Token left = E5(ref input, ref pointer, offset, length);
            SkipWhitespace(ref input, ref pointer, offset, length);
            return T4(ref input, ref pointer, offset, length)(left);
        }

        private static Func<Token, Token> T4(ref string input, ref int pointer, int offset, int length)
        {
            SkipWhitespace(ref input, ref pointer, offset, length);
            if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.ArithmeticPlus], false))
            {
                Token right = E5(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T4(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.ArithmeticPlus, new List<Token>() { left, right }));
            }
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.ArithmeticMinus], false))
            {
                Token right = E5(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T4(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.ArithmeticMinus, new List<Token>() { left, right }));
            }
            return (Token t) => t;
        }

        private static Token E5(ref string input, ref int pointer, int offset, int length)
        {
            Token left = E6(ref input, ref pointer, offset, length);
            SkipWhitespace(ref input, ref pointer, offset, length);
            return T5(ref input, ref pointer, offset, length)(left);
        }

        private static Func<Token, Token> T5(ref string input, ref int pointer, int offset, int length)
        {
            SkipWhitespace(ref input, ref pointer, offset, length);
            if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.ArithmeticMultiply], false))
            {
                Token right = E6(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T5(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.ArithmeticMultiply, new List<Token>() { left, right }));
            }
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.ArithmeticDivide], false))
            {
                Token right = E6(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T5(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.ArithmeticDivide, new List<Token>() { left, right }));
            }
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.ArithmeticModulus], false))
            {
                Token right = E6(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T5(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.ArithmeticModulus, new List<Token>() { left, right }));
            }
            return (Token left) => left;
        }

        private static Token E6(ref string input, ref int pointer, int offset, int length)
        {
            Token left = E7(ref input, ref pointer, offset, length);
            SkipWhitespace(ref input, ref pointer, offset, length);
            return T6(ref input, ref pointer, offset, length)(left);
        }

        private static Func<Token, Token> T6(ref string input, ref int pointer, int offset, int length)
        {
            SkipWhitespace(ref input, ref pointer, offset, length);
            if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.ArithmeticPower], false))
            {
                Token right = E7(ref input, ref pointer, offset, length);
                Func<Token, Token> f = T6(ref input, ref pointer, offset, length);
                return (Token left) => f(new Token(Operator.ArithmeticPower, new List<Token>() { left, right }));
            }
            return (Token t) => t;
        }

        private static Token E7(ref string input, ref int pointer, int offset, int length)
        {
            SkipWhitespace(ref input, ref pointer, offset, length);
            string tmp;
            if (BetweenSymbols(ref input, ref pointer, offset, length, out tmp, "(", ")", false))
            {
                int pointer2 = 0;
                return E1(ref input, ref pointer2, pointer - 1 - tmp.Length, tmp.Length);
            }
            else if (ConsumeFloat(ref input, ref pointer, offset, length, out double f_val, false))
            {
                if (BetweenSymbols(ref input, ref pointer, offset, length, out tmp, "(", ")", false))
                {
                    int pointer2 = 0;
                    return new Token(Operator.ArithmeticMultiply, new List<Token>() { new Token(f_val), E1(ref input, ref pointer2, pointer - 1 - tmp.Length + offset, tmp.Length) });
                }
                else
                    return new Token(f_val);
            }
            else if (ConsumeDate(ref input, ref pointer, offset, length, out DateTime d_val, true))
                return new Token(Primitive.Date, d_val);
            else if (ConsumeTime(ref input, ref pointer, offset, length, out DateTime t_val, true))
                return new Token(Primitive.Time, t_val);
            else if (ConsumeInteger(ref input, ref pointer, offset, length, out long i_val, false))
            {
                if (BetweenSymbols(ref input, ref pointer, offset, length, out tmp, "(", ")", false))
                {
                    int pointer2 = 0;
                    return new Token(Operator.ArithmeticMultiply, new List<Token>() { new Token(i_val), E1(ref input, ref pointer2, pointer - 1 - tmp.Length + offset, tmp.Length) });
                }
                else
                    return new Token(i_val);
            }
            else if (ConsumeKeyword(ref input, ref pointer, offset, length, _operatorMap[Operator.ArithmeticSign], false))
                return new Token(Operator.ArithmeticSign, E4(ref input, ref pointer, offset, length));
            else if (ConsumeString(ref input, ref pointer, offset, length, out string s_val, true))
                return new Token(Primitive.String, s_val);
            else if (ConsumeVariable(ref input, ref pointer, offset, length, out string v_val, true))
            {
                if (_reservedKeywordsMap.ContainsKey(v_val))
                    return _reservedKeywordsMap[v_val];
                else
                    return new Token(Primitive.Variable, v_val);
            }
            else if (ConsumeVariable(ref input, ref pointer, offset, length, out string fun_val, false))
            {
                if (_reservedKeywordsMap.ContainsKey(fun_val))
                    return _reservedKeywordsMap[fun_val];

                //Iterate through all simple functions
                for (int i = 200; i <= 209; i++)
                {
                    if (fun_val == _operatorMap[(Operator)i] && BetweenSymbols(ref input, ref pointer, offset, length, out tmp, "(", ")", false))
                    {
                        int pointer2 = 0;
                        bool nonzero = ConsumeArguments(ref tmp, ref pointer2, 0, tmp.Length, ",", out List<string> arguments);
                        return new Token((Operator)i, arguments.Select(arg => { int p = 0; return E1(ref arg, ref p, 0, arg.Length); }).ToList());
                    }
                }
            }

            Tuple<string, int, int, int> tuple = GetRowCol(ref input, offset, pointer);
            throw new ParseException("Could not parse expression", tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }
    }

    public class RuntimeException : Exception
    {
        private string _desc;
        private Token _token;
        public RuntimeException(string desc, Token token)
        {
            this._desc = desc;
            this._token = token;
        }

        public override string ToString()
        {
            return $"{_desc}, while executing: {_token}";
        }
    }

    public class Parser
    {
        public struct ColumnData
        {
            public string Value;
            public ColumnData(string value, string defaultvalue = "NULL")
            {
                Value = string.IsNullOrEmpty(value) ? defaultvalue : value;
            }
        }

        private Parser() { }

        private static Dictionary<Operator, Func<IComparable, IComparable, bool>> _logicComparisonMap = new Dictionary<Operator, Func<IComparable, IComparable, bool>>()
        {
            { Operator.LogicEqual, (left, right) => left.CompareTo(right) == 0 },
            { Operator.LogicNotEqual, (left, right) => left.CompareTo(right) != 0 },
            { Operator.LogicLessThan, (left, right) => left.CompareTo(right) < 0 },
            { Operator.LogicLessThanOrEqual, (left, right) => left.CompareTo(right) <= 0 },
            { Operator.LogicGreaterThan, (left, right) => left.CompareTo(right) > 0 },
            { Operator.LogicGreaterThanOrEqual, (left, right) => left.CompareTo(right) >= 0 }
        };

        private static Dictionary<Operator, Func<double, double, double>> _arithmeticFloatOperatorMap = new Dictionary<Operator, Func<double, double, double>>()
        {
            { Operator.ArithmeticPlus, (left, right) => left + right},
            { Operator.ArithmeticMinus, (left, right) => left - right},
            { Operator.ArithmeticMultiply, (left, right) => left * right},
            { Operator.ArithmeticDivide, (left, right) => left / right},
            { Operator.ArithmeticPower, (left, right) => Math.Pow(left, right)}
        };

        public class ReturnValue
        {
            public Type Type;
            public dynamic Value;

            public ReturnValue(dynamic value, Type type)
            {
                this.Type = type;
                this.Value = type == typeof(double) && double.IsNaN(value) ? 0.0 : value;
            }
        }

        private static ReturnValue CastPrimitive<T>(Token token)
        {
            if (token.Value is T)
            {
                try { return new ReturnValue((T)token.Value, typeof(T)); }
                catch (Exception) { throw new RuntimeException("Incompatible types attempted to be cast", token); }
            }
            else
                throw new RuntimeException("Descriped type did not match the value", token);
        }

        public static ReturnValue ExecuteExpression(string input, ref Dictionary<string, ColumnData> environment) => ExecuteExpression(Lexer.GetTokenExpression(input), ref environment);

        public static ReturnValue ExecuteExpression(Token token, ref Dictionary<string, ColumnData> environment)
        {
            switch (token.Type)
            {
                case Token.Kind.Primitive:
                    switch (token.Primitive)
                    {
                        case Primitive.Boolean:
                            return CastPrimitive<bool>(token);
                        case Primitive.Integer:
                            return CastPrimitive<long>(token);
                        case Primitive.Float:
                            return CastPrimitive<double>(token);
                        case Primitive.Variable:
                            string value = environment[token.Value].Value;
                            if (bool.TryParse(value, out bool res1))
                            {
                                token.Value = res1;
                                return CastPrimitive<bool>(token);
                            }
                            else if (long.TryParse(value, out long res2))
                            {
                                token.Value = res2;
                                return CastPrimitive<long>(token);
                            }
                            else if (double.TryParse(value, out double res3))
                            {
                                token.Value = res3;
                                return CastPrimitive<double>(token);
                            }
                            else
                            {
                                token.Value = value;
                                return CastPrimitive<string>(token);
                            }
                        case Primitive.String:
                            return CastPrimitive<string>(token);
                    }
                    break;
                case Token.Kind.Operation:
                    switch (token.Operation)
                    {
                        case Operator.LogicEqual:
                        case Operator.LogicNotEqual:
                        case Operator.LogicLessThan:
                        case Operator.LogicLessThanOrEqual:
                        case Operator.LogicGreaterThan:
                        case Operator.LogicGreaterThanOrEqual:
                            ReturnValue left1 = ExecuteExpression(token.Tokens[0], ref environment);
                            ReturnValue right1 = ExecuteExpression(token.Tokens[1], ref environment);
                            if (left1.Type == typeof(double) || right1.Type == typeof(double))
                            {
                                left1 = new ReturnValue(Convert.ToDouble(left1.Value), typeof(double));
                                right1 = new ReturnValue(Convert.ToDouble(right1.Value), typeof(double));
                            }
                            else if (left1.Type == typeof(long) || right1.Type == typeof(long))
                            {
                                left1 = new ReturnValue(Convert.ToInt64(left1.Value), typeof(long));
                                right1 = new ReturnValue(Convert.ToInt64(right1.Value), typeof(long));
                            }
                            else if (left1.Type == typeof(bool) || right1.Type == typeof(bool))
                            {
                                left1 = new ReturnValue(Convert.ToBoolean(left1.Value), typeof(bool));
                                right1 = new ReturnValue(Convert.ToBoolean(right1.Value), typeof(bool));
                            }
                            else
                            {
                                left1 = new ReturnValue(Convert.ToString(left1.Value), typeof(string));
                                right1 = new ReturnValue(Convert.ToString(right1.Value), typeof(string));
                            }
                            if (left1.Value is IComparable l1 && right1.Value is IComparable r1)
                            {
                                try { return new ReturnValue(_logicComparisonMap[token.Operation](l1, r1), typeof(bool)); }
                                catch (Exception) { throw new RuntimeException("Incompatible types compared", token); }
                            }
                            else if (left1.Value is IComparable)
                                throw new RuntimeException("Right hand side of expression is not compariable", token);
                            else
                                throw new RuntimeException("Left hand side of expression is not compariable", token);
                        case Operator.LogicNegate:
                            ReturnValue left2 = ExecuteExpression(token.Tokens[0], ref environment);
                            if (left2.Type == typeof(bool))
                                return new ReturnValue(!(bool)left2.Value, typeof(bool));
                            else
                                throw new RuntimeException("Expression was not a boolean", token);
                        case Operator.LogicAnd:
                            ReturnValue left3 = ExecuteExpression(token.Tokens[0], ref environment);
                            if (left3.Type == typeof(bool) && (bool)left3.Value == true)
                            {
                                ReturnValue right3 = ExecuteExpression(token.Tokens[1], ref environment);
                                if (right3.Type == typeof(bool))
                                    return new ReturnValue((bool)right3.Value, typeof(bool));
                                else
                                    throw new RuntimeException("Right expression was not a boolean", token);
                            }
                            else if (left3.Type == typeof(bool) && (bool)left3.Value == false)
                                return new ReturnValue(false, typeof(bool));
                            else
                                throw new RuntimeException("Left expression was not a boolean", token);
                        case Operator.LogicOr:
                            ReturnValue left4 = ExecuteExpression(token.Tokens[0], ref environment);
                            if (left4.Type == typeof(bool) && (bool)left4.Value == false)
                            {
                                ReturnValue right4 = ExecuteExpression(token.Tokens[1], ref environment);
                                if (right4.Type == typeof(bool))
                                    return new ReturnValue((bool)right4.Value, typeof(bool));
                                else
                                    throw new RuntimeException("Right expression was not a boolean", token);
                            }
                            else if (left4.Type == typeof(bool) && (bool)left4.Value == true)
                                return new ReturnValue(true, typeof(bool));
                            else
                                throw new RuntimeException("Left expression was not a boolean", token);
                        case Operator.ArithmeticPlus:
                        case Operator.ArithmeticMinus:
                        case Operator.ArithmeticMultiply:
                        case Operator.ArithmeticDivide:
                        case Operator.ArithmeticPower:
                            ReturnValue left8 = ExecuteExpression(token.Tokens[0], ref environment);
                            ReturnValue right8 = ExecuteExpression(token.Tokens[1], ref environment);
                            if ((left8.Type == typeof(double) || left8.Type == typeof(long) || left8.Type == typeof(bool)) 
                               && (right8.Type == typeof(double) || right8.Type == typeof(long) || right8.Type == typeof(bool)))
                            {
                                try { return new ReturnValue(_arithmeticFloatOperatorMap[token.Operation](Convert.ToDouble(left8.Value), Convert.ToDouble(right8.Value)), typeof(double)); }
                                catch (Exception ex) { throw new RuntimeException(ex.Message, token); }
                            }
                            else if (left8.Type == typeof(double) || left8.Type == typeof(long) || left8.Type == typeof(bool))
                                throw new RuntimeException("Right hand side of expression is not compatible with operation", token);
                            else
                                throw new RuntimeException("Left hand side of expression is not compatible with operation", token);
                        case Operator.ArithmeticSign:
                            ReturnValue left9 = ExecuteExpression(token.Tokens[0], ref environment);
                            if (left9.Type == typeof(double))
                            {
                                try { return new ReturnValue(-(double)left9.Value, typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible types attempted to be cast", token); }
                            }
                            else if (left9.Type == typeof(long))
                            {
                                try { return new ReturnValue(-(long)left9.Value, typeof(long)); }
                                catch (Exception) { throw new RuntimeException("Incompatible types attempted to be cast", token); }
                            }
                            else if (left9.Type == typeof(bool))
                            {
                                try { return new ReturnValue(-Convert.ToInt64(left9.Value), typeof(long)); }
                                catch (Exception) { throw new RuntimeException("Incompatible types attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Expression is not compatible with operation", token);
                        case Operator.ArithmeticModulus:
                            ReturnValue left10 = ExecuteExpression(token.Tokens[0], ref environment);
                            ReturnValue right10 = ExecuteExpression(token.Tokens[1], ref environment);
                            if (left10.Type == typeof(long) && right10.Type == typeof(long))
                            {
                                try { return new ReturnValue((long)left10.Value % (long)right10.Value, typeof(long)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else if (left10.Type == typeof(long))
                                throw new RuntimeException("Right hand side of expression is not compatible with operation", token);
                            else
                                throw new RuntimeException("Left hand side of expression is not compatible with operation", token);
                        case Operator.FunctionSquareRoot:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue sqt = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue(Math.Sqrt(sqt.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionPower:
                            if (token.Tokens.Count > 0 && token.Tokens.Count <= 2)
                            {
                                ReturnValue root = ExecuteExpression(token.Tokens[0], ref environment);
                                double power = 2;
                                if (token.Tokens.Count == 2)
                                {
                                    ReturnValue pow = ExecuteExpression(token.Tokens[1], ref environment);
                                    if (pow.Type == typeof(double) || pow.Type == typeof(long))
                                        power = (double)pow.Value;
                                    else
                                        throw new RuntimeException("Invalid datatype provided in the power operation", token);
                                }
                                try { return new ReturnValue(Math.Pow(root.Value, power), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionFloor:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue floor = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Floor(floor.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionCeil:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue ceil = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Ceiling(ceil.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionCosine:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue cos = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Cos(cos.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionSine:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue sin = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Sin(sin.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionTangent:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue tan = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Tan(tan.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionInverseCosine:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue invcos = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Acos(invcos.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionInverseSine:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue invsin = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Asin(invsin.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                        case Operator.FunctionInverseTangent:
                            if (token.Tokens.Count == 1)
                            {
                                ReturnValue invtan = ExecuteExpression(token.Tokens[0], ref environment);
                                try { return new ReturnValue((double)Math.Atan(invtan.Value), typeof(double)); }
                                catch (Exception) { throw new RuntimeException("Incompatible attempted to be cast", token); }
                            }
                            else
                                throw new RuntimeException("Incorrect syntax parsed to function", token);
                    }
                    break;
            }
            return null;
        }
    }
}
