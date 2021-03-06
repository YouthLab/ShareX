﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (C) 2008-2013 ShareX Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using HelpersLib;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace ImageEffectsLib
{
    public class WatermarkManager
    {
        public WatermarkConfig Config { get; private set; }

        public WatermarkManager(WatermarkConfig config)
        {
            Config = config;
        }

        public static DialogResult ShowFontDialog(WatermarkConfig config)
        {
            DialogResult result = DialogResult.Cancel;

            try
            {
                FontDialog fDialog = new FontDialog
                {
                    ShowColor = true
                };

                try
                {
                    fDialog.Color = config.WatermarkFontArgb;
                    fDialog.Font = config.WatermarkFont;
                }
                catch (Exception err)
                {
                    DebugHelper.WriteException(err, "Error while initializing font.");
                }

                result = fDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    config.WatermarkFont = fDialog.Font;
                    config.WatermarkFontArgb = fDialog.Color;
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex, "Error while setting Watermark Font");
            }

            return result;
        }

        public Image ApplyWatermark(Image img)
        {
            switch (Config.WatermarkMode)
            {
                case WatermarkType.TEXT:
                    NameParser parser = new NameParser(NameParserType.Text) { Picture = img };
                    return DrawWatermarkText(img, parser.Parse(Config.WatermarkText));
                case WatermarkType.IMAGE:
                    return DrawWatermarkImage(img, Config.WatermarkImageLocation);
            }

            return img;
        }

        private Image DrawWatermarkImage(Image img, string imgPath)
        {
            Image img2 = null;

            try
            {
                if (img != null && !string.IsNullOrEmpty(imgPath) && File.Exists(imgPath) && Config.WatermarkImageScale > 0)
                {
                    img2 = Helpers.GetImageFromFile(imgPath);

                    int offset = Config.WatermarkOffset;
                    int width = (int)(Config.WatermarkImageScale / 100f * img2.Width);
                    int height = (int)(Config.WatermarkImageScale / 100f * img2.Height);

                    if (Config.WatermarkAutoHide && ((img.Width < width + offset) || (img.Height < height + offset)))
                    {
                        return img;
                    }

                    if (Config.WatermarkImageScale != 100)
                    {
                        img2 = ImageHelpers.ResizeImage(img2, width, height);
                    }

                    Point imgPos = Helpers.GetPosition((PositionType)Config.WatermarkPositionMode, offset, img.Size, img2.Size);

                    using (Graphics g = Graphics.FromImage(img))
                    {
                        g.SetHighQuality();
                        g.DrawImage(img2, imgPos.X, imgPos.Y, img2.Width, img2.Height);

                        if (Config.WatermarkAddReflection)
                        {
                            using (Bitmap bmp = ImageHelpers.AddReflection(img2, 50, 150, 10))
                            {
                                g.DrawImage(bmp, new Rectangle(imgPos.X, imgPos.Y + img2.Height - 1, bmp.Width, bmp.Height));
                            }
                        }

                        if (Config.WatermarkUseBorder)
                        {
                            g.DrawRectangleProper(Pens.Black, imgPos.X, imgPos.Y, img2.Width, img2.Height);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex, "Error while drawing image watermark");
            }
            finally
            {
                if (img2 != null) img2.Dispose();
            }

            return img;
        }

        private Image DrawWatermarkText(Image img, string drawText)
        {
            if (string.IsNullOrEmpty(drawText) || Config.WatermarkFont == null || Config.WatermarkFont.Size < 1)
            {
                return img;
            }

            Font font = null;
            Brush backgroundBrush = null;

            try
            {
                font = Config.WatermarkFont;
                Size textSize = TextRenderer.MeasureText(drawText, font);
                Size labelSize = new Size(textSize.Width + 10, textSize.Height + 10);

                if (Config.WatermarkAutoHide && ((img.Width < labelSize.Width + Config.WatermarkOffset) || (img.Height < labelSize.Height + Config.WatermarkOffset)))
                {
                    return img;
                }

                Rectangle labelRectangle = new Rectangle(Point.Empty, labelSize);
                Color fontColor = Config.WatermarkFontArgb;

                using (Bitmap bmp = new Bitmap(labelRectangle.Width, labelRectangle.Height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                    if (Config.WatermarkUseCustomGradient)
                    {
                        backgroundBrush = GradientMaker.CreateGradientBrush(labelRectangle.Size, Config.WatermarkGradient);
                    }
                    else
                    {
                        backgroundBrush = new LinearGradientBrush(labelRectangle, Config.WatermarkGradient1Argb, Config.WatermarkGradient2Argb, Config.WatermarkGradientType);
                    }

                    using (GraphicsPath gPath = new GraphicsPath())
                    using (Pen borderPen = new Pen(Config.WatermarkBorderArgb))
                    {
                        gPath.AddRoundedRectangle(labelRectangle, Config.WatermarkCornerRadius);
                        g.FillPath(backgroundBrush, gPath);
                        g.DrawPath(borderPen, gPath);
                    }

                    using (Brush textBrush = new SolidBrush(fontColor))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString(drawText, font, textBrush, bmp.Width / 2f, bmp.Height / 2f, sf);
                    }

                    using (Graphics gImg = Graphics.FromImage(img))
                    {
                        gImg.SmoothingMode = SmoothingMode.HighQuality;

                        Point labelPosition = Helpers.GetPosition((PositionType)Config.WatermarkPositionMode, Config.WatermarkOffset, img.Size, labelSize);
                        gImg.DrawImage(bmp, labelPosition.X, labelPosition.Y, bmp.Width, bmp.Height);

                        if (Config.WatermarkAddReflection)
                        {
                            using (Bitmap bmp2 = ImageHelpers.AddReflection(bmp, 50, 150, 10))
                            {
                                gImg.DrawImage(bmp2, new Rectangle(labelPosition.X, labelPosition.Y + bmp.Height - 1, bmp2.Width, bmp2.Height));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugHelper.WriteException(ex, "Error while drawing watermark");
            }
            finally
            {
                if (font != null) font.Dispose();
                if (backgroundBrush != null) backgroundBrush.Dispose();
            }

            return img;
        }
    }
}