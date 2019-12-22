﻿using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Brainf_ckSharp.UWP.Bindings.Functions
{
    /// <summary>
    /// A <see langword="class"/> with a collection of helper functions for bindings with a <see cref="Windows.UI.Xaml.Controls.Pivot"/> control
    /// </summary>
    public static class PivotSelectionFunctions
    {
        /// <summary>
        /// Checks whether the input index matches a target value
        /// </summary>
        /// <param name="index">The input index to match</param>
        /// <param name="target">The target value to match</param>
        /// <returns><see langword="true"/> if the input values match, <see langword="false"/> otherwise</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndexToBool(int index, int target) => index == target;
    }
}