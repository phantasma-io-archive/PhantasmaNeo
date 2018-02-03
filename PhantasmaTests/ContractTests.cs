using System;
using System.IO;

using NUnit.Framework;
using Neo;
using Neo.VM;
using Neo.Cryptography;
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

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            // path is hardcoded for now...
            contractBytes = File.ReadAllBytes(@"D:\Code\Phantasma\PhantasmaContract\bin\Debug\PhantasmaContract.avm");
        }

        [Test]
        public void TestContract()
        {
            var engine = new ExecutionEngine(null, Crypto.Default);
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

            Assert.IsTrue("PHI".Equals(result));
        }
    }
}
