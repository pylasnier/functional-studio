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

        public static Queue<TokeniserReturnError> GetTokeniserErrors(string sourceCode)
        {
            return Tokenise(sourceCode, out _).Errors;
        }

        public static CompilerReturnState Compile(string sourceCode, out PContext context)
        {
            CompilerReturnState returnState;
            Token[] tokenCode;
            context = null;
            if (!Tokenise(sourceCode, out tokenCode).Success)
            {
                returnState = new CompilerReturnState(false);
                returnState.Exceptions.Enqueue(new PaskellCompileException("Couldn't parse tokens", 0));
            }
            else
            {
                returnState = Compile(tokenCode, out context);
            }
            return returnState;
        }

        private static TokeniserReturnState Tokenise(string sourceCode, out Token[] TokenCode)
        {
            List<Token> tokenCollection = new List<Token>();
            bool[] codeMatched = new bool[sourceCode.Length];
            TokeniserReturnState returnState;
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

            //Finding whitespace just to fill codeMatched, so that it doesn't detect spaces or backslashes before newline as syntax errors
            matches = new Regex(@"\s|\\(?=\n)").Matches(sourceCode);
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
                returnState = new TokeniserReturnState(false);
                //Loops through to find each occurence of an error
                for (int i = 0; i < codeMatched.Length; /*Increments handled in loop*/)
                {
                    if (codeMatched[i] == false)
                    {
                        returnState.Errors.Enqueue(new TokeniserReturnError(i));
                        while (i < codeMatched.Length && codeMatched[i] == false) i++;
                    }
                    else i++;
                }
            }
            else
            {
                returnState = new TokeniserReturnState(true);
            }

            return returnState;
        }

        private static CompilerReturnState Compile(Token[] tokenCode, out PContext Context)
        {
            CompilerReturnState returnState;
            Queue<PaskellCompileException> exceptions = new Queue<PaskellCompileException>();

            int deletedLines = 0;

            List<(Token[], (int, int))> lines = new List<(Token[] line, (int, int))>();

            List<PExpression> Expressions = new List<PExpression>();
            Expressions.Add(new PExpression(Add, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Add"));
            Expressions.Add(new PExpression(Subtract, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Subtract"));
            Expressions.Add(new PExpression(Multiply, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Multiply"));
            Expressions.Add(new PExpression(Divide, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Divide"));
            Expressions.Add(new PExpression(Not, new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool))), "Not"));
            Expressions.Add(new PExpression(And, new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool)))), "And"));
            Expressions.Add(new PExpression(Or, new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool)))), "Or"));
            Expressions.Add(new PExpression(Xor, new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool)))), "Xor"));
            Expressions.Add(new PExpression(EqualTo, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature(typeof(bool)))), "EqualTo"));
            Expressions.Add(new PExpression(GreaterThan, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature(typeof(bool)))), "GreaterThan"));
            Expressions.Add(new PExpression(LessThan, new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature(typeof(bool)))), "LessThan"));

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
            if (copyLine.Length > 1)
            {
                lines.Add((copyLine, (0, 0)));
            }

            //Instantiate expressions
            for (int i = 0; i < lines.Count; i++)
            {
                try
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
                            throw new PaskellCompileException("No type signature for expression given", 0, i);
                        }
                        if (line.divisions.equateIndex == 0)
                        {
                            throw new PaskellCompileException("Expression not defined", line.divisions.expressionSignatureIndex, i);
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
                catch (PaskellCompileException e)
                {
                    //Adding deletedLines compensates for how when errors are caught, lines get deleted
                    exceptions.Enqueue(new PaskellCompileException(e.ErrorMessage, e.Index, e.Line + deletedLines));
                    lines.Remove(lines[i]);
                    deletedLines++;
                    i--;
                }
            }

            //Create definitions
            for (int i = 0; i < lines.Count; i++)
            {
                try
                {
                    (Token[] line, (int expressionSignatureIndex, int equateIndex) divisions) line = lines[i];
                    List<PExpression> parameters = new List<PExpression>();
                    PExpression expression;

                    Token[] subLine = new Token[line.divisions.equateIndex - line.divisions.expressionSignatureIndex];
                    Array.Copy(line.line, line.divisions.expressionSignatureIndex, subLine, 0, subLine.Length);

                    //Matching line to expression previously instantiated
                    if (subLine[0].TokenType != TokenType.Word) //This should never be reached as this is handled in the for loop above, at expression instantiation
                    {
                        throw new PaskellCompileException("Expected expression identifier", line.divisions.expressionSignatureIndex, i);
                    }
                    else
                    {
                        PExpression[] results = Expressions.Where(x => x.Identifier == subLine[0].Code).ToArray();
                        if (results.Length != 1)
                        {
                            throw new PaskellCompileException($"No unique definition for expression {subLine[0].Code}", line.divisions.expressionSignatureIndex, i);
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
                            throw new PaskellCompileException("Expected function parameter", line.divisions.expressionSignatureIndex + j, i);
                        }
                        else
                        {
                            if (parameterCount < expression.TypeSignature.ArgumentCount)
                            {
                                parameters.Add(new PExpression(parameterCount, expression.TypeSignature[parameterCount].Parameter, subLine[j].Code));
                                parameterCount++;
                            }
                            else
                            {
                                throw new PaskellCompileException("Unexpected token", line.divisions.expressionSignatureIndex + j, i);
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
                        PushSubExpressions(subLine, Expressions.Concat(parameters).ToList(), expression.TypeSignature.FinalType, expression, null, true);
                    }
                    catch (PaskellCompileException e)
                    {
                        throw new PaskellCompileException(e.ErrorMessage, e.Index + line.divisions.equateIndex + 1, i);
                    }
                }
                catch (PaskellCompileException e)
                {
                    exceptions.Enqueue(new PaskellCompileException(e.ErrorMessage, e.Index, e.Line + deletedLines));
                }
            }

            Context =  new PContext(Expressions.ToArray());
            if (exceptions.Count > 0)
            {
                returnState = new CompilerReturnState(false);
                while (exceptions.Count > 0)
                {
                    returnState.Exceptions.Enqueue(exceptions.Dequeue());
                }
            }
            else
            {
                returnState = new CompilerReturnState(true);
            }
            return returnState;
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
                throw new PaskellCompileException("Expected type signature expression", 0);
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
                    try
                    {
                        typeSignature = ConstructTypeSignature(newTokenCode);
                    }
                    catch (PaskellCompileException e)
                    {
                        throw new PaskellCompileException(e.ErrorMessage, e.Index + bracketStartIndex);
                    }
                }
                else if (tokenCode.Length > 1)
                {
                    throw new PaskellCompileException("Too many tokens in type signature", 0);
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

        private static TypeSignature PushSubExpressions(Token[] tokenCode, List<PExpression> expressions, TypeSignature targetTypeSignature,
                                                            PExpression outExpression, Stack<ConditionSpecifier> conditionSpecifiers = null, bool typeSignatureMustMatch = false)
        {
            int bracketNesting = 0;
            int bracketStartIndex = 0;

            int conditionNesting = 0;
            int clauseStartIndex = 0;

            int argumentCount = 0;

            TypeSignature baseTypeSignature = null;
            TypeSignature argumentTypeSignature = targetTypeSignature;      //Only for arguments of base subexpressions or literal values

            TypeSignature ifTrueTypeSignature = null;      //Used to match both then and else clause type signatures

            if (conditionSpecifiers == null)
            {
                conditionSpecifiers = new Stack<ConditionSpecifier>();
            }

            for (int i = 0; i < tokenCode.Length; i++)
            {
                Token token = tokenCode[i];
                if (token.TokenType == TokenType.FunctionMap || token.TokenType == TokenType.Equate)
                {
                    throw new PaskellCompileException("Unexpected token", i);
                }

                if (baseTypeSignature != null)
                {
                    if (argumentCount < baseTypeSignature.ArgumentCount)
                    {
                        argumentTypeSignature = baseTypeSignature[argumentCount].Parameter;
                    }
                    else
                    {
                        throw new PaskellCompileException("Unexpected token", i);
                    }
                }

                if (token.TokenType == TokenType.Bracket && conditionNesting == 0)
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
                            bracketNesting--;

                            if (bracketNesting == 0)
                            {
                                Token[] newTokenCode = new Token[i - bracketStartIndex];
                                Array.Copy(tokenCode, bracketStartIndex, newTokenCode, 0, newTokenCode.Length);
                                try
                                {
                                    if (baseTypeSignature == null)
                                    {
                                        baseTypeSignature = PushSubExpressions(newTokenCode, expressions, targetTypeSignature, outExpression, conditionSpecifiers);
                                    }
                                    else
                                    {
                                        PushSubExpressions(newTokenCode, expressions, argumentTypeSignature, outExpression, conditionSpecifiers, true);
                                        argumentCount++;
                                    }
                                }
                                catch (PaskellCompileException e)
                                {
                                    throw new PaskellCompileException(e.ErrorMessage, e.Index + bracketStartIndex);
                                }

                                argumentCount++;
                            }
                        }
                    }
                }
                else if (token.TokenType == TokenType.ConditionStatement && bracketNesting == 0)
                {
                    if (token.Code == "if")
                    {
                        if (conditionNesting == 0)
                        {
                            conditionNesting = 1;
                            clauseStartIndex = i + 1;
                            conditionSpecifiers.Push(ConditionSpecifier.Condition);
                        }
                        else
                        {
                            conditionNesting++;
                        }
                    }
                    else if (conditionNesting == 1 && conditionSpecifiers.Peek() == ConditionSpecifier.Condition && token.Code == "then")
                    {
                        Token[] newTokenCode = new Token[i - clauseStartIndex];
                        Array.Copy(tokenCode, clauseStartIndex, newTokenCode, 0, newTokenCode.Length);

                        try
                        {
                            PushSubExpressions(newTokenCode, expressions, new TypeSignature(typeof(bool)), outExpression, conditionSpecifiers, true);
                        }
                        catch (PaskellCompileException e)
                        {
                            throw new PaskellCompileException(e.ErrorMessage, e.Index + clauseStartIndex);
                        }

                        clauseStartIndex = i + 1;
                        conditionSpecifiers.Pop();
                        conditionSpecifiers.Push(ConditionSpecifier.IfTrue);
                    }
                    else if (conditionNesting == 1 && conditionSpecifiers.Peek() == ConditionSpecifier.IfTrue && token.Code == "else")
                    {
                        Token[] newTokenCode = new Token[i - clauseStartIndex];
                        Array.Copy(tokenCode, clauseStartIndex, newTokenCode, 0, newTokenCode.Length);

                        try
                        {
                            if (baseTypeSignature == null)
                            {
                                ifTrueTypeSignature = PushSubExpressions(newTokenCode, expressions, targetTypeSignature, outExpression, conditionSpecifiers);
                            }
                            else
                            {
                                PushSubExpressions(newTokenCode, expressions, argumentTypeSignature, outExpression, conditionSpecifiers, true);
                                //Doesn't increment argumentCount here as the else clause must be compiled first
                            }
                        }
                        catch (PaskellCompileException e)
                        {
                            throw new PaskellCompileException(e.ErrorMessage, e.Index + clauseStartIndex);
                        }

                        clauseStartIndex = i + 1;
                        conditionSpecifiers.Pop();
                        conditionSpecifiers.Push(ConditionSpecifier.IfFalse);
                    }
                    else if (token.Code == "endif")
                    {
                        if (conditionNesting == 1 && conditionSpecifiers.Peek() == ConditionSpecifier.IfFalse)
                        {
                            Token[] newTokenCode = new Token[i - clauseStartIndex];
                            Array.Copy(tokenCode, clauseStartIndex, newTokenCode, 0, newTokenCode.Length);

                            try
                            {
                                if (baseTypeSignature == null)
                                {
                                    baseTypeSignature = PushSubExpressions(newTokenCode, expressions, targetTypeSignature, outExpression, conditionSpecifiers);
                                    if (ifTrueTypeSignature != baseTypeSignature)
                                    {
                                        throw new PaskellCompileException("Both clauses in if block don't match type signature", clauseStartIndex);
                                    }
                                }
                                else
                                {
                                    PushSubExpressions(newTokenCode, expressions, argumentTypeSignature, outExpression, conditionSpecifiers, true);
                                    argumentCount++;
                                }
                            }
                            catch (PaskellCompileException e)
                            {
                                throw new PaskellCompileException(e.ErrorMessage, e.Index + clauseStartIndex);
                            }

                            conditionSpecifiers.Pop();
                            conditionNesting--;
                        }
                        else if (conditionNesting > 1)
                        {
                            conditionNesting--;
                        }
                    }
                }
                else if (bracketNesting == 0 && conditionNesting == 0)
                {
                    if (token.TokenType == TokenType.Word)
                    {
                        PExpression expression;
                        PExpression[] results = expressions.Where(x => x.Identifier == token.Code).ToArray();
                        if (results.Length != 1)
                        {
                            throw new PaskellCompileException($"No unique definition for expression {tokenCode[i].Code}", i);
                        }
                        else
                        {
                            expression = results[0];
                        }

                        if (baseTypeSignature == null)
                        {
                            baseTypeSignature = expression.TypeSignature;
                            outExpression.PushSubExpression(expression, baseTypeSignature.ArgumentCount - targetTypeSignature.ArgumentCount, new Stack<ConditionSpecifier>(conditionSpecifiers));
                        }
                        else
                        {
                            if (expression.TypeSignature != argumentTypeSignature)
                            {
                                throw new PaskellCompileException($"Argument must be of type {argumentTypeSignature.Value}", i);
                            }
                            else
                            {
                                outExpression.PushSubExpression(expression, 0, new Stack<ConditionSpecifier>(conditionSpecifiers));
                                argumentCount++;
                            }
                        }
                    }
                    else
                    {
                        if (!argumentTypeSignature.IsFunction)
                        {
                            try
                            {
                                Type type = null;
                                dynamic variable = null;
                                if (argumentTypeSignature.Type != null)
                                {
                                    type = argumentTypeSignature.Type;
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
                                            type = operandType.GetType();
                                            TypeConverter converter = TypeDescriptor.GetConverter(type);
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
                                outExpression.PushSubExpression(new PExpression(variable), 0, new Stack<ConditionSpecifier>(conditionSpecifiers));
                                if (baseTypeSignature == null)
                                {
                                    baseTypeSignature = new TypeSignature(type);
                                }
                                else
                                {
                                    argumentCount++;
                                }
                            }
                            catch (Exception)
                            {
                                throw new PaskellCompileException($"Expected literal value of type {argumentTypeSignature.Value}", i);
                            }
                        }
                        else
                        {
                            throw new PaskellCompileException($"Expected expression of type {argumentTypeSignature.Value}", i);
                        }
                    }
                }
            }

            if (bracketNesting > 0)
            {
                throw new PaskellCompileException("Expected close bracket", tokenCode.Length - 1);
            }
            if (conditionNesting > 0)
            {
                string statement = "";
                switch (conditionSpecifiers.Peek())
                {
                    case ConditionSpecifier.Condition:
                        statement = "then";
                        break;
                    case ConditionSpecifier.IfTrue:
                        statement = "else";
                        break;
                    case ConditionSpecifier.IfFalse:
                        statement = "endif";
                        break;
                }
                throw new PaskellCompileException($"Expected {statement} statement", tokenCode.Length - 1);
            }

            if (baseTypeSignature == null)
            {
                throw new PaskellCompileException("Expected expression", 0);
            }

            if (typeSignatureMustMatch && baseTypeSignature[argumentCount] != targetTypeSignature)
            {
                throw new PaskellCompileException($"Expression must be of type {targetTypeSignature.Value}", 0);
            }

            return baseTypeSignature[argumentCount];
        }

        private static PExpression Add(Stack<PExpression> a)
        {
            return new PExpression(a.Pop().Evaluate().Value + a.Pop().Evaluate().Value);
        }

        private static PExpression Subtract(Stack<PExpression> a)
        {
            return new PExpression(a.Pop().Evaluate().Value - a.Pop().Evaluate().Value);
        }

        private static PExpression Multiply(Stack<PExpression> a)
        {
            return new PExpression(a.Pop().Evaluate().Value * a.Pop().Evaluate().Value);
        }

        private static PExpression Divide(Stack<PExpression> a)
        {
            return new PExpression(a.Pop().Evaluate().Value / a.Pop().Evaluate().Value);
        }

        private static PExpression Not(Stack<PExpression> a)
        {
            return new PExpression(!(bool)a.Pop().Evaluate().Value);
        }

        private static PExpression And(Stack<PExpression> a)
        {
            return new PExpression((bool)a.Pop().Evaluate().Value & (bool)a.Pop().Evaluate().Value);
        }

        private static PExpression Or(Stack<PExpression> a)
        {
            return new PExpression((bool)a.Pop().Evaluate().Value | (bool)a.Pop().Evaluate().Value);
        }

        private static PExpression Xor(Stack<PExpression> a)
        {
            return new PExpression((bool)a.Pop().Evaluate().Value ^ (bool)a.Pop().Evaluate().Value);
        }

        private static PExpression EqualTo(Stack<PExpression> a)
        {
            return new PExpression(a.Pop().Evaluate().Value == a.Pop().Evaluate().Value);
        }

        private static PExpression GreaterThan(Stack<PExpression> a)
        {
            return new PExpression(a.Pop().Evaluate().Value > a.Pop().Evaluate().Value);
        }

        private static PExpression LessThan(Stack<PExpression> a)
        {
            return new PExpression(a.Pop().Evaluate().Value < a.Pop().Evaluate().Value);
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

    public struct TokeniserReturnState
    {
        public bool Success { get; }
        public Queue<TokeniserReturnError> Errors { get; }

        public TokeniserReturnState(bool success)
        {
            Success = success;
            Errors = new Queue<TokeniserReturnError>();
        }
    }

    public struct TokeniserReturnError
    {
        public int Index { get; }

        public TokeniserReturnError(int index)
        {
            Index = index;
        }
    }

    public struct CompilerReturnState
    {
        public bool Success { get; }
        public Queue<PaskellCompileException> Exceptions { get; }

        public CompilerReturnState(bool success)
        {
            Success = success;
            Exceptions = new Queue<PaskellCompileException>();
        }
    }

    enum TokenType
    {
        [RegexPattern(@"\(|\)")]
        Bracket,
        [RegexPattern("=")]
        Equate,
        [RegexPattern("->")]
        FunctionMap,
        [RegexPattern(@"((?<=\s|\(|\)|=|(->))|^)(?!((true|false|if|then|else|endif)\b))[A-Za-z][A-Za-z0-9]*")]
        Word,
        [RegexPattern(@"((?<=\s|\(|\)|=|(->))|^)(-?([0-9]+(\.[0-9]+)?)|('.')|true|false)(?![A-Za-z0-9]|\.)")]
        Operand,
        [RegexPattern(@"((?<=\s|\(|\)|=|(->))|^)(if|then|else|endif)\b")]
        ConditionStatement,
        [RegexPattern(@"(?<!\\)\n\s*")]
        LineBreak
    }

    //@ symbol so to allow type keywords as names
    public enum OperandType
    {
        [PaskellType(typeof(long))]
        @int,
        [PaskellType(typeof(double))]
        @float,
        [PaskellType(typeof(char))]
        @char,
        [PaskellType(typeof(bool))]
        @bool
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
