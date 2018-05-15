using System;
using System.IO;

using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;

/*
 * DOCS HERE => http://docs.neo.org/en-us/sc/test.html
 * NOTE - When pushing parameters into the script stack, don't forget that order is inverted
 */
namespace PhantasmaTests
{
    [TestFixture]
    public class ContractTests
    {
        private byte[] contractBytes;

        private string ContractFolder;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            var temp = TestContext.CurrentContext.TestDirectory.Split(new char[] { '\\', '/' }).ToList();

            for (int i=0; i<3; i++)
            {
                temp.RemoveAt(temp.Count - 1);
            }

            temp.Add("PhantasmaContract");
            temp.Add("bin");
            temp.Add("Debug");

            ContractFolder = String.Join("\\", temp.ToArray());

            // path is hardcoded for now...
            contractBytes = File.ReadAllBytes(ContractFolder  +  "PhantasmaContract.avm");
        }

        [Test]
        public void TestContract()
        {
            /*var engine = new ExecutionEngine(null, Crypto.Default);
            engine.LoadScript(contractBytes);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush("");
                sb.EmitPush("symbol");
                engine.LoadScript(sb.ToArray());
            }

            engine.Execute(); // start execution

            var result = engine.EvaluationStack.Peek().GetString(); 
            Assert.NotNull(result);

            Assert.IsTrue("PHI".Equals(result));*/
        }
    }
}
