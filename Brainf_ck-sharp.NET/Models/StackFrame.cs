﻿using Brainf_ck_sharp.NET.Extensions.Types;

namespace Brainf_ck_sharp.NET.Models
{
    /// <summary>
    /// A <see langword="struct"/> that represents a stack frame for the interpreter
    /// </summary>
    internal readonly struct StackFrame
    {
        /// <summary>
        /// The <see cref="Range"/> instance that indicates the operators to execute in the current stack frame
        /// </summary>
        public readonly Range Range;

        /// <summary>
        /// The operator offset for the current stack frame
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// Creates a new <see cref="StackFrame"/> instance with the specified parameters
        /// </summary>
        /// <param name="range">The range of operators to execute</param>
        /// <param name="offset">The current offset during execution</param>
        public StackFrame(Range range, int offset)
        {
            Range = range;
            Offset = offset;
        }
    }
}