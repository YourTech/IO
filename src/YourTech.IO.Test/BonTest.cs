using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class BonTest {
        private static string _asmLocation;
        static BonTest() {
            _asmLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [TestMethod]
        public void BonJsonConvertTest() {
            string srcPath = Path.Combine(_asmLocation, "AllCards.json");
            string indentJsonTestPath = Path.Combine(_asmLocation, "IndentJsonTest.json");
            string bonPath = Path.Combine(_asmLocation, "BonTest.bon");
            string bonJsonPath = Path.Combine(_asmLocation, "BonTest.json");
            if (!File.Exists(indentJsonTestPath)) new JsonTest().IndentJsonTest();

            try {
                using (BonWriter writer = new BonWriter(File.OpenWrite(bonPath), true)) {
                    using (JsonReader reader = new JsonReader(new StreamReader(File.OpenRead(srcPath)), true)) {
                        writer.Write(reader);
                    }
                }
                using (JsonWriter writer = new JsonWriter(new StreamWriter(bonJsonPath), true, true)) {
                    using (BonReader reader = new BonReader(File.OpenRead(bonPath), true)) {
                        writer.Write(reader);
                    }
                }
                FileAssert.AreEqual(indentJsonTestPath, bonJsonPath);
            } catch (Exception ex) {
                Assert.Fail(ex.ToString());
            }
        }
    }
}
