
using System;
using Pencil.Gaming;
using Pencil.Gaming.Graphics;
using FontStashnet;

namespace Demo1
{
	class Program
	{
		static FONScontext fs;
		static int fontNormal;
		static int fontItalic;
		static int fontBold;
		static int fontJapanese;
		static int width = 1336, height = 768;
		static bool debug = true;

		static void dash(float dx, float dy)
		{
			GL.Begin(BeginMode.Lines);
			GL.Color4(0f, 0f, 0f, 128f);
			GL.Vertex2(dx - 5, dy);
			GL.Vertex2(dx - 10, dy);
			GL.End();
		}

		static void line(float sx, float sy, float ex, float ey)
		{
			GL.Begin(BeginMode.Lines);
			GL.Color4(0f, 0f, 0f, 128f);
			GL.Vertex2(sx, sy);
			GL.Vertex2(ex, ey);
			GL.End();
		}

		static void key(GlfwWindowPtr window, Key key, int scanCode, KeyAction action, KeyModifiers mods)
		{
			if (key == Key.Escape && action == KeyAction.Press)
				Glfw.SetWindowShouldClose(window, true);
			if (key == Key.Space && action == KeyAction.Press)
				debug = !debug;
		}

		public static void Main(string[] args)
		{
			try
			{
				if (Glfw.Init() == false)
				{
					Console.Error.WriteLine("Failed to initialize GLFW!");
					Environment.Exit(1);
				}

				try
				{
					Glfw.SetErrorCallback((code, des) =>
						{
							Console.Error.WriteLine("ERROR ({0}): {1}", code, des);
						});

					// Create GLFW window
					GlfwWindowPtr window = Glfw.CreateWindow(width, height, "FontStash.net-Pencil.Gaming.GLFW3", GlfwMonitorPtr.Null, GlfwWindowPtr.Null);
					if (window.Equals(GlfwWindowPtr.Null))
					{ // Does this line actually work???
						Console.Error.WriteLine("ERROR: Failed to create GLFW window, shutting down");
						Environment.Exit(1);
					}

					Glfw.SetKeyCallback(window, key);
					// Enable the OpenGL context for the current window
					Glfw.MakeContextCurrent(window);
	
					//Glfw.SetWindowTitle(window, "This is a GLFW window!");

					Glfw.SwapInterval(1);
					Glfw.WindowHint(WindowHint.Samples, 2); // Turns on 2× mutlisampling

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


					Glfw.PollEvents(); // Get events
					while (!Glfw.WindowShouldClose(window))
					{
						// Poll GLFW window events
						Glfw.PollEvents();

						float sx, sy, dx, dy, lh = 0, pf1 = 0, pf2 = 0;
						uint white, black, brown, blue;

						Glfw.GetWindowSize(window, out width, out height);

						// Update and render
						GL.Viewport(0, 0, width, height);
						GL.ClearColor(0.3f, 0.3f, 0.32f, 1.0f);
						GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
						GL.Enable(EnableCap.Blend);
						GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
						GL.Disable(EnableCap.Texture2D);
						GL.MatrixMode(MatrixMode.Projection);
						GL.LoadIdentity();
						GL.Ortho(0, width, height, 0, -1, 1);

						GL.MatrixMode(MatrixMode.Modelview);
						GL.LoadIdentity();
						GL.Disable(EnableCap.DepthTest);
						GL.Color4(255f, 255f, 255f, 255f);

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
						dx = FontStash.fonsDrawText(ref fs, dx, dy, "brown ");

						FontStash.fonsSetSize(ref fs, 24.0f);
						FontStash.fonsSetFont(ref fs, fontNormal);
						FontStash.fonsSetColor(ref fs, white);
						dx = FontStash.fonsDrawText(ref fs, dx, dy, "fox ");

						FontStash.fonsVertMetrics(ref fs, ref pf1, ref pf2, ref lh);
						dx = sx;
						dy += lh * 1.2f;
						dash(dx, dy);
						FontStash.fonsSetFont(ref fs, fontItalic);
						dx = FontStash.fonsDrawText(ref fs, dx, dy, "jumps over ");
						FontStash.fonsSetFont(ref fs, fontBold);
						dx = FontStash.fonsDrawText(ref fs, dx, dy, "the lazy ");
						FontStash.fonsSetFont(ref fs, fontNormal);
						dx = FontStash.fonsDrawText(ref fs, dx, dy, "dog.");

						dx = sx;
						dy += lh * 1.2f;
						dash(dx, dy);
						FontStash.fonsSetSize(ref fs, 12.0f);
						FontStash.fonsSetFont(ref fs, fontNormal);
						FontStash.fonsSetColor(ref fs, blue);
						FontStash.fonsDrawText(ref fs, dx, dy, "Now is the time for all good men to come to the aid of the party.");

						FontStash.fonsVertMetrics(ref fs, ref pf1, ref pf2, ref lh);
						dx = sx;
						dy += lh * 1.2f * 2;
						dash(dx, dy);
						FontStash.fonsSetSize(ref fs, 18.0f);
						FontStash.fonsSetFont(ref fs, fontItalic);
						FontStash.fonsSetColor(ref fs, white);
						FontStash.fonsDrawText(ref fs, dx, dy, "Ég get etið gler án þess að meiða mig.");

						FontStash.fonsVertMetrics(ref fs, ref pf1, ref pf2, ref lh);
						dx = sx;
						dy += lh * 1.2f;
						dash(dx, dy);
						FontStash.fonsSetFont(ref fs, fontJapanese);
						FontStash.fonsDrawText(ref fs, dx, dy, "私はガラスを食べられます。それは私を傷つけません。");

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
						FontStash.fonsDrawText(ref fs, dx, dy, "Blurry...");

						dy += 50.0f;

						FontStash.fonsSetSize(ref fs, 18.0f);
						FontStash.fonsSetFont(ref fs, fontBold);
						FontStash.fonsSetColor(ref fs, black);
						FontStash.fonsSetSpacing(ref fs, 0.0f);
						FontStash.fonsSetBlur(ref fs, 3.0f);
						FontStash.fonsDrawText(ref fs, dx, dy + 2, "DROP THAT SHADOW");

						FontStash.fonsSetColor(ref fs, white);
						FontStash.fonsSetBlur(ref fs, 0);
						FontStash.fonsDrawText(ref fs, dx, dy, "DROP THAT SHADOW");

						if (debug)
							FontStash.fonsDrawDebug(fs, 800.0f, 50.0f);


						GL.Enable(EnableCap.DepthTest);

						Glfw.SwapBuffers(window);
					}
				}
				finally
				{
					GlFontStash.glfonsDelete(fs);
				}
			}
			finally
			{
				Glfw.Terminate();
			}
		}
	}
}
