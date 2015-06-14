﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YourTech.IO;
using YourTech.IO.Json;
using YourTech.IO.Yron;

namespace YourTech {
    [TestClass]
    public class YronTest {
        private static string _asmLocation;
        static YronTest() {
            _asmLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [TestMethod]
        public void YrodicTest() {
            string indentJsonTestPath = Path.Combine(_asmLocation, "IndentJsonTest.json");
            string yrodicTestPath = Path.Combine(_asmLocation, "YrodicTest.json");

            if (!File.Exists(indentJsonTestPath)) new JsonTest().IndentJsonTest();

            try {
                Dictionary<string, Card> cards = new Dictionary<string, Card>();
                using (YronWriter writer = new YronWriter(cards, new YrodicType(typeof(Card)))) {
                    using (JsonReader reader = new JsonReader(new StreamReader(File.OpenRead(indentJsonTestPath)), true)) {
                        writer.Write(reader);
                    }
                }
                using (JsonWriter writer = new JsonWriter(new StreamWriter(yrodicTestPath), true, true)) {
                    using (YronReader reader = new YronReader(cards, new YrodicType(typeof(Card)))) {
                        writer.Write(reader);
                    }
                }
            } catch (Exception ex) {
                Assert.Fail(ex.ToString());
            }
        }
    }

    [YronObject]
    public class Card {
        [YronProperty]
        public string layout { get; set; }
        [YronProperty]
        public string name { get; set; }
        [YronProperty]
        public string manaCost { get; set; }
        [YronProperty]
        public string cmc { get; set; }
        [YronProperty(GetOnly = true)]
        public List<string> colors { get; set; }
        [YronProperty]
        public string type { get; set; }
        [YronProperty(GetOnly = true)]
        public List<string> types { get; set; }
        [YronProperty(GetOnly = true)]
        public List<string> subtypes { get; set; }
        [YronProperty]
        public string text { get; set; }
        [YronProperty]
        public string imageName { get; set; }
        [YronProperty]
        public string power { get; set; }
        [YronProperty]
        public string toughness { get; set; }
        [YronProperty(GetOnly = true)]
        public List<string> supertypes { get; set; }
        [YronProperty]
        public string starter { get; set; }
        [YronProperty]
        public string loyalty { get; set; }
        [YronProperty]
        public string hand { get; set; }
        [YronProperty]
        public string life { get; set; }
        [YronProperty]
        public List<string> names { get; set; }
        public Card() {
            colors = new List<string>();
            types = new List<string>();
            subtypes = new List<string>();
            supertypes = new List<string>();
            names = new List<string>();
        }
    }
}