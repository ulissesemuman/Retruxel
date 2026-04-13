using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

namespace Retruxel.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        
        // Format: 0.2.0-alpha (using only Major.Minor.Build + manual suffix)
        TxtVersion.Text = version != null 
            ? $"{version.Major}.{version.Minor}.{version.Build}-alpha"
            : "dev";

        LoadCredits();
        LoadLicenses();
    }

    private void DeveloperLink_Click(object sender, MouseButtonEventArgs e)
        => OpenUrl("https://github.com/ulissesemuman");

    private void ProjectLink_Click(object sender, MouseButtonEventArgs e)
        => OpenUrl("https://github.com/ulissesemuman/Retruxel");

    private void CreditLink_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock tb && tb.Tag is string url)
            OpenUrl($"https://{url}");
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { /* Silently fail if browser can't open */ }
    }

    private void LoadCredits()
    {
        CreditsPanel.ItemsSource = new[]
        {
            new CreditEntry
            {
                Name = "GB Studio",
                License = "MIT",
                Description = "Visual game creator for Game Boy. Conceptual inspiration for Retruxel's visual development approach.",
                Url = "www.gbstudio.dev"
            },
            new CreditEntry
            {
                Name = ".NET / WPF",
                License = "MIT",
                Description = "Application framework and UI toolkit by Microsoft. Foundation of Retruxel's desktop application.",
                Url = "dotnet.microsoft.com"
            },
            new CreditEntry
            {
                Name = "SDCC — Small Device C Compiler",
                License = "GPL v2",
                Description = "C compiler targeting Z80 and other embedded architectures. Used to compile SMS game code.",
                Url = "sdcc.sourceforge.net"
            },
            new CreditEntry
            {
                Name = "devkitSMS",
                License = "MIT",
                Description = "Development kit and libraries for Sega Master System homebrew programming in C.",
                Url = "github.com/sverx/devkitSMS"
            },
            new CreditEntry
            {
                Name = "SMSlib",
                License = "MIT",
                Description = "C library for SMS/Game Gear programming. Part of devkitSMS by sverx.",
                Url = "github.com/sverx/devkitSMS/tree/master/SMSlib"
            },
            new CreditEntry
            {
                Name = "ihx2sms",
                License = "MIT",
                Description = "Converts SDCC .ihx output into a valid Sega Master System ROM file. Part of devkitSMS.",
                Url = "github.com/sverx/devkitSMS/tree/master/ihx2sms"
            },
            new CreditEntry
            {
                Name = "cc65",
                License = "Zlib",
                Description = "C compiler and toolchain for 6502-based systems including NES. Used to compile NES game code.",
                Url = "cc65.github.io"
            },
            new CreditEntry
            {
                Name = "neslib",
                License = "Zlib",
                Description = "C library for NES programming. Provides high-level functions for graphics, sound, and input.",
                Url = "github.com/clbr/neslib"
            },
            new CreditEntry
            {
                Name = "Space Grotesk",
                License = "SIL OFL 1.1",
                Description = "Display typeface used for titles and headlines throughout the Retruxel UI.",
                Url = "fonts.google.com/specimen/Space+Grotesk"
            },
            new CreditEntry
            {
                Name = "Inter",
                License = "SIL OFL 1.1",
                Description = "Body and label typeface used for readable text throughout the Retruxel UI.",
                Url = "fonts.google.com/specimen/Inter"
            },
            new CreditEntry
            {
                Name = "SkiaSharp",
                License = "MIT",
                Description = "Cross-platform 2D graphics API for .NET. Used for font rasterization and glyph rendering in the Tile Editor.",
                Url = "github.com/mono/SkiaSharp"
            }
        };
    }

    private void LoadLicenses()
    {
        TxtLicenses.Text = """
            === GB Studio ===
            Copyright (c) 2019-2024 Chris Maltby
            
            Permission is hereby granted, free of charge, to any person obtaining a copy
            of this software and associated documentation files (the "Software"), to deal
            in the Software without restriction, including without limitation the rights
            to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            copies of the Software, and to permit persons to whom the Software is
            furnished to do so, subject to the following conditions:
            
            The above copyright notice and this permission notice shall be included in all
            copies or substantial portions of the Software.
            
            === .NET / WPF ===
            Copyright (c) Microsoft Corporation
            
            Licensed under the MIT License.
            See: github.com/dotnet/runtime/blob/main/LICENSE.TXT
            
            === devkitSMS / SMSlib / ihx2sms ===
            Copyright (c) 2011-2024 sverx
            
            Permission is hereby granted, free of charge, to any person obtaining a copy
            of this software and associated documentation files (the "Software"), to deal
            in the Software without restriction, including without limitation the rights
            to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            copies of the Software, and to permit persons to whom the Software is
            furnished to do so, subject to the following conditions:
            
            The above copyright notice and this permission notice shall be included in all
            copies or substantial portions of the Software.
            
            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
            IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
            FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
            
            === Space Grotesk / Inter ===
            Copyright (c) The respective authors
            
            These fonts are licensed under the SIL Open Font License, Version 1.1.
            This license is available with a FAQ at: scripts.sil.org/OFL
            
            === SkiaSharp ===
            Copyright (c) 2015-2024 Xamarin, Microsoft Corporation, and contributors
            
            Permission is hereby granted, free of charge, to any person obtaining a copy
            of this software and associated documentation files (the "Software"), to deal
            in the Software without restriction, including without limitation the rights
            to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            copies of the Software, and to permit persons to whom the Software is
            furnished to do so, subject to the following conditions:
            
            The above copyright notice and this permission notice shall be included in all
            copies or substantial portions of the Software.
            
            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
            IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
            FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
            
            === SDCC ===
            SDCC is licensed under the GNU General Public License version 2.
            See: www.gnu.org/licenses/gpl-2.0.html
            
            === cc65 / neslib ===
            Copyright (c) The cc65 and neslib authors
            
            This software is provided 'as-is', without any express or implied
            warranty. In no event will the authors be held liable for any damages
            arising from the use of this software.
            
            Permission is granted to anyone to use this software for any purpose,
            including commercial applications, and to alter it and redistribute it
            freely, subject to the following restrictions:
            
            1. The origin of this software must not be misrepresented; you must not
               claim that you wrote the original software.
            2. Altered source versions must be plainly marked as such, and must not be
               misrepresented as being the original software.
            3. This notice may not be removed or altered from any source distribution.
            """;
    }
}

/// <summary>Represents a third-party credit entry shown in the About view.</summary>
public class CreditEntry
{
    public string Name { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}