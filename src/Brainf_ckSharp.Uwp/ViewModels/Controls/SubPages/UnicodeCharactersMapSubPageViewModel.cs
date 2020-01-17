﻿using System.Collections.Generic;
using Brainf_ckSharp.Uwp.Models;

namespace Brainf_ckSharp.Uwp.ViewModels.Controls.SubPages
{
    public sealed class UnicodeCharactersMapSubPageViewModel
    {
        /// <summary>
        /// The collection of characters in the [32, 127] range
        /// </summary>
        private static readonly IReadOnlyList<UnicodeCharacter> _32To127;

        /// <summary>
        /// The collection of characters in the [160, 255] range
        /// </summary>
        private static readonly IReadOnlyList<UnicodeCharacter> _160To255;

        /// <summary>
        /// Initializes <see cref="_32To127"/> and <see cref="_160To255"/> for future use
        /// </summary>
        static UnicodeCharactersMapSubPageViewModel()
        {
            UnicodeCharacter[] first = new UnicodeCharacter[128 - 32];

            for (int i = 0; i < first.Length; i++)
            {
                first[i] = new UnicodeCharacter((char)(i + 32));
            }

            _32To127 = first;

            UnicodeCharacter[] second = new UnicodeCharacter[256 - 160];

            for (int i = 0; i < second.Length; i++)
            {
                second[i] = new UnicodeCharacter((char)(i + 160));
            }

            _160To255 = second;
        }

        /// <summary>
        /// Gets the collection of characters in the [32, 127] range
        /// </summary>
        public IReadOnlyList<UnicodeCharacter> FirstCollection => _32To127;

        /// <summary>
        /// Gets the collection of characters in the [128, 159] range
        /// </summary>
        public IReadOnlyList<UnicodeCharacter> SecondCollection => _160To255;
    }
}