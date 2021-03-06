﻿// Copyright (c) micah686. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Media;
using System.Timers;
using Stylet;

namespace VnManager.ViewModels.Dialogs
{
    public class DeleteEverythingViewModel: Screen, IDisposable
    {
        private readonly Timer _timer= new Timer(1500);
        private int _timerHitCount = 0;
        public bool CanPressDelete { get; set; }
        public string YesButtonTest { get; private set; }

        public DeleteEverythingViewModel()
        {
            CanPressDelete = false;
            YesButtonTest = "5";
            SystemSounds.Asterisk.Play();
            _timer.AutoReset = true;
            _timer.Elapsed += TimerOnElapsed;
            _timer.Enabled = true;
            _timer.Start();
        }

        /// <summary>
        /// Wait for a bit before allowing user to delete everything
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            const int initialCount = 0;
            const int firstCount = 1;
            const int secondCount = 2;
            const int thirdCount = 3;
            const int fourthCount = 4;
            
            switch (_timerHitCount)
            {
                case initialCount:
                    _timerHitCount += 1;
                    YesButtonTest = "4";
                    break;
                case firstCount:
                    _timerHitCount += 1;
                    YesButtonTest = "3";
                    break;
                case secondCount:
                    _timerHitCount += 1;
                    YesButtonTest = "2";
                    break;
                case thirdCount:
                    _timerHitCount += 1;
                    YesButtonTest = "1";
                    break;
                case fourthCount:
                    _timerHitCount += 1;
                    YesButtonTest = App.ResMan.GetString("Yes");
                    CanPressDelete = true;
                    _timer.Stop();
                    break;
                default:
                    _timer.Stop();
                    break;
            }
        }

        /// <summary>
        /// Close window setting it to Delete Everything
        /// </summary>
        public void DeleteEverything()
        {
            RequestClose(true);
        }

        /// <summary>
        /// Cancel the delete everything
        /// </summary>
        public void Cancel()
        {
            RequestClose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
        }
    }
}
