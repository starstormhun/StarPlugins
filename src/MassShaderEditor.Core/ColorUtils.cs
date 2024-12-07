using System;
using System.Linq;
using UnityEngine;

namespace MassShaderEditor.Koikatu {
    public static class ColorUtils {
        public static float[] ToArray(this Color c) {
            return new float[] { c.r, c.g, c.b, c.a };
        }

        public static Color ToColor(this string _text) {
            int r = ParseHexChar(_text[0]) * 16 + ParseHexChar(_text[1]);
            int g = ParseHexChar(_text[2]) * 16 + ParseHexChar(_text[3]);
            int b = ParseHexChar(_text[4]) * 16 + ParseHexChar(_text[5]);
            int a = ParseHexChar(_text[6]) * 16 + ParseHexChar(_text[7]);
            return new Color(r/255f, g/255f, b/255f, a/255f);
        }

        public static Color ToColor(this float[] a) {
            if (a.Length != 4) throw new ArgumentException("Array must have 4 members!");
            return new Color(a[0], a[1], a[2], a[3]);
        }

        public static Color AddClamp(this Color c1, Color c2, float min, float max) {
            var a1 = c1.ToArray();
            var a2 = c2.ToArray();
            return Enumerable.Range(0, a1.Length).Select(i => Mathf.Clamp(a1[i] + a2[i], min, max)).ToArray().ToColor();
        }

        public static Color SubClamp(this Color c1, Color c2, float min, float max) {
            var a1 = c1.ToArray();
            var a2 = c2.ToArray();
            return Enumerable.Range(0, a1.Length).Select(i => Mathf.Clamp(a1[i] - a2[i], min, max)).ToArray().ToColor();
        }

        public static string ToHex(this Color _color) {
            string r = ParseNum((int)(_color.r * 255) / 16) + ParseNum(((int)(_color.r * 255) % 16));
            string g = ParseNum((int)(_color.g * 255) / 16) + ParseNum(((int)(_color.g * 255) % 16));
            string b = ParseNum((int)(_color.b * 255) / 16) + ParseNum(((int)(_color.b * 255) % 16));
            string a = ParseNum((int)(_color.a * 255) / 16) + ParseNum(((int)(_color.a * 255) % 16));
            return r + g + b + a;
        }

        public static Color Clamp(this Color c) {
            float r = Mathf.Clamp(c.r, 0, 1);
            float g = Mathf.Clamp(c.g, 0, 1);
            float b = Mathf.Clamp(c.b, 0, 1);
            float a = Mathf.Clamp(c.a, 0, 1);
            return new Color(r, g, b, a);
        }

        public static bool Matches(this Color c1, Color c) {
            return (new Vector4(c1.r, c1.g, c1.b, c1.a) - new Vector4(c.r, c.g, c.b, c.a)).magnitude < 1.2f / 255f;
        }

        public static bool Matches(this float[] c1, float[] c) {
            if (!(c1.Length == 4 && c.Length == 4)) throw new ArgumentException("Both arrays must have 4 members!");
            return (new Vector4(c1[0], c1[1], c1[2], c1[3]) - new Vector4(c[0], c[1], c[2], c[3])).magnitude < 1E-05;
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