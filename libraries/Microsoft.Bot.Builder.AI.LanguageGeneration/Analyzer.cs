﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.Bot.Builder.Expressions;
using Microsoft.Bot.Builder.Expressions.Parser;

namespace Microsoft.Bot.Builder.AI.LanguageGeneration
{
    public class Analyzer : LGFileParserBaseVisitor<List<string>>
    {
        public readonly EvaluationContext Context;

        private IExpressionParser _expressionParser;

        private Stack<EvaluationTarget> evaluationTargetStack = new Stack<EvaluationTarget>();
        private EvaluationTarget CurrentTarget()
        {
            // just don't want to write evaluationTargetStack.Peek() everywhere
            return evaluationTargetStack.Peek();
        }

        public Analyzer(EvaluationContext context)
        {
            Context = context;
            _expressionParser = new ExpressionEngine(new GetMethodExtensions(null).GetMethodX);
        }

        public List<string> AnalyzeTemplate(string templateName)
        {
            if (!Context.TemplateContexts.ContainsKey(templateName))
            {
                throw new Exception($"No such template: {templateName}");
            }

            if (evaluationTargetStack.Any(e => e.TemplateName == templateName))
            {
                throw new Exception($"Loop detected: {String.Join(" => ", evaluationTargetStack.Reverse().Select(e => e.TemplateName))} => {templateName}");
            }

            // Using a stack to track the evalution trace
            evaluationTargetStack.Push(new EvaluationTarget(templateName, null));
            var rawDependencies = Visit(Context.TemplateContexts[templateName]);

            var parameters = ExtractParameters(templateName);

            // we need to exclude parameters from raw dependencies
            var dependencies = rawDependencies.Except(parameters).Distinct().ToList();

            evaluationTargetStack.Pop();

            return dependencies;
        }

        public override List<string> VisitTemplateDefinition([NotNull] LGFileParser.TemplateDefinitionContext context)
        {
            var templateNameContext = context.templateNameLine();
            if (templateNameContext.templateName().GetText().Equals(CurrentTarget().TemplateName))
            {
                if (context.templateBody() != null)
                {
                    return Visit(context.templateBody());
                }
            }
            throw new Exception("template name match failed");
        }

        public override List<string> VisitNormalBody([NotNull] LGFileParser.NormalBodyContext context)
        {
            return Visit(context.normalTemplateBody());
        }

        public override List<string> VisitNormalTemplateBody([NotNull] LGFileParser.NormalTemplateBodyContext context)
        {
            var result = new List<string>();

            foreach (var templateStr in context.normalTemplateString())
            {
                var item = Visit(templateStr);
                result.AddRange(item);
            }

            return result;
        }

        public override List<string> VisitConditionalBody([NotNull] LGFileParser.ConditionalBodyContext context)
        {
            var result = new List<string>();

            var caseRules = context.conditionalTemplateBody().caseRule();
            foreach (var caseRule in caseRules)
            {
                if (caseRule.caseCondition().EXPRESSION() != null
                    && caseRule.caseCondition().EXPRESSION().Length >= 0)
                {
                    var conditionExpression = caseRule.caseCondition().EXPRESSION(0).GetText();
                    var childConditionResult = AnalyzeExpression(conditionExpression);
                    result.AddRange(childConditionResult);
                }

                if (caseRule.normalTemplateBody() != null)
                {
                    var childTemplateBodyResult = Visit(caseRule.normalTemplateBody());
                    result.AddRange(childTemplateBodyResult);
                }
            }

            if (context?.conditionalTemplateBody()?.defaultRule() != null)
            {
                var childDefaultRuleResult = Visit(context.conditionalTemplateBody().defaultRule().normalTemplateBody());
                result.AddRange(childDefaultRuleResult);
            }

            return result;
        }


        public override List<string> VisitNormalTemplateString([NotNull] LGFileParser.NormalTemplateStringContext context)
        {
            var result = new List<string>();
            foreach (ITerminalNode node in context.children)
            {
                switch (node.Symbol.Type)
                {
                    case LGFileParser.DASH:
                        break;
                    case LGFileParser.EXPRESSION:
                        result.AddRange(AnalyzeExpression(node.GetText()));
                        break;
                    case LGFileParser.TEMPLATE_REF:
                        result.AddRange(AnalyzeTemplateRef(node.GetText()));
                        break;
                    case LGFileLexer.MULTI_LINE_TEXT:
                        result.AddRange(AnalyzeMultiLineText(node.GetText()));
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        private List<string> AnalyzeExpression(string exp)
        {
            exp = exp.TrimStart('{').TrimEnd('}');
            var parse = _expressionParser.Parse(exp);
            var references = new HashSet<string>();
            var path = Microsoft.Bot.Builder.Expressions.Extensions.ReferenceWalk(parse, references,
                (expression) =>
                {
                    var found = false;
                    if (expression is Constant cnst && cnst.Value is string str)
                    {
                        if (str.StartsWith("[") && str.EndsWith("]"))
                        {
                            found = true;
                            var end = str.IndexOf('(');
                            if (end == -1)
                            {
                                end = str.Length - 1;
                            }
                            var template = str.Substring(1, end - 1);
                            var analyzer = new Analyzer(Context);
                            foreach (var reference in analyzer.AnalyzeTemplate(template))
                            {
                                references.Add(reference);
                            }
                        }
                        else if (str.StartsWith("{") && str.EndsWith("}"))
                        {
                            found = true;
                            foreach (var childRef in AnalyzeExpression(str))
                            {
                                references.Add(childRef);
                            }
                        }
                    }
                    return found;
                });
            if (path != null)
            {
                references.Add(path);
            }
            return references.ToList();
        }

        private List<string> AnalyzeTemplateRef(string exp)
        {
            exp = exp.TrimStart('[').TrimEnd(']').Trim();

            var argsStartPos = exp.IndexOf('(');
            if (argsStartPos > 0) // Do have args
            {
                // EvaluateTemplate all arguments using ExpressoinEngine
                var argsEndPos = exp.LastIndexOf(')');

                var templateName = exp.Substring(0, argsStartPos);

                return AnalyzeTemplate(templateName);
            }
            else
            {
                return AnalyzeTemplate(exp);
            }
        }

        private List<string> AnalyzeMultiLineText(string exp)
        {
            var result = new List<string>();
            exp = exp.Substring(3, exp.Length - 6); //remove ``` ```

            var matches = Regex.Matches(exp, @"@\{[^{}]+\}");
            foreach (Match matchItem in matches)
            {
                if (matchItem.Success)
                {
                    var value = matchItem.Value.Substring(1);// remove @

                    if (value.StartsWith("{[") && value.EndsWith("]}"))
                    {
                        result.AddRange(AnalyzeTemplateRef(value.Substring(2, value.Length - 4)));//[ ]
                    }
                    else
                    {
                        result.AddRange(AnalyzeExpression(value));//{ }
                    }
                }
            }

            return result;
        }

        private List<string> ExtractParameters(string templateName)
        {
            var hasParameters = Context.TemplateParameters.TryGetValue(templateName, out var parameters);
            return hasParameters ? parameters : new List<string>();
        }
    }
}
