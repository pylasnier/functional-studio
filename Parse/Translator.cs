using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;
using Utility;

 namespace Parse
{
    public static class Translator
    {
        public static void CallMe(string code)
        {
            Token[] output;
            PContext context;
            Tokenise(code, out output);
            Compile(output, out context);
            PExpression result = context.Expressions.Where(x => x.Identifier == "main").ToArray()[0].Evaluate();
        }

        private static ParserReturnState Tokenise(string sourceCode, out Token[] TokenCode)
        {
            List<Token> tokenCollection = new List<Token>();
            bool[] codeMatched = new bool[sourceCode.Length];
            ParserReturnState returnState;
            MatchCollection matches;

            codeMatched.Populate(false, 0, codeMatched.Length);

            //Loops through every token for their regex patterns, as given by their RegexPattern custom attribute
            foreach (var tokenType in Enum.GetValues(typeof(TokenType)))
            {
                matches = new Regex(((TokenType) tokenType).GetPattern()).Matches(sourceCode);
                foreach (Match match in matches)
                {
                    tokenCollection.Add(new Token(match.Value, (TokenType) tokenType, match.Index));
                    codeMatched.Populate(true, match.Index, match.Length);
                }
            }

            //Finding whitespace just to fill codeMatched, so that it doesn't detect spaces as syntax errors
            matches = new Regex(@"\s").Matches(sourceCode);
            foreach (Match match in matches)
            {
                codeMatched.Populate(true, match.Index, match.Length);
            }

            //Main useful output, the tokenised code
            TokenCode = tokenCollection.ToArray();
            TokenCode.Sort();

            //Error output, indicates success or partial failure and errors if there are failures
            if (codeMatched.Any(element => element == false))
            {
                returnState = new ParserReturnState(false);
                //Loops through to find each occurence of an error
                for (int i = 0; i < codeMatched.Length; /*Increments handled in loop*/)
                {
                    if (codeMatched[i] == false)
                    {
                        returnState.Errors.Push(new ParserReturnErrorInfo(ParserReturnError.BadIdentifier, i));
                        while (i < codeMatched.Length && codeMatched[i] == false) i++;
                    }
                    else i++;
                }
            }
            else
            {
                returnState = new ParserReturnState(true);
            }

            return returnState;
        }

        private static ParserReturnState Compile(Token[] tokenCode, out PContext Context)
        {
            ParserReturnState returnState = new ParserReturnState();

            List<(Token[], (int, int))> lines = new List<(Token[] line, (int, int))>();

            List<PExpression> Expressions = new List<PExpression>();
            Expressions.Add(new PExpression(Add, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Add"));
            Expressions.Add(new PExpression(IfThenElse, new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature()))), "IfThenElse"));

            bool[] codeMatched = new bool[tokenCode.Length];

            ParserState parserState = ParserState.TypeSignature;

            int startIndex = 0;
            int endIndex;

            //Divided code into lines
            Token[] copyLine;
            for (int i = 0; i < tokenCode.Length; i++)
            {
                if (tokenCode[i].TokenType == TokenType.LineBreak)
                {
                    endIndex = i;
                    copyLine = new Token[endIndex - startIndex];
                    Array.Copy(tokenCode, startIndex, copyLine, 0, copyLine.Length);
                    lines.Add((copyLine, (0, 0)));
                    startIndex = endIndex + 1;
                }
            }
            endIndex = tokenCode.Length;
            copyLine = new Token[endIndex - startIndex];
            Array.Copy(tokenCode, startIndex, copyLine, 0, copyLine.Length);
            lines.Add((copyLine, (0, 0)));

