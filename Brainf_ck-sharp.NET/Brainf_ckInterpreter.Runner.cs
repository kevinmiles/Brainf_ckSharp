﻿using System;
using System.Threading;
using Brainf_ck_sharp.NET.Buffers;
using Brainf_ck_sharp.NET.Buffers.IO;
using Brainf_ck_sharp.NET.Constants;
using Brainf_ck_sharp.NET.Enums;
using Brainf_ck_sharp.NET.Extensions.Types;
using Brainf_ck_sharp.NET.Helpers;
using Brainf_ck_sharp.NET.Models;
using Brainf_ck_sharp.NET.Models.Base;
using Brainf_ck_sharp.NET.Models.Internal;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Brainf_ck_sharp.NET
{
    /// <summary>
    /// A <see langword="class"/> responsible for interpreting and debugging Brainf*ck/PBrain scripts
    /// </summary>
    public static partial class Brainf_ckInterpreter
    {
        /// <summary>
        /// Runs a given Brainf*ck/PBrain executable with the given parameters
        /// </summary>
        /// <param name="operators">The executable to run</param>
        /// <param name="stdin">The input buffer to read data from</param>
        /// <param name="machineState">The target machine state to use to run the script</param>
        /// <param name="executionToken">A <see cref="CancellationToken"/> that can be used to halt the execution</param>
        /// <returns>An <see cref="InterpreterResult"/> instance with the results of the execution</returns>
        private static InterpreterResult RunCore(
            UnsafeMemoryBuffer<byte> operators,
            string stdin,
            TuringMachineState machineState,
            CancellationToken executionToken)
        {
            DebugGuard.MustBeGreaterThanOrEqualTo(operators.Size, 0, nameof(operators));
            DebugGuard.MustBeGreaterThanOrEqualTo(machineState.Size, 0, nameof(machineState));

            // Initialize the temporary buffers
            using UnsafeMemoryBuffer<bool> breakpoints = UnsafeMemoryBuffer<bool>.Allocate(operators.Size, true);
            using UnsafeMemoryBuffer<int> jumpTable = LoadJumpTable(operators, out int functionsCount);
            using UnsafeMemoryBuffer<Range> functions = UnsafeMemoryBuffer<Range>.Allocate(ushort.MaxValue, true);
            using UnsafeMemoryBuffer<ushort> definitions = LoadDefinitionsTable(functionsCount);
            using UnsafeMemoryBuffer<StackFrame> stackFrames = UnsafeMemoryBuffer<StackFrame>.Allocate(Specs.MaximumStackSize, false);
            using StdoutBuffer stdout = new StdoutBuffer();

            // Shared counters
            int depth = 0;
            int totalOperations = 0;
            int totalFunctions = 0;

            // Manually set the initial stack frame to the entire script
            stackFrames[0] = new StackFrame(new Range(0, operators.Size), 0);

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Start the interpreter
            ExitCode exitCode = Run(
                operators.Memory,
                breakpoints.Memory,
                jumpTable.Memory,
                functions.Memory,
                definitions.Memory,
                stackFrames.Memory,
                ref depth,
                ref totalOperations,
                ref totalFunctions,
                machineState,
                new StdinBuffer(stdin),
                stdout,
                executionToken,
                CancellationToken.None);

            stopwatch.Stop();

            // Rebuild the compacted source code
            string sourceCode = Brainf_ckParser.ExtractSource(operators.Memory);

            // Prepare the debug info
            HaltedExecutionInfo? debugInfo = LoadDebugInfo(
                operators.Memory,
                stackFrames.Memory,
                depth);

            // Build the collection of defined functions
            FunctionDefinition[] functionDefinitions = LoadFunctionDefinitions(
                operators.Memory,
                functions.Memory,
                definitions.Memory,
                totalFunctions);

            return new InterpreterResult(
                sourceCode,
                exitCode,
                debugInfo,
                machineState,
                functionDefinitions,
                stdin,
                stdout.ToString(),
                stopwatch.Elapsed,
                totalOperations);
        }

        /// <summary>
        /// Creates a new Brainf*ck/PBrain session with the given parameters
        /// </summary>
        /// <param name="source">The source code to parse and execute</param>
        /// <param name="breakpoints">The sequence of indices for the breakpoints to apply to the script</param>
        /// <param name="stdin">The input buffer to read data from</param>
        /// <param name="memorySize">The size of the state machine to create to run the script</param>
        /// <param name="overflowMode">The overflow mode to use in the state machine used to run the script</param>
        /// <param name="executionToken">A <see cref="CancellationToken"/> that can be used to halt the execution</param>
        /// <param name="debugToken">A <see cref="CancellationToken"/> that is used to ignore/respect existing breakpoints</param>
        /// <returns>An <see cref="Option{T}"/> of <see cref="InterpreterSession"/> instance with the results of the execution</returns>
        private static Option<InterpreterSession> TryCreateSessionCore(
            string source,
            ReadOnlySpan<int> breakpoints,
            string stdin,
            int memorySize,
            OverflowMode overflowMode,
            CancellationToken executionToken,
            CancellationToken debugToken)
        {
            Guard.MustBeGreaterThanOrEqualTo(memorySize, 32, nameof(memorySize));
            Guard.MustBeLessThanOrEqualTo(memorySize, 1024, nameof(memorySize));

            UnsafeMemoryBuffer<byte> operators = Brainf_ckParser.TryParse(source, out SyntaxValidationResult validationResult)!;

            if (!validationResult.IsSuccess) return Option<InterpreterSession>.From(validationResult);

            // Initialize the temporary buffers
            UnsafeMemoryBuffer<bool> breakpointsTable = LoadBreakpointsTable(source, validationResult.OperatorsCount, breakpoints);
            UnsafeMemoryBuffer<int> jumpTable = LoadJumpTable(operators, out int functionsCount);
            UnsafeMemoryBuffer<Range> functions = UnsafeMemoryBuffer<Range>.Allocate(ushort.MaxValue, true);
            UnsafeMemoryBuffer<ushort> definitions = LoadDefinitionsTable(functionsCount);
            UnsafeMemoryBuffer<StackFrame> stackFrames = UnsafeMemoryBuffer<StackFrame>.Allocate(Specs.MaximumStackSize, false);

            // Initialize the root stack frame
            stackFrames[0] = new StackFrame(new Range(0, operators.Size), 0);

            // Create the interpreter session
            InterpreterSession session = new InterpreterSession(
                operators,
                breakpointsTable,
                jumpTable,
                functions,
                definitions,
                stackFrames,
                stdin,
                memorySize,
                overflowMode,
                executionToken,
                debugToken);

            return Option<InterpreterSession>.From(validationResult, session);
        }

        /// <summary>
        /// Tries to run a given input Brainf*ck/PBrain executable
        /// </summary>
        /// <param name="operators">The sequence of parsed operators to execute</param>
        /// <param name="breakpoints">The table of breakpoints for the current executable</param>
        /// <param name="jumpTable">The jump table for loops and function declarations</param>
        /// <param name="functions">The mapping of functions for the current execution</param>
        /// <param name="definitions">The lookup table to check which functions are defined</param>
        /// <param name="stackFrames">The sequence of stack frames for the current execution</param>
        /// <param name="depth">The current stack depth</param>
        /// <param name="totalOperations">The total number of executed operators</param>
        /// <param name="totalFunctions">The total number of defined functions</param>
        /// <param name="state">The target <see cref="TuringMachineState"/> instance to execute the code on</param>
        /// <param name="stdin">The input buffer to read characters from</param>
        /// <param name="stdout">The output buffer to write characters to</param>
        /// <param name="executionToken">A <see cref="CancellationToken"/> that can be used to halt the execution</param>
        /// <param name="debugToken">A <see cref="CancellationToken"/> that is used to ignore/respect existing breakpoints</param>
        /// <returns>The resulting <see cref="ExitCode"/> value for the current execution of the input script</returns>
        internal static ExitCode Run(
            UnsafeMemory<byte> operators,
            UnsafeMemory<bool> breakpoints,
            UnsafeMemory<int> jumpTable,
            UnsafeMemory<Range> functions,
            UnsafeMemory<ushort> definitions,
            UnsafeMemory<StackFrame> stackFrames,
            ref int depth,
            ref int totalOperations,
            ref int totalFunctions,
            TuringMachineState state,
            StdinBuffer stdin,
            StdoutBuffer stdout,
            CancellationToken executionToken,
            CancellationToken debugToken)
        {
            DebugGuard.MustBeTrue(operators.Size > 0, nameof(operators));
            DebugGuard.MustBeEqualTo(breakpoints.Size, operators.Size, nameof(breakpoints));
            DebugGuard.MustBeEqualTo(jumpTable.Size, operators.Size, nameof(jumpTable));
            DebugGuard.MustBeEqualTo(functions.Size, ushort.MaxValue, nameof(functions));
            DebugGuard.MustBeGreaterThanOrEqualTo(definitions.Size, 0, nameof(definitions));
            DebugGuard.MustBeLessThanOrEqualTo(definitions.Size, operators.Size / 3, nameof(definitions));
            DebugGuard.MustBeEqualTo(stackFrames.Size, Specs.MaximumStackSize, nameof(stackFrames));
            DebugGuard.MustBeGreaterThanOrEqualTo(depth, 0, nameof(depth));
            DebugGuard.MustBeGreaterThanOrEqualTo(totalOperations, 0, nameof(totalOperations));
            DebugGuard.MustBeGreaterThanOrEqualTo(totalFunctions, 0, nameof(totalFunctions));

            // Outer loop to go through the existing stack frames
            StackFrame frame;
            int i;
            do
            {
                frame = stackFrames[depth];

                /* This label is used when a function call is performed: a new stack frame
                 * is pushed in the frames collection and then a goto is used to jump out
                 * of both the switch case and the inner loop. This is faster than using
                 * another variable to manually handle the two consecutive breaks to
                 * reach the start of the inner loop from a switch case. */
                StackFrameLoop:

                // Iterate over the current operators
                for (i = frame.Offset; i < frame.Range.End; i++)
                {
                    // Check if a breakpoint has been reached
                    if (breakpoints[i] && !debugToken.IsCancellationRequested)
                    {
                        /* Disable the current breakpoint so that it won't be
                         * triggered again when the execution resumes from this point */
                        breakpoints[i] = false;
                        goto BreakpointReached;
                    }

                    // Execute the current operator
                    switch (operators[i])
                    {
                        // ptr++
                        case Operators.ForwardPtr:
                            if (state.TryMoveNext()) totalOperations++;
                            else goto UpperBoundExceeded;
                            break;

                        // ptr--
                        case Operators.BackwardPtr:
                            if (state.TryMoveBack()) totalOperations++;
                            else goto LowerBoundExceeded;
                            break;

                        // (*ptr)++
                        case Operators.Plus:
                            if (state.TryIncrement()) totalOperations++;
                            else goto MaxValueExceeded;
                            break;

                        // (*ptr)--
                        case Operators.Minus:
                            if (state.TryDecrement()) totalOperations++;
                            else goto NegativeValue;
                            break;

                        // putch(*ptr)
                        case Operators.PrintChar:
                            if (stdout.TryWrite((char)state.Current)) totalOperations++;
                            else goto StdoutBufferLimitExceeded;
                            break;

                        // *ptr = getch()
                        case Operators.ReadChar:
                            if (stdin.TryRead(out char c))
                            {
                                // Check if the input character can be stored in the current cell
                                if (state.TryInput(c)) totalOperations++;
                                else goto MaxValueExceeded;
                            }
                            else goto StdinBufferExhausted;
                            break;

                        // while (*ptr) {
                        case Operators.LoopStart:

                            // Check whether the loop is active
                            if (state.Current == 0)
                            {
                                i = jumpTable[i];
                                totalOperations++;
                            }
                            else if (jumpTable[i] == i + 2 &&
                                     operators[i + 1] == Operators.Minus &&
                                     (!breakpoints[i + 1] &&
                                      !breakpoints[i + 2] ||
                                      debugToken.IsCancellationRequested))
                            {
                                // Fast path for [-] loops
                                state.ResetCell();
                                totalOperations += state.Current * 2 + 1;
                                i += 2;
                            }
                            else if (executionToken.IsCancellationRequested)
                            {
                                // Check whether the code can still be executed before starting an active loop
                                goto ThresholdExceeded;
                            }
                            break;

                        // {
                        case Operators.LoopEnd:
                            if (state.Current > 0) i = jumpTable[i] - 1;
                            totalOperations++;
                            break;

                        // f[*ptr] = []() {
                        case Operators.FunctionStart:
                            {
                                // Check for duplicate function definitions
                                if (functions[state.Current].Length != 0) goto DuplicateFunctionDefinition;

                                // Save the new function definition
                                Range function = new Range(i + 1, jumpTable[i]);
                                functions[state.Current] = function;
                                definitions[totalFunctions++] = state.Current;
                                totalOperations++;
                                i += function.Length;
                                break;
                            }

                        // f[*ptr]()
                        case Operators.FunctionCall:
                            {
                                // Try to retrieve the function to invoke
                                Range function = functions[state.Current];
                                if (function.Length == 0) goto UndefinedFunctionCalled;

                                // Ensure the stack has space for the new function invocation
                                if (depth == Specs.MaximumStackSize - 1) goto StackLimitExceeded;

                                // Update the current stack frame and exit the inner loop
                                stackFrames[depth++] = frame.WithOffset(i + 1);
                                frame = new StackFrame(function);
                                totalOperations++;
                                goto StackFrameLoop;
                            }
                    }
                }
            } while (--depth >= 0);

            return ExitCode.Success;

            /* Exit paths for all failures or partial executions in the interpreter.
             * Whenever an executable completes its execution and the current stack
             * frame needs to be updated with the current position, it is done from
             * one of these labels: each of them sets the right exit flag and then
             * jumps to the exit label, which updates the current stack frame and
             * returns. Having all these exit paths here makes the code more compact
             * into the inner loop, and the two jumps don't produce overhead since
             * one of them would only be triggered when the inner loop has terminated. */
            BreakpointReached:
            ExitCode exitCode = ExitCode.BreakpointReached;
            goto UpdateStackFrameAndExit;

            UpperBoundExceeded:
            exitCode = ExitCode.UpperBoundExceeded;
            goto UpdateStackFrameAndExit;

            LowerBoundExceeded:
            exitCode = ExitCode.LowerBoundExceeded;
            goto UpdateStackFrameAndExit;

            MaxValueExceeded:
            exitCode = ExitCode.MaxValueExceeded;
            goto UpdateStackFrameAndExit;

            NegativeValue:
            exitCode = ExitCode.NegativeValue;
            goto UpdateStackFrameAndExit;

            StdoutBufferLimitExceeded:
            exitCode = ExitCode.StdoutBufferLimitExceeded;
            goto UpdateStackFrameAndExit;

            StdinBufferExhausted:
            exitCode = ExitCode.StdinBufferExhausted;
            goto UpdateStackFrameAndExit;

            ThresholdExceeded:
            exitCode = ExitCode.ThresholdExceeded;
            goto UpdateStackFrameAndExit;

            DuplicateFunctionDefinition:
            exitCode = ExitCode.DuplicateFunctionDefinition;
            goto UpdateStackFrameAndExit;

            UndefinedFunctionCalled:
            exitCode = ExitCode.UndefinedFunctionCalled;
            goto UpdateStackFrameAndExit;

            StackLimitExceeded:
            exitCode = ExitCode.StackLimitExceeded;

            UpdateStackFrameAndExit:
            stackFrames[depth] = frame.WithOffset(i);
            return exitCode;
        }
    }
}
