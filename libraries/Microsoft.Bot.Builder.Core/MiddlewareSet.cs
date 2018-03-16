﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder
{
    public class MiddlewareSet : IMiddleware
    {
        public delegate Task NextDelegate();

        private readonly IList<IMiddleware> _middleware = new List<IMiddleware>();

        public MiddlewareSet Use(IMiddleware middleware)
        {
            BotAssert.MiddlewareNotNull(middleware);
            _middleware.Add(middleware);
            return this;
        }

        public async Task ReceiveActivity(IBotContext context)
        {
            await ReceiveActivityInternal(context, null).ConfigureAwait(false);
        }

        public async Task OnProcessRequest(IBotContext context, NextDelegate next)
        {
            await ReceiveActivityInternal(context, null).ConfigureAwait(false);
            await next().ConfigureAwait(false);
        }

        /// <summary>
        /// Intended to be called from Bot, this method performs exactly the same as the
        /// standard ReceiveActivity, except that it runs a user-defined delegate returns 
        /// if all Middlware in the receive pipeline was run.
        /// </summary>
        public async Task ReceiveActivityWithStatus(IBotContext context, Func<IBotContext, Task> callback)
        {
            await ReceiveActivityInternal(context, callback).ConfigureAwait(false);
        }

        private Task ReceiveActivityInternal(IBotContext context, Func<IBotContext, Task> callback, int nextMiddlewareIndex = 0)
        {
            if(nextMiddlewareIndex == _middleware.Count)
            {
                // If all the Middlware ran, the "leading edge" of the tree is now complete. 
                // This means it's time to run any developer specified callback. 
                // Once this callback is done, the "trailing edge" calls are then completed. This
                // allows code that looks like:
                //      console.print("before");
                //      await next();
                //      console.print("after"); 
                // to run as expected.


                return callback?.Invoke(context) ?? Task.CompletedTask;
            }

            // Grab the current middleware, which is the 1st element in the array, and execute it            
            return _middleware[nextMiddlewareIndex].OnProcessRequest(
                context,
                () => ReceiveActivityInternal(context, callback, nextMiddlewareIndex + 1));
        }
    }
}
