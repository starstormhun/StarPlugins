using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Cecil;

[assembly: System.Reflection.AssemblyFileVersion(KKShadowPatcher.ShadowPatcher.Version)]

namespace KKShadowPatcher {
    public static class ShadowPatcher {
        public const string PluginName = "ShadowPatcher";
        public const string GUID = "starstorm.kk.shadow.patcher";
        public const string Version = "1.0.0." + BuildNumber.Version;

        // This method and property need to exist for BepInEx to recognise the patcher
        public static IEnumerable<string> TargetDLLs { get; } = new string[] { /* Noone here but us Charles Dickens */ };
        public static void Patch(AssemblyDefinition assembly) {}

        // All three single-byte changes with enough surrounding bytes to identify them uniquely in the bytecode
        // 40 bytes each, tested for uniqueness in Ghidra
        // These sections are also non-overlapping, so they can be easily tested for at the same time
        static readonly string[][] patchStrings = new string[][] {
            // Directional lights
            new string[] {
                       // ↓  Here
                "00 b9 00 10 00 00 8b 90 fc 00 00 00 48 8d 44 24 20 89 54 24 58 40 84 ed 74 11 4c 8d 44 24 58 3b d1 89 4c 24 20 49 0f 4e",
                "00 b9 00 40 00 00 8b 90 fc 00 00 00 48 8d 44 24 20 89 54 24 58 40 84 ed 74 11 4c 8d 44 24 58 3b d1 89 4c 24 20 49 0f 4e"
            },
            // Spotlights
            new string[] {                                                         // ↓↓ Here
                "8b c1 c1 e8 02 0b c8 8b f9 d1 ef 0b f9 41 8b cc ff c7 d3 ff 41 b8 00 08 00 00 be 00 04 00 00 40 84 ed 41 0f 45 f0 89 74",
                "8b c1 c1 e8 02 0b c8 8b f9 d1 ef 0b f9 41 8b cc ff c7 d3 ff 41 b8 00 40 00 00 be 00 04 00 00 40 84 ed 41 0f 45 f0 89 74"
            },
            // Point lights
            new string[] {                                                                  // ↓↓ Here
                "c1 c1 e8 02 0b c8 8b f9 d1 ef 0b f9 41 8b cc ff c7 d3 ff bb 00 02 00 00 be 00 04 00 00 40 84 ed 0f 45 de 89 5c 24 58 e8",
                "c1 c1 e8 02 0b c8 8b f9 d1 ef 0b f9 41 8b cc ff c7 d3 ff bb 00 02 00 00 be 00 20 00 00 40 84 ed 0f 45 de 89 5c 24 58 e8"
            },
        };

        // Files wherein the byte patterns to be patched exist
        public static readonly string[] moduleNames = new[] {
            "CharaStudio.exe",
            "Koikatu.exe",
            "KoikatuVR.exe"
        };

        public static void Initialize() {
            Log.SetLogSource(new BepInEx.Logging.ManualLogSource(PluginName));
            Log.SetLogSource(BepInEx.Logging.Logger.CreateLogSource(PluginName));
            Log.Info("Patching shadow resolutions...");
            // Patch!
            foreach (var patch in patchStrings) {
                Apply(patch[0], patch[1]);
            }
        }

        private static unsafe void Apply(string patternStr, string patchStr) {
            var sw = Stopwatch.StartNew();

            ProcessModule module = null;
            if (moduleNames.Contains(Process.GetCurrentProcess().MainModule.ModuleName)) {
                module = Process.GetCurrentProcess().MainModule;
            } else {
                Log.Error($"Failed to find module to patch!");
                return;
            }

            byte[] pattern;
            byte[] patch;

            try {
                pattern = patternStr.Split(' ').Select(x => Convert.ToByte(x, 16)).ToArray();
                patch = patchStr.Split(' ').Select(x => Convert.ToByte(x, 16)).ToArray();
            } catch (Exception e) {
                Log.Error("Failed to parse settings: " + e);
                return;
            }

            if (pattern.Length == 0 || patch.Length == 0) {
                Log.Error("Empty pattern or patch, doing nothing.");
                return;
            }

            unsafe {
                var baseAddress = module.BaseAddress;
                var memorySize = module.ModuleMemorySize;

                using (var stream = new UnmanagedMemoryStream((byte*)baseAddress, memorySize, memorySize, FileAccess.ReadWrite)) {
                    var position = FindPosition(stream, pattern);

                    if (position < 0) {
                        Log.Warning("Could not find the byte pattern, check the settings!");
                        return;
                    }

                    Log.Info($"Found byte pattern at 0x{baseAddress.ToInt64() + position:X}, replacing...");

                    stream.Seek(position, SeekOrigin.Begin);

                    var matchPtr = (IntPtr)stream.PositionPointer;
                    if (!NativeMethods.VirtualProtect(matchPtr, (UIntPtr)patch.Length, NativeMethods.PAGE_EXECUTE_READWRITE, out var oldProtect)) {
                        Log.Error($"Failed to change memory protection, aborting. Error code: {Marshal.GetLastWin32Error()}");
                        return;
                    }

                    stream.Write(patch, 0, patch.Length);

                    NativeMethods.VirtualProtect(matchPtr, (UIntPtr)patch.Length, oldProtect, out _);

                    Log.Info($"Bytes overwritten successfully in {sw.ElapsedMilliseconds}ms!");
                };
            }
        }

        private static long FindPosition(Stream stream, byte[] pattern) {
            long foundPosition = -1;
            int i = 0;
            int b;

            while ((b = stream.ReadByte()) > -1) {
                if (pattern[i++] != b) {
                    stream.Position -= i - 1;
                    i = 0;
                    continue;
                }

                if (i == pattern.Length) {
                    foundPosition = stream.Position - i;
                    break;
                }
            }

            return foundPosition;
        }

        private static class NativeMethods {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

            public static uint PAGE_EXECUTE_READWRITE = 0x40;
        }
    }
}
