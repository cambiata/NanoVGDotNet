
//
// Copyright (c) 2009-2013 Mikko Mononen memon@inside.org
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
//

/*
 * Por to C#
 * Copyright (c) 2016 Miguel A. Guirado L. https://sites.google.com/site/bitiopia/
 * 
 * 	NanoVG.net is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  any later version.
 *
 *  NanoVG.net is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with NanoVG.net.  If not, see <http://www.gnu.org/licenses/>. See
 *  the file lgpl-3.0.txt for more details.
 */

using System;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using NanoVGDotNet;

namespace Demo
{
	class GLWindow : GameWindow
	{
		NVGcontext vg = new NVGcontext();
		DemoData data = new DemoData();

		int premult = 0;

		protected static int AntiAliasingSamples = 0;

		#region Constructors

		static GLWindow()
		{
			AntiAliasingModes = CalculeAntiAliasingModes();
		}

		public GLWindow(GraphicsMode gm)
			: base(1000, 600, gm)
		{
			KeyDown += Keyboard_KeyDown;
		}

		#endregion Constructors

		#region OpenTK-Utils

		protected static int[] AntiAliasingModes
		{
			get;
			private set;
		}

		static int[] CalculeAntiAliasingModes()
		{
			List<int> aa_modes = new List<int>();
			int aa = 0;
			do
			{
				try
				{
					GraphicsMode mode = new GraphicsMode(32, 0, 0, aa);
					if (!aa_modes.Contains(mode.Samples))
						aa_modes.Add(aa);
				}
				catch (Exception)
				{
				}
				finally
				{
					aa += 2;
				}
			} while (aa <= 32);

			return aa_modes.ToArray();
		}

		#region GetMaxAntiAliasingAvailable()

		/// <summary>
		/// Devuelve el modo disponible más próximo a <see cref="anti_aliasing_desired"/> pero que sea inferior o igual a este último.
		/// </summary>
		/// <param name="anti_aliasing_desired"></param>
		/// <returns></returns>
		protected static int GetMaxAntiAliasingAvailable(int anti_aliasing_desired)
		{
			int aa = 0;

			foreach (int i in AntiAliasingModes)
				if (i == anti_aliasing_desired)
				{
					aa = i;
					break;
				}
				else if (i < anti_aliasing_desired && i > aa)
				{
					aa = i;
				}

			return aa;
		}

		#endregion GetMaxAntiAliasingAvailable()

		#endregion OpenTK-Utils

		#region Demo-Widgets

		byte[] icon = new byte[8];

		/// <summary>
		/// cp to UTF8. (mysterious code)
		/// </summary>
		/// <returns>The to UTF8.</returns>
		/// <param name="cp">Cp.</param>
		string cpToUTF8(int cp)
		{
			int n = 0;
			if (cp < 0x80)
				n = 1;
			else if (cp < 0x800)
				n = 2;
			else if (cp < 0x10000)
				n = 3;
			else if (cp < 0x200000)
				n = 4;
			else if (cp < 0x4000000)
				n = 5;
			else if (cp <= 0x7fffffff)
				n = 6;
			icon[n] = (byte)'\0';
			switch (n)
			{
				case 6:
					goto case_6;
				case 5:
					goto case_5;
				case 4:
					goto case_4;
				case 3:
					goto case_3;
				case 2:
					goto case_2;
				case 1:
					goto case_1;
			}
			goto end;

			case_6:
			icon[5] = (byte)(0x80 | (cp & 0x3f));
			cp = cp >> 6;
			cp |= 0x4000000;
			case_5:
			icon[4] = (byte)(0x80 | (cp & 0x3f));
			cp = cp >> 6;
			cp |= 0x200000;
			case_4:
			icon[3] = (byte)(0x80 | (cp & 0x3f));
			cp = cp >> 6;
			cp |= 0x10000;
			case_3:
			icon[2] = (byte)(0x80 | (cp & 0x3f));
			cp = cp >> 6;
			cp |= 0x800;
			case_2:
			icon[1] = (byte)(0x80 | (cp & 0x3f));
			cp = cp >> 6;
			cp |= 0xc0;
			case_1:
			icon[0] = (byte)cp;

			end:

			string r = new string(Encoding.UTF8.GetChars(icon, 0, n));
			r = r.Trim(new char[]{ '\0' });
			int rl = r.Length;

			return r;
		}

		/// <summary>
		/// Returns 1 if col.rgba is (0.0f,0.0f,0.0f,0.0f), 0 otherwise
		/// </summary>
		/// <returns><c>true</c>, if black was ised, <c>false</c> otherwise.</returns>
		/// <param name="col">Col.</param>
		bool isBlack(NVGcolor col)
		{
			if (col.r == 0.0f && col.g == 0.0f && col.b == 0.0f && col.a == 0.0f)
			{
				return true;
			}
			return false;
		}

		void drawButton(NVGcontext vg, int preicon, string text, float x, float y, float w, float h, NVGcolor col)
		{
			NVGpaint bg;
			float cornerRadius = 4.0f;
			float tw = 0, iw = 0;

			NVGcolor icol = NanoVG.nvgRGBA(255, 255, 255, (byte)(isBlack(col) ? 16 : 32));
			NVGcolor ocol = NanoVG.nvgRGBA(0, 0, 0, (byte)(isBlack(col) ? 16 : 32));

			bg = NanoVG.nvgLinearGradient(vg, x, y, x, y + h, icol, ocol);
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 1, y + 1, w - 2, h - 2, cornerRadius - 1);
			//NanoVG.nvgRect(vg, x+1,y+1, w-2,h-2);

