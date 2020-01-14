﻿using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;

#nullable enable

namespace Brainf_ckSharp.Uwp.Models.Ide
{
    /// <summary>
    /// A model that represents a single source code from the user's library
    /// </summary>
    public sealed class CodeLibraryEntry
    {
        /// <summary>
        /// The desired length of each loaded code preview
        /// </summary>
        private const int CodePreviewLength = 120;

        /// <summary>
        /// Creates a new <see cref="CodeLibraryEntry"/> instance with the specified parameters
        /// </summary>
        /// <param name="file">The underlying <see cref="StorageFile"/> instance for the new entry</param>
        /// <param name="metadata">The metadata for the current file</param>
        /// <param name="title">The title of the new entry</param>
        /// <param name="preview">The preview code for the new entry</param>
        private CodeLibraryEntry(StorageFile file, CodeMetadata metadata, string title, string preview)
        {
            File = file;
            Metadata = metadata;
            Title = title;
            Preview = preview;
        }

        /// <summary>
        /// Gets the underlying <see cref="StorageFile"/> instance for the current entry
        /// </summary>
        public StorageFile File { get; }

        /// <summary>
        /// Gets the associated <see cref="CodeMetadata"/> instance for the current entry
        /// </summary>
        public CodeMetadata Metadata { get; }

        /// <summary>
        /// Gets the title of the current entry
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The code preview for the current entry
        /// </summary>
        public string Preview { get; }

        /// <summary>
        /// Tries to load a new <see cref="CodeLibraryEntry"/> instance for a specified file
        /// </summary>
        /// <param name="file">The input file to read data from</param>
        /// <param name="metadata">The metadata for the current file</param>
        /// <returns>A new <see cref="CodeLibraryEntry"/> instance for <paramref name="file"/></returns>
        [Pure]
        public static async Task<CodeLibraryEntry?> TryLoadFromFileAsync(StorageFile file, CodeMetadata metadata)
        {
            try
            {
                string preview = await LoadCodePreviewAsync(file, CodePreviewLength);

                return new CodeLibraryEntry(file, metadata, file.DisplayName, preview);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tries to load a new <see cref="CodeLibraryEntry"/> instance for a specified file
        /// </summary>
        /// <param name="file">The input file to read data from</param>
        /// <param name="title">The name to use for the new <see cref="CodeLibraryEntry"/> instance</param>
        /// <returns>A new <see cref="CodeLibraryEntry"/> instance for <paramref name="file"/></returns>
        [Pure]
        public static async Task<CodeLibraryEntry?> TryLoadFromFileAsync(StorageFile file, string title)
        {
            try
            {
                string preview = await LoadCodePreviewAsync(file, CodePreviewLength);

                return new CodeLibraryEntry(file, CodeMetadata.Default, title, preview);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads a code preview with a maximum specified length from a given file
        /// </summary>
        /// <param name="file">The input <see cref="StorageFile"/> to read from</param>
        /// <param name="length">The maximum length of the preview to load</param>
        /// <returns>A <see cref="string"/> with a preview of the Brainf*ck/PBrain source code in <paramref name="file"/></returns>
        [Pure]
        private static async Task<string> LoadCodePreviewAsync(StorageFile file, int length)
        {
            Guard.MustBeGreaterThan(length, 0, nameof(length));

            // Open the input file and a streama reader to decode the text
            using Stream stream = await file.OpenStreamForReadAsync();
            using StreamReader reader = new StreamReader(stream);

            // Allocate a temporary buffer for the resulting characters and one for the blocks to read
            char[] charBuffer = ArrayPool<char>.Shared.Rent(length);
            char[] tempBuffer = ArrayPool<char>.Shared.Rent(length);

            int previewLength = 0;

            try
            {
                while (previewLength < length)
                {
                    // Read a new block of characters from the input stream
                    int maxCharactersToRead = length - previewLength;
                    int read = await reader.ReadAsync(tempBuffer, 0, maxCharactersToRead);

                    if (read == 0) break;

                    // Accumulate the operators in the current block
                    previewLength += ExtractOperators(
                        new ReadOnlySpan<char>(tempBuffer, 0, read),
                        charBuffer.AsSpan(previewLength));
                }

                // Create a string with the parsed operators up to this point
                return new string(charBuffer, 0, previewLength);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charBuffer);
                ArrayPool<char>.Shared.Return(tempBuffer);
            }
        }

        /// <summary>
        /// Reads all the valid Brainf*ck/PBrain operators from a source <see cref="ReadOnlySpan{T}"/> and writes them into a target <see cref="Span{T}"/>
        /// </summary>
        /// <param name="source">The source <see cref="ReadOnlySpan{T}"/> with the characters to read</param>
        /// <param name="destination">The target <see cref="Span{T}"/> to write the parsed operators to</param>
        /// <returns>The number of Brainf*ck/PBrain operators that have been parsed and written to <paramref name="destination"/></returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ExtractOperators(ReadOnlySpan<char> source, Span<char> destination)
        {
            DebugGuard.MustBeGreaterThan(source.Length, 0, nameof(source));
            DebugGuard.MustBeGreaterThan(destination.Length, 0, nameof(destination));

            ref char sourceRef = ref MemoryMarshal.GetReference(source);
            ref char destinationRef = ref MemoryMarshal.GetReference(destination);
            int sourceLength = source.Length;
            int destinationLength = destination.Length;
            int j = 0;

            for (int i = 0; i < sourceLength; i++)
            {
                char c = Unsafe.Add(ref sourceRef, i);

                // Ignore characters that are not valid operators (comments)
                if (!Brainf_ckParser.IsOperator(c)) continue;

                Unsafe.Add(ref destinationRef, j++) = c;

                // Stop if the destination span is full
                if (j == destinationLength) return destinationLength;
            }

            return j;
        }
    }
}
