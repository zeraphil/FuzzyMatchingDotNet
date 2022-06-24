using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using FuzzyMatching;

namespace FuzzyMatching.Testing
{
    public class FuzzyCompareTest
    {
        private readonly ITestOutputHelper _output;

        public FuzzyCompareTest(ITestOutputHelper output)
        {
            this._output = output;
        }

        private readonly List<string> appList = new List<string>(){
          "3d viewer",
          "7-zip file manager",
          "access",
          "adobe creative cloud",
          "adobe lightroom classic",
          "adobe photoshop 2020",
          "adobe premiere pro 2020",
          "adobe xd",
          "alarms & clock",
          "anaconda navigator (anaconda3)",
          "anyconnect",
          "application verifier (wow)",
          "application verifier (x64)",
          "battery",
          "blend for visual studio 2017",
          "blend for visual studio 2019",
          "bluetooth",
          "browser",
          "calculator",
          "calendar",
          "camera",
          "character map",
          "cisco anyconnect secure mobility client",
          "cisco webex meetings",
          "citrix workspace",
          "command prompt",
          "computermanagement",
          "connect",
          "controlpanel",
          "database compare",
          "dateandtime",
          "desktop app converter",
          "desktopbackground",
          "devicemanager",
          "dfrgui",
          "diagrams.net desktop",
          "disk cleanup",
          "diskmanagement",
          "display",
          "email",
          "excel",
          "farm heroes saga",
          "feedback hub",
          "file explorer",
          "filezilla",
          "get help",
          "git bash",
          "git cmd (deprecated)",
          "git gui",
          "gitkraken",
          "google chrome",
          "groove music",
          "hyper-v manager",
          "idle (python 3.8 32-bit)",
          "immersive control panel",
          "internet explorer",
          "iscsi initiator",
          "itunes",
          "jetbrains pycharm community edition 2019.2.3",
          "jupyter notebook (anaconda3)",
          "lenovovoice",
          "lenovovoiceassistant",
          "lexmark printer home",
          "localservices",
          "logitech options",
          "magnify",
          "mail",
          "maps",
          "math input panel",
          "memory diagnostics tool",
          "messaging",
          "microsoft edge",
          "microsoft emulator",
          "microsoft solitaire collection",
          "microsoft store",
          "microsoft teams",
          "mixed reality portal",
          "mobile plans",
          "mongodb compass",
          "movies & tv",
          "msix packaging tool",
          "narrator",
          "network",
          "notepad",
          "notification manager for adobe creative cloud",
          "nvidia control panel",
          "odbc data sources (32-bit)",
          "odbc data sources (64-bit)",
          "office",
          "office language preferences",
          "onedrive",
          "onenote for windows 10",
          "on-screen keyboard",
          "outlook",
          "pageant",
          "paint",
          "paint 3d",
          "people",
          "photos",
          "powerandsleep",
          "poweroptions",
          "powerpoint",
          "print 3d",
          "programsandfeatures",
          "projection",
          "psftp",
          "publisher",
          "putty",
          "puttygen",
          "python 3.8 (32-bit)",
          "quick assist",
          "recoverydrive",
          "registry editor",
          "remote desktop connection",
          "resolution",
          "resource monitor",
          "schedulemanagement",
          "security",
          "settings",
          "simpleink c# sample",
          "skype for business",
          "skype for business recording manager",
          "snip & sketch",
          "snipping tool",
          "soundcontrolpanel",
          "soundmixeroptions",
          "speech recognition",
          "steps recorder",
          "sticky notes",
          "studio 3t",
          "system configuration",
          "system information",
          "task manager",
          "taskmanager",
          "telemetry dashboard for office",
          "telemetry log for office",
          "telerik ui for wpf examples",
          "tips",
          "video editor",
          "visual studio 2017",
          "visual studio 2019",
          "visual studio 2019 preview",
          "visual studio code",
          "vmcreate",
          "voice recorder",
          "volume",
          "wcostest",
          "weather",
          "windows",
          "windows app cert kit",
          "windows fax and scan",
          "windows media player",
          "windows powershell",
          "windows powershell (x86)",
          "windows powershell ise",
          "windows powershell ise (x86)",
          "windows security",
          "windowsupdate",
          "word",
          "wordpad",
          "xaml controls gallery",
          "xbox console companion",
          "xbox game bar",
          "your phone" };

        [Theory]
        // symbol issues
        [InlineData("alarm", new string[] { "alarms & clock" })]
        [InlineData("alarms", new string[] { "alarms & clock" })]
        [InlineData("alarms and clock", new string[] { "alarms & clock" })]
        [InlineData("alarm and clock", new string[] { "alarms & clock" })]
        [InlineData("alarm clock", new string[] { "alarms & clock" })]
        [InlineData("alarms clock", new string[] { "alarms & clock" })]
        [InlineData("7 zip manager", new string[] { "7-zip file manager" })]
        [InlineData("7 zip", new string[] { "7-zip file manager" })]
        [InlineData("7zip", new string[] { "7-zip file manager" })]
        [InlineData("zip manager", new string[] { "7-zip file manager" })]
        [InlineData("hyper v", new string[] { "hyper-v manager" })]
        [InlineData("snip", new string[] { "snip & sketch", "snipping tool" })]
        [InlineData("snip and sketch", new string[] { "snip & sketch" })]
        [InlineData("snip sketch", new string[] { "snip & sketch" })]
        // typo issues
        [InlineData("acess", new string[] { "access" })]
        [InlineData("world", new string[] { "word" })]
        // Note: known failures
        //[InlineData("7z", new string[] { "7-zip file manager" })] // Maybe not a failure, but it isn't ambiguous to a person
        //[InlineData("7zip manager", new string[] { "7-zip file manager" })] // Fails because 7-zip gets cleaned to 7 zip, not 7zip. This needs to be fixed for typing...
        public void FuzzyCompareAppMatchTest(string input, string[] expectedMatch)
        {
            List<FuzzyResult> resultList = new List<FuzzyResult>();
            foreach (string appName in this.appList)
            {
                FuzzyResult matchResult = FuzzyCompare.Ratio(input, appName, method: RatioMethod.PartialTokenSort);
                matchResult.Target = appName; // TODO: Should not need to fix the matchResult
                resultList.Add(matchResult);
            }

            _output.WriteLine(string.Join(Environment.NewLine, resultList.OrderByDescending(r => r.Ratio).Select(r => $"{r.Source} vs {r.Target} => {r.Ratio}")));

            double maxRatio = resultList.Max(r => r.Ratio);
            Assert.True(maxRatio > 70);

            var bestResult = resultList
                .Where(r => r.Ratio == maxRatio)
                .Select(r => r.Target)
                .ToArray();
            Assert.Equal(expectedMatch, bestResult);
        }
    }
}