			if (!isBlack(col))
			{
				NanoVG.nvgFillColor(vg, col);
				NanoVG.nvgFill(vg);
			}
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			// Representa el trazo que delimita al botón
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 0.5f, y + 0.5f, w - 1, h - 1, cornerRadius - 0.5f);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 48));
			NanoVG.nvgStroke(vg);

			NanoVG.nvgFontSize(vg, 20.0f);
			NanoVG.nvgFontFace(vg, "sans-bold");
			tw = NanoVG.nvgTextBounds(vg, 0, 0, text, null);

			if (preicon != 0)
			{
				NanoVG.nvgFontSize(vg, h * 1.3f);
				NanoVG.nvgFontFace(vg, "icons");
				string str1 = cpToUTF8(preicon);
				iw = NanoVG.nvgTextBounds(vg, 0, 0, str1, null);
				iw += h * 0.15f;
			}

			if (preicon != 0)
			{
				NanoVG.nvgFontSize(vg, h * 1.3f);
				NanoVG.nvgFontFace(vg, "icons");
				NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 96));
				NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_LEFT | NVGalign.NVG_ALIGN_MIDDLE));
				NanoVG.nvgText(vg, x + w * 0.5f - tw * 0.5f - iw * 0.75f, y + h * 0.5f, cpToUTF8(preicon));
			}

			NanoVG.nvgFontSize(vg, 20.0f);
			NanoVG.nvgFontFace(vg, "sans-bold");
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_LEFT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(0, 0, 0, 160));
			NanoVG.nvgText(vg, x + w * 0.5f - tw * 0.5f + iw * 0.25f, y + h * 0.5f - 1, text);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 160));
			NanoVG.nvgText(vg, x + w * 0.5f - tw * 0.5f + iw * 0.25f, y + h * 0.5f, text);
		}

		void drawWindow(NVGcontext vg, string title, float x, float y, float w, float h)
		{
			float cornerRadius = 3.0f;
			NVGpaint shadowPaint;
			NVGpaint headerPaint;

			NanoVG.nvgSave(vg);
			//NanoVG.nvgClearState(vg);

			// Window
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x, y, w, h, cornerRadius);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(28, 30, 34, 192));
			//	nvgFillColor(vg, nvgRGBA(0,0,0,128));
			NanoVG.nvgFill(vg);

			// Drop shadow
			if (Environment.OSVersion.Platform != PlatformID.Unix)
			{
				shadowPaint = NanoVG.nvgBoxGradient(vg, x, y + 2, w, h, cornerRadius * 2, 10, 
					NanoVG.nvgRGBA(0, 0, 0, 128), NanoVG.nvgRGBA(0, 0, 0, 0));
				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgRect(vg, x - 10, y - 10, w + 20, h + 30);
				NanoVG.nvgRoundedRect(vg, x, y, w, h, cornerRadius);
				NanoVG.nvgPathWinding(vg, (int)NVGsolidity.NVG_HOLE);
				NanoVG.nvgFillPaint(vg, shadowPaint);
				NanoVG.nvgFill(vg);
			}

			// Header
			headerPaint = NanoVG.nvgLinearGradient(vg, x, y, x, y + 15, NanoVG.nvgRGBA(255, 255, 255, 8), NanoVG.nvgRGBA(0, 0, 0, 16));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 1, y + 1, w - 2, 30, cornerRadius - 1);
			NanoVG.nvgFillPaint(vg, headerPaint);
			NanoVG.nvgFill(vg);
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgMoveTo(vg, x + 0.5f, y + 0.5f + 30);
			NanoVG.nvgLineTo(vg, x + 0.5f + w - 1, y + 0.5f + 30);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 32));
			NanoVG.nvgStroke(vg);

			NanoVG.nvgFontSize(vg, 18.0f);
			NanoVG.nvgFontFace(vg, "sans-bold");
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_CENTER | NVGalign.NVG_ALIGN_MIDDLE));

			NanoVG.nvgFontBlur(vg, 2);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(0, 0, 0, 128));
			NanoVG.nvgText(vg, x + w / 2, y + 16 + 1, title);

			NanoVG.nvgFontBlur(vg, 0);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(220, 220, 220, 160));
			NanoVG.nvgText(vg, x + w / 2, y + 16, title);

			NanoVG.nvgRestore(vg);
		}

		void drawSearchBox(NVGcontext vg, string text, float x, float y, float w, float h)
		{
			NVGpaint bg;
			//char icon[8];
			float cornerRadius = h / 2 - 1;

			// Edit
			bg = NanoVG.nvgBoxGradient(vg, x, y + 1.5f, w, h, h / 2, 5, NanoVG.nvgRGBA(0, 0, 0, 16), NanoVG.nvgRGBA(0, 0, 0, 92));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x, y, w, h, cornerRadius);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			/*	nvgBeginPath(vg);
			nvgRoundedRect(vg, x+0.5f,y+0.5f, w-1,h-1, cornerRadius-0.5f);
			nvgStrokeColor(vg, nvgRGBA(0,0,0,48));
			nvgStroke(vg);*/

			NanoVG.nvgFontSize(vg, h * 1.3f);
			NanoVG.nvgFontFace(vg, "icons");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 64));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_CENTER | NVGalign.NVG_ALIGN_MIDDLE));
			string sts = cpToUTF8(GlNanoVG.ICON_SEARCH);
			NanoVG.nvgText(vg, x + h * 0.55f, y + h * 0.55f, sts);

			NanoVG.nvgFontSize(vg, 20.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 32));

			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_LEFT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + h * 1.05f, y + h * 0.5f, text);

			NanoVG.nvgFontSize(vg, h * 1.3f);
			NanoVG.nvgFontFace(vg, "icons");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 32));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_CENTER | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + w - h * 0.55f, y + h * 0.55f, cpToUTF8(GlNanoVG.ICON_CIRCLED_CROSS));
		}

		void drawDropDown(NVGcontext vg, string text, float x, float y, float w, float h)
		{
			NVGpaint bg;
			//char icon[8];
			float cornerRadius = 4.0f;

			bg = NanoVG.nvgLinearGradient(vg, x, y, x, y + h, NanoVG.nvgRGBA(255, 255, 255, 16), NanoVG.nvgRGBA(0, 0, 0, 16));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 1, y + 1, w - 2, h - 2, cornerRadius - 1);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 0.5f, y + 0.5f, w - 1, h - 1, cornerRadius - 0.5f);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 48));
			NanoVG.nvgStroke(vg);

			NanoVG.nvgFontSize(vg, 20.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 160));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_LEFT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + h * 0.3f, y + h * 0.5f, text);

			NanoVG.nvgFontSize(vg, h * 1.3f);
			NanoVG.nvgFontFace(vg, "icons");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 64));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_CENTER | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + w - h * 0.5f, y + h * 0.5f, cpToUTF8(GlNanoVG.ICON_CHEVRON_RIGHT));
		}

		void drawLabel(NVGcontext vg, string text, float x, float y, float w, float h)
		{
			//NVG_NOTUSED(w);

			NanoVG.nvgFontSize(vg, 18.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 128));

			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_LEFT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x, y + h * 0.5f, text);
		}

		void drawEditBoxBase(NVGcontext vg, float x, float y, float w, float h)
		{
			NVGpaint bg;
			// Edit
			bg = NanoVG.nvgBoxGradient(vg, x + 1, y + 1 + 1.5f, w - 2, h - 2, 3, 4, NanoVG.nvgRGBA(255, 255, 255, 32), NanoVG.nvgRGBA(32, 32, 32, 32));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 1, y + 1, w - 2, h - 2, 4 - 1);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 0.5f, y + 0.5f, w - 1, h - 1, 4 - 0.5f);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 48));
			NanoVG.nvgStroke(vg);
		}

		void drawEditBox(NVGcontext vg, string text, float x, float y, float w, float h)
		{

			drawEditBoxBase(vg, x, y, w, h);

			NanoVG.nvgFontSize(vg, 20.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 64));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_LEFT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + h * 0.3f, y + h * 0.5f, text);
		}

		void drawEditBoxNum(NVGcontext vg, string text, string units, float x, float y, float w, float h)
		{
			float uw;

			drawEditBoxBase(vg, x, y, w, h);

			uw = NanoVG.nvgTextBounds(vg, 0, 0, units, null);

			NanoVG.nvgFontSize(vg, 18.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 64));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_RIGHT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + w - h * 0.3f, y + h * 0.5f, units);

			NanoVG.nvgFontSize(vg, 20.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 128));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_RIGHT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + w - uw - h * 0.5f, y + h * 0.5f, text);
		}

		void drawCheckBox(NVGcontext vg, string text, float x, float y, float w, float h)
		{
			NVGpaint bg;
			//char icon[8];
			//NVG_NOTUSED(w);

			NanoVG.nvgFontSize(vg, 18.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 160));

			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_LEFT | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + 28, y + h * 0.5f, text);

			bg = NanoVG.nvgBoxGradient(vg, x + 1, y + (int)(h * 0.5f) - 9 + 1, 18, 18, 3, 3, NanoVG.nvgRGBA(0, 0, 0, 32), NanoVG.nvgRGBA(0, 0, 0, 92));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + 1, y + (int)(h * 0.5f) - 9, 18, 18, 3);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			NanoVG.nvgFontSize(vg, 40);
			NanoVG.nvgFontFace(vg, "icons");
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 128));
			NanoVG.nvgTextAlign(vg, (int)(NVGalign.NVG_ALIGN_CENTER | NVGalign.NVG_ALIGN_MIDDLE));
			NanoVG.nvgText(vg, x + 9 + 2, y + h * 0.5f, cpToUTF8(GlNanoVG.ICON_CHECK));
		}

		void drawSlider(NVGcontext vg, float pos, float x, float y, float w, float h)
		{
			NVGpaint bg, knob;
			float cy = y + (int)(h * 0.5f);
			float kr = (int)(h * 0.25f);

			NanoVG.nvgSave(vg);
			//	nvgClearState(vg);

			// Slot
			bg = NanoVG.nvgBoxGradient(vg, x, cy - 2 + 1, w, 4, 2, 2, NanoVG.nvgRGBA(0, 0, 0, 32), NanoVG.nvgRGBA(0, 0, 0, 128));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x, cy - 2, w, 4, 2);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			// Knob Shadow
			bg = NanoVG.nvgRadialGradient(vg, x + (int)(pos * w), cy + 1, kr - 3, kr + 3, NanoVG.nvgRGBA(0, 0, 0, 64), NanoVG.nvgRGBA(0, 0, 0, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, x + (int)(pos * w) - kr - 5, cy - kr - 5, kr * 2 + 5 + 5, kr * 2 + 5 + 5 + 3);
			NanoVG.nvgCircle(vg, x + (int)(pos * w), cy, kr);
			NanoVG.nvgPathWinding(vg, (int)NVGsolidity.NVG_HOLE);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			// Knob
			knob = NanoVG.nvgLinearGradient(vg, x, cy - kr, x, cy + kr, NanoVG.nvgRGBA(255, 255, 255, 16), NanoVG.nvgRGBA(0, 0, 0, 16));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgCircle(vg, x + (int)(pos * w), cy, kr - 1);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(40, 43, 48, 255));
			NanoVG.nvgFill(vg);
			NanoVG.nvgFillPaint(vg, knob);
			NanoVG.nvgFill(vg);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgCircle(vg, x + (int)(pos * w), cy, kr - 0.5f);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 92));
			NanoVG.nvgStroke(vg);

			NanoVG.nvgRestore(vg);
		}

		void drawEyes(NVGcontext vg, float x, float y, float w, float h, float mx, float my, float t)
		{
			NVGpaint gloss, bg;
			float ex = w * 0.23f;
			float ey = h * 0.5f;
			float lx = x + ex;
			float ly = y + ey;
			float rx = x + w - ex;
			float ry = y + ey;
			float dx, dy, d;
			float br = (ex < ey ? ex : ey) * 0.5f;
			float blink = (float)(1 - Math.Pow(Math.Sin(t * 0.5f), 200) * 0.8f);

			bg = NanoVG.nvgLinearGradient(vg, x, y + h * 0.5f, x + w * 0.1f, y + h, NanoVG.nvgRGBA(0, 0, 0, 32), NanoVG.nvgRGBA(0, 0, 0, 16));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgEllipse(vg, lx + 3.0f, ly + 16.0f, ex, ey);
			if (Environment.OSVersion.Platform != PlatformID.Unix)
				NanoVG.nvgEllipse(vg, rx + 3.0f, ry + 16.0f, ex, ey);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				NanoVG.nvgBeginPath(vg);
				//NanoVG.nvgEllipse(vg, lx + 3.0f, ly + 16.0f, ex, ey);
				NanoVG.nvgEllipse(vg, rx + 3.0f, ry + 16.0f, ex, ey);
				NanoVG.nvgFillPaint(vg, bg);
				NanoVG.nvgFill(vg);
			}

			//_____________________________________________________________

			bg = NanoVG.nvgLinearGradient(vg, x, y + h * 0.25f, x + w * 0.1f, y + h, NanoVG.nvgRGBA(220, 220, 220, 255), NanoVG.nvgRGBA(128, 128, 128, 255));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgEllipse(vg, lx, ly, ex, ey);
			if (Environment.OSVersion.Platform != PlatformID.Unix)
				NanoVG.nvgEllipse(vg, rx, ry, ex, ey);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				NanoVG.nvgBeginPath(vg);
				//NanoVG.nvgEllipse(vg, lx, ly, ex, ey);
				NanoVG.nvgEllipse(vg, rx, ry, ex, ey);
				NanoVG.nvgFillPaint(vg, bg);
				NanoVG.nvgFill(vg);
			}

			dx = (mx - rx) / (ex * 10);
			dy = (my - ry) / (ey * 10);
			d = (float)Math.Sqrt(dx * dx + dy * dy);
			if (d > 1.0f)
			{
				dx /= d;
				dy /= d;
			}
			dx *= ex * 0.4f;
			dy *= ey * 0.5f;
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgEllipse(vg, lx + dx, ly + dy + ey * 0.25f * (1 - blink), br, br * blink);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(32, 32, 32, 255));
			NanoVG.nvgFill(vg);

			dx = (mx - rx) / (ex * 10);
			dy = (my - ry) / (ey * 10);
			d = (float)Math.Sqrt(dx * dx + dy * dy);
			if (d > 1.0f)
			{
				dx /= d;
				dy /= d;
			}
			dx *= ex * 0.4f;
			dy *= ey * 0.5f;
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgEllipse(vg, rx + dx, ry + dy + ey * 0.25f * (1 - blink), br, br * blink);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(32, 32, 32, 255));
			NanoVG.nvgFill(vg);

			gloss = NanoVG.nvgRadialGradient(vg, lx - ex * 0.25f, ly - ey * 0.5f, ex * 0.1f, ex * 0.75f, NanoVG.nvgRGBA(255, 255, 255, 128), NanoVG.nvgRGBA(255, 255, 255, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgEllipse(vg, lx, ly, ex, ey);
			NanoVG.nvgFillPaint(vg, gloss);
			NanoVG.nvgFill(vg);

			gloss = NanoVG.nvgRadialGradient(vg, rx - ex * 0.25f, ry - ey * 0.5f, ex * 0.1f, ex * 0.75f, NanoVG.nvgRGBA(255, 255, 255, 128), NanoVG.nvgRGBA(255, 255, 255, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgEllipse(vg, rx, ry, ex, ey);
			NanoVG.nvgFillPaint(vg, gloss);
			NanoVG.nvgFill(vg);
		}

		void drawWidths(NVGcontext vg, float x, float y, float width)
		{
			int i;

			NanoVG.nvgSave(vg);

			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 255));

			for (i = 0; i < 20; i++)
			{
				float w = (i + 0.5f) * 0.1f;
				NanoVG.nvgStrokeWidth(vg, w);
				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgMoveTo(vg, x, y);
				NanoVG.nvgLineTo(vg, x + width, y + width * 0.3f);
				NanoVG.nvgStroke(vg);
				y += 10;
			}

			NanoVG.nvgRestore(vg);
		}

		void drawCaps(NVGcontext vg, float x, float y, float width)
		{
			int i;
			int[] caps = new int[]
			{ 
				(int)NVGlineCap.NVG_BUTT, 
				(int)NVGlineCap.NVG_ROUND, 
				(int)NVGlineCap.NVG_SQUARE
			};
			float lineWidth = 8.0f;

			NanoVG.nvgSave(vg);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, x - lineWidth / 2, y, width + lineWidth, 40);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 32));
			NanoVG.nvgFill(vg);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, x, y, width, 40);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 32));
			NanoVG.nvgFill(vg);

			NanoVG.nvgStrokeWidth(vg, lineWidth);
			for (i = 0; i < 3; i++)
			{
				NanoVG.nvgLineCap(vg, caps[i]);
				NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 255));
				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgMoveTo(vg, x, y + i * 10 + 5);
				NanoVG.nvgLineTo(vg, x + width, y + i * 10 + 5);
				NanoVG.nvgStroke(vg);
			}

			NanoVG.nvgRestore(vg);
		}

		void drawScissor(NVGcontext vg, float x, float y, float t)
		{
			NanoVG.nvgSave(vg);

			// Draw first rect and set scissor to it's area.
			NanoVG.nvgTranslate(vg, x, y);
			NanoVG.nvgRotate(vg, NanoVG.nvgDegToRad(5));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, -20, -20, 60, 40);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 0, 0, 255));
			NanoVG.nvgFill(vg);
			NanoVG.nvgScissor(vg, -20, -20, 60, 40);

			// Draw second rectangle with offset and rotation.
			NanoVG.nvgTranslate(vg, 40, 0);
			NanoVG.nvgRotate(vg, t);

			// Draw the intended second rectangle without any scissoring.
			NanoVG.nvgSave(vg);
			NanoVG.nvgResetScissor(vg);
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, -20, -10, 60, 30);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 128, 0, 64));
			NanoVG.nvgFill(vg);
			NanoVG.nvgRestore(vg);

			// Draw second rectangle with combined scissoring.
			NanoVG.nvgIntersectScissor(vg, -20, -10, 60, 30);
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, -20, -10, 60, 30);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 128, 0, 255));
			NanoVG.nvgFill(vg);

			NanoVG.nvgRestore(vg);
		}

		void drawLines(NVGcontext vg, float x, float y, float w, float h, float t)
		{
			int i, j;
			float pad = 5.0f, s = w / 9.0f - pad * 2;
			float[] pts = new float[4 * 2];
			float fx, fy;
			int[] joins = new int[]
			{
				(int)NVGlineCap.NVG_MITER, 
				(int)NVGlineCap.NVG_ROUND, 
				(int)NVGlineCap.NVG_BEVEL
			};
			int[] caps = new int[]
			{
				(int)NVGlineCap.NVG_BUTT, 
				(int)NVGlineCap.NVG_ROUND, 
				(int)NVGlineCap.NVG_SQUARE
			};
			//NVG_NOTUSED(h);

			NanoVG.nvgSave(vg);
			pts[0] = -s * 0.25f + (float)Math.Cos(t * 0.3f) * s * 0.5f;
			pts[1] = (float)Math.Sin(t * 0.3f) * s * 0.5f;
			pts[2] = -s * 0.25f;
			pts[3] = 0;
			pts[4] = s * 0.25f;
			pts[5] = 0;
			pts[6] = s * 0.25f + (float)Math.Cos(-t * 0.3f) * s * 0.5f;
			pts[7] = (float)Math.Sin(-t * 0.3f) * s * 0.5f;

			for (i = 0; i < 3; i++)
			{
				for (j = 0; j < 3; j++)
				{
					fx = x + s * 0.5f + (i * 3 + j) / 9.0f * w + pad;
					fy = y - s * 0.5f + pad;

					if (i == 2)
					{
						i = 2;
					}

					NanoVG.nvgLineCap(vg, caps[i]);
					NanoVG.nvgLineJoin(vg, joins[j]);

					NanoVG.nvgStrokeWidth(vg, s * 0.3f);
					NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 160));
					NanoVG.nvgBeginPath(vg);
					NanoVG.nvgMoveTo(vg, fx + pts[0], fy + pts[1]);
					NanoVG.nvgLineTo(vg, fx + pts[2], fy + pts[3]);
					NanoVG.nvgLineTo(vg, fx + pts[4], fy + pts[5]);
					NanoVG.nvgLineTo(vg, fx + pts[6], fy + pts[7]);
					NanoVG.nvgStroke(vg);

					NanoVG.nvgLineCap(vg, (int)NVGlineCap.NVG_BUTT);
					NanoVG.nvgLineJoin(vg, (int)NVGlineCap.NVG_BEVEL);

					NanoVG.nvgStrokeWidth(vg, 1.0f);
					NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 192, 255, 255));
					NanoVG.nvgBeginPath(vg);
					NanoVG.nvgMoveTo(vg, fx + pts[0], fy + pts[1]);
					NanoVG.nvgLineTo(vg, fx + pts[2], fy + pts[3]);
					NanoVG.nvgLineTo(vg, fx + pts[4], fy + pts[5]);
					NanoVG.nvgLineTo(vg, fx + pts[6], fy + pts[7]);
					NanoVG.nvgStroke(vg);
				}
			}
				
			NanoVG.nvgRestore(vg);
		}

		void drawGraph(NVGcontext vg, float x, float y, float w, float h, float t)
		{
			NVGpaint bg;
			float[] samples = new float[6];
			float[] sx = new float[6], sy = new float[6];
			float dx = w / 5.0f;
			int i;

			samples[0] = (1 + (float)Math.Sin(t * 1.2345f + (float)Math.Cos(t * 0.33457f) * 0.44f)) * 0.5f;
			samples[1] = (1 + (float)Math.Sin(t * 0.68363f + (float)Math.Cos(t * 1.3f) * 1.55f)) * 0.5f;
			samples[2] = (1 + (float)Math.Sin(t * 1.1642f + (float)Math.Cos(t * 0.33457) * 1.24f)) * 0.5f;
			samples[3] = (1 + (float)Math.Sin(t * 0.56345f + (float)Math.Cos(t * 1.63f) * 0.14f)) * 0.5f;
			samples[4] = (1 + (float)Math.Sin(t * 1.6245f + (float)Math.Cos(t * 0.254f) * 0.3f)) * 0.5f;
			samples[5] = (1 + (float)Math.Sin(t * 0.345f + (float)Math.Cos(t * 0.03f) * 0.6f)) * 0.5f;

			for (i = 0; i < 6; i++)
			{
				sx[i] = x + i * dx;
				sy[i] = y + h * samples[i] * 0.8f;
			}

			// Graph background
			bg = NanoVG.nvgLinearGradient(vg, x, y, x, y + h, 
				NanoVG.nvgRGBA(0, 160, 192, 0), NanoVG.nvgRGBA(0, 160, 192, 64));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgMoveTo(vg, sx[0], sy[0]);
			for (i = 1; i < 6; i++)
				NanoVG.nvgBezierTo(vg, sx[i - 1] + dx * 0.5f, sy[i - 1], sx[i] - dx * 0.5f, sy[i], sx[i], sy[i]);
			NanoVG.nvgLineTo(vg, x + w, y + h);
			NanoVG.nvgLineTo(vg, x, y + h);
			NanoVG.nvgFillPaint(vg, bg);
			NanoVG.nvgFill(vg);

			// Graph line
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgMoveTo(vg, sx[0], sy[0] + 2);
			for (i = 1; i < 6; i++)
				NanoVG.nvgBezierTo(vg, sx[i - 1] + dx * 0.5f, sy[i - 1] + 2, sx[i] - dx * 0.5f, sy[i] + 2, sx[i], sy[i] + 2);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 32));
			NanoVG.nvgStrokeWidth(vg, 3.0f);
			NanoVG.nvgStroke(vg);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgMoveTo(vg, sx[0], sy[0]);
			for (i = 1; i < 6; i++)
				NanoVG.nvgBezierTo(vg, sx[i - 1] + dx * 0.5f, sy[i - 1], sx[i] - dx * 0.5f, sy[i], sx[i], sy[i]);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 160, 192, 255));
			NanoVG.nvgStrokeWidth(vg, 3.0f);
			NanoVG.nvgStroke(vg);

			// Graph sample pos
			for (i = 0; i < 6; i++)
			{
				bg = NanoVG.nvgRadialGradient(vg, sx[i], sy[i] + 2, 3.0f, 8.0f, 
					NanoVG.nvgRGBA(0, 0, 0, 32), NanoVG.nvgRGBA(0, 0, 0, 0));
				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgRect(vg, sx[i] - 10, sy[i] - 10 + 2, 20, 20);
				NanoVG.nvgFillPaint(vg, bg);
				NanoVG.nvgFill(vg);
			}

			NanoVG.nvgBeginPath(vg);
			for (i = 0; i < 6; i++)
				NanoVG.nvgCircle(vg, sx[i], sy[i], 4.0f);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(0, 160, 192, 255));
			NanoVG.nvgFill(vg);
			NanoVG.nvgBeginPath(vg);
			for (i = 0; i < 6; i++)
				NanoVG.nvgCircle(vg, sx[i], sy[i], 2.0f);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(220, 220, 220, 255));
			NanoVG.nvgFill(vg);

			NanoVG.nvgStrokeWidth(vg, 1.0f);
		}

		void drawColorwheel(NVGcontext vg, float x, float y, float w, float h, float t)
		{
			int i;
			float r0, r1, ax, ay, bx, by, cx, cy, aeps, r;
			float hue = (float)Math.Sin(t * 0.12f);
			NVGpaint paint;

			NanoVG.nvgSave(vg);

			/*	nvgBeginPath(vg);
			nvgRect(vg, x,y,w,h);
			nvgFillColor(vg, nvgRGBA(255,0,0,128));
			nvgFill(vg);*/

			cx = x + w * 0.5f;
			cy = y + h * 0.5f;
			r1 = (w < h ? w : h) * 0.5f - 5.0f;
			r0 = r1 - 20.0f;
			aeps = 0.5f / r1;	// half a pixel arc length in radians (2pi cancels out).

			for (i = 0; i < 6; i++)
			{
				float a0 = (float)i / 6.0f * NanoVG.NVG_PI * 2.0f - aeps;
				float a1 = (float)(i + 1.0f) / 6.0f * NanoVG.NVG_PI * 2.0f + aeps;
				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgArc(vg, cx, cy, r0, a0, a1, (int)NVGwinding.NVG_CW);
				NanoVG.nvgArc(vg, cx, cy, r1, a1, a0, (int)NVGwinding.NVG_CCW);
				NanoVG.nvgClosePath(vg);
				ax = cx + (float)Math.Cos(a0) * (r0 + r1) * 0.5f;
				ay = cy + (float)Math.Sin(a0) * (r0 + r1) * 0.5f;
				bx = cx + (float)Math.Cos(a1) * (r0 + r1) * 0.5f;
				by = cy + (float)Math.Sin(a1) * (r0 + r1) * 0.5f;
				paint = NanoVG.nvgLinearGradient(vg, ax, ay, bx, by, 
					NanoVG.nvgHSLA(a0 / (NanoVG.NVG_PI * 2), 1.0f, 0.55f, 255), 
					NanoVG.nvgHSLA(a1 / (NanoVG.NVG_PI * 2), 1.0f, 0.55f, 255));
				NanoVG.nvgFillPaint(vg, paint);
				NanoVG.nvgFill(vg);
			}

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgCircle(vg, cx, cy, r0 - 0.5f);
			NanoVG.nvgCircle(vg, cx, cy, r1 + 0.5f);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 64));
			NanoVG.nvgStrokeWidth(vg, 1.0f);
			NanoVG.nvgStroke(vg);

			// Selector
			NanoVG.nvgSave(vg);
			NanoVG.nvgTranslate(vg, cx, cy);
			NanoVG.nvgRotate(vg, hue * NanoVG.NVG_PI * 2);

			// Marker on
			NanoVG.nvgStrokeWidth(vg, 2.0f);
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, r0 - 1, -3, r1 - r0 + 2, 6);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(255, 255, 255, 192));
			NanoVG.nvgStroke(vg);

			paint = NanoVG.nvgBoxGradient(vg, r0 - 3, -5, r1 - r0 + 6, 10, 2, 4, 
				NanoVG.nvgRGBA(0, 0, 0, 128), NanoVG.nvgRGBA(0, 0, 0, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, r0 - 2 - 10, -4 - 10, r1 - r0 + 4 + 20, 8 + 20);
			NanoVG.nvgRect(vg, r0 - 2, -4, r1 - r0 + 4, 8);
			NanoVG.nvgPathWinding(vg, (int)NVGsolidity.NVG_HOLE);
			NanoVG.nvgFillPaint(vg, paint);
			NanoVG.nvgFill(vg);

			// Center triangle
			r = r0 - 6;
			ax = (float)Math.Cos(120.0f / 180.0f * NanoVG.NVG_PI) * r;
			ay = (float)Math.Sin(120.0f / 180.0f * NanoVG.NVG_PI) * r;
			bx = (float)Math.Cos(-120.0f / 180.0f * NanoVG.NVG_PI) * r;
			by = (float)Math.Sin(-120.0f / 180.0f * NanoVG.NVG_PI) * r;
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgMoveTo(vg, r, 0);
			NanoVG.nvgLineTo(vg, ax, ay);
			NanoVG.nvgLineTo(vg, bx, by);
			NanoVG.nvgClosePath(vg);
			paint = NanoVG.nvgLinearGradient(vg, r, 0, ax, ay, 
				NanoVG.nvgHSLA(hue, 1.0f, 0.5f, 255), NanoVG.nvgRGBA(255, 255, 255, 255));
			NanoVG.nvgFillPaint(vg, paint);
			NanoVG.nvgFill(vg);
			paint = NanoVG.nvgLinearGradient(vg, (r + ax) * 0.5f, (0 + ay) * 0.5f, bx, by, 
				NanoVG.nvgRGBA(0, 0, 0, 0), NanoVG.nvgRGBA(0, 0, 0, 255));
			NanoVG.nvgFillPaint(vg, paint);
			NanoVG.nvgFill(vg);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(0, 0, 0, 64));
			NanoVG.nvgStroke(vg);

			// Select circle on triangle
			ax = (float)Math.Cos(120.0f / 180.0f * NanoVG.NVG_PI) * r * 0.3f;
			ay = (float)Math.Sin(120.0f / 180.0f * NanoVG.NVG_PI) * r * 0.4f;
			NanoVG.nvgStrokeWidth(vg, 2.0f);
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgCircle(vg, ax, ay, 5);
			NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(255, 255, 255, 192));
			NanoVG.nvgStroke(vg);

			paint = NanoVG.nvgRadialGradient(vg, ax, ay, 7, 9, 
				NanoVG.nvgRGBA(0, 0, 0, 64), NanoVG.nvgRGBA(0, 0, 0, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, ax - 20, ay - 20, 40, 40);
			NanoVG.nvgCircle(vg, ax, ay, 7);
			NanoVG.nvgPathWinding(vg, (int)NVGsolidity.NVG_HOLE);
			NanoVG.nvgFillPaint(vg, paint);
			NanoVG.nvgFill(vg);

			NanoVG.nvgRestore(vg);

			NanoVG.nvgRestore(vg);
		}

		static float clampf(float a, float mn, float mx)
		{ 
			return a < mn ? mn : (a > mx ? mx : a); 
		}


		static void drawParagraph(NVGcontext vg, float x, float y, float width, float height, float mx, float my)
		{
			NVGtextRow[] rows = new NVGtextRow[3];
			NVGglyphPosition[] glyphs = new NVGglyphPosition[100];
			string text = "This is longer chunk of text.\n  \n  Would have used lorem ipsum but she    was busy jumping over the lazy dog with the fox and all the men who came to the aid of the party.🎉";
			int start;
			//int end;
			int nrows, i, nglyphs, j, lnum = 0;
			float lineh = 0;
			float caretx, px;
			float[] bounds = new float[4];
			float a;
			float gx = 10, gy = 10;
			int gutter = 0;
			//NVG_NOTUSED(height);
			float fnull = 0;
			string textShow = "Hover your mouse over the text to see calculated caret position.";

			NanoVG.nvgSave(vg);

			NanoVG.nvgFontSize(vg, 18.0f);
			NanoVG.nvgFontFace(vg, "sans");
			NanoVG.nvgTextAlign(vg, (int)NVGalign.NVG_ALIGN_LEFT | (int)NVGalign.NVG_ALIGN_TOP);
			NanoVG.nvgTextMetrics(vg, ref fnull, ref fnull, ref lineh);

			// The text break API can be used to fill a large buffer of rows,
			// or to iterate over the text just few lines (or just one) at a time.
			// The "next" variable of the last returned item tells where to continue.
			//start = text;
			start = 0;
			//end = text + strlen(text);
			//end = text.Length - 1;

			while ((nrows = NanoVG.nvgTextBreakLines(vg, text, width, rows, 3)) > 0)
			{
				for (i = 0; i < nrows; i++)
				{
					NVGtextRow row = rows[i];
					int hit = mx > x && mx < (x + width) && my >= y && my < (y + lineh) ? 1 : 0;

					NanoVG.nvgBeginPath(vg);
					NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, (byte)(hit != 0 ? 64 : 16)));
					NanoVG.nvgRect(vg, x, y, row.width, lineh);
					NanoVG.nvgFill(vg);

					NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 255, 255, 255));
					string subStr1 = text.Substring(row.start, row.end - row.start);
					NanoVG.nvgText(vg, x, y, subStr1);

					if (hit != 0)
					{
						caretx = (mx < x + row.width / 2) ? x : x + row.width;
						px = x;
						string str = text.Substring(row.start, row.end - row.start);
						nglyphs = NanoVG.nvgTextGlyphPositions(vg, x, y, str, glyphs, 100);
						for (j = 0; j < nglyphs; j++)
						{
							float x0 = glyphs[j].x;
							float x1 = (j + 1 < nglyphs) ? glyphs[j + 1].x : x + row.width;
							float gx1 = x0 * 0.3f + x1 * 0.7f;
							if (mx >= px && mx < gx1)
								caretx = glyphs[j].x;
							px = gx1;
						}
						NanoVG.nvgBeginPath(vg);
						NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 192, 0, 255));
						NanoVG.nvgRect(vg, caretx, y, 1, lineh);
						NanoVG.nvgFill(vg);

						gutter = lnum + 1;
						gx = x - 10;
						gy = y + lineh / 2;
					}
					lnum++;
					y += lineh;
				}
				// Keep going...
				start = rows[nrows - 1].next;
				int numChars = text.Length - start - 1;

				if (numChars > 0)
					text = text.Substring(start);
				else
					break;
			}

			if (gutter != 0)
			{
				//char[] txt = new char[16];
				string txt = String.Format("{0}", gutter);
				//snprintf(txt, sizeof(txt), "%d", gutter);
				NanoVG.nvgFontSize(vg, 13.0f);
				NanoVG.nvgTextAlign(vg, (int)NVGalign.NVG_ALIGN_RIGHT | (int)NVGalign.NVG_ALIGN_MIDDLE);

				NanoVG.nvgTextBounds(vg, gx, gy, txt, bounds);

				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(255, 192, 0, 255));
				NanoVG.nvgRoundedRect(vg, (int)bounds[0] - 4, (int)bounds[1] - 2, 
					(int)(bounds[2] - bounds[0]) + 8, 
					(int)(bounds[3] - bounds[1]) + 4, 
					((int)(bounds[3] - bounds[1]) + 4) / 2 - 1);
				NanoVG.nvgFill(vg);

				NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(32, 32, 32, 255));
				NanoVG.nvgText(vg, gx, gy, txt);
			}

			y += 20.0f;

			NanoVG.nvgFontSize(vg, 13.0f);
			NanoVG.nvgTextAlign(vg, (int)NVGalign.NVG_ALIGN_LEFT | (int)NVGalign.NVG_ALIGN_TOP);
			NanoVG.nvgTextLineHeight(vg, 1.2f);

			NanoVG.nvgTextBoxBounds(vg, x, y, 150, textShow, bounds);

			// Fade the tooltip out when close to it.
			gx = (float)Math.Abs((mx - (bounds[0] + bounds[2]) * 0.5f) / (bounds[0] - bounds[2]));
			gy = (float)Math.Abs((my - (bounds[1] + bounds[3]) * 0.5f) / (bounds[1] - bounds[3]));
			a = (float)Math.Max(gx, gy) - 0.5f;
			a = clampf(a, 0, 1);
			NanoVG.nvgGlobalAlpha(vg, a);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(220, 220, 220, 255));
			NanoVG.nvgRoundedRect(vg, bounds[0] - 2, bounds[1] - 2, (int)(bounds[2] - bounds[0]) + 4, (int)(bounds[3] - bounds[1]) + 4, 3);
			px = (int)((bounds[2] + bounds[0]) / 2);
			NanoVG.nvgMoveTo(vg, px, bounds[1] - 10);
			NanoVG.nvgLineTo(vg, px + 7, bounds[1] + 1);
			NanoVG.nvgLineTo(vg, px - 7, bounds[1] + 1);
			NanoVG.nvgFill(vg);

			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(0, 0, 0, 220));
			NanoVG.nvgTextBox(vg, x, y, 150, textShow);

			NanoVG.nvgRestore(vg);
		}
			
		static void drawSpinner(NVGcontext vg, float cx, float cy, float r, float t)
		{
			float a0 = 0.0f + t * 6;
			float a1 = NanoVG.NVG_PI + t * 6;
			float r0 = r;
			float r1 = r * 0.75f;
			float ax, ay, bx, by;
			NVGpaint paint;

			NanoVG.nvgSave(vg);

			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgArc(vg, cx, cy, r0, a0, a1, (int)NVGwinding.NVG_CW);
			NanoVG.nvgArc(vg, cx, cy, r1, a1, a0, (int)NVGwinding.NVG_CCW);
			NanoVG.nvgClosePath(vg);
			ax = cx + (float)Math.Cos(a0) * (r0 + r1) * 0.5f;
			ay = cy + (float)Math.Sin(a0) * (r0 + r1) * 0.5f;
			bx = cx + (float)Math.Cos(a1) * (r0 + r1) * 0.5f;
			by = cy + (float)Math.Sin(a1) * (r0 + r1) * 0.5f;
			paint = NanoVG.nvgLinearGradient(vg, ax, ay, bx, by, 
				NanoVG.nvgRGBA(0, 0, 0, 0), NanoVG.nvgRGBA(0, 0, 0, 128));
			NanoVG.nvgFillPaint(vg, paint);
			NanoVG.nvgFill(vg);

			NanoVG.nvgRestore(vg);
		}

		static void drawThumbnails(NVGcontext vg, float x, float y, float w, float h, int[] images, int nimages, float t)
		{
			float cornerRadius = 3.0f;
			NVGpaint shadowPaint, imgPaint, fadePaint;
			float ix, iy, iw, ih;
			float thumb = 60.0f;
			float arry = 30.5f;
			int imgw = 0, imgh = 0;
			float stackh = (nimages / 2) * (thumb + 10) + 10;
			int i;
			float u = (1 + (float)Math.Cos(t * 0.5f)) * 0.5f;
			float u2 = (1 - (float)Math.Cos(t * 0.2f)) * 0.5f;
			float scrollh, dv;

			NanoVG.nvgSave(vg);
			//	nvgClearState(vg);

			// Drop shadow
			shadowPaint = NanoVG.nvgBoxGradient(vg, x, y + 4, w, h, cornerRadius * 2, 20, 
				NanoVG.nvgRGBA(0, 0, 0, 128), NanoVG.nvgRGBA(0, 0, 0, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, x - 10, y - 10, w + 20, h + 30);
			NanoVG.nvgRoundedRect(vg, x, y, w, h, cornerRadius);
			NanoVG.nvgPathWinding(vg, (int)NVGsolidity.NVG_HOLE);
			NanoVG.nvgFillPaint(vg, shadowPaint);
			NanoVG.nvgFill(vg);

			// Window
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x, y, w, h, cornerRadius);
			NanoVG.nvgMoveTo(vg, x - 10, y + arry);
			NanoVG.nvgLineTo(vg, x + 1, y + arry - 11);
			NanoVG.nvgLineTo(vg, x + 1, y + arry + 11);
			NanoVG.nvgFillColor(vg, NanoVG.nvgRGBA(200, 200, 200, 255));
			NanoVG.nvgFill(vg);

			NanoVG.nvgSave(vg);
			NanoVG.nvgScissor(vg, x, y, w, h);
			NanoVG.nvgTranslate(vg, 0, -(stackh - h) * u);

			dv = 1.0f / (float)(nimages - 1);

			for (i = 0; i < nimages; i++)
			{
				float tx, ty, v, a;
				tx = x + 10;
				ty = y + 10;
				tx += (i % 2) * (thumb + 10);
				ty += (i / 2) * (thumb + 10);
				NanoVG.nvgImageSize(vg, images[i], ref imgw, ref imgh);
				if (imgw < imgh)
				{
					iw = thumb;
					ih = iw * (float)imgh / (float)imgw;
					ix = 0;
					iy = -(ih - thumb) * 0.5f;
				}
				else
				{
					ih = thumb;
					iw = ih * (float)imgw / (float)imgh;
					ix = -(iw - thumb) * 0.5f;
					iy = 0;
				}

				v = i * dv;
				a = clampf((u2 - v) / dv, 0, 1);

				if (a < 1.0f)
					drawSpinner(vg, tx + thumb / 2, ty + thumb / 2, thumb * 0.25f, t);

				imgPaint = NanoVG.nvgImagePattern(vg, tx + ix, ty + iy, iw, ih, 0.0f / 180.0f * NanoVG.NVG_PI, images[i], a);
				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgRoundedRect(vg, tx, ty, thumb, thumb, 5);
				NanoVG.nvgFillPaint(vg, imgPaint);
				NanoVG.nvgFill(vg);

				shadowPaint = NanoVG.nvgBoxGradient(vg, tx - 1, ty, thumb + 2, thumb + 2, 5, 3, 
					NanoVG.nvgRGBA(0, 0, 0, 128), NanoVG.nvgRGBA(0, 0, 0, 0));
				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgRect(vg, tx - 5, ty - 5, thumb + 10, thumb + 10);
				NanoVG.nvgRoundedRect(vg, tx, ty, thumb, thumb, 6);
				NanoVG.nvgPathWinding(vg, (int)NVGsolidity.NVG_HOLE);
				NanoVG.nvgFillPaint(vg, shadowPaint);
				NanoVG.nvgFill(vg);

				NanoVG.nvgBeginPath(vg);
				NanoVG.nvgRoundedRect(vg, tx + 0.5f, ty + 0.5f, thumb - 1, thumb - 1, 4 - 0.5f);
				NanoVG.nvgStrokeWidth(vg, 1.0f);
				NanoVG.nvgStrokeColor(vg, NanoVG.nvgRGBA(255, 255, 255, 192));
				NanoVG.nvgStroke(vg);
			}
			NanoVG.nvgRestore(vg);

			// Hide fades
			fadePaint = NanoVG.nvgLinearGradient(vg, x, y, x, y + 6, 
				NanoVG.nvgRGBA(200, 200, 200, 255), NanoVG.nvgRGBA(200, 200, 200, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, x + 4, y, w - 8, 6);
			NanoVG.nvgFillPaint(vg, fadePaint);
			NanoVG.nvgFill(vg);

			fadePaint = NanoVG.nvgLinearGradient(vg, x, y + h, x, y + h - 6, 
				NanoVG.nvgRGBA(200, 200, 200, 255), NanoVG.nvgRGBA(200, 200, 200, 0));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRect(vg, x + 4, y + h - 6, w - 8, 6);
			NanoVG.nvgFillPaint(vg, fadePaint);
			NanoVG.nvgFill(vg);

			// Scroll bar
			shadowPaint = NanoVG.nvgBoxGradient(vg, x + w - 12 + 1, y + 4 + 1, 8, h - 8, 3, 4, 
				NanoVG.nvgRGBA(0, 0, 0, 32), NanoVG.nvgRGBA(0, 0, 0, 92));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + w - 12, y + 4, 8, h - 8, 3);
			NanoVG.nvgFillPaint(vg, shadowPaint);
			//	nvgFillColor(vg, nvgRGBA(255,0,0,128));
			NanoVG.nvgFill(vg);

			scrollh = (h / stackh) * (h - 8);
			shadowPaint = NanoVG.nvgBoxGradient(vg, x + w - 12 - 1, y + 4 + (h - 8 - scrollh) * u - 1, 8, scrollh, 3, 4, 
				NanoVG.nvgRGBA(220, 220, 220, 255), NanoVG.nvgRGBA(128, 128, 128, 255));
			NanoVG.nvgBeginPath(vg);
			NanoVG.nvgRoundedRect(vg, x + w - 12 + 1, y + 4 + 1 + (h - 8 - scrollh) * u, 8 - 2, scrollh - 2, 2);
			NanoVG.nvgFillPaint(vg, shadowPaint);
			//	nvgFillColor(vg, nvgRGBA(0,0,0,128));
			NanoVG.nvgFill(vg);

			NanoVG.nvgRestore(vg);
		}

		#endregion Demo-Widgets

		#region Keyboard_KeyDown

		/// <summary>
		/// Occurs when a key is pressed.
		/// </summary>
		/// <param name="sender">The KeyboardDevice which generated this event.</param>
		/// <param name="e">The key that was pressed.</param>
		void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				this.Exit();

			if (e.Key == Key.F12)
			if (this.WindowState == WindowState.Fullscreen)
				this.WindowState = WindowState.Normal;
			else
				this.WindowState = WindowState.Fullscreen;
		}

		#endregion

		#region OnLoad

		bool loadDemoData(NVGcontext vg, DemoData data)
		{
			for (int i = 0; i < 12; i++)
			{
				string file = String.Format("demo-data/images/image{0}.jpg", i + 1);
				//snprintf(file, 128, "../example/images/image%d.jpg", i+1);
				data.images[i] = NanoVG.nvgCreateImage(ref vg, file, 0);
				if (data.images[i] == 0)
				{
					System.Console.WriteLine(String.Format("Could not load {0}.\n", file));
					return false;
				}
			}

			data.fontIcons = NanoVG.nvgCreateFont(vg, "icons", "demo-data/entypo.ttf");
			if (data.fontIcons == -1)
			{
				System.Console.WriteLine("Could not add font icons.\n");
				return false;
			}
			data.fontNormal = NanoVG.nvgCreateFont(vg, "sans", "demo-data/Roboto-Regular.ttf");
			if (data.fontNormal == -1)
			{
				System.Console.WriteLine("Could not add font italic.\n");
				return false;
			}
			data.fontBold = NanoVG.nvgCreateFont(vg, "sans-bold", "demo-data/Roboto-Bold.ttf");
			if (data.fontBold == -1)
			{
				System.Console.WriteLine("Could not add font bold.\n");
				return false;
			}
			data.fontEmoji = NanoVG.nvgCreateFont(vg, "emoji", "demo-data/NotoEmoji-Regular.ttf");
			if (data.fontEmoji == -1)
			{
				System.Console.WriteLine("Could not add font emoji.\n");
				return false;
			}
			NanoVG.nvgAddFallbackFontId(vg, data.fontNormal, data.fontEmoji);
			NanoVG.nvgAddFallbackFontId(vg, data.fontBold, data.fontEmoji);

			return true;
		}

		/// <summary>
		/// Setup OpenGL and load resources here.
		/// </summary>
		/// <param name="e">Not used.</param>
		protected override void OnLoad(EventArgs e)
		{
			string wi = this.WindowInfo.ToString();
			this.Title = "OpenTK-NanoVG_GL2 - " + wi;

			GlNanoVG.nvgCreateGL(ref vg, (int)NVGcreateFlags.NVG_ANTIALIAS |
				(int)NVGcreateFlags.NVG_STENCIL_STROKES |
				(int)NVGcreateFlags.NVG_DEBUG);

			if (!loadDemoData(vg, data))
			{
				freeDemoData(vg, data);
				Console.Error.WriteLine("ERROR: Failed to load demo data, shutting down");
				Environment.Exit(1);
			}

			PerfGraph.InitGraph((int)GraphrenderStyle.GRAPH_RENDER_FPS, "Frame Time");
		}

		#endregion

		#region OnResize

		/// <summary>
		/// Respond to resize events here.
		/// </summary>
		/// <param name="e">Contains information on the new GameWindow size.</param>
		/// <remarks>There is no need to call the base implementation.</remarks>
		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(0, 0, Width, Height);

			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
		}

		#endregion

		#region OnUpdateFrame

		/// <summary>
		/// Add your game logic here.
		/// </summary>
		/// <param name="e">Contains timing information.</param>
		/// <remarks>There is no need to call the base implementation.</remarks>
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			// Nothing to do!
		}

		#endregion

		int mx = 0, my = 0;
		Point mp = new Point();
		double prevt = 0;
		float at = 0f;

		#region OnRenderFrame

		/// <summary>
		/// Add your game rendering code here.
		/// </summary>
		/// <param name="e">Contains timing information.</param>
		/// <remarks>There is no need to call the base implementation.</remarks>
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			float t, dt;
			int blowup = 0;

			t = (float)e.Time;
			at += t;
			dt = (float)(at - prevt);
			prevt = at;

			MouseState ms = Mouse.GetCursorState();
			mp.X = ms.X;
			mp.Y = ms.Y;
			Point mpc = this.PointToClient(mp);

			mx = mpc.X;
			my = mpc.Y;

			PerfGraph.UpdateGraph(dt);

			//glfwGetFramebufferSize(window, &fbWidth, &fbHeight);

			// Calculate pixel ration for hi-dpi devices.
			//pxRatio = (float)fbWidth / (float)winWidth;
			int pxRatio = 1;

			// Update and render
			GL.Viewport(0, 0, Width, Height);
			if (premult != 0)
				GL.ClearColor(0f, 0f, 0f, 0f);
			else
				GL.ClearColor(0.3f, 0.3f, 0.32f, 1.0f);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			NanoVG.nvgBeginFrame(vg, Width, Height, pxRatio);

			renderDemo(vg, mx, my, Width, Height, at, blowup, data);
			PerfGraph.RenderGraph(vg, 5, 5);

			NanoVG.nvgEndFrame(vg);

			this.SwapBuffers();
		}

		#endregion

		#region DEMO

		void renderDemo(NVGcontext vg, float mx, float my, float width, float height,
		                float t, int blowup, DemoData data)
		{
			float x, y;
			float popy;
			
			drawEyes(vg, width - 250, 50, 150, 100, mx, my, t);

			drawParagraph(vg, width - 450, 50, 150, 100, mx, my);
			drawGraph(vg, 0, height / 2, width, height / 2, t);
			drawColorwheel(vg, width - 300, height - 300, 250.0f, 250.0f, t);

			// Line joints
			drawLines(vg, 120, height - 50, 600, 50, t);
			//drawLines(vg, 120, height - 50, 600, 50, 4);

			// Line caps
			drawWidths(vg, 10, 50, 30);

			// Line caps
			drawCaps(vg, 10, 300, 30);

			drawScissor(vg, 50, height - 80, t);

			NanoVG.nvgSave(vg);
			if (blowup != 0)
			{
				NanoVG.nvgRotate(vg, (float)Math.Sin(t * 0.3f) * 5.0f / 180.0f * NanoVG.NVG_PI);
				NanoVG.nvgScale(vg, 2.0f, 2.0f);
			}

			// Widgets
			drawWindow(vg, "Widgets `n Stuff", 50, 50, 300, 400);
			x = 60;
			y = 95;
			drawSearchBox(vg, "Search", x, y, 280, 25);
			y += 40;
			drawDropDown(vg, "Effects", x, y, 280, 28);
			popy = y + 14;
			y += 45;

			// Form
			drawLabel(vg, "Login", x, y, 280, 20);
			y += 25;
			drawEditBox(vg, "Email", x, y, 280, 28);
			y += 35;
			drawEditBox(vg, "Password", x, y, 280, 28);
			y += 38;
			drawCheckBox(vg, "Remember me", x, y, 140, 28);
			drawButton(vg, GlNanoVG.ICON_LOGIN, "Sign in", x + 138, y, 140, 28, NanoVG.nvgRGBA(0, 96, 128, 255));
			y += 45;

			// Slider
			drawLabel(vg, "Diameter", x, y, 280, 20);
			y += 25;
			drawEditBoxNum(vg, "123.00", "px", x + 180, y, 100, 28);
			drawSlider(vg, 0.4f, x, y, 170, 28);
			y += 55;

			drawButton(vg, GlNanoVG.ICON_TRASH, "Delete", x, y, 160, 28, NanoVG.nvgRGBA(128, 16, 8, 255));
			drawButton(vg, 0, "Cancel", x + 170, y, 110, 28, NanoVG.nvgRGBA(0, 0, 0, 0));

			// Thumbnails box
			drawThumbnails(vg, 365, popy - 30, 160, 300, data.images, 12, t);

			NanoVG.nvgRestore(vg);
		}

		#endregion DEMO

		static void freeDemoData(NVGcontext vg, DemoData data)
		{
			int i;

			if (vg == null)
				return;

			for (i = 0; i < 12; i++)
				NanoVG.nvgDeleteImage(vg, data.images[i]);
		}

		[STAThread]
		public static void Main(string[] args)
		{
			ToolkitOptions tkOptions = new ToolkitOptions();
			// En Linux PlatformBackend.Default equivale a SDL2 si está presente la librería
			tkOptions.Backend = PlatformBackend.PreferNative;
			Toolkit.Init(tkOptions);

			int aa = GetMaxAntiAliasingAvailable(2);
			GraphicsMode gm = new GraphicsMode(32, 16, 8, aa);

			using (GLWindow glWin = new GLWindow(gm))
			{
				try
				{
					glWin.Run();
				}
				finally
				{
					freeDemoData(glWin.vg, glWin.data);
				}
			}
		}
	}
}
