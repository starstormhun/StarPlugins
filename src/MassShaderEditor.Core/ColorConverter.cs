using System;
using UnityEngine;

namespace MassShaderEditor.Koikatu {
	public static class ColorConverter {
		public static Color ConvertFromString(string _text) {
			int r = ParseHexChar(_text[0]) * 16 + ParseHexChar(_text[1]);
			int g = ParseHexChar(_text[2]) * 16 + ParseHexChar(_text[3]);
			int b = ParseHexChar(_text[4]) * 16 + ParseHexChar(_text[5]);
			int a = ParseHexChar(_text[6]) * 16 + ParseHexChar(_text[7]);
			return new Color(r/255f, g/255f, b/255f, a/255f);
		}

		public static string ConvertToString(Color _color) {
			string r = ParseNum((int)(_color.r * 255) / 16) + ParseNum(((int)(_color.r * 255) % 16));
			string g = ParseNum((int)(_color.g * 255) / 16) + ParseNum(((int)(_color.g * 255) % 16));
			string b = ParseNum((int)(_color.b * 255) / 16) + ParseNum(((int)(_color.b * 255) % 16));
			string a = ParseNum((int)(_color.a * 255) / 16) + ParseNum(((int)(_color.a * 255) % 16));
			return r + g + b + a;
		}

		private static int ParseHexChar(char c) {
			if (c >= '0' && c <= '9') {
				return (int)(c - '0');
			}
			if (c >= 'a' && c <= 'f') {
				return (int)(c - 'a' + '\n');
			}
			if (c >= 'A' && c <= 'F') {
				return (int)(c - 'A' + '\n');
			}
			throw new FormatException("Bad input!");
		}

		private static string ParseNum(int _num) {
			if (_num >= 0 && _num <= 9) {
				return ((char)('0' + _num)).ToString();
            }
			if (_num >= 10 && _num <= 15) {
				return ((char)('a' + _num - 10)).ToString();
            }
			throw new FormatException("Bad input!");
		}
	}
}