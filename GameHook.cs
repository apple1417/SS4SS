using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MemTools;

namespace SS4SS {
  static class GameHook {
    private static MemManager manager = null;
    private static Pointer statsPtr = null;
    public static bool IsHooked => manager != null && manager.IsHooked && statsPtr != null;

    public static PlayerStats Stats {
      get {
        if (!IsHooked && !TryHook()) {
          return default;
        }
        return manager.Read<PlayerStats>(statsPtr);
      }
    }

    public static bool TryHook() {
      foreach (Process p in Process.GetProcessesByName("Sam4")) {
        manager = new MemManager(p);

        try {
          IntPtr ptr = manager.SigScan(
            p.MainModule.BaseAddress,
            p.MainModule.ModuleMemorySize,
            12,
            "FF 92 80000000",       // call qword ptr [rdx+00000080]
            "44 8B E0",             // mov r12d,eax
            "48 8B 0D ????????"     // mov rcx,[Sam4.exe+2242138]       <----
          );
          if (ptr == IntPtr.Zero) {
            continue;
          }

          Int64 baseAddr = ptr.ToInt64() + 4 + manager.Read<Int32>(ptr);
          statsPtr = new Pointer(new IntPtr(baseAddr), 0x0, 0x10, 0x238, 0x1e0, 0x0);
          break;

        } catch (Win32Exception) {
          continue;
        }
      }
      return IsHooked;
    }
  }

  #region GameDefines
  [StructLayout(LayoutKind.Explicit)]
 struct PlayerStats {
    [FieldOffset(0x28)]
    public Boolean HasCheated;
    [FieldOffset(0x58)]
    public UInt32 Score;
    [FieldOffset(0x68)]
    public UInt32 IgtSeconds;
    [FieldOffset(0x88)]
    public UInt32 Kills;
    [FieldOffset(0x98)]
    public UInt32 MaxKills;
    [FieldOffset(0xA8)]
    public UInt32 Secrets;
    [FieldOffset(0xB8)]
    public UInt32 MaxSecrets;
    [FieldOffset(0xD8)]
    public Difficulty GameDifficulty;
    [FieldOffset(0xF4)]
    public UInt32 Saves;
    [FieldOffset(0x104)]
    public UInt32 MaxSaves;
    [FieldOffset(0x3D8)]
    public UInt32 Deaths;

    [FieldOffset(0x190)]
    public Single EnemySpeed;
    [FieldOffset(0x194)]
    public Single EnemyThink;
    [FieldOffset(0x198)]
    public Single PlayerDamage;
    [FieldOffset(0x19C)]
    public Single SelfDamage;
    [FieldOffset(0x1A0)]
    public Boolean HealthRegen;
    [FieldOffset(0x1A4)]
    public Single DelayFactor;
    [FieldOffset(0x1A8)]
    public Single AutoAimFactor;
    [FieldOffset(0x1AC)]
    public Single AmmoQuantity;
  }

  enum Difficulty {
    None = 0,
    Tourist = 1,
    Easy = 2,
    Normal = 3,
    Hard = 4,
    Serious = 5
  }
  #endregion
}
