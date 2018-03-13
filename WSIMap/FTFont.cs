using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Drawing;

namespace WSIMap
{
    /**
     * \class FTFont
     * \brief Represents a font that can be attached to a Label.  This font
     * is a wrapper around FTGL and FreeType 2.
     */
    public class FTFont
    {
        #region Data Members
        private int fontIndex;
        protected int pointSize;
        #endregion

        public FTFont(string fontFileName, bool createBitmapFont)
        {
            if (string.IsNullOrEmpty(fontFileName)) fontFileName = "Arial.ttf";
            string fontFile = Environment.GetEnvironmentVariable("windir") + "\\Fonts\\" + fontFileName;
            if (createBitmapFont)
                fontIndex = CreateBitmapFont(fontFile);
            else
                fontIndex = CreatePixmapFont(fontFile);
            if (fontIndex == -2)
                throw new WSIMapException("The maximum number of fonts has been exceeded.");
            else if (fontIndex == -1)
                throw new WSIMapException("The font could not be initialized.");
        }

        public void Dispose()
        {
            DeleteFont(fontIndex);
        }

        public int PointSize
        {
            get { return pointSize; }
            set
            {
                if (value <= 3) value = 4;
                pointSize = value;
                if (!SetFaceSize(fontIndex, pointSize))
                    throw new WSIMapException("The font point size could not be set.");
            }
        }

        internal RectangleF GetBoundingBox(string str)
        {
            float llx = 0, lly = 0, llz = 0, urx = 0, ury = 0, urz = 0; 
            GetBoundingBox(fontIndex, str, ref llx, ref lly, ref llz, ref urx, ref ury, ref urz);
            return new RectangleF(llx, lly, urx-llx, ury-lly);
        }

        internal void Draw(string str)
        {
            RenderString(fontIndex, str);
        }

        // FTGL Library functions
        [DllImport("ftgl.dll", EntryPoint = "CreatePixmapFont"), SuppressUnmanagedCodeSecurity]
        unsafe private static extern int CreatePixmapFont(string fontFile);
        [DllImport("ftgl.dll", EntryPoint = "CreateBitmapFont"), SuppressUnmanagedCodeSecurity]
        unsafe private static extern int CreateBitmapFont(string fontFile);
        [DllImport("ftgl.dll", EntryPoint = "SetFaceSize"), SuppressUnmanagedCodeSecurity]
        unsafe private static extern bool SetFaceSize(int fontIndex, int pointSize);
        [DllImport("ftgl.dll", EntryPoint = "RenderString"), SuppressUnmanagedCodeSecurity]
        unsafe private static extern void RenderString(int fontIndex, string str);
        [DllImport("ftgl.dll", EntryPoint = "DeleteFont"), SuppressUnmanagedCodeSecurity]
        unsafe private static extern void DeleteFont(int fontIndex);
        [DllImport("ftgl.dll", EntryPoint = "GetBoundingBox"), SuppressUnmanagedCodeSecurity]
        unsafe private static extern void GetBoundingBox(int fontIndex, string str, ref float llx, ref float lly, ref float llz, ref float urx, ref float ury, ref float urz);
    }
}
