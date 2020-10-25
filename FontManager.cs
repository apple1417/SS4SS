using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;

namespace SS4SS {
  static class FontManager {
    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr pFileView, Int32 cjSize, IntPtr pvResrved, ref Int32 pNumFonts);

    private static PrivateFontCollection pfc = new PrivateFontCollection();

    public static FontFamily Load(byte[] fontResource) {
      Int32 length = fontResource.Length;
      IntPtr memFont = IntPtr.Zero;

      try {
        memFont = Marshal.AllocCoTaskMem(length);
        Marshal.Copy(fontResource, 0, memFont, length);

        Int32 numFonts = 0;
        AddFontMemResourceEx(memFont, length, IntPtr.Zero, ref numFonts);

        pfc.AddMemoryFont(memFont, length);
      } finally {
        if (memFont != IntPtr.Zero) {
          Marshal.FreeCoTaskMem(memFont);
        }
      }

      return pfc.Families[pfc.Families.Length - 1];
    }

    public static FontFamily GetFont(string name) => pfc.Families.Where(f => f.Name == name).FirstOrDefault();
  }
}
