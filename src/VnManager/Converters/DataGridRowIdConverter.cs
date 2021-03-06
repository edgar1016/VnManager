﻿// Copyright (c) micah686. All Rights Reserved.
// Licensed under the MIT License.  See the LICENSE file in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace VnManager.Converters
{
    /// <summary>
    /// XAML converter for getting index of a DataGrid Row
    /// </summary>
    public class DataGridRowIdConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return ((DataGridRow) value)?.GetIndex() ?? -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