            //Instantiate expressions
            for (int i = 0; i < lines.Count; i++)
            {
                (Token[] line, (int expressionSignatureIndex, int equateIndex) divisions) line = lines[i];
                {
                    bool wasLastWord = false;

                    //Finding points to split at for type signature, expression signature, and expression definition
                    for (int j = 0; j < line.line.Length; j++)
                    {
                        if (line.divisions.expressionSignatureIndex == 0)
                        {
                            if (line.line[j].TokenType == TokenType.Word)
                            {
                                if (wasLastWord)
                                {
                                    line.divisions.expressionSignatureIndex = j;
                                }
                                else
                                {
                                    wasLastWord = true;
                                }
                            }
                            else if (line.line[j].TokenType != TokenType.Bracket)
                            {
                                wasLastWord = false;
                            }
                        }
                        else if (line.divisions.equateIndex == 0)
                        {
                            if (line.line[j].TokenType == TokenType.Equate)
                            {
                                line.divisions.equateIndex = j;
                                break;
                            }
                        }
                    }

                    if (line.divisions.expressionSignatureIndex == 0)
                    {
                        throw new PaskellCompileException("No signature for expression given", line.line.Length, i);
                    }
                    if (line.divisions.equateIndex == 0)
                    {
                        throw new PaskellCompileException("Expression not set to anything", line.line.Length, i);
                    }

                    //Getting type signature and expression signature
                    Token[] subLine = new Token[line.divisions.expressionSignatureIndex];
                    Array.Copy(line.line, 0, subLine, 0, subLine.Length);
                    TypeSignature typeSignature;
                    try
                    {
                        typeSignature = ConstructTypeSignature(subLine);
                    }
                    catch (PaskellCompileException e)
                    {
                        throw new PaskellCompileException(e.ErrorMessage, e.Index, i);
                    }

                    subLine = new Token[line.divisions.equateIndex - line.divisions.expressionSignatureIndex];
                    Array.Copy(line.line, line.divisions.expressionSignatureIndex, subLine, 0, subLine.Length);
                    if (subLine.Length == 0 || subLine[0].TokenType != TokenType.Word)
                    {
                        throw new PaskellCompileException("Expected expression identifier", line.divisions.expressionSignatureIndex, i);
                    }
                    PExpression pExpression = new PExpression(typeSignature, subLine[0].Code);
                    Expressions.Add(pExpression);

                    lines[i] = line;
                }
            }

            //Create definitions
            for (int i = 0; i < lines.Count; i++)
            {
                (Token[] line, (int expressionSignatureIndex, int equateIndex) divisions) line = lines[i];
                List<PExpression> parameters = new List<PExpression>();
                PExpression expression;

                Token[] subLine = new Token[line.divisions.equateIndex - line.divisions.expressionSignatureIndex];
                Array.Copy(line.line, line.divisions.expressionSignatureIndex, subLine, 0, subLine.Length);

                //Matching line to expression previously instantiated
                if (subLine[0].TokenType != TokenType.Word)
                {
                    throw new PaskellCompileException("Expected expression identifier", line.divisions.expressionSignatureIndex, i);
                }
                else
                {
                    PExpression[] results = Expressions.Where(x => x.Identifier == subLine[0].Code).ToArray();
                    if (results.Length != 1)
                    {
                        throw new PaskellCompileException($"No unique definition for identifier {subLine[0].Code}", line.divisions.expressionSignatureIndex, i);
                    }
                    else
                    {
                        expression = results[0];
                    }
                }

                //Setting up parameters of expression
                int parameterCount = 0;
                for (int j = 1; j < subLine.Length; j++)
                {
                    if (subLine[j].TokenType != TokenType.Word)
                    {
                        throw new PaskellCompileException("Expected parameter expression", line.divisions.expressionSignatureIndex + j, i);
                    }
                    else
                    {
                        if (parameterCount >= expression.TypeSignature.ArgumentCount)
                        {
                            throw new PaskellCompileException("Unexpected token", line.divisions.expressionSignatureIndex + j, i);
                        }
                        else
                        {
                            parameters.Add(new PExpression(parameterCount, expression.TypeSignature[j - 1], subLine[j].Code));
                            parameterCount++;
                        }
                    }
                }
                if (parameterCount < expression.TypeSignature.ArgumentCount)
                {
                    throw new PaskellCompileException($"Too few parameters for function {expression.Identifier}", line.divisions.equateIndex - 1, i);
                }

                subLine = new Token[line.line.Length - line.divisions.equateIndex - 1];
                Array.Copy(line.line, line.divisions.equateIndex + 1, subLine, 0, subLine.Length);

                //Defining expression
                try
                {
                    PushSubExpressions(subLine, Expressions.Concat(parameters).ToList(), expression.TypeSignature.FinalType, expression);
                }
                catch (PaskellCompileException e)
                {
                    throw new PaskellCompileException(e.ErrorMessage, e.Index + line.divisions.equateIndex + 1, i);
                }
            }

