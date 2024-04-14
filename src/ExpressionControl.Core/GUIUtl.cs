using System;
using UnityEngine;
namespace ExpressionControl
{
	internal class GUIUtl
	{
		static GUIUtl()
		{
			Color color = new Color(0f, 0f, 0f, 1f);
			Color32[] array = new Color32[16];
			for (int i = 0; i < 16; i++)
			{
				array[i] = color;
			}
			GUIUtl.texBlack.SetPixels32(array);
			GUIUtl.texBlack.Apply();
			GUIUtl.texGray = new Texture2D(4, 4);
			color = new Color(0.8f, 0.8f, 0.8f, 1f);
			array = new Color32[16];
			for (int j = 0; j < 16; j++)
			{
				array[j] = color;
			}
			GUIUtl.texGray.SetPixels32(array);
			GUIUtl.texGray.Apply();
#if KKS
			GUIUtl.texGuiWindowOn = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiWindowOn, Convert.FromBase64String(GUIUtl.base64texWindowOn));
			GUIUtl.texGuiWindowOff = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiWindowOff, Convert.FromBase64String(GUIUtl.base64texWindowOff));
			GUIUtl.texGuiWindowFoldOn = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiWindowFoldOn, Convert.FromBase64String(GUIUtl.base64texWindowFoldOn));
			GUIUtl.texGuiWindowFoldOff = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiWindowFoldOff, Convert.FromBase64String(GUIUtl.base64texWindowFoldOff));
			GUIUtl.texGuiBackgroundOn = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiBackgroundOn, Convert.FromBase64String(GUIUtl.base64texBackgroundOn));
			GUIUtl.texGuiBackgroundOff = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiBackgroundOff, Convert.FromBase64String(GUIUtl.base64texBackgroundOff));
			GUIUtl.texGuiButtonHover = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiButtonHover, Convert.FromBase64String(GUIUtl.base64texButtonHover));
			GUIUtl.texGuiButtonNormal = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiButtonNormal, Convert.FromBase64String(GUIUtl.base64texButtonNormal));
			GUIUtl.texGuiButtonActive = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiButtonActive, Convert.FromBase64String(GUIUtl.base64texButtonActive));
			GUIUtl.texGuiPanelNormal = new Texture2D(8, 8);
			ImageConversion.LoadImage(GUIUtl.texGuiPanelNormal, Convert.FromBase64String(GUIUtl.base64texPanelNormal));
			GUIUtl.texGuiPanelBorderRed = new Texture2D(16, 16);
			ImageConversion.LoadImage(GUIUtl.texGuiPanelBorderRed, Convert.FromBase64String(GUIUtl.base64texPanelBoderRed));
			GUIUtl.texGuiInputNormal = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiInputNormal, Convert.FromBase64String(GUIUtl.base64texInputNormal));
			GUIUtl.texGuiInputActive = new Texture2D(32, 32);
			ImageConversion.LoadImage(GUIUtl.texGuiInputActive, Convert.FromBase64String(GUIUtl.base64texInputActive));
#else
			GUIUtl.texGuiWindowOn = new Texture2D(32, 32);
			GUIUtl.texGuiWindowOn.LoadImage(Convert.FromBase64String(GUIUtl.base64texWindowOn));
			GUIUtl.texGuiWindowOff = new Texture2D(32, 32);
			GUIUtl.texGuiWindowOff.LoadImage(Convert.FromBase64String(GUIUtl.base64texWindowOff));
			GUIUtl.texGuiWindowFoldOn = new Texture2D(32, 32);
			GUIUtl.texGuiWindowFoldOn.LoadImage(Convert.FromBase64String(GUIUtl.base64texWindowFoldOn));
			GUIUtl.texGuiWindowFoldOff = new Texture2D(32, 32);
			GUIUtl.texGuiWindowFoldOff.LoadImage(Convert.FromBase64String(GUIUtl.base64texWindowFoldOff));
			GUIUtl.texGuiBackgroundOn = new Texture2D(32, 32);
			GUIUtl.texGuiBackgroundOn.LoadImage(Convert.FromBase64String(GUIUtl.base64texBackgroundOn));
			GUIUtl.texGuiBackgroundOff = new Texture2D(32, 32);
			GUIUtl.texGuiBackgroundOff.LoadImage(Convert.FromBase64String(GUIUtl.base64texBackgroundOff));
			GUIUtl.texGuiButtonHover = new Texture2D(32, 32);
			GUIUtl.texGuiButtonHover.LoadImage(Convert.FromBase64String(GUIUtl.base64texButtonHover));
			GUIUtl.texGuiButtonNormal = new Texture2D(32, 32);
			GUIUtl.texGuiButtonNormal.LoadImage(Convert.FromBase64String(GUIUtl.base64texButtonNormal));
			GUIUtl.texGuiButtonActive = new Texture2D(32, 32);
			GUIUtl.texGuiButtonActive.LoadImage(Convert.FromBase64String(GUIUtl.base64texButtonActive));
			GUIUtl.texGuiPanelNormal = new Texture2D(8, 8);
			GUIUtl.texGuiPanelNormal.LoadImage(Convert.FromBase64String(GUIUtl.base64texPanelNormal));
			GUIUtl.texGuiPanelBorderRed = new Texture2D(16, 16);
			GUIUtl.texGuiPanelBorderRed.LoadImage(Convert.FromBase64String(GUIUtl.base64texPanelBoderRed));
			GUIUtl.texGuiInputNormal = new Texture2D(32, 32);
			GUIUtl.texGuiInputNormal.LoadImage(Convert.FromBase64String(GUIUtl.base64texInputNormal));
			GUIUtl.texGuiInputActive = new Texture2D(32, 32);
			GUIUtl.texGuiInputActive.LoadImage(Convert.FromBase64String(GUIUtl.base64texInputActive));
