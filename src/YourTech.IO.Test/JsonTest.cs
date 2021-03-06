﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YourTech.IO.Bon;
using YourTech.IO.Json;

namespace YourTech {
    [TestClass]
    public class JsonTest {
        private static string _asmLocation;
        static JsonTest() {
            _asmLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [TestMethod]
        public void IndentJsonTest() {
            string srcPath = Path.Combine(_asmLocation, "AllCards.json");
            string destPath = Path.Combine(_asmLocation, "IndentJsonTest.json");

            try {
                using (JsonWriter writer = new JsonWriter(new StreamWriter(destPath), true, true)) {
                    using (JsonReader reader = new JsonReader(new StreamReader(File.OpenRead(srcPath)), true)) {
                        writer.Write(reader);
                    }
                }
            } catch (Exception ex) {
                Assert.Fail(ex.ToString());
            }
        }
    }
}
