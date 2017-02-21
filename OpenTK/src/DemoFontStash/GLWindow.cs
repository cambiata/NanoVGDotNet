
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
 * Port to C#
 * Copyright (c) 2016 Miguel A. Guirado L. https://sites.google.com/site/bitiopia/
 * 
 * 	FontStash.net is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  any later version.
 *
 *  FontStash.net is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with FontStash.net  If not, see <http://www.gnu.org/licenses/>. See
 *  the file lgpl-3.0.txt for more details.
*/

using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using FontStashDotNet;

namespace DemoFontStash
{
	class GLWindow : GameWindow
	{
		FONScontext fs;
		int fontNormal;
		int fontItalic;
		int fontBold;
		int fontJapanese;

		public GLWindow() : base(1336, 768)
		{
			KeyDown += Keyboard_KeyDown;
		}

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

		/// <summary>
		/// Setup OpenGL and load resources here.
		/// </summary>
		/// <param name="e">Not used.</param>
		protected override void OnLoad(EventArgs e)
		{
			this.Title = "OpenTK-FontStash";

			GL.ClearColor(Color.MidnightBlue);

			fontNormal = FontStash.FONS_INVALID;

			fs = GlFontStash.glfonsCreate(512, 512, FONSflags.FONS_ZERO_TOPLEFT);

			fontNormal = FontStash.fonsAddFont(fs, "sans", "DroidSerif-Regular.ttf");
			if (fontNormal == FontStash.FONS_INVALID)
			{
				throw new Exception("Could not add font normal.\n");
			}

			fontItalic = FontStash.fonsAddFont(fs, "sans-italic", "DroidSerif-Italic.ttf");
			if (fontItalic == FontStash.FONS_INVALID)
			{
				throw new Exception("Could not add font italic.\n");
			}
			fontBold = FontStash.fonsAddFont(fs, "sans-bold", "DroidSerif-Bold.ttf");
			if (fontBold == FontStash.FONS_INVALID)
			{
				throw new Exception("Could not add font bold.\n");
			}
			fontJapanese = FontStash.fonsAddFont(fs, "sans-jp", "DroidSansJapanese.ttf");
			if (fontJapanese == FontStash.FONS_INVALID)
			{
				throw new Exception("Could not add font japanese.\n");
			}
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

		#region OnRenderFrame

		/// <summary>
		/// Add your game rendering code here.
		/// </summary>
		/// <param name="e">Contains timing information.</param>
		/// <remarks>There is no need to call the base implementation.</remarks>
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			/*GL.Clear(ClearBufferMask.ColorBufferBit);

			GL.Begin(PrimitiveType.Triangles);

			GL.Color3(Color.MidnightBlue);
			GL.Vertex2(-1.0f, 1.0f);
			GL.Color3(Color.SpringGreen);
			GL.Vertex2(0.0f, -1.0f);
			GL.Color3(Color.Ivory);
			GL.Vertex2(1.0f, 1.0f);

			GL.End();*/

			float sx, sy, dx, dy, lh = 0, pf1 = 0, pf2 = 0;
			//int width, height;
			uint white, black, brown, blue;
			//glfwGetFramebufferSize(window, &width, &height);
			// Update and render
			GL.Viewport(0, 0, Width, Height);
			GL.ClearColor(0.3f, 0.3f, 0.32f, 1.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			//GL.Enable(EnableCap.Blend);
			//GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			//GL.Disable(EnableCap.Texture2D);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, Width, Height, 0, -1, 1);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			GL.Disable(EnableCap.DepthTest);
			//GL.Color4(255f, 255f, 255f, 255f);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.Enable(EnableCap.CullFace);

			white = GlFontStash.glfonsRGBA(255, 255, 255, 255);
			brown = GlFontStash.glfonsRGBA(192, 128, 0, 128);
			blue = GlFontStash.glfonsRGBA(0, 192, 255, 255);
			black = GlFontStash.glfonsRGBA(0, 0, 0, 255);

			sx = 50;
			sy = 50;

			dx = sx;
			dy = sy;

			dash(dx, dy);

			FontStash.fonsClearState(ref fs);

			FontStash.fonsSetSize(ref fs, 124.0f);
			FontStash.fonsSetFont(ref fs, fontNormal);
			pf1 = 0;
			pf2 = 0;
			FontStash.fonsVertMetrics(ref fs, ref pf1, ref pf2, ref lh);
			dx = sx;
			dy += lh;
			dash(dx, dy);

			FontStash.fonsSetSize(ref fs, 124.0f);
			FontStash.fonsSetFont(ref fs, fontNormal);
			FontStash.fonsSetColor(ref fs, white);
			dx = FontStash.fonsDrawText(ref fs, dx, dy, "The quick ");

			FontStash.fonsSetSize(ref fs, 48.0f);
			FontStash.fonsSetFont(ref fs, fontItalic);
			FontStash.fonsSetColor(ref fs, brown);
			dx = FontStash.fonsDrawText(ref fs, dx,dy,"brown ");

			FontStash.fonsSetSize(ref fs, 24.0f);
			FontStash.fonsSetFont(ref fs, fontNormal);
			FontStash.fonsSetColor(ref fs, white);
			dx = FontStash.fonsDrawText(ref fs, dx, dy, "fox ");

			FontStash.fonsVertMetrics(ref fs, ref pf1, ref pf2, ref lh);
			dx = sx;
			dy += lh*1.2f;
			dash(dx,dy);
			FontStash.fonsSetFont(ref fs, fontItalic);
			dx = FontStash.fonsDrawText(ref fs, dx,dy,"jumps over ");
			FontStash.fonsSetFont(ref fs, fontBold);
			dx = FontStash.fonsDrawText(ref fs, dx,dy,"the lazy ");
			FontStash.fonsSetFont(ref fs, fontNormal);
			dx = FontStash.fonsDrawText(ref fs, dx,dy,"dog.");

			dx = sx;
			dy += lh * 1.2f;
			dash(dx, dy);
			FontStash.fonsSetSize(ref fs, 12.0f);
			FontStash.fonsSetFont(ref fs, fontNormal);
			FontStash.fonsSetColor(ref fs, blue);
			FontStash.fonsDrawText(ref fs, dx, dy, "Now is the time for all good men to come to the aid of the party.");

			FontStash.fonsVertMetrics(ref fs, ref pf1, ref pf2, ref lh);
			dx = sx;
			dy += lh*1.2f*2;
			dash(dx,dy);
			FontStash.fonsSetSize(ref fs, 18.0f);
			FontStash.fonsSetFont(ref fs, fontItalic);
			FontStash.fonsSetColor(ref fs, white);
			FontStash.fonsDrawText(ref fs, dx,dy,"Ég get etið gler án þess að meiða mig.");

			FontStash.fonsVertMetrics(ref fs, ref pf1, ref pf2, ref lh);
			dx = sx;
			dy += lh*1.2f;
			dash(dx,dy);
			FontStash.fonsSetFont(ref fs, fontJapanese);
			FontStash.fonsDrawText(ref fs, dx,dy,"私はガラスを食べられます。それは私を傷つけません。");

			// Font alignment
			FontStash.fonsSetSize(ref fs, 18.0f);
			FontStash.fonsSetFont(ref fs, fontNormal);
			FontStash.fonsSetColor(ref fs, white);

			dx = 50;
			dy = 350;
			line(dx - 10, dy, dx + 250, dy);
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_LEFT | FONSalign.FONS_ALIGN_TOP);
			dx = FontStash.fonsDrawText(ref fs, dx, dy, "Top");
			dx += 10;
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_LEFT | FONSalign.FONS_ALIGN_MIDDLE);
			dx = FontStash.fonsDrawText(ref fs, dx, dy, "Middle");
			dx += 10;
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_LEFT | FONSalign.FONS_ALIGN_BASELINE);
			dx = FontStash.fonsDrawText(ref fs, dx, dy, "Baseline");
			dx += 10;
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_LEFT | FONSalign.FONS_ALIGN_BOTTOM);
			FontStash.fonsDrawText(ref fs, dx, dy, "Bottom");

			dx = 150;
			dy = 400;
			line(dx, dy - 30, dx, dy + 80.0f);
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_LEFT | FONSalign.FONS_ALIGN_BASELINE);
			FontStash.fonsDrawText(ref fs, dx, dy, "Left");
			dy += 30;
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_CENTER | FONSalign.FONS_ALIGN_BASELINE);
			FontStash.fonsDrawText(ref fs, dx, dy, "Center");
			dy += 30;
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_RIGHT | FONSalign.FONS_ALIGN_BASELINE);
			FontStash.fonsDrawText(ref fs, dx, dy, "Right");

			// Blur
			dx = 500;
			dy = 350;
			FontStash.fonsSetAlign(fs, FONSalign.FONS_ALIGN_LEFT | FONSalign.FONS_ALIGN_BASELINE);

			FontStash.fonsSetSize(ref fs, 60.0f);
			FontStash.fonsSetFont(ref fs, fontItalic);
			FontStash.fonsSetColor(ref fs, white);
			FontStash.fonsSetSpacing(ref fs, 5.0f);
			FontStash.fonsSetBlur(ref fs, 10.0f);
			FontStash.fonsDrawText(ref fs, dx,dy,"Blurry...");

			dy += 50.0f;

			FontStash.fonsSetSize(ref fs, 18.0f);
			FontStash.fonsSetFont(ref fs, fontBold);
			FontStash.fonsSetColor(ref fs, black);
			FontStash.fonsSetSpacing(ref fs, 0.0f);
			FontStash.fonsSetBlur(ref fs, 3.0f);
			FontStash.fonsDrawText(ref fs, dx,dy+2,"DROP THAT SHADOW");

			FontStash.fonsSetColor(ref fs, white);
			FontStash.fonsSetBlur(ref fs, 0);
			FontStash.fonsDrawText(ref fs, dx,dy,"DROP THAT SHADOW");

			//if (debug)
			FontStash.fonsDrawDebug(fs, 800.0f, 50.0f);


			GL.Enable(EnableCap.DepthTest);

			this.SwapBuffers();
		}

		#endregion

		void dash(float dx, float dy)
		{
			GL.Begin(PrimitiveType.Lines);
			GL.Color4(0f, 0f, 0f, 128f);
			GL.Vertex2(dx - 5, dy);
			GL.Vertex2(dx - 10, dy);
			GL.End();
		}

		void line(float sx, float sy, float ex, float ey)
		{
			GL.Begin(PrimitiveType.Lines);
			GL.Color4(0f, 0f, 0f, 128f);
			GL.Vertex2(sx, sy);
			GL.Vertex2(ex, ey);
			GL.End();
		}

		[STAThread]
		public static void Main(string[] args)
		{
			using (GLWindow glWin = new GLWindow())
			{
				glWin.Run();
			}
		}
	}
}
