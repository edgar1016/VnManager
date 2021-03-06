﻿// Copyright (c) micah686. All Rights Reserved.
// Licensed under the MIT License.  See the LICENSE file in the project root for license information.

using FluentValidation;

namespace VnManager.Helpers
{
    internal static class ValidationHelpers
    {
        /// <summary>
        /// Default exe validator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, string> ExeValidation<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage(App.ResMan.GetString("ValidationExePathEmpty"))
                .Must(ValidateFiles.EndsWithExe).WithMessage(App.ResMan.GetString("ValidationExePathNotValid"))
                .Must(ValidateFiles.ValidateExe).WithMessage(App.ResMan.GetString("ValidationExeNotValid"));
        }

        /// <summary>
        /// Default icon validator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, string> IcoValidation<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage(App.ResMan.GetString("ValidationIconPathEmpty"))
                .Must(ValidateFiles.EndsWithIcoOrExe).WithMessage(App.ResMan.GetString("ValidationIconPathNotValid"));
        }

        /// <summary>
        /// Default exe arguments validator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, string> ArgsValidation<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage(App.ResMan.GetString("ValidationArgumentsEmpty"));

        }





        /// <summary>
        /// Checks if a string contains invalid characters. This is mostly used when checking file paths
        /// </summary>
        /// <param name="format">String to check for bad characters</param>
        /// <returns>Returns true if the string did have illegal characters in it</returns>
        public static bool ContainsIllegalCharacters(string format)
        {
            if (format == null)
            {
                return false;
            }
            string allowableLetters = $@"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890/\-_ !?;:'+={'"'}";

            foreach (char c in format)
            {
                if (!allowableLetters.Contains(c))
                {
                    return false;
                }
            }

            return true;
        }


    }
}