            Context =  new PContext(Expressions.ToArray());
            return null;
        }

        private static TypeSignature ConstructTypeSignature(Token[] tokenCode)
        {
            int bracketNesting = 0;
            int bracketStartIndex = 0;
            int bracketEndIndex = 0;
            int functionMapIndex = 0;

            TypeSignature typeSignature = null;
            TypeSignature left = null;
            bool isFunction = false;

            if (tokenCode.Length == 0)
            {
                throw new PaskellCompileException("Expected type", 0);
            }

            for (int i = 0; i < tokenCode.Length; i++)
            {
                Token token = tokenCode[i];
                if (token.TokenType == TokenType.Bracket)
                {
                    if (token.Code == "(")
                    {
                        if (bracketNesting == 0)
                        {
                            bracketStartIndex = i + 1;
                        }
                        bracketNesting++;
                    }
                    else
                    {
                        if (bracketNesting == 0)
                        {
                            throw new PaskellCompileException("Unexpected bracket", i);
                        }
                        else
                        {
                            bracketEndIndex = i;
                            bracketNesting--;
                        }
                    }
                }
                else if (bracketNesting == 0)
                {
                    if (token.TokenType == TokenType.FunctionMap)
                    {
                        isFunction = true;
                        functionMapIndex = i;
                        if (bracketEndIndex == 0)
                        {
                            bracketEndIndex = i;
                        }
                        Token[] newTokenCode = new Token[bracketEndIndex - bracketStartIndex];
                        Array.Copy(tokenCode, bracketStartIndex, newTokenCode, 0, newTokenCode.Length);
                        try
                        {
                            left = ConstructTypeSignature(newTokenCode);
                        }
                        catch (PaskellCompileException e)
                        {
                            throw new PaskellCompileException(e.ErrorMessage, e.Index + bracketStartIndex);
                        }
                        break;
                    }
                }
            }

            if (bracketNesting > 0)
            {
                throw new PaskellCompileException("Expected bracket", tokenCode.Length - 1);
            }

            if (isFunction)
            {
                Token[] newTokenCode = new Token[tokenCode.Length - functionMapIndex - 1];
                Array.Copy(tokenCode, functionMapIndex + 1, newTokenCode, 0, newTokenCode.Length);
                try
                {
                    typeSignature = new TypeSignature(left, ConstructTypeSignature(newTokenCode));
                }
                catch (PaskellCompileException e)
                {
                    throw new PaskellCompileException(e.ErrorMessage, e.Index + functionMapIndex + 1);
                }

            }
            else
            {
                if (bracketEndIndex != 0)
                {
                    Token[] newTokenCode = new Token[tokenCode.Length - 2];
                    Array.Copy(tokenCode, 1, newTokenCode, 0, newTokenCode.Length);
                    typeSignature = ConstructTypeSignature(newTokenCode);
                }
                else if (tokenCode.Length > 1)
                {
                    throw new PaskellCompileException("Too many expressions", 0);
                }
                else
                {
                    if (tokenCode[0].TokenType != TokenType.Word)
                    {
                        throw new PaskellCompileException("Expected type", 0);
                    }
                    foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
                    {
                        if (tokenCode[0].Code == operandType.ToString())
                        {
                            typeSignature = new TypeSignature(operandType.GetPType());
                            break;
                        }
                    }
                    if (typeSignature == null)
                    {
                        throw new PaskellCompileException("Invalid type", 0);
                    }
                }
            }

            return typeSignature;
        }

