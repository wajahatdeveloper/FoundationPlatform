#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Framework.Inspector.Editor
{
    /// <summary>
    /// Resolves the member-name / "$member" / "@expression" strings that the attributes accept
    /// (ShowIf, EnableIf, ValidateInput, GUIColor getters, ValueDropdown, dynamic labels, ...)
    /// against a target object via reflection.
    ///
    /// Supported resolver forms —
    ///  * bare member name (field / property / parameterless method), including dotted chains "a.b.c";
    ///  * "$member" value references in display strings (labels, titles, info boxes, button names);
    ///  * "@expression" boolean expressions with !, &amp;&amp;, ||, parentheses and the comparison
    ///    operators == != &gt;= &lt;= &gt; &lt; over members, enum literals (Type.Member or bare
    ///    Member), numbers, quoted strings, true/false/null, and "member.HasFlag(EnumLiteral)".
    /// Full C# expression compilation (general method calls with args, arithmetic, ternary) is out of scope;
    /// such strings return <c>failed = true</c> and callers degrade gracefully (skip the color,
    /// treat the condition as its fallback) rather than throwing.
    /// </summary>
    internal static class InspectorMemberResolver
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        // ------------------------------------------------------------------ member access

        /// <summary>Resolve a value from a member name (field/property/parameterless method), supporting dotted chains.</summary>
        public static object GetValue(object target, string nameOrExpr, out bool failed)
        {
            failed = false;
            if (target == null || string.IsNullOrEmpty(nameOrExpr))
            {
                failed = true;
                return null;
            }

            string name = nameOrExpr;
            if (name[0] == '@' || name[0] == '$')
                name = name.Substring(1);
            name = name.Trim();

            if (!IsMemberChain(name))
            {
                failed = true;
                return null;
            }

            return GetMemberChainValue(target, name, out failed);
        }

        private static object GetMemberChainValue(object target, string chain, out bool failed)
        {
            failed = false;
            object current = target;
            int start = 0;
            while (start < chain.Length)
            {
                int dot = chain.IndexOf('.', start);
                string segment = dot < 0 ? chain.Substring(start) : chain.Substring(start, dot - start);
                if (current == null) { failed = true; return null; }

                current = GetSingleMember(current, segment, out bool segFailed);
                if (segFailed) { failed = true; return null; }

                if (dot < 0) break;
                start = dot + 1;
            }
            return current;
        }

        private static object GetSingleMember(object target, string name, out bool failed)
        {
            failed = false;
            Type t = target.GetType();
            try
            {
                for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                {
                    const BindingFlags F = BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                    var field = cur.GetField(name, F);
                    if (field != null) return field.GetValue(field.IsStatic ? null : target);

                    var prop = cur.GetProperty(name, F);
                    if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                        return prop.GetValue(prop.GetGetMethod(true).IsStatic ? null : target);

                    var method = cur.GetMethod(name, F, null, Type.EmptyTypes, null);
                    if (method != null && method.ReturnType != typeof(void))
                        return method.Invoke(method.IsStatic ? null : target, null);
                }
            }
            catch
            {
                failed = true;
                return null;
            }

            failed = true;
            return null;
        }

        // ------------------------------------------------------------------ display strings

        /// <summary>
        /// Resolve a display string: "$member" → member value, "@expr" → member/expression value,
        /// anything else → the literal string. Never fails — unresolved references return the raw text
        /// without its prefix.
        /// </summary>
        public static string ResolveString(object target, string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            char c = text[0];
            if (c != '$' && c != '@') return text;

            var v = GetValue(target, text, out bool failed);
            if (!failed) return v?.ToString() ?? string.Empty;

            // '@' may be a full expression that evaluates to a value we can stringify.
            if (c == '@' && TryEvaluateExpression(target, text.Substring(1), out object result))
                return result?.ToString() ?? string.Empty;

            return text.Substring(1);
        }

        // ------------------------------------------------------------------ conditions

        /// <summary>Evaluate a condition member/expression as a boolean, honoring the optional compare value.</summary>
        public static bool EvaluateBool(object target, string condition, object compareValue, bool hasValue, bool fallback)
        {
            if (string.IsNullOrEmpty(condition)) return fallback;

            if (condition[0] == '@')
            {
                if (TryEvaluateExpression(target, condition.Substring(1), out object result))
                    return ToBool(result);
                return fallback;
            }

            var val = GetValue(target, condition, out bool failed);
            if (failed)
            {
                // Bare string may still be an expression (conditions are accepted without '@').
                if (TryEvaluateExpression(target, condition, out object result))
                    return ToBool(result);
                return fallback;
            }

            if (hasValue)
                return ValuesEqual(val, compareValue);

            return ToBool(val);
        }

        private static bool ToBool(object val)
        {
            if (val is bool b) return b;
            if (val == null) return false;
            // Non-bool, no compare value: treat "has a value" as true.
            if (val is UnityEngine.Object uo) return uo != null;
            return true;
        }

        // ------------------------------------------------------------------ expression evaluator
        // Grammar:  or   := and ('||' and)*
        //           and  := not ('&&' not)*
        //           not  := '!' not | rel
        //           rel  := operand (('=='|'!='|'>='|'<='|'>'|'<') operand)?
        //           operand := '(' or ')' | literal | memberChain
        // Values: bool, double, string, enum, object references. Comparisons coerce enum→long,
        // numerics→double; equality falls back to string comparison.

        public static bool TryEvaluateExpression(object target, string expr, out object result)
        {
            result = null;
            if (string.IsNullOrEmpty(expr)) return false;
            try
            {
                var tokens = Tokenize(expr);
                if (tokens == null || tokens.Count == 0) return false;
                int pos = 0;
                if (!ParseOr(target, tokens, ref pos, out result)) return false;
                return pos == tokens.Count;
            }
            catch
            {
                return false;
            }
        }

        private readonly struct Token
        {
            public enum Kind { Identifier, Number, String, Op, LParen, RParen }
            public readonly Kind K;
            public readonly string Text;
            public Token(Kind k, string text) { K = k; Text = text; }
        }

        private static List<Token> Tokenize(string s)
        {
            var tokens = new List<Token>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '(') { tokens.Add(new Token(Token.Kind.LParen, "(")); i++; continue; }
                if (c == ')') { tokens.Add(new Token(Token.Kind.RParen, ")")); i++; continue; }
                if (c == '"')
                {
                    int end = s.IndexOf('"', i + 1);
                    if (end < 0) return null;
                    tokens.Add(new Token(Token.Kind.String, s.Substring(i + 1, end - i - 1)));
                    i = end + 1;
                    continue;
                }
                if (c == '&' || c == '|' || c == '=' || c == '!' || c == '>' || c == '<')
                {
                    if (i + 1 < s.Length)
                    {
                        string two = s.Substring(i, 2);
                        if (two == "&&" || two == "||" || two == "==" || two == "!=" || two == ">=" || two == "<=")
                        {
                            tokens.Add(new Token(Token.Kind.Op, two));
                            i += 2;
                            continue;
                        }
                    }
                    if (c == '!' || c == '>' || c == '<')
                    {
                        tokens.Add(new Token(Token.Kind.Op, c.ToString()));
                        i++;
                        continue;
                    }
                    return null; // single & | = — unsupported
                }
                if (char.IsDigit(c) || (c == '-' && i + 1 < s.Length && char.IsDigit(s[i + 1]) && ExpectsOperand(tokens)))
                {
                    int start = i;
                    i++;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'f' || s[i] == 'F')) i++;
                    tokens.Add(new Token(Token.Kind.Number, s.Substring(start, i - start).TrimEnd('f', 'F')));
                    continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_' || s[i] == '.')) i++;
                    string ident = s.Substring(start, i - start);
                    if (i + 1 < s.Length && s[i] == '(' && s[i + 1] == ')')
                    {
                        // Tolerate a no-arg call suffix "Method()".
                        i += 2;
                    }
                    else if (i < s.Length && s[i] == '(')
                    {
                        // Single supported call form: "member.HasFlag(EnumLiteral)" — capture the whole
                        // call as one identifier token; ParseOperand evaluates it as a flags test.
                        if (!ident.EndsWith(".HasFlag", StringComparison.Ordinal)) return null;
                        int close = s.IndexOf(')', i);
                        if (close < 0) return null;
                        ident += s.Substring(i, close - i + 1);
                        i = close + 1;
                    }
                    tokens.Add(new Token(Token.Kind.Identifier, ident));
                    continue;
                }
                return null;
            }
            return tokens;
        }

        private static bool ExpectsOperand(List<Token> tokens)
        {
            if (tokens.Count == 0) return true;
            var last = tokens[tokens.Count - 1];
            return last.K == Token.Kind.Op || last.K == Token.Kind.LParen;
        }

        private static bool ParseOr(object target, List<Token> t, ref int pos, out object result)
        {
            if (!ParseAnd(target, t, ref pos, out result)) return false;
            while (pos < t.Count && t[pos].K == Token.Kind.Op && t[pos].Text == "||")
            {
                pos++;
                if (!ParseAnd(target, t, ref pos, out object rhs)) return false;
                result = ToBool(result) || ToBool(rhs);
            }
            return true;
        }

        private static bool ParseAnd(object target, List<Token> t, ref int pos, out object result)
        {
            if (!ParseNot(target, t, ref pos, out result)) return false;
            while (pos < t.Count && t[pos].K == Token.Kind.Op && t[pos].Text == "&&")
            {
                pos++;
                if (!ParseNot(target, t, ref pos, out object rhs)) return false;
                result = ToBool(result) && ToBool(rhs);
            }
            return true;
        }

        private static bool ParseNot(object target, List<Token> t, ref int pos, out object result)
        {
            if (pos < t.Count && t[pos].K == Token.Kind.Op && t[pos].Text == "!")
            {
                pos++;
                if (!ParseNot(target, t, ref pos, out object inner)) { result = null; return false; }
                result = !ToBool(inner);
                return true;
            }
            return ParseRel(target, t, ref pos, out result);
        }

        private static readonly HashSet<string> RelOps = new HashSet<string> { "==", "!=", ">=", "<=", ">", "<" };

        private static bool ParseRel(object target, List<Token> t, ref int pos, out object result)
        {
            if (!ParseOperand(target, t, ref pos, out result, null)) return false;
            if (pos < t.Count && t[pos].K == Token.Kind.Op && RelOps.Contains(t[pos].Text))
            {
                string op = t[pos].Text;
                pos++;
                // Pass the lhs so an rhs bare identifier can resolve as an enum literal of lhs's type.
                if (!ParseOperand(target, t, ref pos, out object rhs, result)) return false;
                result = Compare(op, result, rhs, out bool ok);
                return ok;
            }
            return true;
        }

        private static bool ParseOperand(object target, List<Token> t, ref int pos, out object result, object enumContext)
        {
            result = null;
            if (pos >= t.Count) return false;
            var tok = t[pos];
            switch (tok.K)
            {
                case Token.Kind.LParen:
                {
                    pos++;
                    if (!ParseOr(target, t, ref pos, out result)) return false;
                    if (pos >= t.Count || t[pos].K != Token.Kind.RParen) return false;
                    pos++;
                    return true;
                }
                case Token.Kind.Number:
                    if (!double.TryParse(tok.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) return false;
                    result = d;
                    pos++;
                    return true;
                case Token.Kind.String:
                    result = tok.Text;
                    pos++;
                    return true;
                case Token.Kind.Identifier:
                {
                    pos++;
                    string ident = tok.Text;
                    if (ident == "true") { result = true; return true; }
                    if (ident == "false") { result = false; return true; }
                    if (ident == "null") { result = null; return true; }

                    // "member.HasFlag(EnumLiteral)" flags test.
                    int call = ident.IndexOf(".HasFlag(", StringComparison.Ordinal);
                    if (call > 0 && ident.EndsWith(")", StringComparison.Ordinal))
                        return TryEvaluateHasFlag(target, ident, call, out result);

                    var v = GetMemberChainValue(target, ident, out bool failed);
                    if (!failed) { result = v; return true; }

                    // Enum literal: "Type.Member" or bare "Member" against the comparison's lhs enum.
                    if (enumContext is Enum)
                    {
                        string member = ident;
                        int dot = member.LastIndexOf('.');
                        if (dot >= 0) member = member.Substring(dot + 1);
                        try { result = Enum.Parse(enumContext.GetType(), member, true); return true; }
                        catch { return false; }
                    }
                    return false;
                }
                default:
                    return false;
            }
        }

        // Evaluate "chain.HasFlag(arg)": chain resolves to an enum on the target; arg is an enum
        // literal (Type.Member or bare Member) of the same type, or another member reference.
        private static bool TryEvaluateHasFlag(object target, string ident, int callIndex, out object result)
        {
            result = null;
            string chain = ident.Substring(0, callIndex);
            string arg = ident.Substring(callIndex + ".HasFlag(".Length).TrimEnd(')').Trim();
            if (arg.Length == 0) return false;

            var lhs = GetMemberChainValue(target, chain, out bool failed);
            if (failed || !(lhs is Enum)) return false;

            object rhs = null;
            string member = arg;
            int dot = member.LastIndexOf('.');
            if (dot >= 0) member = member.Substring(dot + 1);
            try { rhs = Enum.Parse(lhs.GetType(), member, true); }
            catch
            {
                var v = GetMemberChainValue(target, arg, out bool argFailed);
                if (argFailed || !(v is Enum)) return false;
                rhs = v;
            }

            try
            {
                long l = Convert.ToInt64(lhs);
                long r = Convert.ToInt64(rhs);
                result = r == 0 ? l == 0 : (l & r) == r;
                return true;
            }
            catch { return false; }
        }

        private static object Compare(string op, object lhs, object rhs, out bool ok)
        {
            ok = true;
            switch (op)
            {
                case "==": return ValuesEqual(lhs, rhs);
                case "!=": return !ValuesEqual(lhs, rhs);
                default:
                    if (!TryToDouble(lhs, out double a) || !TryToDouble(rhs, out double b)) { ok = false; return false; }
                    return op switch { ">" => a > b, "<" => a < b, ">=" => a >= b, "<=" => a <= b, _ => false };
            }
        }

        // ------------------------------------------------------------------ value coercion

        private static bool TryToDouble(object o, out double d)
        {
            d = 0;
            if (o == null) return false;
            try { d = Convert.ToDouble(o, CultureInfo.InvariantCulture); return true; } catch { return false; }
        }

        public static bool ValuesEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a is Enum || b is Enum)
            {
                try { return Convert.ToInt64(a) == Convert.ToInt64(b); } catch { }
                try { return string.Equals(Convert.ToString(a), Convert.ToString(b), StringComparison.Ordinal); } catch { }
                return false;
            }
            if (a.Equals(b)) return true;
            if (TryToDouble(a, out double da) && TryToDouble(b, out double db)) return da.Equals(db);
            try { return Convert.ToString(a) == Convert.ToString(b); } catch { return false; }
        }

        /// <summary>Find a method by name walking the type hierarchy (any parameter list matching <paramref name="paramTypes"/>).</summary>
        public static MethodInfo FindMethod(Type t, string name, params Type[] paramTypes)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                var mi = cur.GetMethod(name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null, paramTypes, null);
                if (mi != null) return mi;
            }
            return null;
        }

        /// <summary>All methods with the given name (for signature probing), most-derived first.</summary>
        public static IEnumerable<MethodInfo> FindMethods(Type t, string name)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                var methods = cur.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                    if (methods[i].Name == name) yield return methods[i];
            }
        }

        private static bool IsMemberChain(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            bool segmentStart = true;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '.')
                {
                    if (segmentStart) return false; // ".." or leading '.'
                    segmentStart = true;
                    continue;
                }
                if (segmentStart)
                {
                    if (!(char.IsLetter(c) || c == '_')) return false;
                    segmentStart = false;
                }
                else if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            }
            return !segmentStart;
        }
    }
}
#endif
