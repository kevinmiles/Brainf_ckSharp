﻿using Brainf_ck_sharp.NET.Enums;
using Brainf_ck_sharp.NET.Models;
using Brainf_ck_sharp.NET.Models.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brainf_ck_sharp.NET.Unit
{
    [TestClass]
    public class FunctionTest
    {
        [TestMethod]
        public void SingleCall()
        {
            const string script = "+(,[>+<-]>.)>+:";

            Option<InterpreterResult> result = Brainf_ckInterpreter.TryRun(script, "a");

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(result.Value!.ExitCode, ExitCode.Success);
            Assert.AreEqual(result.Value.MachineState.Current.Character, 'a');
            Assert.AreEqual(result.Value.Stdout, "a");
            Assert.AreEqual(result.Value.Functions.Count, 1);
            Assert.AreEqual(result.Value.Functions[0].Index, 0);
            Assert.AreEqual(result.Value.Functions[0].Offset, 1);
            Assert.AreEqual(result.Value.Functions[0].Value, 1);
            Assert.AreEqual(result.Value.Functions[0].Body, ",[>+<-]>.");
        }

        [TestMethod]
        public void MultipleCalls()
        {
            const string script = "(+++):>:";

            Option<InterpreterResult> result = Brainf_ckInterpreter.TryRun(script);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(result.Value!.ExitCode, ExitCode.Success);
            Assert.AreEqual(result.Value.MachineState[0].Value, 3);
            Assert.AreEqual(result.Value.MachineState[1].Value, 3);
            Assert.AreEqual(result.Value.Functions.Count, 1);
            Assert.AreEqual(result.Value.Functions[0].Index, 0);
            Assert.AreEqual(result.Value.Functions[0].Offset, 0);
            Assert.AreEqual(result.Value.Functions[0].Value, 0);
            Assert.AreEqual(result.Value.Functions[0].Body, "+++");
        }

        [TestMethod]
        public void Recursion()
        {
            const string script = ">,<(>[>+<-<:]):>[<<+>>-]<<.[-]";

            Option<InterpreterResult> result = Brainf_ckInterpreter.TryRun(script, "%");

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(result.Value!.ExitCode, ExitCode.Success);
            Assert.AreEqual(result.Value.MachineState.Current.Value, 0);
            Assert.AreEqual(result.Value.Stdout, "%");
        }
    }
}
