using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kronicle {

    class Record {
        public string title;
        public bool resolved;
        public string date;
        public string body;
        public string requestedBy;
        public string assignedTo;
        public HashSet<string> tags;
        public List<Comment> comments;
    }
    class Comment {
        public string author;
        public string date;
        public string body;
    }
    class Program {
        public static void Main(string[] args) => new Program();

        public static string ministryPage = "page.html";
        public static string recordCache = "records.json";

        List<Record> records = new List<Record>();
        public Program() {

            string recordJson;
            if(File.Exists(recordCache)) {
                recordJson = File.ReadAllText(recordCache);
                records = JsonConvert.DeserializeObject<List<Record>>(recordJson);
            } else {
                ReadRecords();
                recordJson = JsonConvert.SerializeObject(records);
                File.WriteAllText(recordCache, recordJson);
            }

            if (!File.Exists(ministryPage)) {
                ExtractMinistry();
                ExtractRecords();
            }

            File.WriteAllText("Transcendence Dev.html", File.ReadAllText("Transcendence Dev Template.html").Replace("%recordJson%", recordJson));

            var chrome = new ChromeDriver();
            var driver = new EventFiringWebDriver(chrome);
            driver.ElementClicked += (o, e) => {
                int a = 0;
            };
            driver.Navigate().GoToUrl($"file://{Path.GetFullPath("Transcendence Dev.html")}");
            }

        void ReadRecords() {

            var recordFiles = new HashSet<string>();//Directory.GetFiles(".", "record*.html")

            var doc = new HtmlDocument();
            doc.Load(ministryPage);
            foreach (var n in doc.GetElementbyId("idRecordList").ChildNodes.Where(c => c.GetAttributeValue("class", "") == "tableRow")) {

                var a = n.ChildNodes[2];
                var recordLink = n.ChildNodes[2].ChildNodes["a"].GetAttributes("href").First().Value;
                var recordFile = $"record{recordLink.Substring(recordLink.IndexOf('=') + 1)}.html";
                recordFiles.Add(recordFile);
            }

            foreach (var recordPage in recordFiles) {
                doc = new HtmlDocument();
                doc.Load(recordPage);

                var e = doc.DocumentNode;
                e = e.ChildNodes["html"];
                e = e.ChildNodes["body"];
                e = e.Elements("div").Skip(1).First();
                e = e.Elements("div").First();
                e = e.ChildNodes["p"];
                var title = e.InnerText;


                e = doc.DocumentNode;
                e = e.ChildNodes["html"];
                e = e.ChildNodes["body"];
                e = e.Elements("div").Skip(1).First();
                e = e.Elements("div").Skip(1).First();
                var resolved = e.Element("div").InnerText == "Resolved";


                var date = e.ChildNodes.Last().InnerText;

                e = doc.GetElementbyId("idRecordBody");
                var body = e.InnerHtml;

                e = doc.GetElementbyId("idRecordRequester");
                var requestedBy = e.ChildNodes[1].InnerText;

                e = doc.GetElementbyId("idRecordAssign");
                var assignedTo = e.ChildNodes[1].InnerText;

                e = doc.GetElementbyId("idRecordTags");
                var tags = e.ChildNodes.Where(c => c.GetAttributeValue("class", "") != "formLabelBlack").Select(c => c.InnerText).ToHashSet();

                e = doc.GetElementbyId("idRecordComments");
                var comments = e.ChildNodes.Select(c => {

                    var title = c.ChildNodes[0];

                    var author = title.ChildNodes[0].InnerText;
                    title.ChildNodes[0].Remove();

                    var date = title.InnerText;

                    var body = c.ChildNodes[1].InnerHtml;
                    return new Comment() { author = author, date = date, body = body };
                }).ToList();

                records.Add(new Record() {
                    
                assignedTo = assignedTo,
                body=body,
                comments=comments,
                date=date,
                requestedBy=requestedBy,
                resolved=resolved,
                tags=tags,
                title = title
                });
            }
        }
        void ExtractMinistry() {

            var driver = new ChromeDriver();
            try {
                // Navigate to Url
                driver.Navigate().GoToUrl("http://ministry.kronosaur.com/program.hexm?id=1&status=all");

                Action<Action<Actions>> Perform = perform => {
                    var a = new Actions(driver);
                    perform(a);
                    a.Build().Perform();
                };

                try {
                    while (true) {
                        var f = driver.FindElement(By.Id("idLoadMore"));
                        Perform(a => a.Click(f));

                        Thread.Sleep(500);
                    }
                } catch { }

                File.WriteAllText(ministryPage, driver.PageSource);
            } finally {
                driver.Quit();
            }
        }
        void ExtractRecords() {
            var doc = new HtmlDocument();
            doc.Load(ministryPage);

            var driver = new ChromeDriver();
            foreach (var n in doc.GetElementbyId("idRecordList").ChildNodes.Where(c => c.GetAttributeValue("class", "") == "tableRow")) {

                var a = n.ChildNodes[2];
                var recordLink = n.ChildNodes[2].ChildNodes["a"].GetAttributes("href").First().Value;
                var recordFile = $"record{recordLink.Substring(recordLink.IndexOf('=') + 1)}.html";

                if (!File.Exists(recordFile)) {
                    driver.Navigate().GoToUrl($"http://ministry.kronosaur.com/{recordLink}");

                    Thread.Sleep(2000);
                    File.WriteAllText(recordFile, driver.PageSource);
                }
            }
            driver.Quit();
        }
    }
}
