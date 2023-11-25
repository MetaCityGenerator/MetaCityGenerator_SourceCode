using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;

using System.Drawing;

namespace UrbanXTools
{
    /// <作用>
    /// 更改电池颜色
    /// </作用>
    public class CustomAttributesForAll : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
    {
        public int type = new int();
        public CustomAttributesForAll(IGH_Component component, int type) : base(component)
        {
            this.type = type;
        }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            GH_PaletteStyle styleStandard = null;
            GH_PaletteStyle styleSelected = null;
            GH_PaletteStyle styleStandard_Hidden = null;
            GH_PaletteStyle styleSelected_Hidden = null;


            if (channel == GH_CanvasChannel.Objects)
            {
                styleStandard = GH_Skin.palette_normal_standard;
                styleSelected = GH_Skin.palette_normal_selected;
                styleStandard_Hidden = GH_Skin.palette_hidden_standard;
                styleSelected_Hidden = GH_Skin.palette_hidden_selected;


                if (this.type == 0)
                {
                    GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.PowderBlue, Color.MidnightBlue, Color.MidnightBlue);
                    GH_Skin.palette_normal_selected = new GH_PaletteStyle(Color.LightSkyBlue, Color.MidnightBlue, Color.MidnightBlue);
                    GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color.Transparent, Color.LightSkyBlue, Color.MidnightBlue);
                    GH_Skin.palette_hidden_selected = new GH_PaletteStyle(Color.LightSteelBlue, Color.MidnightBlue, Color.MidnightBlue);
                }
                if (this.type == 1)
                {
                    GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.Black, Color.Black, Color.DarkGray);
                    GH_Skin.palette_normal_selected = new GH_PaletteStyle(Color.DarkGray, Color.Red, Color.Black);
                    GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color.Black, Color.Gray, Color.DarkGray);
                    GH_Skin.palette_hidden_selected = new GH_PaletteStyle(Color.DarkGray, Color.Red, Color.Black);
                }
                if (this.type == 2)
                {
                    GH_Skin.palette_normal_standard = new GH_PaletteStyle(Color.Black, Color.Black, Color.LightSkyBlue);
                    GH_Skin.palette_normal_selected = new GH_PaletteStyle(Color.LightSkyBlue, Color.Red, Color.Black);
                    GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color.Black, Color.Gray, Color.LightSkyBlue);
                    GH_Skin.palette_hidden_selected = new GH_PaletteStyle(Color.LightSkyBlue, Color.Red, Color.Black);
                }
            }

            base.Render(canvas, graphics, channel);
            if (channel == GH_CanvasChannel.Objects)
            {
                // Restore the cached styles.
                GH_Skin.palette_normal_standard = styleStandard;
                GH_Skin.palette_normal_selected = styleSelected;
                GH_Skin.palette_hidden_standard = styleStandard_Hidden;
                GH_Skin.palette_hidden_selected = styleSelected_Hidden;
            }
        }
    }
}
