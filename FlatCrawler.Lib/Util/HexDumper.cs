using System;
using System.Text;

// Content here is licensed CC BY-SA 3.0 instead of the repository's license.

namespace FlatCrawler.Lib
{
    /// <summary>
    /// Modified from https://codereview.stackexchange.com/q/145506
    /// </summary>
    public sealed class HexDumper
    {
        private readonly byte[] _bytes;
        private readonly int _bytesPerLine;
        private readonly bool _showHeader;
        private readonly bool _showOffset;
        private readonly bool _showAscii;

        private readonly int _length;

        private readonly int _start;
        private int _index;
        private readonly StringBuilder _sb = new();

        private HexDumper(byte[] bytes, int offset, int length, int bytesPerLine, bool showHeader, bool showOffset, bool showAscii)
        {
            _bytes = bytes;
            _index = offset;
            _start = _index;
            _bytesPerLine = bytesPerLine;
            _showHeader = showHeader;
            _showOffset = showOffset;
            _showAscii = showAscii;
            _length = (int)Math.Min((uint)length, bytes.Length - _index);
        }

        public static string Dump(byte[] bytes, int offset, int length = 0x80, int bytesPerLine = 16, bool showHeader = true, bool showOffset = true, bool showAscii = true)
        {
            return (new HexDumper(bytes, offset, length, bytesPerLine, showHeader, showOffset, showAscii)).Dump();
        }

        private string Dump()
        {
            if (_showHeader)
                WriteHeader();
            WriteBody();
            return _sb.ToString();
        }

        private void WriteHeader()
        {
            const string ofs = "Offset(h)| ";

            for (int i = 0; i < (_bytesPerLine * 3) + ofs.Length - 1; i++)
                _sb.Append('-');
            _sb.AppendLine();

            if (_showOffset)
            {
                _sb.Append(ofs);
            }
            for (int i = 0; i < _bytesPerLine; i++)
            {
                _sb.AppendFormat("{0:X2}", i & 0xFF);
                if (i + 1 < _bytesPerLine)
                {
                    _sb.Append(' ');
                }
            }
            _sb.AppendLine();

            for (int i = 0; i < (_bytesPerLine * 3) + ofs.Length - 1; i++)
                _sb.Append('-');
            _sb.AppendLine();
        }

        private void WriteBody()
        {
            int ctr;
            while ((ctr = _index - _start) < _length)
            {
                if (ctr % _bytesPerLine == 0)
                {
                    if (ctr > 0)
                    {
                        if (_showAscii)
                        {
                            WriteAscii();
                        }
                        _sb.AppendLine();
                    }

                    if (_showOffset)
                    {
                        WriteOffset();
                    }
                }

                WriteByte();
                if ((ctr = _index - _start) % _bytesPerLine != 0 && ctr < _length)
                {
                    _sb.Append(' ');
                }
            }

            if (_showAscii)
            {
                WriteAscii();
            }
        }

        private void WriteOffset()
        {
            _sb.AppendFormat("{0:X8}", _index).Append(" | ");
        }

        private void WriteByte()
        {
            _sb.AppendFormat("{0:X2}", _bytes[_index]);
            _index++;
        }

        private void WriteAscii()
        {
            var ctr = _index - _start;
            int backtrack = ((ctr - 1) / _bytesPerLine) * _bytesPerLine;
            int length = ctr - backtrack;

            // This is to fill up last string of the dump if it's shorter than _bytesPerLine
            _sb.Append(new string(' ', (_bytesPerLine - length) * 3));

            _sb.Append("   ");
            for (int i = 0; i < length; i++)
            {
                _sb.Append(Translate(_bytes[backtrack + i]));
            }
        }

        private static string Translate(byte b)
        {
            return b < 32 ? "." : ((char)b).ToString();
        }
    }
}