        private static void PushSubExpressions(Token[] tokenCode, List<PExpression> expressions, TypeSignature targetTypeSignature, PExpression outExpression, TypeSignature oldTypeSignature = null)
        {
            int bracketNesting = 0;
            int bracketStartIndex = 0;
            int bracketEndIndex = 0;

            TypeSignature typeSignature;
            if (oldTypeSignature == null)
            {
                typeSignature = outExpression.TypeSignature;
            }
            else
            {
                typeSignature = oldTypeSignature;
            }
            int argumentCount = 0;

            for (int i = 0; i < tokenCode.Length; i++)
            {
                Token token = tokenCode[i];
                if (token.TokenType != TokenType.Word && token.TokenType != TokenType.Operand && token.TokenType != TokenType.Bracket)
                {
                    throw new PaskellCompileException("Unexpected token", i);
                }

                if (token.TokenType == TokenType.Bracket)
                {
                    if (token.Code == "(")
                    {
                        if (bracketNesting == 0)
                        {
                            bracketStartIndex = i + 1;
                        }
                        bracketNesting++;
                    }
                    else
                    {
                        if (bracketNesting == 0)
                        {
                            throw new PaskellCompileException("Unexpected bracket", i);
                        }
                        else
                        {
                            bracketEndIndex = i;
                            bracketNesting--;
                        }
                    }
                }
                if (bracketNesting == 0)
                {
                    if (bracketEndIndex != 0)
                    {
                        Token[] newTokenCode = new Token[bracketEndIndex - bracketStartIndex];
                        Array.Copy(tokenCode, bracketStartIndex, newTokenCode, 0, newTokenCode.Length);
                        try
                        {
                            PushSubExpressions(newTokenCode, expressions, typeSignature[argumentCount].Parameter, outExpression, typeSignature);
                        }
                        catch (PaskellCompileException e)
                        {
                            throw new PaskellCompileException(e.ErrorMessage, e.Index + i);
                        }

                        argumentCount++;
                    }
                    else if (token.TokenType == TokenType.Word)
                    {
                        PExpression expression;
                        PExpression[] results = expressions.Where(x => x.Identifier == token.Code).ToArray();
                        if (results.Length != 1)
                        {
                            throw new PaskellCompileException($"No unique definition for identifier {tokenCode[i]}", i);
                        }
                        else
                        {
                            expression = results[0];
                        }

                        if (i == 0)
                        {
                            typeSignature = expression.TypeSignature;
                            if (!typeSignature.ContainsGenericTypeSignatures && typeSignature[typeSignature.ArgumentCount - targetTypeSignature.ArgumentCount] != targetTypeSignature)
                            {
                                throw new PaskellCompileException($"Argument must be of return type {typeSignature}", i);
                            }
                            outExpression.PushSubExpression(expression, typeSignature.ArgumentCount - targetTypeSignature.ArgumentCount);
                        }
                        else
                        {
                            outExpression.PushSubExpression(expression, 0);
                            argumentCount++;
                        }
                    }
                    else
                    {
                        if (i == 0 && !typeSignature.IsFunction || !typeSignature[argumentCount].Parameter.IsFunction)
                        {
                            try
                            {
                                dynamic variable = null;
                                if (!typeSignature.ContainsGenericTypeSignatures)
                                {
                                    Type type;
                                    if (i == 0)
                                    {
                                        type = typeSignature.Type;
                                    }
                                    else
                                    {
                                        type = typeSignature[argumentCount].Parameter.Type;
                                    }
                                    TypeConverter converter = TypeDescriptor.GetConverter(type);
                                    variable = converter.ConvertFromString(token.Code);
                                }
                                else
                                {
                                    bool success = false;
                                    foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
                                    {
                                        try
                                        {
                                            TypeConverter converter = TypeDescriptor.GetConverter(operandType.GetPType());
                                            variable = converter.ConvertFromString(token.Code);
                                            success = true;
                                            break;
                                        }
                                        catch
                                        {
                                            success = false;
                                        }
                                    }
                                    if (!success)
                                    {
                                        throw new PaskellCompileException("You messed up your code P", i);
                                    }
                                }
                                outExpression.PushSubExpression(new PExpression(variable), 0);
                            }
                            catch (Exception)
                            {
                                throw new PaskellCompileException("Invalid type for literal value", i);
                            }
                        }
                    }
                }
            }
        }

        private static PExpression Add(Stack<PExpression> a, string identifier)
        {
            var result = a.Pop().Evaluate().Value + a.Pop().Evaluate().Value;
            return new PExpression(result, identifier);
        }

