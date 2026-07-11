using System;
using System.Globalization;

namespace Wangcaisoft.DotNet.BaanWindowsLib
{
    /// <summary>
    /// Decodes a 4GL DLL return value that follows the <c>"&lt;code&gt;|&lt;message&gt;"</c>
    /// convention (for example <c>"0|Success"</c> or <c>"1|Error"</c>) into a structured result.
    /// </summary>
    /// <remarks>
    /// The library itself only ever returns the raw OLE <c>ReturnValue</c> (see
    /// <see cref="Baan.ReturnValue"/>). Use <see cref="Parse"/> to turn that raw value into a
    /// <see cref="BaanResult"/> so callers do not have to split the string by hand. By convention
    /// <see cref="Code"/> equal to <c>0</c> means success.
    /// </remarks>
    public readonly struct BaanResult
    {
        /// <summary>Gets the status code (0 = success, any other value = error, by convention).</summary>
        public int Code { get; }

        /// <summary>Gets the message part (for example "Success" or "Error").</summary>
        public string Message { get; }

        /// <summary>Gets the original, unparsed return value.</summary>
        public string Raw { get; }

        /// <summary>Gets a value indicating whether the call succeeded (i.e. <see cref="Code"/> == 0).</summary>
        public bool IsSuccess => Code == 0;

        /// <summary>Initializes a new <see cref="BaanResult"/>.</summary>
        public BaanResult(int code, string message, string raw)
        {
            Code = code;
            Message = message ?? string.Empty;
            Raw = raw;
        }

        /// <summary>
        /// Parses a raw OLE return value into a <see cref="BaanResult"/>.
        /// </summary>
        /// <param name="returnValue">The value returned by a call (typically a <see cref="string"/>).</param>
        /// <param name="delimiter">The separator between code and message. Defaults to <c>'|'</c>.</param>
        /// <returns>
        /// A <see cref="BaanResult"/>. Rules:
        /// <list type="bullet">
        ///   <item><description><c>"0|Success"</c> → <c>Code = 0, Message = "Success"</c>.</description></item>
        ///   <item><description><c>"1|Error"</c> → <c>Code = 1, Message = "Error"</c>.</description></item>
        ///   <item><description>A bare number (e.g. <c>"1"</c>) → that number as <see cref="Code"/>, empty message.</description></item>
        ///   <item><description>A string without the delimiter → <see cref="Code"/> = 0 and the whole string as <see cref="Message"/>.</description></item>
        ///   <item><description><c>null</c> or empty → <see cref="Code"/> = 0, empty message.</description></item>
        /// </list>
        /// </returns>
        public static BaanResult Parse(object returnValue, char delimiter = '|')
        {
            var raw = returnValue?.ToString();

            if (string.IsNullOrEmpty(raw))
            {
                return new BaanResult(0, string.Empty, raw);
            }

            var idx = raw.IndexOf(delimiter);
            if (idx < 0)
            {
                // No delimiter: a bare integer is the code; otherwise the whole string is the message.
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
                {
                    return new BaanResult(code, string.Empty, raw);
                }

                return new BaanResult(0, raw, raw);
            }

            var codePart = raw.Substring(0, idx);
            var message = raw.Substring(idx + 1);

            if (int.TryParse(codePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCode))
            {
                return new BaanResult(parsedCode, message, raw);
            }

            // Code part is not numeric: treat the whole value as a (success) message.
            return new BaanResult(0, raw, raw);
        }

        /// <inheritdoc />
        public override string ToString() => Raw ?? Code.ToString(CultureInfo.InvariantCulture);
    }
}
