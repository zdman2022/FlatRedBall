﻿using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.Managers;
using FlatRedBall.Screens;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.Forms.Controls.Popups
{
    #region Classes

    class ToastInfo
    {
        public string Message { get; set; }
        public Layer FrbLayer { get; set; }
        public double DurationInSeconds { get; set; }
    }

    #endregion

    /// <summary>
    /// Object responsible for manging the lifecycle of toasts. This can be used to perform fire-and-forget showing of Toast objects.
    /// </summary>
    public static class ToastManager 
    {
        static BlockingCollection<ToastInfo> toastMessages = new BlockingCollection<ToastInfo>();

        /// <summary>
        /// The default layer for showing toast. If this is set at the Screen level, it should
        /// be set back to null when the Screen is destroyed.
        /// </summary>
        public static Layer DefaultToastLayer { get; set; }
        static IList liveToasts;
        static bool hasBeenStarted;
#if !UWP
        // threading works differently in UWP. do we care? Is UWP going to live?

        static void Start()
        {
            if(!hasBeenStarted)
            {
                // from:
                // https://stackoverflow.com/questions/5874317/thread-safe-listt-property
                // This seems to be the only one that supports add, remove, and foreach
                liveToasts = ArrayList.Synchronized(new ArrayList());
                hasBeenStarted = true;

                // Make sure we destroy toasts when the screen navigates
                ScreenManager.AfterScreenDestroyed += (unused) => DestroyLiveToasts();

                var thread = new System.Threading.Thread(new ThreadStart(DoLoop));
                thread.Start();
            }
        }
#endif

        public static void Show(string message, Layer frbLayer = null, double durationInSeconds = 2.0)
        {
            if(!hasBeenStarted)
            {
#if !UWP
                if(FlatRedBallServices.IsThreadPrimary())
                {
                    Start();
                }
                else
                {
                    Instructions.InstructionManager.AddSafe(Start);
                }
#endif
            }

            var toastInfo = new ToastInfo { Message = message, FrbLayer = frbLayer, DurationInSeconds = durationInSeconds};

            toastMessages.Add(toastInfo);
        }

        public static void DestroyLiveToasts()
        {
            int numberOfToastsToClean = liveToasts?.Count ?? 0;
            if(liveToasts != null)
            {
                foreach(Toast item in liveToasts)
                {
                    item?.Close();
                }

            }
            liveToasts?.Clear();

#if DEBUG
            if(GuiManager.Windows.Any(item => item is Toast))
            {
                throw new Exception("Toasts did not clean up and they should have. Why not?");
            }
#endif
        }

        private static async void DoLoop()
        {
            const int msDelayBetweenToasts = 100;

            foreach (var message in toastMessages.GetConsumingEnumerable(CancellationToken.None))
            {
                Toast toast = null;
                
                // This must be done on the primary thread in case it loads
                // the PNG for the first time:
                await Instructions.InstructionManager.DoOnMainThreadAsync(() =>
                {
                    try
                    {
                        toast = new FlatRedBall.Forms.Controls.Popups.Toast();
                    }
                    // If the user doesn't have any toast implemented in Gum, this will error. 
                    // This causes problems in edit mode, so let's just consume it:
                    catch
                    {

                    }
                });

                if(toast != null)
                {
                    toast.Text = message.Message;
                    liveToasts.Add(toast);
                    toast.Show(message.FrbLayer ?? DefaultToastLayer);
                    await Task.Delay( TimeSpan.FromSeconds(message.DurationInSeconds) );
                    toast.Close();
                    liveToasts.Remove(toast);
                    await Instructions.InstructionManager.DoOnMainThreadAsync(() =>
                    {
                        toast.Visual.RemoveFromManagers();
                    });
                    // so there's a small gap between toasts
                    await Task.Delay(msDelayBetweenToasts);
                }
            }
        }
    }
}
