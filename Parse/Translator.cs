using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using Utility;

 namespace Parse
{
    public static class Translator
    {
        //Just retrieves tokeniser errors
        public static Queue<TokeniserReturnError> GetTokeniserErrors(string sourceCode)
        {
            return Tokenise(sourceCode, out _).Errors;
        }

        //Public wrapper of tokeniser and compiler together, with singular compiler error indicating tokeniser error
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

        //Converts source code into tokens of consistent type that make compiling easier
        private static TokeniserReturnState Tokenise(string sourceCode, out Token[] TokenCode)
        {
            List<Token> tokenCollection = new List<Token>();
            bool[] codeMatched = new bool[sourceCode.Length];   //Used for finding errors i.e. anything that wasn't parsed, so didn't match with any token Regex
            TokeniserReturnState returnState;
            MatchCollection matches;

            codeMatched.Populate(false, 0, codeMatched.Length);

            //Loops through every token for their regex patterns, as given by their RegexPattern custom attribute
            foreach (TokenType tokenType in Enum.GetValues(typeof(TokenType)))
            {
                matches = new Regex(tokenType.GetPattern()).Matches(sourceCode);
                foreach (Match match in matches)
                {
                    tokenCollection.Add(new Token(match.Value, tokenType, match.Index));
                    codeMatched.Populate(true, match.Index, match.Length);      //Token parsed, so the code it matched with must be valid
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
                //True in return state indicates success i.e. no errors
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

            //All the base expressions (included pseudocode to show equivalent C# code):
            //An underscore _ represents a generic type i.e. could be any type valid for the function

            /*Arithmetic*/
            Expressions.Add(new PExpression(Add,
                new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Add"));       //_ -> _ -> _ Add a b = a + b
            Expressions.Add(new PExpression(Subtract,
                new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Subtract"));  //_ -> _ -> _ Subtract a b = a - b
            Expressions.Add(new PExpression(Multiply,
                new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Multiply"));  //_ -> _ -> _ Multiply a b = a * b
            Expressions.Add(new PExpression(Divide,
                new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature())), "Divide"));    //_ -> _ -> _ Divide a b = a / b

            /*Boolean logic*/
            /* bool -> bool Not a = !a */
            Expressions.Add(new PExpression(Not,
                new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool))), "Not"));
            /* bool -> bool -> bool And a b = a && b */
            Expressions.Add(new PExpression(And,
                new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool)))), "And"));
            /* bool -> bool -> bool Or a b = a || b */
            Expressions.Add(new PExpression(Or,
                new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool)))), "Or"));
            /* bool -> bool -> bool Xor a b = a ^ b */
            Expressions.Add(new PExpression(Xor,
                new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(new TypeSignature(typeof(bool)), new TypeSignature(typeof(bool)))), "Xor"));

            /*Comparison and inequalities*/
            Expressions.Add(new PExpression(EqualTo,
                new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature(typeof(bool)))), "EqualTo"));    //_ -> _ -> bool EqualTo a b = a == b
            Expressions.Add(new PExpression(GreaterThan,
                new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature(typeof(bool)))), "GreaterThan"));//_ -> _ -> bool GreaterThan a b = a > b
            Expressions.Add(new PExpression(LessThan,
                new TypeSignature(new TypeSignature(), new TypeSignature(new TypeSignature(), new TypeSignature(typeof(bool)))), "LessThan"));   //_ -> _ -> bool LessThan a b = a < b

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
                        //For start of expression signature, there must be two consecutive words i.e. final type in type signature, then expression identifier
                        //For the start of the definition, an equals sign
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
                        //Takes subline of just type signature
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

                        //Takes just the first token in the expression signature and uses its identifier
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
                    //Adding to deletedLines compensates for how when errors are caught, lines get deleted
                    //It is important to still track which line the code is on
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
                        //Here the expression available to be defined are the base expression first added, the other expressions in the file, and finally this expressions parameters
                        //The parameters are local to the expression and are not accessible from other expressions
                        //This also allows for parameters of different expressions to have the same name
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

        //Used to parse a type signature
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
                //When there are bracket clauses, this function gets recursed and passed the contents of the bracket clause
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
                    //This is where when a function map is found, the type signature is considered a function and the left and right side must be constructed as type signatures
                    //This uses the same technique as with bracket nesting
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
            
            //Once totally looped through, the type signature is either a function signature, a type, or is surrounded by brackets so must be recursed through with their contents
            if (bracketNesting > 0)
            {
                throw new PaskellCompileException("Expected bracket", tokenCode.Length - 1);
            }

            if (isFunction)
            {
                //Find what's on the right side of the function map
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
                    //When there's only one token, the type signature must be just a type given by that token
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

        //This works to take an expression definition in token code and push subexpressions into the stack in RPN fashion
        //It also applies condition specifiers to the subexpression depending on if they're part of a condition block, and where
        private static TypeSignature PushSubExpressions(Token[] tokenCode, List<PExpression> expressions, TypeSignature targetTypeSignature,
                                                            PExpression outExpression, Stack<ConditionSpecifier> conditionSpecifiers = null, bool typeSignatureMustMatch = false)
        {
            int bracketNesting = 0;
            int bracketStartIndex = 0;

            int conditionNesting = 0;
            int clauseStartIndex = 0;

            int argumentCount = 0;

            TypeSignature baseTypeSignature = null;
            TypeSignature argumentTypeSignature = targetTypeSignature;      //Only assigning as such for literal values before a base subexpression has been declared

            TypeSignature ifTrueTypeSignature = null;      //Used to match both then and else clause type signatures, if part of a condition block

            //If this function is recursed, it may be compiling part of a condition clause and so must retain the condition specifiers already applied
            //Otherwise a new stack must be initialised
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
                    //If a base subexpression has already been declared, then the next subexpression evaluated must be an argument
                    //The type signature must match that of the argument type of the type signature of the base subexpression for the given argument expected
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
                    //With bracket clauses, this function gets recursed with their contents
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
                                    //Importantly, if the current bracket clause is expected to be the base expression (which by definition of the language wouldn't be necessary to
                                    //even include, but since it's valid to write it must be considered) then it must be compiled, but not necessarily match the target type signature
                                    //yet, as the base subexpression will still be passed arguments
                                    if (baseTypeSignature == null)
                                    {
                                        baseTypeSignature = PushSubExpressions(newTokenCode, expressions, targetTypeSignature, outExpression, conditionSpecifiers);
                                    }
                                    //Otherwise, the bracket clause must represent an argument of the base subexpression, and so the returned type signature must match that of the
                                    //argument type signature, hence passing true to typeSignatureMustMatch here
                                    else
                                    {
                                        PushSubExpressions(newTokenCode, expressions, argumentTypeSignature, outExpression, conditionSpecifiers, true);
                                        argumentCount++;        //Important to move on to the next argument type signature
                                    }
                                }
                                catch (PaskellCompileException e)
                                {
                                    throw new PaskellCompileException(e.ErrorMessage, e.Index + bracketStartIndex);
                                }
                            }
                        }
                    }
                }
                else if (token.TokenType == TokenType.ConditionStatement && bracketNesting == 0)
                {
                    //Throughout the condition block compiling, the exact same applies as with the bracket clauses regarding the base expression not having to match the target
                    //type signature, but the arguments having to match the argument type signature
                    //When recursing, the conditionSpecifiers stack is passed with an additional appropriate specifier at the top of the stack
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
                            //Nested condition statements are ignored and handled by the recursive call of this function, with another layer in the condition specifiers stack
                            conditionNesting++;
                        }
                    }
                    else if (conditionNesting == 1 && conditionSpecifiers.Peek() == ConditionSpecifier.Condition && token.Code == "then")
                    {
                        Token[] newTokenCode = new Token[i - clauseStartIndex];
                        Array.Copy(tokenCode, clauseStartIndex, newTokenCode, 0, newTokenCode.Length);

                        try
                        {
                            //The exception for the type signature matching is for the if clause where it must always be a bool value
                            //As such the type signature is given as being a bool, and the clause is required to match that
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
                                    //The type signatures of both the then and else clauses must match, otherwise there is ambiguity about the type signature of the condition block
                                    if (ifTrueTypeSignature != baseTypeSignature)
                                    {
                                        throw new PaskellCompileException("Both clauses in condition block don't match type signature", clauseStartIndex);
                                    }
                                }
                                else
                                {
                                    //A check for if the clauses match isn't necessary here as they are both checked again the argument type signature in recursed calls
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

                        //If the expression is set to be the base subexpression,  its type signature doesn't have to match the target type signature as it will be passed arguments
                        if (baseTypeSignature == null)
                        {
                            baseTypeSignature = expression.TypeSignature;
                            //Pushing the base subexpression onto the stack, argument count given as the number of arguments to make its type signature match the target type signature
                            outExpression.PushSubExpression(expression, baseTypeSignature.ArgumentCount - targetTypeSignature.ArgumentCount,
                                new Stack<ConditionSpecifier>(conditionSpecifiers));
                        }
                        //Otherwise, it must match the type signature of the next argument of the base subexpression
                        else
                        {
                            if (expression.TypeSignature != argumentTypeSignature)
                            {
                                throw new PaskellCompileException($"Argument must be of type {argumentTypeSignature.Value}", i);
                            }
                            else
                            {
                                //Pushing the subexpression onto the stack, argument count 0 as it will not evaluate anything
                                outExpression.PushSubExpression(expression, 0, new Stack<ConditionSpecifier>(conditionSpecifiers));
                                argumentCount++;
                            }
                        }
                    }
                    else
                    {
                        //If the token is an operand, it can either be the value of the whole expression, or must be an argument
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
                                    //Every operand type must be checked if the type required is generic
                                    bool success = false;
                                    foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
                                    {
                                        try
                                        {
                                            type = operandType.GetPType();
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
                                        //This won't happen
                                        throw new PaskellCompileException("You messed up your code P", i);
                                    }
                                }
                                //Pushes the variable onto the stack, argument count 0 as it is not a function
                                outExpression.PushSubExpression(new PExpression(variable), 0, new Stack<ConditionSpecifier>(conditionSpecifiers));
                                if (baseTypeSignature == null)
                                {
                                    baseTypeSignature = new TypeSignature(type);
                                    //After this the current call of this function should end, as a literal value cannot take arguments
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

            //Checking for bad nesting or unclosed clauses
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

            //Only if the type signature must match the target i.e. when the subexpression is an argument or when it composes the final definition of an expression
            if (typeSignatureMustMatch && baseTypeSignature[argumentCount] != targetTypeSignature)
            {
                throw new PaskellCompileException($"Expression must be of type {targetTypeSignature.Value}", 0);
            }

            //Returns the type signature of the new base subexpression, if nested
            return baseTypeSignature[argumentCount];
        }


        //All the base expression functions that are added at the beginning of compilation
        private static PExpression Add(Queue<PExpression> a)
        {
            return new PExpression(a.Dequeue().Evaluate().Value + a.Dequeue().Evaluate().Value);
        }

        private static PExpression Subtract(Queue<PExpression> a)
        {
            return new PExpression(a.Dequeue().Evaluate().Value - a.Dequeue().Evaluate().Value);
        }

        private static PExpression Multiply(Queue<PExpression> a)
        {
            return new PExpression(a.Dequeue().Evaluate().Value * a.Dequeue().Evaluate().Value);
        }

        private static PExpression Divide(Queue<PExpression> a)
        {
            return new PExpression(a.Dequeue().Evaluate().Value / a.Dequeue().Evaluate().Value);
        }

        private static PExpression Not(Queue<PExpression> a)
        {
            return new PExpression(!(bool)a.Dequeue().Evaluate().Value);
        }

        private static PExpression And(Queue<PExpression> a)
        {
            return new PExpression((bool)a.Dequeue().Evaluate().Value && (bool)a.Dequeue().Evaluate().Value);
        }

        private static PExpression Or(Queue<PExpression> a)
        {
            return new PExpression((bool)a.Dequeue().Evaluate().Value || (bool)a.Dequeue().Evaluate().Value);
        }

        private static PExpression Xor(Queue<PExpression> a)
        {
            return new PExpression((bool)a.Dequeue().Evaluate().Value ^ (bool)a.Dequeue().Evaluate().Value);
        }

        private static PExpression EqualTo(Queue<PExpression> a)
        {
            return new PExpression(a.Dequeue().Evaluate().Value == a.Dequeue().Evaluate().Value);
        }

        private static PExpression GreaterThan(Queue<PExpression> a)
        {
            return new PExpression(a.Dequeue().Evaluate().Value > a.Dequeue().Evaluate().Value);
        }

        private static PExpression LessThan(Queue<PExpression> a)
        {
            return new PExpression(a.Dequeue().Evaluate().Value < a.Dequeue().Evaluate().Value);
        }
    }

    //https://stackoverflow.com/questions/479410/enum-tostring-with-user-friendly-strings
    static class CustomEnumExtensions
    {
        //Used to retrieve the Regex pattern given by the RegexPattern attribute in the TokenType enum
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

        //Used to retrieve the type given by the PaskellType attribute in the OperandType enum
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
        public readonly string Code;            //What the actual source code contained (necessary to distinguish tokens of the same type such as operands)
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

    //Just an easier object to return for the IDE
    public class PContext
    {
        public PExpression[] Expressions;

        public PContext(PExpression[] expressions)
        {
            Expressions = expressions;
        }
    }

    //Summarises tokeniser success and errors
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

    //Summarises compiler success and errors
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

    //Thrown inside runtime, when things fail
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

    //Thrown by the compiler to indicate where errors occurred
    //These get caught and catalogued so that all the errors can be displayed an made useful
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
