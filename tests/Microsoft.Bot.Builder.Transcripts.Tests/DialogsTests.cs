﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Tests;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Transcripts.Tests
{
    [TestClass]
    public class DialogsTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task AttachmentPrompt()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);
            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                    var prompt = new AttachmentPrompt();

                    var dialogCompletion = await prompt.ContinueDialogAsync(turnContext, state);
                    if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                    {
                        await prompt.BeginAsync(turnContext, state, new PromptOptions { PromptString = "please add an attachment." });
                    }
                    else if (dialogCompletion.IsCompleted)
                    {
                        var attachmentResult = (AttachmentResult)dialogCompletion.Result;
                        var reply = (string)attachmentResult.Attachments.First().Content;
                        await turnContext.SendActivityAsync(reply);
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ChoicePrompt()
        {
            var dialogs = new DialogSet();

            dialogs.Add("test-prompt", new Dialogs.ChoicePrompt(Culture.English) { Style = ListStyle.Inline });

            var promptOptions = new ChoicePromptOptions
            {
                Choices = new List<Choice>
                {
                    new Choice { Value = "red" },
                    new Choice { Value = "green" },
                    new Choice { Value = "blue" },
                },
                RetryPromptString = "I didn't catch that. Select a color from the list."
            };

            dialogs.Add("test",
                new WaterfallStep[]
                {
                    async (dc, args, next) =>
                    {
                        await dc.PromptAsync("test-prompt", "favorite color?", promptOptions);
                    },
                    async (dc, args, next) =>
                    {
                        var choiceResult = (ChoiceResult)args;
                        await dc.Context.SendActivityAsync($"Bot received the choice '{choiceResult.Value.Value}'.");
                        await dc.EndDialogAsync();
                    }
                }
            );

            var activities = TranscriptUtilities.GetFromTestContext(TestContext);

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                    var dc = dialogs.CreateContext(turnContext, state);

                    await dc.ContinueDialogAsync();

                    if (!turnContext.Responded)
                    {
                        await dc.BeginAsync("test");
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        [TestMethod]
        public async Task ConfirmPrompt()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                    var prompt = new ConfirmPrompt(Culture.English) { Style = ListStyle.None };

                    var dialogCompletion = await prompt.ContinueDialogAsync(turnContext, state);
                    if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                    {
                        await prompt.BeginAsync(turnContext, state,
                                new PromptOptions
                                {
                                    PromptString = "Please confirm.",
                                    RetryPromptString = "Please confirm, say 'yes' or 'no' or something like that."
                                });
                    }
                    else if (dialogCompletion.IsCompleted)
                    {
                        if (((ConfirmResult)dialogCompletion.Result).Confirmation)
                        {
                            await turnContext.SendActivityAsync("Confirmed.");
                        }
                        else
                        {
                            await turnContext.SendActivityAsync("Not confirmed.");
                        }
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        [TestMethod]
        public async Task DateTimePrompt()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                    var prompt = new DateTimePrompt(Culture.English);

                    var dialogCompletion = await prompt.ContinueDialogAsync(turnContext, state);
                    if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                    {
                        await prompt.BeginAsync(turnContext, state, new PromptOptions { PromptString = "What date would you like?", RetryPromptString = "Sorry, but that is not a date. What date would you like?" });
                    }
                    else if (dialogCompletion.IsCompleted)
                    {
                        var dateTimeResult = (DateTimeResult)dialogCompletion.Result;
                        var resolution = dateTimeResult.Resolution.First();
                        var reply = $"Timex:'{resolution.Timex}' Value:'{resolution.Value}'";
                        await turnContext.SendActivityAsync(reply);
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        [TestMethod]
        public async Task NumberPrompt()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);


            PromptValidatorEx.PromptValidator<NumberResult<int>> validator = async (ctx, result) =>
            {
                if (result.Value < 0)
                    result.Status = PromptStatus.TooSmall;
                if (result.Value > 100)
                    result.Status = PromptStatus.TooBig;
                await Task.CompletedTask;
            };

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                    var prompt = new NumberPrompt<int>(Culture.English, validator);

                    var dialogCompletion = await prompt.ContinueDialogAsync(turnContext, state);
                    if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                    {
                        await prompt.BeginAsync(turnContext, state,
                                new PromptOptions
                                {
                                    PromptString = "Enter a number.",
                                    RetryPromptString = "You must enter a valid positive number less than 100."
                                });
                    }
                    else if (dialogCompletion.IsCompleted)
                    {
                        var numberResult = (NumberResult<int>)dialogCompletion.Result;
                        await turnContext.SendActivityAsync($"Bot received the number '{numberResult.Value}'.");
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        [TestMethod]
        public async Task TextInput()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);

            PromptValidatorEx.PromptValidator<TextResult> validator = async (ctx, result) =>
            {
                if (result.Value.Length <= 3)
                    result.Status = PromptStatus.TooSmall;
                await Task.CompletedTask;
            };

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {

                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                    var prompt = new TextInput(validator);

                    var dialogCompletion = await prompt.ContinueDialogAsync(turnContext, state);
                    if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                    {
                        await prompt.BeginAsync(turnContext, state,
                                new PromptOptions
                                {
                                    PromptString = "Enter some text.",
                                    RetryPromptString = "Make sure the text is greater than three characters."
                                });
                    }
                    else if (dialogCompletion.IsCompleted)
                    {
                        var textResult = (TextResult)dialogCompletion.Result;
                        await turnContext.SendActivityAsync($"Bot received the text '{textResult.Value}'.");
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        [TestMethod]
        public async Task Waterfall()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {

                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());

                    var waterfall = new Waterfall(new WaterfallStep[]
                    {
                    async (dc, args, next) => { await dc.Context.SendActivityAsync("step1"); },
                    async (dc, args, next) => { await dc.Context.SendActivityAsync("step2"); },
                    async (dc, args, next) => { await dc.Context.SendActivityAsync("step3"); },
                    });


                    var dialogCompletion = await waterfall.ContinueDialogAsync(turnContext, state);
                    if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                    {
                        await waterfall.BeginAsync(turnContext, state);
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        [TestMethod]
        public async Task WaterfallPrompt()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());

                    var dialogs = new DialogSet();
                    dialogs.Add("test-waterfall", Create_Waterfall2());
                    dialogs.Add("number", new NumberPrompt<int>(Culture.English));

                    var dc = dialogs.CreateContext(turnContext, state);

                    await dc.ContinueDialogAsync();

                    if (!turnContext.Responded)
                    {
                        await dc.BeginAsync("test-waterfall");
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        private static WaterfallStep[] Create_Waterfall2()
        {
            return new WaterfallStep[] {
                Waterfall2_Action1,
                Waterfall2_Action2,
                Waterfall2_Action3
            };
        }

        private static async Task Waterfall2_Action1(DialogContext dc, WaterfallStepContext stepContext)
        {
            await dc.Context.SendActivityAsync("step1");
            await dc.PromptAsync("number", "Enter a number.", new PromptOptions { RetryPromptString = "It must be a number" });
        }
        private static async Task Waterfall2_Action2(DialogContext dc, WaterfallStepContext stepContext)
        {
            if (args != null)
            {
                var numberResult = (NumberResult<int>)args;
                await dc.Context.SendActivityAsync($"Thanks for '{numberResult.Value}'");
            }
            await dc.Context.SendActivityAsync("step2");
            await dc.PromptAsync("number", "Enter a number.", new PromptOptions { RetryPromptString = "It must be a number" });
        }
        private static async Task Waterfall2_Action3(DialogContext dc, WaterfallStepContext stepContext)
        {
            if (args != null)
            {
                var numberResult = (NumberResult<int>)args;
                await dc.Context.SendActivityAsync($"Thanks for '{numberResult.Value}'");
            }
            await dc.Context.SendActivityAsync("step3");
            await dc.EndDialogAsync(new Dictionary<string, object> { { "Value", "All Done!" } });
        }

        [TestMethod]
        public async Task WaterfallNested()
        {
            var activities = TranscriptUtilities.GetFromTestContext(TestContext);

            var convState = new ConversationState(new MemoryStorage());
            var testProperty = convState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(convState);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                    var dialogs = new DialogSet();
                    dialogs.Add("test-waterfall-a", Create_Waterfall3());
                    dialogs.Add("test-waterfall-b", Create_Waterfall4());
                    dialogs.Add("test-waterfall-c", Create_Waterfall5());

                    var dc = dialogs.CreateContext(turnContext, state);

                    await dc.ContinueDialogAsync();

                    if (!turnContext.Responded)
                    {
                        await dc.BeginAsync("test-waterfall-a");
                    }
                }
            })
            .Test(activities)
            .StartTestAsync();
        }

        private static WaterfallStep[] Create_Waterfall3()
        {
            return new WaterfallStep[] {
                Waterfall3_Action1,
                Waterfall3_Action2
            };
        }
        private static WaterfallStep[] Create_Waterfall4()
        {
            return new WaterfallStep[] {
                Waterfall4_Action1,
                Waterfall4_Action2
            };
        }

        private static WaterfallStep[] Create_Waterfall5()
        {
            return new WaterfallStep[] {
                Waterfall5_Action1,
                Waterfall5_Action2
            };
        }

        private static async Task Waterfall3_Action1(DialogContext dc, WaterfallStepContext stepContext)
        {
            await dc.Context.SendActivityAsync("step1");
            await dc.BeginAsync("test-waterfall-b");
        }
        private static async Task Waterfall3_Action2(DialogContext dc, WaterfallStepContext stepContext)
        {
            await dc.Context.SendActivityAsync("step2");
            await dc.BeginAsync("test-waterfall-c");
        }

        private static async Task Waterfall4_Action1(DialogContext dc, WaterfallStepContext stepContext)
        {
            await dc.Context.SendActivityAsync("step1.1");
        }
        private static async Task Waterfall4_Action2(DialogContext dc, WaterfallStepContext stepContext)
        {
            await dc.Context.SendActivityAsync("step1.2");
        }

        private static async Task Waterfall5_Action1(DialogContext dc, WaterfallStepContext stepContext)
        {
            await dc.Context.SendActivityAsync("step2.1");
        }
        private static async Task Waterfall5_Action2(DialogContext dc, WaterfallStepContext stepContext)
        {
            await dc.Context.SendActivityAsync("step2.2");
            await dc.EndDialogAsync();
        }
    }
}*/