#endif
        }
        public static int GetPix(int i)
		{
			return (int)((1f + ((float)Screen.width / 1280f - 1f) * 0.6f) * (float)i);
		}
		public static bool GetAnyMouseButtonDown()
		{
			return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
		}
		public static bool GetAnyMouseButton()
		{
			return Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
		}
		public static Rect MoveScreenCenter(Rect rect)
		{
			rect.x = (float)(Screen.width / 2) - rect.width / 2f;
			rect.y = (float)(Screen.height / 2) - rect.height / 2f;
			return rect;
		}
		internal static readonly string base64texWindowOn = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAkCAYAAACTz/ouAAAAtElEQVR42uyX0QqAIAxFnfjsL6e/7A+YlsayMsvZQ+yCIFT3bBPTgfdelAKAKU2NaFN+zx6eRAAeQdHcvxxT6a9OIt+ids41ha+13mUSArXBa/XMJcLmrcY3oKVcksq8+NbkckGadJtfZSLFYEFafbLoyyyGZ8AABjAA7eRRUvjgIY08nQe8yAxgwF8ABm9tyt/EJ/eirO1G3avaTbsbUjM/QJ6A7nqEKqS3AYEKhKSFmgUYAGTwBp/Tq57RAAAAAElFTkSuQmCC";
		internal static readonly string base64texWindowOff = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAkCAYAAACTz/ouAAAAxklEQVR42uyX0Q3DIAxETcQsLMCAhf1gAZahoYIILqWirdOPynxFinPPJ5STrXLOhCfGeKuPjtbOo85a6/GFQkAVXxU+gRAyAFDcGLOkmlKaQg5AL74q/AJ0QDYucfjWtXtUIQQW8ZmTjS4+xUHm7B5dXO5AAAIQAJGeRC4/YI9XVuE9TeWSBSCAvwK4/tfmjImfzEUF4Dld9N2X+bQ5YIGg+BDXFdIG17ciHJoaxncNtb7fWD5wc1pA9JMij6vRNyvUXYABAFQxZQThBU+WAAAAAElFTkSuQmCC";
		internal static readonly string base64texWindowFoldOn = "iVBORw0KGgoAAAANSUhEUgAAABgAAAASCAYAAABB7B6eAAAAfUlEQVR42uyT0QqAIAxFnfTsL6u/vB9YWlvYKpJyb14YCOq5dxOBiJwWAEReJtcnOZcvO9WgraIKp48VNX+5SX6kRsSu+CGEUyclaC6snSkjauG94BejbVx+FFzdTTIu4MVv+FMn3hkL+PWHpdddmHcwDabBNGh+spVWAQYAeaxBMZcK1SAAAAAASUVORK5CYII=";
		internal static readonly string base64texWindowFoldOff = "iVBORw0KGgoAAAANSUhEUgAAABgAAAASCAYAAABB7B6eAAAAeklEQVR42uyUwQnAIAxFo3QWF3DA2v3MAi6TUrHS/mLJId78J0F9Lx9BJyKEYea9LRPpUs/FGA/ccChocC34I0LJS4DwEIKKWkoZSrrgCdeCf0Rd4q3gcDfd7+hyzibwURNPk3M1EMvpscX0BkuwBEtAtA2+XLOcAgwAe189mW2MybwAAAAASUVORK5CYII=";
		internal static readonly string base64texBackgroundOn = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAjElEQVR42uyXUQrAIAxDm13c7eTdHArDdd2PrT8JFAuCeRSEBqoqowCU1u4yR/2d43VTAZ51qZprUBUXYDSfJRfCMo+SCZFl7kGkmVsQmywWGolY3zHUGLjP5RMgAAEIQAACEIAABNh6aOgbSuY2VL2XTyB9M/ZS0ppg8gWRFs3+IKLDKRyIlHh+CjAAfuTkaxIqAb4AAAAASUVORK5CYII=";
		internal static readonly string base64texBackgroundOff = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAr0lEQVR42uyXQQ4AEQxFadzIAYf7cSYzJiSmaWc2ymLalYT0P59Fvy2lGFw556Mtg5lTvU/EG44RnyVsiItEFgCLe++nqF99WSdsf4JRfJbwB8gNAavEUe+qdf8zm1JaIs45AWZzVQfKyttjF7Y7oAAKoAAKoAAKoAAKAH0eHOa0f82E0ObzZS7gbABDUhGHoIKJIzJb6AcFotlDnAqnccxwAm6E13BKpFfxeH4KMAC9TVH8SMXC7AAAAABJRU5ErkJggg==";
		internal static readonly string base64texButtonNormal = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAoklEQVR42uyXMQ7DIAxFbZSFlROFk6cnYmV0cIQrRJsRs3xLjoAM72GEkElEyLLF2VIWpzK+TH4+zLp46Y9aK62MGKMNc8uPCchq8IsIh777XXGyHof37scqBNocEIAABCAAgWNecHyOfyvg8SjNjOAJ/8cK3vBZArcAAhCAgArklJI7uDMz97mUUnYIsB3BUwWPSgycTNYmj72aV3tucQswAKPmcSH9pvfQAAAAAElFTkSuQmCC";
		internal static readonly string base64texButtonHover = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAmElEQVR42uyXMQrAIAxFE+mip9L730fHVCVSETsal/8htLXDe/zSISQiNKYm1pHD0xgfcwWfzirC7YyZ9Z1dKrNfXL2JdC+RtfYr9NaCo8uBAAQgAAEIPOtBKeUo0Hv/38Bp+I7hLOE7lrOGrxL4CyAAAQg0gRRCMAcrM7E+S875hgCPT9BbsGhi4iQaa/K8q1mt5yOvAAMAfwPPqY0XyRgAAAAASUVORK5CYII=";
		internal static readonly string base64texButtonActive = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAmUlEQVR42uyXwQqAIAyGl3RoVx/Ioz5z1x7Ia8da4UKM6OQWtMEPMoT/24YMAQCmSpG0dVasPYdyCKSZBDln6Bneez4m0sIAa2/jBxB0pXqtCEcHNunq6y44UA4DMAAD+AYAIoobs6drE5LmtxFIQLQe49sFewUGYAAGYAC/AEiK6zhdXzMKDQDkEZxdkFrHXD3nVL/nuwADAAyFRZEqr0SGAAAAAElFTkSuQmCC";
		internal static readonly string base64texPanelNormal = "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAIAAABLbSncAAAAH0lEQVR42mJgwAEYgfj////oooyMTLh00EMCJwAIMAADRwMMiHBH2wAAAABJRU5ErkJggg==";
		internal static readonly string base64texPanelBoderRed = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAJ0lEQVR42mL8z0AaYAER/wnpYmSEM5lItGBUw6gGqmlgJDV5AwQYACijBB9exvUlAAAAAElFTkSuQmCC";
		internal static readonly string base64texInputNormal = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAkUlEQVR42uyXQQrAIAwEE+nD6svUl9mfpaYYkUKPmkN3QSTksJO9LYsImUopZ/sqrVVs70opPQMrwMuYbblCzUtmkKMPas60R+ajIMw5Z7t+6eUfScTgYa5qfppEDeSs0Gl8AdwTAAAAAAAAAAAAAAAAwO8B4lSXXIrJ5RjA6IYjhd5YdpXT0Y5tv62e23ALMABi1TXAc3c4+wAAAABJRU5ErkJggg==";
		internal static readonly string base64texInputActive = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAkElEQVR42uyXSw7AIAhEofFgev9DUWjxE+OyYhczCQuJybzMiiERoTqqrCObxzy658KYRqivZwZhWzKzLZhc/nGL1KvZ2DPpIkcYzx4OkrmSRJgv0pCLDovfVISPmP8hAQAAAAAAAAAAAAAAAADAAIo3o/CL+PHuhSX2NK91MPm7pbAbZEi7rPpgWD2vugUYAKxOtLWwqwjPAAAAAElFTkSuQmCC";
		internal static Texture2D texGuiWindowOn;
		internal static Texture2D texGuiWindowOff;
		internal static Texture2D texGuiWindowFoldOn;
		internal static Texture2D texGuiWindowFoldOff;
		internal static Texture2D texGuiBackgroundOn;
		internal static Texture2D texGuiBackgroundOff;
		internal static Texture2D texGuiButtonHover;
		internal static Texture2D texGuiButtonNormal;
		internal static Texture2D texGuiButtonActive;
		internal static Texture2D texGuiPanelNormal;
		internal static Texture2D texGuiPanelBorderRed;
		internal static Texture2D texGuiInputNormal;
		internal static Texture2D texGuiInputActive;
		public static Texture2D texBlack = new Texture2D(4, 4);
		public static Texture2D texGray;
	}
}