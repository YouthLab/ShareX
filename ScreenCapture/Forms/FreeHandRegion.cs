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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenCapture
{
    public class FreeHandRegion : Surface
    {
        private NodeObject lastNode;
        private List<Point> points;
        private bool isAreaCreated;
        private Rectangle currentArea;

        public FreeHandRegion(Image backgroundImage = null)
            : base(backgroundImage)
        {
            points = new List<Point>(128);
            regionFillPath = new GraphicsPath();

            lastNode = new NodeObject();
            DrawableObjects.Add(lastNode);
        }

        protected override void Update()
        {
            base.Update();

            if (InputManager.IsMousePressed(MouseButtons.Right))
            {
                if (isAreaCreated)
                {
                    isAreaCreated = false;
                    regionFillPath.Reset();
                    HideNodes();
                    points.Clear();
                }
                else
                {
                    Close(SurfaceResult.Close);
                }
            }

            if (Config.QuickCrop && isAreaCreated && InputManager.IsMouseReleased(MouseButtons.Left))
            {
                Close(SurfaceResult.Region);
            }

            if (!isAreaCreated && InputManager.IsMouseDown(MouseButtons.Left))
            {
                lastNode.Visible = true;
                isAreaCreated = true;
            }

            if (lastNode.Visible && InputManager.IsMouseDown(MouseButtons.Left))
            {
                if (points.Count == 0 || points.Last() != InputManager.MousePosition0Based)
                {
                    points.Add(InputManager.MousePosition0Based);

                    if (points.Count > 1)
                    {
                        regionFillPath.AddLine(points.Last(1), points.Last());
                    }

                    lastNode.Position = InputManager.MousePosition0Based;
                }
            }

            if (points.Count > 2)
            {
                RectangleF rect = regionFillPath.GetBounds();

                currentArea = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width + 1, (int)rect.Height + 1);
            }
        }

        protected override void Draw(Graphics g)
        {
            if (points.Count > 2)
            {
                g.SmoothingMode = SmoothingMode.HighQuality;

                borderDotPen.DashOffset = (float)timer.Elapsed.TotalSeconds * 10;
                borderDotPen2.DashOffset = 5 + (float)timer.Elapsed.TotalSeconds * 10;

                using (Region region = new Region(regionFillPath))
                {
                    g.Clip = region;
                    g.FillRectangle(lightBackgroundBrush, ScreenRectangle0Based);
                    g.ResetClip();
                }

                g.DrawPath(borderDotPen, regionFillPath);
                g.DrawPath(borderDotPen2, regionFillPath);
                g.DrawLine(borderDotPen, points[points.Count - 1], points[0]);
                g.DrawLine(borderDotPen2, points[points.Count - 1], points[0]);
                g.DrawRectangleProper(borderPen, currentArea);
            }

            base.Draw(g);
        }
    }
}