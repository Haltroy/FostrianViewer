/*
 * Copyright (C) 2022 haltroy
 *
 * Use of this source code is governed by MIT License that can be found in
 * https://github.com/haltroy/FostrianViewer/blob/main/LICENSE
 *
 */

using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FostrianViewer
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            Version.Text = "v"
        + (
            System.Reflection.Assembly.GetExecutingAssembly() is Assembly ass
            && ass.GetName() is AssemblyName name
            && name.Version != null
            ? "" + (name.Version.Major > 0 ? name.Version.Major : "") + (name.Version.Minor > 0 ? "." + name.Version.Minor : "") + (name.Version.Build > 0 ? "." + name.Version.Build : "") + (name.Version.Revision > 0 ? "." + name.Version.Revision : "")
            : "?"
            );
            License.Text = ReadResource("FostrianViewer.LICENSE");
        }

        public static string ReadResource(string name)
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (stream != null)
                {
                    using StreamReader reader = new(stream);
                    return reader.ReadToEnd();
                }
                else
                {
                    throw new NotImplementedException("Stream was null.");
                }
            }
            catch (System.Exception ex)
            {
                return "Error while reading the license file: " + ex.ToString();
            }
        }

        private void Navigate(object? sender, RoutedEventArgs e)
        {
            if (sender is Control control && control.Tag is string link)
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = link
                });
            }
        }
    }
}