        private static PExpression IfThenElse(Stack<PExpression> a, string identifier)
        {
            bool comparison = a.Pop().Evaluate().Value;
            PExpression ifTrue = a.Pop();
            PExpression ifFalse = a.Pop();
            if (comparison)
            {
                return ifTrue;
            }
            else
            {
                return ifFalse;
            }
        }
    }

    //https://stackoverflow.com/questions/479410/enum-tostring-with-user-friendly-strings
    static class CustomEnumExtensions
    {
        public static string GetPattern(this TokenType enumValue)
        {
            string pattern = "";
            MemberInfo memberInfo = enumValue.GetType().GetMember(enumValue.ToString())[0];
            foreach (var customAttribute in memberInfo.GetCustomAttributes(typeof(RegexPattern), false))
            {
                pattern = ((RegexPattern) customAttribute).Pattern;
            }

            return pattern;
        }

        public static Type GetPType(this OperandType enumValue)
        {
            Type type = null;
            MemberInfo memberInfo = enumValue.GetType().GetMember(enumValue.ToString())[0];
            foreach (var customAttribute in memberInfo.GetCustomAttributes(typeof(PaskellType), false))
            {
                type = ((PaskellType)customAttribute).Type;
            }

            return type;
        }
    }

    class RegexPattern : Attribute
    {
        public string Pattern;

        public RegexPattern(string pattern)
        {
            Pattern = pattern;
        }
    }

    class PaskellType : Attribute
    {
        public Type Type;

        public PaskellType(Type type)
        {
            Type = type;
        }
    }

    struct Token : IComparable
    {
        public readonly string Code;
        public readonly TokenType TokenType;
        public readonly int Index;

        public Token(string code, TokenType tokenType, int index)
        {
            Code = code;
            TokenType = tokenType;
            Index = index;
        }

        public int CompareTo(object obj)        //Necessary to be comparable by index so that the tokeniser can sort all tokens once made
        {
            Token token = (Token)obj;
            return Index - token.Index;
        }
    }

    public class PContext
    {
        public PExpression[] Expressions;

        public PContext(PExpression[] expressions)
        {
            Expressions = expressions;
        }
    }

    public class ParserReturnState
    {
        public readonly bool Success;
        public readonly Stack<ParserReturnErrorInfo> Errors;

        public ParserReturnState()
        {
            Success = false;
            Errors = new Stack<ParserReturnErrorInfo>();
        }

        public ParserReturnState(bool success)
        {
            Success = success;
            Errors = new Stack<ParserReturnErrorInfo>();
        }
    }

    public struct ParserReturnErrorInfo
    {
        public ParserReturnError Error;
        public int Index;

        public ParserReturnErrorInfo(ParserReturnError error, int index)
        {
            Error = error;
            Index = index;
        }
    }

    public enum ParserReturnError
    {
        BadIdentifier,
        BadBracketNesting,
        MissingWordBeforeRelation
    }

    enum TokenType
    {
        [RegexPattern(@"\(|\)")]
        Bracket,
        [RegexPattern("=")]
        Equate,
        [RegexPattern("->")]
        FunctionMap,
        [RegexPattern(@"(?!true|false)((?<=(\s|\(|\)))|^)[A-Za-z][A-Za-z0-9]*")]
        Word,
        [RegexPattern(@"((?<=\s)|^)(([0-9]+(\.[0-9]+)?)|('.')|true|false)(?![A-Za-z0-9]|\.)")]
        Operand,
        [RegexPattern(@"(?<!\\)\n")]
        LineBreak
    }

    enum ParserState
    {
        TypeSignature,
        ExpressionSignature,
        Definition
    }

    public enum OperandType
    {
        [PaskellType(typeof(long))]
        Int,
        [PaskellType(typeof(double))]
        Float,
        [PaskellType(typeof(char))]
        Char,
        [PaskellType(typeof(bool))]
        Bool
    }

    public class PaskellRuntimeException : Exception
    {
        public new PaskellRuntimeException InnerException;
        public PExpression PExpression;
        public string ErrorMessage;

        public PaskellRuntimeException(string errorMessage, PExpression pExpression, PaskellRuntimeException innerException = null)
        {
            ErrorMessage = errorMessage;
            PExpression = pExpression;
            InnerException = innerException;
        }
    }

    public class PaskellCompileException : Exception
    {
        public int Index;
        public int Line;
        public string ErrorMessage;

        public PaskellCompileException(string errorMessage, int index, int line = 0)
        {
            ErrorMessage = errorMessage;
            Line = line;
            Index = index;
        }
    }
}
