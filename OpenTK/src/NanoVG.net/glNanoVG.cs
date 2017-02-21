
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

#define NANOVG_GL2_IMPLEMENTATION
#define NANOVG_GL_USE_STATE_FILTER

#if NANOVG_GL2_IMPLEMENTATION
#define NANOVG_GL2
#define NANOVG_GL_IMPLEMENTATION
#elif NANOVG_GL3_IMPLEMENTATION
#define NANOVG_GL3
#define NANOVG_GL_IMPLEMENTATION
#define NANOVG_GL_USE_UNIFORMBUFFER
#endif

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
using TexPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace NanoVGDotNet
{
	public static class GlNanoVG
	{
		const int GL_TRUE = 1;
		const int GL_FALSE = 0;

		public const int ICON_LOGIN = 0xE740;
		public const int ICON_TRASH = 0xE729;
		public const int ICON_SEARCH = 0x1F50D;
		public const int ICON_CIRCLED_CROSS = 0x2716;
		public const int ICON_CHEVRON_RIGHT = 0xE75E;
		public const int ICON_CHECK = 0x2713;

		static GLNVGcontext gl;

		#region SHADERS

		static string shaderHeader =
#if NANOVG_GL2
			"#define NANOVG_GL2 1\n" +
#elif NANOVG_GL3
			"#version 150 core\n" +
			"#define NANOVG_GL3 1\n" +
#elif NANOVG_GLES2
			"#version 100\n" +
			"#define NANOVG_GL2 1\n" +
#elif NANOVG_GLES3
			"#version 300 es\n" +
			"#define NANOVG_GL3 1\n" +
#endif

#if NANOVG_GL_USE_UNIFORMBUFFER
			"#define USE_UNIFORMBUFFER 1\n" +
#else
			"#define UNIFORMARRAY_SIZE 11\n" +
#endif
			"\n";

		static string fillVertShader =
			"#ifdef NANOVG_GL3\n" +
			"	uniform vec2 viewSize;\n" +
			"	in vec2 vertex;\n" +
			"	in vec2 tcoord;\n" +
			"	out vec2 ftcoord;\n" +
			"	out vec2 fpos;\n" +
			"#else\n" +
			"	uniform vec2 viewSize;\n" +
			"	attribute vec2 vertex;\n" +
			"	attribute vec2 tcoord;\n" +
			"	varying vec2 ftcoord;\n" +
			"	varying vec2 fpos;\n" +
			"#endif\n" +
			"void main(void) {\n" +
			"	ftcoord = tcoord;\n" +
			"	fpos = vertex;\n" +
			"	gl_Position = vec4(2.0*vertex.x/viewSize.x - 1.0, 1.0 - 2.0*vertex.y/viewSize.y, 0, 1);\n" +
			"}\n";

		static string fillFragShader =
			"#ifdef GL_ES\n" +
			"#if defined(GL_FRAGMENT_PRECISION_HIGH) || defined(NANOVG_GL3)\n" +
			" precision highp float;\n" +
			"#else\n" +
			" precision mediump float;\n" +
			"#endif\n" +
			"#endif\n" +
			"#ifdef NANOVG_GL3\n" +
			"#ifdef USE_UNIFORMBUFFER\n" +
			"	layout(std140) uniform frag {\n" +
			"		mat3 scissorMat;\n" +
			"		mat3 paintMat;\n" +
			"		vec4 innerCol;\n" +
			"		vec4 outerCol;\n" +
			"		vec2 scissorExt;\n" +
			"		vec2 scissorScale;\n" +
			"		vec2 extent;\n" +
			"		float radius;\n" +
			"		float feather;\n" +
			"		float strokeMult;\n" +
			"		float strokeThr;\n" +
			"		int texType;\n" +
			"		int type;\n" +
			"	};\n" +
			"#else\n" + // NANOVG_GL3 && !USE_UNIFORMBUFFER
			"	uniform vec4 frag[UNIFORMARRAY_SIZE];\n" +
			"#endif\n" +
			"	uniform sampler2D tex;\n" +
			"	in vec2 ftcoord;\n" +
			"	in vec2 fpos;\n" +
			"	out vec4 outColor;\n" +
			"#else\n" + // !NANOVG_GL3
			"	uniform vec4 frag[UNIFORMARRAY_SIZE];\n" +
			"	uniform sampler2D tex;\n" +
			"	varying vec2 ftcoord;\n" +
			"	varying vec2 fpos;\n" +
			"#endif\n" +
			"#ifndef USE_UNIFORMBUFFER\n" +
			"	#define scissorMat mat3(frag[0].xyz, frag[1].xyz, frag[2].xyz)\n" +
			"	#define paintMat mat3(frag[3].xyz, frag[4].xyz, frag[5].xyz)\n" +
			"	#define innerCol frag[6]\n" +
			"	#define outerCol frag[7]\n" +
			"	#define scissorExt frag[8].xy\n" +
			"	#define scissorScale frag[8].zw\n" +
			"	#define extent frag[9].xy\n" +
			"	#define radius frag[9].z\n" +
			"	#define feather frag[9].w\n" +
			"	#define strokeMult frag[10].x\n" +
			"	#define strokeThr frag[10].y\n" +
			"	#define texType int(frag[10].z)\n" +
			"	#define type int(frag[10].w)\n" +
			"#endif\n" +
			"\n" +
			"float sdroundrect(vec2 pt, vec2 ext, float rad) {\n" +
			"	vec2 ext2 = ext - vec2(rad,rad);\n" +
			"	vec2 d = abs(pt) - ext2;\n" +
			"	return min(max(d.x,d.y),0.0) + length(max(d,0.0)) - rad;\n" +
			"}\n" +
			"\n" +
			"// Scissoring\n" +
			"float scissorMask(vec2 p) {\n" +
			"	vec2 sc = (abs((scissorMat * vec3(p,1.0)).xy) - scissorExt);\n" +
			"	sc = vec2(0.5,0.5) - sc * scissorScale;\n" +
			"	return clamp(sc.x,0.0,1.0) * clamp(sc.y,0.0,1.0);\n" +
			"}\n" +
			"#ifdef EDGE_AA\n" +
			"// Stroke - from [0..1] to clipped pyramid, where the slope is 1px.\n" +
			"float strokeMask() {\n" +
			"	return min(1.0, (1.0-abs(ftcoord.x*2.0-1.0))*strokeMult) * min(1.0, ftcoord.y);\n" +
			"}\n" +
			"#endif\n" +
			"\n" +
			"void main(void) {\n" +
			"   vec4 result;\n" +
			"	float scissor = scissorMask(fpos);\n" +
			"#ifdef EDGE_AA\n" +
			"	float strokeAlpha = strokeMask();\n" +
			"#else\n" +
			"	float strokeAlpha = 1.0;\n" +
			"#endif\n" +
			"	if (type == 0) {			// Gradient\n" +
			"		// Calculate gradient color using box gradient\n" +
			"		vec2 pt = (paintMat * vec3(fpos,1.0)).xy;\n" +
			"		float d = clamp((sdroundrect(pt, extent, radius) + feather*0.5) / feather, 0.0, 1.0);\n" +
			"		vec4 color = mix(innerCol,outerCol,d);\n" +
			"		// Combine alpha\n" +
			"		color *= strokeAlpha * scissor;\n" +
			"		result = color;\n" +
			"	} else if (type == 1) {		// Image\n" +
			"		// Calculate color fron texture\n" +
			"		vec2 pt = (paintMat * vec3(fpos,1.0)).xy / extent;\n" +
			"#ifdef NANOVG_GL3\n" +
			"		vec4 color = texture(tex, pt);\n" +
			"#else\n" +
			"		vec4 color = texture2D(tex, pt);\n" +
			"#endif\n" +
			"		if (texType == 1) color = vec4(color.xyz*color.w,color.w);" +
			"		if (texType == 2) color = vec4(color.x);" +
			"		// Apply color tint and alpha.\n" +
			"		color *= innerCol;\n" +
			"		// Combine alpha\n" +
			"		color *= strokeAlpha * scissor;\n" +
			"		result = color;\n" +
			"	} else if (type == 2) {		// Stencil fill\n" +
			"		result = vec4(1,1,1,1);\n" +
			"	} else if (type == 3) {		// Textured tris\n" +
			"#ifdef NANOVG_GL3\n" +
			"		vec4 color = texture(tex, ftcoord);\n" +
			"#else\n" +
			"		vec4 color = texture2D(tex, ftcoord);\n" +
			"#endif\n" +
			"		if (texType == 1) color = vec4(color.xyz*color.w,color.w);" +
			"		if (texType == 2) color = vec4(color.x);" +
			"		color *= scissor;\n" +
			"		result = color * innerCol;\n" +
			"	}\n" +
			"#ifdef EDGE_AA\n" +
			"	if (strokeAlpha < strokeThr) discard;\n" +
			"#endif\n" +
			"#ifdef NANOVG_GL3\n" +
			"	outColor = result;\n" +
			"#else\n" +
			"	gl_FragColor = result;\n" +
			"#endif\n" +
			"}\n";

		#endregion SHADERS

		static void glnvg__checkError(GLNVGcontext gl, string str)
		{
			ErrorCode err;
			if ((gl.flags & (int)NVGcreateFlags.NVG_DEBUG) == 0)
				return;
			err = GL.GetError();
			if (err != ErrorCode.NoError)
			{
				Console.WriteLine(String.Format("Error {0} after {1}\n", err, str));
				return;
			}
		}

		static void glnvg__bindTexture(GLNVGcontext gl, uint tex)
		{
#if NANOVG_GL_USE_STATE_FILTER
			if (gl.boundTexture != tex)
			{
				gl.boundTexture = tex;
				GL.BindTexture(TextureTarget.Texture2D, tex);
			}
#else
			GL.BindTexture(TextureTarget.Texture2D, tex);
#endif
		}

		static int glnvg__maxi(int a, int b)
		{
			return a > b ? a : b;
		}

		static void glnvg__dumpShaderError(uint shader, string name, string type)
		{
			string info;
			GL.GetShaderInfoLog((int)shader, out info);
			// "Shader %s/%s error:\n%s\n", name, type, str
			Console.WriteLine(String.Format("Shader {0}/{1} error:\n{2}\n", name, type, info));
		}

		static void glnvg__dumpProgramError(uint prog, string name)
		{
			StringBuilder sb = new StringBuilder();
			int len = 0;
			GL.GetShaderInfoLog(prog, 512, out len, sb);
			// printf("Program %s error:\n%s\n", name, str);
			Console.WriteLine(String.Format("Shader {0} error:\n{1}\n", name, sb.ToString()));
		}

		static int glnvg__createShader(out GLNVGshader shader, string name, string header, string opts, string vshader, string fshader)
		{
			int status;
			uint prog, vert, frag;
			string[] str = new string[3];
			str[0] = header;
			str[1] = opts != null ? opts : "";

			shader = new GLNVGshader();

			prog = (uint)GL.CreateProgram();
			vert = (uint)GL.CreateShader(ShaderType.VertexShader);
			frag = (uint)GL.CreateShader(ShaderType.FragmentShader);
			str[2] = vshader;
			GL.ShaderSource((int)vert, str[0] + str[1] + vshader);
			str[2] = fshader;
			GL.ShaderSource((int)frag, str[0] + str[1] + fshader);

			GL.CompileShader(vert);
			GL.GetShader(vert, ShaderParameter.CompileStatus, out status);
			if (status != (int)GL_TRUE)
			{
				glnvg__dumpShaderError(vert, name, "vert");
				return 0;
			}

			GL.CompileShader(frag);
			GL.GetShader(frag, ShaderParameter.CompileStatus, out status);
			if (status != (int)GL_TRUE)
			{
				glnvg__dumpShaderError(frag, name, "frag");
				return 0;
			}

			GL.AttachShader(prog, vert);
			GL.AttachShader(prog, frag);

			GL.BindAttribLocation(prog, 0, "vertex");
			GL.BindAttribLocation(prog, 1, "tcoord");

			GL.LinkProgram(prog);
			GL.GetProgram(prog, ProgramParameter.LinkStatus, out status);
			if (status != (int)GL_TRUE)
			{
				glnvg__dumpProgramError(prog, name);
				return 0;
			}

			shader.prog = prog;
			shader.vert = vert;
			shader.frag = frag;

			return 1;
		}

		static GLNVGcall glnvg__allocCall(GLNVGcontext gl)
		{
			GLNVGcall ret = null;

			if (gl.ncalls + 1 > gl.ccalls)
			{
				int ccalls = glnvg__maxi(gl.ncalls + 1, 128) + gl.ccalls / 2; // 1.5x Overallocate
				//calls = (GLNVGcall*)realloc(gl->calls, sizeof(GLNVGcall) * ccalls);
				Array.Resize<GLNVGcall>(ref gl.calls, ccalls);

				for (int cont = gl.ncalls; cont < ccalls; cont++)
					gl.calls[cont] = new GLNVGcall();

				gl.ccalls = ccalls;
			}

			ret = gl.calls[gl.ncalls++];
			//memset(ret, 0, sizeof(GLNVGcall));
			return ret;
		}

		static int glnvg__deleteTexture(GLNVGcontext gl, int id)
		{
			int i;
			for (i = 0; i < gl.ntextures; i++)
			{
				if (gl.textures[i].id == id)
				{
					if (gl.textures[i].tex != 0 && (gl.textures[i].flags & (int)NVGimageFlagsGL.NVG_IMAGE_NODELETE) == 0)
						GL.DeleteTextures(1, ref gl.textures[i].tex);
					//memset(&gl.textures[i], 0, sizeof(gl.textures[i]));
					gl.textures[i] = new GLNVGtexture();
					return 1;
				}
			}
			return 0;
		}

		static void glnvg__allocTexture(GLNVGcontext gl, out GLNVGtexture tex)
		{
			int i;
			tex = null;

			for (i = 0; i < gl.ntextures; i++)
			{
				if (gl.textures[i].id == 0)
				{
					tex = gl.textures[i];
					break;
				}
			}
			if (tex == null)
			{
				if (gl.ntextures + 1 > gl.ctextures)
				{
					//GLNVGtexture[] textures;
					int ctextures = glnvg__maxi(gl.ntextures + 1, 4) + gl.ctextures / 2; // 1.5x Overallocate
					Array.Resize<GLNVGtexture>(ref gl.textures, ctextures);
					//textures = new GLNVGtexture[ctextures];
					for (int cont = gl.ntextures; cont < ctextures; cont++)
						gl.textures[cont] = new GLNVGtexture();
					//gl.textures = textures;
					gl.ctextures = ctextures;
				}
				tex = gl.textures[gl.ntextures++];
			}
			else
				tex = new GLNVGtexture();

			tex.id = ++gl.textureId;
		}

		static void glnvg__getUniforms(GLNVGshader shader)
		{
			shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_VIEWSIZE] = GL.GetUniformLocation(shader.prog, "viewSize");
			shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_TEX] = GL.GetUniformLocation(shader.prog, "tex");

#if NANOVG_GL_USE_UNIFORMBUFFER
			shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_FRAG] = GL.GetUniformBlockIndex(shader.prog, "frag");
#else
			shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_FRAG] = GL.GetUniformLocation(shader.prog, "frag");
#endif
		}

		static int glnvg__renderCreate(object uptr)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			int align = 4;

			glnvg__checkError(gl, "init");

			if ((gl.flags & (int)NVGcreateFlags.NVG_ANTIALIAS) != 0)
			{
				if (glnvg__createShader(out gl.shader, "shader", shaderHeader, "#define EDGE_AA 1\n", fillVertShader, fillFragShader) == 0)
					return 0;
			}
			else
			{
				if (glnvg__createShader(out gl.shader, "shader", shaderHeader, null, fillVertShader, fillFragShader) == 0)
					return 0;
			}

			glnvg__checkError(gl, "uniform locations");
			glnvg__getUniforms(gl.shader);

			// Create dynamic vertex array
#if NANOVG_GL3
			GL.GenVertexArrays(1, out gl.vertArr);
#endif
			GL.GenBuffers(1, out gl.vertBuf);

#if NANOVG_GL_USE_UNIFORMBUFFER
			// Create UBOs
			uint iBlock = (uint)gl.shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_FRAG];
			GL.UniformBlockBinding(gl.shader.prog, iBlock, (int)GLNVGuniformBindings.GLNVG_FRAG_BINDING);
			GL.GenBuffers(1, out gl.fragBuf);
			GL.GetInteger(GetPName.UniformBufferOffsetAlignment, out align);
#endif

			int size = (int)GLNVGfragUniforms.GetSize; 
			gl.fragSize = size + align - size % align;

			glnvg__checkError(gl, "create done");

			GL.Finish();

			return 1;
		}

		static void glnvg__deleteShader(GLNVGshader shader)
		{
			if (shader.prog != 0)
				GL.DeleteProgram(shader.prog);
			if (shader.vert != 0)
				GL.DeleteShader(shader.vert);
			if (shader.frag != 0)
				GL.DeleteShader(shader.frag);
		}

		static void glnvg__renderCancel(object uptr)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			gl.nverts = 0;
			gl.npaths = 0;
			gl.ncalls = 0;
			gl.nuniforms = 0;
		}

		public static void glnvg__renderDelete(object uptr)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			int i;
			if (gl == null)
				return;

			glnvg__deleteShader(gl.shader);

			#if NANOVG_GL3
			#if NANOVG_GL_USE_UNIFORMBUFFER
		if (gl->fragBuf != 0)
			glDeleteBuffers(1, &gl->fragBuf);
			#endif
		if (gl->vertArr != 0)
			glDeleteVertexArrays(1, &gl->vertArr);
			#endif
			if (gl.vertBuf != 0)
				GL.DeleteBuffers(1, ref gl.vertBuf);

			for (i = 0; i < gl.ntextures; i++)
			{
				if (gl.textures[i].tex != 0 && (gl.textures[i].flags & (int)NVGimageFlagsGL.NVG_IMAGE_NODELETE) == 0)
					GL.DeleteTextures(1, ref gl.textures[i].tex);
			}
			//free(gl.textures);

			//free(gl.paths);
			//free(gl.verts);
			//free(gl.uniforms);
			//free(gl.calls);

			//free(gl);
			gl = null;
		}

		static int glnvg__renderDeleteTexture(object uptr, int image)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			return glnvg__deleteTexture(gl, image);
		}

		static int glnvg__renderCreateTexture2(object uptr, int type, int w, int h, int imageFlags, Bitmap bmp)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			GLNVGtexture tex;
			glnvg__allocTexture(gl, out tex);

			GL.GenTextures(1, out tex.tex);
			tex.width = w;
			tex.height = h;
			tex.type = type;
			tex.flags = imageFlags;
			glnvg__bindTexture(gl, tex.tex);

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
			#if !NANOVG_GLES2
			GL.PixelStore(PixelStoreParameter.UnpackRowLength, tex.width);
			GL.PixelStore(PixelStoreParameter.UnpackSkipPixels, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipRows, 0);
			#endif

			#if NANOVG_GL2
			// GL 1.4 and later has support for generating mipmaps using a tex parameter.
			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_GENERATE_MIPMAPS) != 0)
			{
				//glTexParameteri(GL_TEXTURE_2D, GL_GENERATE_MIPMAP, GL_TRUE);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, (int)GL_TRUE);
			}
			#endif

			BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
				                  ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		
			if (type == (int)NVGtexture.NVG_TEXTURE_RGBA)
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0,
					TexPixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			else
				//glTexImage2D(GL_TEXTURE_2D, 0, GL_RED, w, h, 0, GL_RED, GL_UNSIGNED_BYTE, data);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, w, h, 0,
					TexPixelFormat.Red, PixelType.UnsignedByte, data.Scan0);

			bmp.UnlockBits(data);

			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_GENERATE_MIPMAPS) != 0)
			{
				GL.TexParameter(TextureTarget.Texture2D,
					TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);
			}
			else
			{
				GL.TexParameter(TextureTarget.Texture2D,
					TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
			}
			GL.TexParameter(TextureTarget.Texture2D,
				TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_REPEATX) != 0)
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.Repeat);
			else
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);

			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_REPEATY) != 0)
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.Repeat);
			else
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
			#if !NANOVG_GLES2
			GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipPixels, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipRows, 0);
			#endif

			glnvg__checkError(gl, "create tex");
			glnvg__bindTexture(gl, 0);

			return tex.id;
		}

		static int glnvg__renderCreateTexture(object uptr, int type, int w, int h, int imageFlags, byte[] data)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			GLNVGtexture tex;
			glnvg__allocTexture(gl, out tex);

			GL.GenTextures(1, out tex.tex);
			tex.width = w;
			tex.height = h;
			tex.type = type;
			tex.flags = imageFlags;
			glnvg__bindTexture(gl, tex.tex);

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
#if !NANOVG_GLES2
			GL.PixelStore(PixelStoreParameter.UnpackRowLength, tex.width);
			GL.PixelStore(PixelStoreParameter.UnpackSkipPixels, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipRows, 0);
#endif

#if NANOVG_GL2
			// GL 1.4 and later has support for generating mipmaps using a tex parameter.
			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_GENERATE_MIPMAPS) != 0)
			{
				//glTexParameteri(GL_TEXTURE_2D, GL_GENERATE_MIPMAP, GL_TRUE);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, (int)GL_TRUE);
			}
#endif

			if (type == (int)NVGtexture.NVG_TEXTURE_RGBA)
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0,
					TexPixelFormat.Rgba, PixelType.UnsignedByte, data);
			else
                //glTexImage2D(GL_TEXTURE_2D, 0, GL_RED, w, h, 0, GL_RED, GL_UNSIGNED_BYTE, data);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, w, h, 0,
					TexPixelFormat.Red, PixelType.UnsignedByte, data);

			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_GENERATE_MIPMAPS) != 0)
			{
				GL.TexParameter(TextureTarget.Texture2D,
					TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);
			}
			else
			{
				GL.TexParameter(TextureTarget.Texture2D,
					TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
			}
			GL.TexParameter(TextureTarget.Texture2D,
				TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_REPEATX) != 0)
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.Repeat);
			else
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);

			if ((imageFlags & (int)NVGimageFlags.NVG_IMAGE_REPEATY) != 0)
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.Repeat);
			else
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
#if !NANOVG_GLES2
			GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipPixels, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipRows, 0);
#endif

			glnvg__checkError(gl, "create tex");
			glnvg__bindTexture(gl, 0);

			return tex.id;
		}

		static void glnvg__renderViewport(object uptr, int width, int height, float devicePixelRatio)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			gl.view[0] = (float)width;
			gl.view[1] = (float)height;
		}

		static void glnvg__vset(ref NVGvertex vtx, float x, float y, float u, float v)
		{
			vtx.x = x;
			vtx.y = y;
			vtx.u = u;
			vtx.v = v;
		}

		static int glnvg__maxVertCount(NVGpath[] paths, int npaths)
		{
			int i, count = 0;
			for (i = 0; i < npaths; i++)
			{
				count += paths[i].nfill;
				count += paths[i].nstroke;
			}
			return count;
		}

		static int glnvg__allocPaths(GLNVGcontext gl, int n)
		{
			int ret = 0;
			if (gl.npaths + n > gl.cpaths)
			{
				int cpaths = glnvg__maxi(gl.npaths + n, 128) + gl.cpaths / 2; // 1.5x Overallocate
				//paths = (GLNVGpath*)realloc(gl->paths, sizeof(GLNVGpath) * cpaths);
				Array.Resize<GLNVGpath>(ref gl.paths, cpaths);
				gl.cpaths = cpaths;
			}
			ret = gl.npaths;
			gl.npaths += n;
			return ret;
		}

		static int glnvg__allocVerts(GLNVGcontext gl, int n)
		{
			int ret = 0;
			if (gl.nverts + n > gl.cverts)
			{
				int cverts = glnvg__maxi(gl.nverts + n, 4096) + gl.cverts / 2; // 1.5x Overallocate
				//verts = (NVGvertex*)realloc(gl->verts, sizeof(NVGvertex) * cverts);
				Array.Resize<NVGvertex>(ref gl.verts, cverts);
				gl.cverts = cverts;
			}
			ret = gl.nverts;
			gl.nverts += n;
			return ret;
		}

		static int glnvg__allocFragUniforms(GLNVGcontext gl, int n)
		{
			int ret = 0, structSize = gl.fragSize;
			if (gl.nuniforms + n > gl.cuniforms)
			{
				int cuniforms = glnvg__maxi(gl.nuniforms + n, 128) + gl.cuniforms / 2; // 1.5x Overallocate
				//uniforms = (unsigned char*)realloc(gl->uniforms, structSize * cuniforms);
				Array.Resize<GLNVGfragUniforms>(ref gl.uniforms, cuniforms);
				for (int cont = gl.nuniforms; cont < cuniforms; cont++)
					gl.uniforms[cont] = new GLNVGfragUniforms();
				gl.cuniforms = cuniforms;
			}
			ret = gl.nuniforms * structSize;
			gl.nuniforms += n;
			return ret;
		}

		static NVGcolor glnvg__premulColor(NVGcolor c)
		{
			c.r *= c.a;
			c.g *= c.a;
			c.b *= c.a;
			return c;
		}

		static void glnvg__xformToMat3x4(float[] m3, float[] t)
		{
			m3[0] = t[0];
			m3[1] = t[1];
			m3[2] = 0.0f;
			m3[3] = 0.0f;
			m3[4] = t[2];
			m3[5] = t[3];
			m3[6] = 0.0f;
			m3[7] = 0.0f;
			m3[8] = t[4];
			m3[9] = t[5];
			m3[10] = 1.0f;
			m3[11] = 0.0f;
		}

		static int glnvg__convertPaint(GLNVGcontext gl, ref GLNVGfragUniforms frag, ref NVGpaint paint,
		                               ref NVGscissor scissor, float width, float fringe, float strokeThr)
		{
			GLNVGtexture tex = null;
			float[] invxform = new float[6];

			//memset((byte*)frag, 0, Marshal.SizeOf(*frag));

			frag.innerCol = glnvg__premulColor(paint.innerColor);
			frag.outerCol = glnvg__premulColor(paint.outerColor);

			if (scissor.extent[0] < -0.5f || scissor.extent[1] < -0.5f)
			{
				//memset((byte*)frag->unifGL2.scissorMat, 0, Marshal.SizeOf(frag->unifGL2.scissorMat));
				for (int cont = 0; cont < 12; cont++)
					frag.scissorMat[cont] = 0;
				frag.scissorExt[0] = 1.0f;
				frag.scissorExt[1] = 1.0f;
				frag.scissorScale[0] = 1.0f;
				frag.scissorScale[1] = 1.0f;
			}
			else
			{
				NanoVG.nvgTransformInverse(invxform, scissor.xform);
				glnvg__xformToMat3x4(frag.scissorMat, invxform);
				frag.scissorExt[0] = scissor.extent[0];
				frag.scissorExt[1] = scissor.extent[1];
				frag.scissorScale[0] = (float)Math.Sqrt(scissor.xform[0] * scissor.xform[0] +
					scissor.xform[2] * scissor.xform[2]) / fringe;
				frag.scissorScale[1] = (float)Math.Sqrt(scissor.xform[1] * scissor.xform[1] +
					scissor.xform[3] * scissor.xform[3]) / fringe;
			}

			//memcpy((float*)frag.extent, paint.extent, 2);
			Array.Copy(paint.extent, frag.extent, 2);
			frag.strokeMult = (width * 0.5f + fringe * 0.5f) / fringe;
			frag.strokeThr = strokeThr;

			if (paint.image != 0)
			{
				tex = glnvg__findTexture(gl, paint.image);
				if (tex == null)
					return 0;
				if ((tex.flags & (int)NVGimageFlags.NVG_IMAGE_FLIPY) != 0)
				{
					float[] flipped = new float[6];
					NanoVG.nvgTransformScale(flipped, 1.0f, -1.0f);
					NanoVG.nvgTransformMultiply(flipped, paint.xform);
					NanoVG.nvgTransformInverse(invxform, flipped);
				}
				else
				{
					NanoVG.nvgTransformInverse(invxform, paint.xform);
				}
				frag.type = (int)GLNVGshaderType.NSVG_SHADER_FILLIMG;

				if (tex.type == (int)NVGtexture.NVG_TEXTURE_RGBA)
					frag.texType = (tex.flags & (int)NVGimageFlags.NVG_IMAGE_PREMULTIPLIED) != 0 ? 0 : 1;
				else
					frag.texType = 2;
				//		printf("frag->texType = %d\n", frag->texType);
			}
			else
			{
				frag.type = (int)GLNVGshaderType.NSVG_SHADER_FILLGRAD;
				frag.radius = paint.radius;
				frag.feather = paint.feather;
				NanoVG.nvgTransformInverse(invxform, paint.xform);
			}

			glnvg__xformToMat3x4(frag.paintMat, invxform);

#if ONLY_FOR_DEBUG
			frag->ShowDebug();
#endif

			return 1;
		}

		public static int glnvg__renderUpdateTexture(object uptr, int image, int x, int y, int w, int h, byte[] data)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			GLNVGtexture tex = glnvg__findTexture(gl, image);

			if (tex == null)
				return 0;
			glnvg__bindTexture(gl, tex.tex);

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

#if NANOVG_GLES2 == false
			GL.PixelStore(PixelStoreParameter.UnpackRowLength, tex.width);
			GL.PixelStore(PixelStoreParameter.UnpackSkipPixels, x);
			GL.PixelStore(PixelStoreParameter.UnpackSkipRows, y);
#else
			// No support for all of skip, need to update a whole row at a time.
			/*if (tex.type == (int)NVGtexture.NVG_TEXTURE_RGBA)
			data += y * tex.width * 4;
			else
			data += y * tex.width;*/
			x = 0;
			w = tex.width;
#endif

			if (tex.type == (int)NVGtexture.NVG_TEXTURE_RGBA)
                //glTexSubImage2D(GL_TEXTURE_2D, 0, x, y, w, h, GL_RGBA, GL_UNSIGNED_BYTE, data);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, w, h,
					TexPixelFormat.Rgba, PixelType.UnsignedByte, data);
			else
#if NANOVG_GLES2
				glTexSubImage2D(GL_TEXTURE_2D, 0, x,y, w,h, GL_LUMINANCE, GL_UNSIGNED_BYTE, data);
#else
                //glTexSubImage2D(GL_TEXTURE_2D, 0, x, y, w, h, GL_RED, GL_UNSIGNED_BYTE, data);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, w, h, 
					TexPixelFormat.Red, PixelType.UnsignedByte, data);
#endif

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
#if NANOVG_GLES2 == false
			GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipPixels, 0);
			GL.PixelStore(PixelStoreParameter.UnpackSkipRows, 0);
#endif

			glnvg__bindTexture(gl, 0);

			return 1;
		}

		public static void glnvg__renderTriangles(object uptr, ref NVGpaint paint, ref NVGscissor scissor,
		                                          NVGvertex[] verts, int nverts)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			GLNVGcall call = glnvg__allocCall(gl);
			GLNVGfragUniforms frag;

			//if (call == NULL) return;

			call.type = (int)GLNVGcallType.GLNVG_TRIANGLES;
			call.image = paint.image;

			// Allocate vertices for all the paths.
			call.triangleOffset = glnvg__allocVerts(gl, nverts);
			if (call.triangleOffset == -1)
				goto error;
			call.triangleCount = nverts;

			//memcpy(&gl->verts[call->triangleOffset], verts, sizeof(NVGvertex) * nverts);
			Array.Copy(verts, 0, gl.verts, call.triangleOffset, nverts);

			// Fill shader
			call.uniformOffset = glnvg__allocFragUniforms(gl, 1);
			if (call.uniformOffset == -1)
				goto error;

			frag = nvg__fragUniformPtr(gl, call.uniformOffset);
			// aquí 'frag' es una copia de 'gl.uniforms[call.uniformOffset]'

			glnvg__convertPaint(gl, ref frag, ref paint, ref scissor, 1.0f, 1.0f, -1.0f);

			frag.type = (int)GLNVGshaderType.NSVG_SHADER_IMG;

			nvg__setFragUniform(gl, call.uniformOffset, ref frag);

			// only for debug
#if ONLY_FOR_DEBUG
			Console.WriteLine("Frag Show");
			frag.ShowDebug();
#endif

			// only for debug
#if ONLY_FOR_DEBUG
			Console.WriteLine("Uniforms[0] Show");
			gl.uniforms[0].ShowDebug();
#endif

			return;

			error:
			// We get here if call alloc was ok, but something else is not.
			// Roll back the last call to prevent drawing it.
			if (gl.ncalls > 0)
				gl.ncalls--;
		}

		public static int glnvg__renderGetTextureSize(object uptr, int image, ref int w, ref int h)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			GLNVGtexture tex = glnvg__findTexture(gl, image);
			if (tex == null)
				return 0;
			w = tex.width;
			h = tex.height;
			return 1;
		}

		public static void glnvg__renderStroke(object uptr, ref NVGpaint paint, ref NVGscissor scissor,
		                                       float fringe, float strokeWidth, NVGpath[] paths, int npaths)
		{
			GLNVGfragUniforms frag;
			GLNVGcontext gl = (GLNVGcontext)uptr;
			GLNVGcall call = glnvg__allocCall(gl);
			int i, maxverts, offset;

			//if (call == NULL) return;

			call.type = (int)GLNVGcallType.GLNVG_STROKE;
			call.pathOffset = glnvg__allocPaths(gl, npaths);
			if (call.pathOffset == -1)
				goto error;
			call.pathCount = npaths;
			call.image = paint.image;

			// Allocate vertices for all the paths.
			maxverts = glnvg__maxVertCount(paths, npaths);
			offset = glnvg__allocVerts(gl, maxverts);
			if (offset == -1)
				goto error;

			for (i = 0; i < npaths; i++)
			{
				GLNVGpath copy = gl.paths[call.pathOffset + i];
				NVGpath path = paths[i];
				//memset(copy, 0, sizeof(GLNVGpath));
				copy.fillCount = 0;
				copy.fillOffset = 0;
				copy.strokeCount = 0;
				copy.strokeOffset = 0;

				if (path.nstroke != 0)
				{
					copy.strokeOffset = offset;
					copy.strokeCount = path.nstroke;
					//memcpy(&gl->verts[offset], path->stroke, sizeof(NVGvertex) * path->nstroke);
					Array.Copy(path.stroke, 0, gl.verts, offset, path.nstroke);
					offset += path.nstroke;
					// TODO ¿Es necesario? ¡¡Sí!! es necesrio
					gl.paths[call.pathOffset + i] = copy;
				}
			}

			if ((gl.flags & (int)NVGcreateFlags.NVG_STENCIL_STROKES) != 0)
			{
				// Fill shader
				call.uniformOffset = glnvg__allocFragUniforms(gl, 2);
				if (call.uniformOffset == -1)
					goto error;

				frag = nvg__fragUniformPtr(gl, call.uniformOffset);
				glnvg__convertPaint(gl, ref frag, ref paint, ref scissor, strokeWidth, fringe, -1.0f);
				// new setfrag
				nvg__setFragUniform(gl, call.uniformOffset, ref frag);

				frag = nvg__fragUniformPtr(gl, call.uniformOffset + gl.fragSize);
				glnvg__convertPaint(gl, ref frag, ref paint, ref scissor, strokeWidth, fringe, 1.0f - 0.5f / 255.0f);
				// new setfrag
				nvg__setFragUniform(gl, call.uniformOffset + gl.fragSize, ref frag);
			}
			else
			{
				// Fill shader
				call.uniformOffset = glnvg__allocFragUniforms(gl, 1);
				if (call.uniformOffset == -1)
					goto error;
				frag = nvg__fragUniformPtr(gl, call.uniformOffset);
				glnvg__convertPaint(gl, ref frag, ref paint, ref scissor, strokeWidth, fringe, -1.0f);
				// new setfrag
				nvg__setFragUniform(gl, call.uniformOffset, ref frag);
			}

			return;

			error:

			// We get here if call alloc was ok, but something else is not.
			// Roll back the last call to prevent drawing it.
			if (gl.ncalls > 0)
				gl.ncalls--;
		}

		static void glnvg__renderFill(object uptr, ref NVGpaint paint, ref NVGscissor scissor, float fringe,
		                              float[] bounds, NVGpath[] paths, int npaths)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			GLNVGcall call = glnvg__allocCall(gl);
			NVGvertex[] quad;
			int iquad = 0;

			GLNVGfragUniforms frag;
			GLNVGfragUniforms frag1;

			int i, maxverts, offset;

			//if (call == NULL) return;

			call.type = (int)GLNVGcallType.GLNVG_FILL;
			call.pathOffset = glnvg__allocPaths(gl, npaths);
			if (call.pathOffset == -1)
				goto error;
			call.pathCount = npaths;
			call.image = paint.image;

			if (npaths == 1 && paths[0].convex != 0)
				call.type = (int)GLNVGcallType.GLNVG_CONVEXFILL;

			// Allocate vertices for all the paths.
			maxverts = glnvg__maxVertCount(paths, npaths) + 6;
			offset = glnvg__allocVerts(gl, maxverts);
			if (offset == -1)
				goto error;

			for (i = 0; i < npaths; i++)
			{
				int icopy = call.pathOffset + i;
				GLNVGpath copy = gl.paths[icopy];
				int ipath = i;
				NVGpath path = paths[ipath];

				//memset(copy, 0, sizeof(GLNVGpath));
				copy.fillCount = 0;
				copy.fillOffset = 0;
				copy.strokeCount = 0;
				copy.strokeOffset = 0;

				if (path.nfill > 0)
				{
					copy.fillOffset = offset;
					copy.fillCount = path.nfill;
					//memcpy(&gl->verts[offset], path->fill, sizeof(NVGvertex) * path->nfill);
					Array.Copy(path.fill, path.ifill, gl.verts, offset, path.nfill);
					offset += path.nfill;
				}
				if (path.nstroke > 0)
				{
					copy.strokeOffset = offset;
					copy.strokeCount = path.nstroke;
					//memcpy(&gl->verts[offset], path->stroke, sizeof(NVGvertex) * path->nstroke);
					Array.Copy(path.stroke, path.istroke, gl.verts, offset, path.nstroke);
					offset += path.nstroke;
				}

				gl.paths[icopy] = copy;
			}

			// Quad
			call.triangleOffset = offset;
			call.triangleCount = 6;
			quad = gl.verts;
			iquad = call.triangleOffset;
			glnvg__vset(ref quad[0 + iquad], bounds[0], bounds[3], 0.5f, 1.0f);
			glnvg__vset(ref quad[1 + iquad], bounds[2], bounds[3], 0.5f, 1.0f);
			glnvg__vset(ref quad[2 + iquad], bounds[2], bounds[1], 0.5f, 1.0f);

			glnvg__vset(ref quad[3 + iquad], bounds[0], bounds[3], 0.5f, 1.0f);
			glnvg__vset(ref quad[4 + iquad], bounds[2], bounds[1], 0.5f, 1.0f);
			glnvg__vset(ref quad[5 + iquad], bounds[0], bounds[1], 0.5f, 1.0f);

			// Setup uniforms for draw calls
			if (call.type == (int)GLNVGcallType.GLNVG_FILL)
			{
				call.uniformOffset = glnvg__allocFragUniforms(gl, 2);
				if (call.uniformOffset == -1)
					goto error;
				// Simple shader for stencil
				frag = nvg__fragUniformPtr(gl, call.uniformOffset);
				//memset(frag, 0, sizeof(*frag));
				frag.strokeThr = -1.0f;
				frag.type = (int)GLNVGshaderType.NSVG_SHADER_SIMPLE;
				// new setfrag
				nvg__setFragUniform(gl, call.uniformOffset, ref frag);
				// Fill shader
				frag1 = nvg__fragUniformPtr(gl, call.uniformOffset + gl.fragSize);
				glnvg__convertPaint(gl, ref frag1, ref paint, ref scissor, fringe, fringe, -1.0f);
				// new setfrag
				nvg__setFragUniform(gl, call.uniformOffset + gl.fragSize, ref frag1);
			}
			else
			{
				call.uniformOffset = glnvg__allocFragUniforms(gl, 1);
				if (call.uniformOffset == -1)
					goto error;
				// Fill shader
				frag = nvg__fragUniformPtr(gl, call.uniformOffset);

#if ONLY_FOR_DEBUG
				frag.ShowDebug();
#endif

				glnvg__convertPaint(gl, ref frag, ref paint, ref scissor, fringe, fringe, -1.0f);
				// new setfrag
				nvg__setFragUniform(gl, call.uniformOffset, ref frag);
			}

			return;

			error:
			// We get here if call alloc was ok, but something else is not.
			// Roll back the last call to prevent drawing it.
			if (gl.ncalls > 0)
				gl.ncalls--;
		}

		static BlendingFactorSrc glnvg_convertBlendFuncFactor(int factor)
		{
			//NVGblendFactor bf = (NVGblendFactor)factor;

			if (factor == (int)NVGblendFactor.NVG_ONE)
				return BlendingFactorSrc.One;
			if (factor == (int)NVGblendFactor.NVG_SRC_COLOR)
				return BlendingFactorSrc.Src1Color;
			if (factor == (int)NVGblendFactor.NVG_ONE_MINUS_SRC_COLOR)
				return BlendingFactorSrc.OneMinusSrc1Color;
			if (factor == (int)NVGblendFactor.NVG_DST_COLOR)
				return BlendingFactorSrc.DstColor;
			if (factor == (int)NVGblendFactor.NVG_ONE_MINUS_DST_COLOR)
				return BlendingFactorSrc.OneMinusDstColor;
			if (factor == (int)NVGblendFactor.NVG_SRC_ALPHA)
				return BlendingFactorSrc.SrcAlpha;
			if (factor == (int)NVGblendFactor.NVG_ONE_MINUS_SRC_ALPHA)
				return BlendingFactorSrc.OneMinusSrcAlpha;
			if (factor == (int)NVGblendFactor.NVG_DST_ALPHA)
				return BlendingFactorSrc.DstAlpha;
			if (factor == (int)NVGblendFactor.NVG_ONE_MINUS_DST_ALPHA)
				return BlendingFactorSrc.OneMinusDstAlpha;
			if (factor == (int)NVGblendFactor.NVG_SRC_ALPHA_SATURATE)
				return BlendingFactorSrc.SrcAlphaSaturate;
			//if (factor == (int)NVGblendFactor.NVG_ZERO)
			return BlendingFactorSrc.Zero;
		}

		static void glnvg__blendCompositeOperation(NVGcompositeOperationState op)
		{
			BlendingFactorSrc bfs1 = glnvg_convertBlendFuncFactor(op.srcRGB);
			BlendingFactorDest bfd1 = (BlendingFactorDest)glnvg_convertBlendFuncFactor(op.dstRGB);
			BlendingFactorSrc bfs2 = glnvg_convertBlendFuncFactor(op.srcAlpha);
			BlendingFactorDest bfd2 = (BlendingFactorDest)glnvg_convertBlendFuncFactor(op.dstAlpha);
			/*
			int bs1 = (int)bfs1;
			int bs2 = (int)bfs2;
			int bd1 = (int)bfd1;
			int bd2 = (int)bfd2;
			*/
			GL.BlendFuncSeparate(bfs1, bfd1, bfs2, bfd2);
		}

		static void glnvg__stencilMask(GLNVGcontext gl, uint mask)
		{
#if NANOVG_GL_USE_STATE_FILTER
			if (gl.stencilMask != mask)
			{
				gl.stencilMask = mask;
				GL.StencilMask(mask);
			}
#else
			GL.StencilMask(mask);
#endif
		}

		static void glnvg__stencilFunc(GLNVGcontext gl, StencilFunction func, int ref_, uint mask)
		{
#if NANOVG_GL_USE_STATE_FILTER
			if ((gl.stencilFunc != func) ||
			    (gl.stencilFuncRef != ref_) ||
			    (gl.stencilFuncMask != mask))
			{

				gl.stencilFunc = func;
				gl.stencilFuncRef = ref_;
				gl.stencilFuncMask = mask;
				GL.StencilFunc(func, ref_, mask);
			}
#else
			GL.StencilFunc(func, ref_, mask);
#endif
		}

		static GLNVGtexture glnvg__findTexture(GLNVGcontext gl, int id)
		{
			int i;
			for (i = 0; i < gl.ntextures; i++)
				if (gl.textures[i].id == id)
					return gl.textures[i];
			return null;
		}

		#region ¡POINTERS!

		static GLNVGfragUniforms nvg__fragUniformPtr(GLNVGcontext gl, int offset)
		{
			// size of GLNVGfragUniforms = 180 bytes
			offset = (offset / 180);

			return gl.uniforms[offset];
		}

		static void nvg__setFragUniform(GLNVGcontext gl, int offset, ref GLNVGfragUniforms frag)
		{
			// size of GLNVGfragUniforms = 180 bytes
			offset = (offset / 180);

			gl.uniforms[offset] = frag;
		}

		static void glnvg__setUniforms(GLNVGcontext gl, int uniformOffset, int image)
		{
#if NANOVG_GL_USE_UNIFORMBUFFER
			glBindBufferRange(GL_UNIFORM_BUFFER, GLNVG_FRAG_BINDING, gl->fragBuf, uniformOffset, sizeof(GLNVGfragUniforms));
#else
			GLNVGfragUniforms frag = nvg__fragUniformPtr(gl, uniformOffset);

			//CorrigeFrag(ref frag);

			int lt = gl.shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_FRAG];
			/*
			// only for debug
			Console.WriteLine("************** UniformsArray NO Corregido *************");
			frag.ShowDebug();

			CorrigeSetUniforms(ref frag);

			// only for debug
			Console.WriteLine("************** UniformsArray Corregido ****************");
			frag.ShowDebug();
			*/

			// GL.Uniform4(); NanoVG.NANOVG_GL_UNIFORMARRAY_SIZE = 11; Indica que se pasan 11 vectores de 4 floats

			float[] farr = frag.GetFloats;

			GL.Uniform4(lt, NanoVG.NANOVG_GL_UNIFORMARRAY_SIZE, farr); //frag.uniformArray);

#endif

			if (image != 0)
			{
				GLNVGtexture tex = glnvg__findTexture(gl, image);
				glnvg__bindTexture(gl, tex != null ? tex.tex : 0);
				glnvg__checkError(gl, "tex paint tex");
			}
			else
			{
				glnvg__bindTexture(gl, 0);
			}
		}

		#endregion ¡POINTERS!

		static void glnvg__fill(GLNVGcontext gl, ref GLNVGcall call)
		{
			GLNVGpath[] paths = gl.paths;
			int pathOffset = call.pathOffset;
			int i, npaths = call.pathCount;

			// Draw shapes
			GL.Enable(EnableCap.StencilTest);
			glnvg__stencilMask(gl, 0xff);
			glnvg__stencilFunc(gl, StencilFunction.Always, 0x00, 0xff);
			GL.ColorMask(false, false, false, false);

			// set bindpoint for solid loc
			glnvg__setUniforms(gl, call.uniformOffset, 0);
			glnvg__checkError(gl, "fill simple");

			GL.StencilOpSeparate(StencilFace.Front, StencilOp.Keep, StencilOp.Keep, StencilOp.IncrWrap);
			GL.StencilOpSeparate(StencilFace.Back, StencilOp.Keep, StencilOp.Keep, StencilOp.DecrWrap);

			GL.Disable(EnableCap.CullFace);
			for (i = 0; i < npaths; i++)
				GL.DrawArrays(BeginMode.TriangleFan, 
					paths[i + pathOffset].fillOffset,
					paths[i + pathOffset].fillCount);
			GL.Enable(EnableCap.CullFace);

			// Draw anti-aliased pixels
			GL.ColorMask(true, true, true, true);

			glnvg__setUniforms(gl, call.uniformOffset + gl.fragSize, call.image);
			glnvg__checkError(gl, "fill fill");

			if ((gl.flags & (int)NVGcreateFlags.NVG_ANTIALIAS) != 0)
			{
				glnvg__stencilFunc(gl, StencilFunction.Equal, 0x00, 0xff);
				GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
				// Draw fringes
				for (i = 0; i < npaths; i++)
					GL.DrawArrays(BeginMode.TriangleStrip, 
						paths[i + pathOffset].strokeOffset,
						paths[i + pathOffset].strokeCount);
			}

			// Draw fill
			glnvg__stencilFunc(gl, StencilFunction.Notequal, 0x0, 0xff);
			GL.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Zero);
			GL.DrawArrays(BeginMode.Triangles, call.triangleOffset, call.triangleCount);

			GL.Disable(EnableCap.StencilTest);
		}

		static void glnvg__convexFill(GLNVGcontext gl, ref GLNVGcall call)
		{
			GLNVGpath[] paths = gl.paths;
			int pathOffset = call.pathOffset;
			int i, npaths = call.pathCount;

			glnvg__setUniforms(gl, call.uniformOffset, call.image);
			glnvg__checkError(gl, "convex fill");

			for (i = 0; i < npaths; i++)
				GL.DrawArrays(BeginMode.TriangleFan, 
					paths[i + pathOffset].fillOffset,
					paths[i + pathOffset].fillCount);
		
			if ((gl.flags & (int)NVGcreateFlags.NVG_ANTIALIAS) != 0)
			{
				// Draw fringes
				for (i = 0; i < npaths; i++)
					GL.DrawArrays(BeginMode.TriangleStrip, 
						paths[i + pathOffset].strokeOffset,
						paths[i + pathOffset].strokeCount);
			}
		}

		static void glnvg__stroke(GLNVGcontext gl, ref GLNVGcall call)
		{
			GLNVGpath[] paths = gl.paths;
			int pathOffset = call.pathOffset;
			int npaths = call.pathCount, i;

			if ((gl.flags & (int)NVGcreateFlags.NVG_STENCIL_STROKES) != 0)
			{

				GL.Enable(EnableCap.StencilTest);
				glnvg__stencilMask(gl, 0xff);

				// Fill the stroke base without overlap
				glnvg__stencilFunc(gl, StencilFunction.Equal, 0x0, 0xff);
				GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Incr);
				glnvg__setUniforms(gl, call.uniformOffset + gl.fragSize, call.image);
				glnvg__checkError(gl, "stroke fill 0");

				for (i = 0; i < npaths; i++)
					GL.DrawArrays(BeginMode.TriangleStrip, 
						paths[i + pathOffset].strokeOffset,
						paths[i + pathOffset].strokeCount);

				// Draw anti-aliased pixels.
				glnvg__setUniforms(gl, call.uniformOffset, call.image);
				glnvg__stencilFunc(gl, StencilFunction.Equal, 0x00, 0xff);
				GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);

				for (i = 0; i < npaths; i++)
					GL.DrawArrays(BeginMode.TriangleStrip, 
						paths[i + pathOffset].strokeOffset,
						paths[i + pathOffset].strokeCount);

				// Clear stencil buffer.
				GL.ColorMask(false, false, false, false);
				glnvg__stencilFunc(gl, StencilFunction.Always, 0x0, 0xff);
				GL.StencilOp(StencilOp.Zero, StencilOp.Zero, StencilOp.Zero);
				glnvg__checkError(gl, "stroke fill 1");

				for (i = 0; i < npaths; i++)
					GL.DrawArrays(BeginMode.TriangleStrip, 
						paths[i + pathOffset].strokeOffset,
						paths[i + pathOffset].strokeCount);
			
				GL.ColorMask(true, true, true, true);
				GL.Disable(EnableCap.StencilTest);

				// glnvg__convertPaint(gl, nvg__fragUniformPtr(gl, call->uniformOffset + gl->fragSize), paint, scissor, strokeWidth, fringe, 1.0f - 0.5f/255.0f);

			}
			else
			{
				glnvg__setUniforms(gl, call.uniformOffset, call.image);
				glnvg__checkError(gl, "stroke fill");

				// Draw Strokes
				for (i = 0; i < npaths; i++)
					GL.DrawArrays(BeginMode.TriangleStrip, 
						paths[i + pathOffset].strokeOffset,
						paths[i + pathOffset].strokeCount);
			}
		}

		static void glnvg__triangles(GLNVGcontext gl, ref GLNVGcall call)
		{
			glnvg__setUniforms(gl, call.uniformOffset, call.image);
			glnvg__checkError(gl, "triangles fill");

			GL.DrawArrays(BeginMode.Triangles, call.triangleOffset, call.triangleCount);
		}

		static void glnvg__renderFlush(object uptr, NVGcompositeOperationState compositeOperation)
		{
			GLNVGcontext gl = (GLNVGcontext)uptr;
			int i;

			if (gl.ncalls > 0)
			{
				// Setup require GL state.
				GL.UseProgram(gl.shader.prog);

				glnvg__blendCompositeOperation(compositeOperation);
				GL.Enable(EnableCap.CullFace);
				GL.CullFace(CullFaceMode.Back);
				GL.FrontFace(FrontFaceDirection.Ccw);
				GL.Enable(EnableCap.Blend);
				GL.Disable(EnableCap.DepthTest);
				GL.Disable(EnableCap.ScissorTest);
				GL.ColorMask(true, true, true, true);
				GL.StencilMask(0xffffffff);
				GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
				GL.StencilFunc(StencilFunction.Always, 0, 0xffffffff);
				GL.ActiveTexture(TextureUnit.Texture0);
				GL.BindTexture(TextureTarget.Texture2D, 0);

#if NANOVG_GL_USE_STATE_FILTER
				gl.boundTexture = 0;
				gl.stencilMask = 0xffffffff;
				gl.stencilFunc = StencilFunction.Always;
				gl.stencilFuncRef = 0;
				gl.stencilFuncMask = 0xffffffff;
#endif

#if NANOVG_GL_USE_UNIFORMBUFFER
				// Upload ubo for frag shaders
				glBindBuffer(GL_UNIFORM_BUFFER, gl.fragBuf);
				glBufferData(GL_UNIFORM_BUFFER, gl.nuniforms * gl.fragSize, gl.uniforms, GL_STREAM_DRAW);
#endif

				// Upload vertex data
#if NANOVG_GL3
				GL.BindVertexArray(gl.vertArr);
#endif
				GL.BindBuffer(BufferTarget.ArrayBuffer, gl.vertBuf);
				//GL.BufferData(BufferTarget.ArrayBuffer, gl.nverts * Marshal.SizeOf(typeof(NVGvertex)), gl.verts, BufferUsageHint.StaticDraw);
				IntPtr iptr = (IntPtr)(gl.nverts * Marshal.SizeOf(typeof(NVGvertex)));
				GL.BufferData<NVGvertex>(BufferTarget.ArrayBuffer, iptr, gl.verts, BufferUsageHint.StreamDraw);
				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);

				int s = Marshal.SizeOf(typeof(NVGvertex));
				GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, s, IntPtr.Zero);
				int st = 2 * sizeof(float);
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, s, (IntPtr)st);

				// Set view and texture just once per frame.
				int loc1 = gl.shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_TEX];
				GL.Uniform1(loc1, 0);
				int loc2 = gl.shader.loc[(int)GLNVGuniformLoc.GLNVG_LOC_VIEWSIZE];
				GL.Uniform2(loc2, 1, gl.view);

#if NANOVG_GL_USE_UNIFORMBUFFER
				glBindBuffer(GL_UNIFORM_BUFFER, gl->fragBuf);
#endif

				for (i = 0; i < gl.ncalls; i++)
				{
					GLNVGcall call = gl.calls[i];
					if (call.type == (int)GLNVGcallType.GLNVG_FILL)
						glnvg__fill(gl, ref call);
					else if (call.type == (int)GLNVGcallType.GLNVG_CONVEXFILL)
						glnvg__convexFill(gl, ref call);
					else if (call.type == (int)GLNVGcallType.GLNVG_STROKE)
						glnvg__stroke(gl, ref call);
					else if (call.type == (int)GLNVGcallType.GLNVG_TRIANGLES)
						glnvg__triangles(gl, ref call);
				}

				GL.DisableVertexAttribArray(0);
				GL.DisableVertexAttribArray(1);
#if NANOVG_GL3
				GL.BindVertexArray(0);
#endif
				GL.Disable(EnableCap.CullFace);
				GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
				GL.UseProgram(0);
				glnvg__bindTexture(gl, 0);
			}

			// Reset calls
			gl.nverts = 0;
			gl.npaths = 0;
			gl.ncalls = 0;
			gl.nuniforms = 0;
		}

		/// <summary>
		/// nvgCreateGL2 == nvgCreateGL3
		/// </summary>
		/// <param name="ctx">Context.</param>
		/// <param name="flags">Flags.</param>
		public static void nvgCreateGL(ref NVGcontext ctx, int flags)
		{
			NVGparams params_ = new NVGparams();
			ctx = null;
			gl = new GLNVGcontext();

			params_.renderCreate = glnvg__renderCreate;
			params_.renderCreateTexture = glnvg__renderCreateTexture;
			params_.renderCreateTexture2 = glnvg__renderCreateTexture2;
			params_.renderFlush = glnvg__renderFlush;
			params_.renderFill = glnvg__renderFill;
			params_.renderStroke = glnvg__renderStroke;
			params_.renderTriangles = glnvg__renderTriangles;
			params_.renderGetTextureSize = glnvg__renderGetTextureSize;
			params_.renderViewport = glnvg__renderViewport;
			params_.renderUpdateTexture = glnvg__renderUpdateTexture;
			params_.renderDeleteTexture = glnvg__renderDeleteTexture;
			params_.renderCancel = glnvg__renderCancel;
			params_.renderDelete = glnvg__renderDelete;
			params_.userPtr = gl;
			params_.edgeAntiAlias = (flags & (int)NVGcreateFlags.NVG_ANTIALIAS) != 0 ? 1 : 0;

			gl.flags = flags;

			NanoVG.nvgCreateInternal(ref params_, out ctx);
		}
	}

	public class DemoData
	{
		public int fontNormal, fontBold, fontIcons, fontEmoji;
		public int[] images;
		//[12];

		public DemoData()
		{
			images = new int[12];
		}
	}

	public struct GLNVGpath
	{
		public int fillOffset;
		public int fillCount;
		public int strokeOffset;
		public int strokeCount;
	}

	public class GLNVGtexture
	{
		public int id;
		public uint tex;
		public int width, height;
		public int type;
		public int flags;
	}

	public class GLNVGcontext
	{
		public GLNVGshader shader;
		public GLNVGtexture[] textures;
		// [2]
		public float[] view;
		public int ntextures;
		public int ctextures;
		public int textureId;
		public uint vertBuf;
		#if NANOVG_GL3
		public uint vertArr;
#endif
		#if NANOVG_GL_USE_UNIFORMBUFFER
		public uint fragBuf;
#endif
		public int fragSize;
		public int flags;

		// Per frame buffers
		public GLNVGcall[] calls;
		public int ccalls;
		public int ncalls;
		public GLNVGpath[] paths;
		public int cpaths;
		public int npaths;
		public NVGvertex[] verts;
		public int cverts;
		public int nverts;
		public GLNVGfragUniforms[] uniforms;
		public int cuniforms;
		public int nuniforms;

		// cached state
		#if NANOVG_GL_USE_STATE_FILTER
		public uint boundTexture;
		public uint stencilMask;
		public StencilFunction stencilFunc;
		public int stencilFuncRef;
		public uint stencilFuncMask;
		#endif

		public GLNVGcontext()
		{
			view = new float[2];
		}
	}

	public class GLNVGshader
	{
		public uint prog;
		public uint frag;
		public uint vert;
		//[GLNVG_MAX_LOCS];
		public int[] loc;

		public GLNVGshader()
		{
			loc = new int[(int)GLNVGuniformLoc.GLNVG_MAX_LOCS];
		}
	}

	public class GLNVGcall
	{
		public int type;
		public int image;
		public int pathOffset;
		public int pathCount;
		public int triangleOffset;
		public int triangleCount;
		public int uniformOffset;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class GLNVGfragUniforms
	{
		// matrices are actually 3 vec4s

		// float[12]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
		public float[] scissorMat;
		// float[12]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
		public float[] paintMat;
		//[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public NVGcolor innerCol;
		//[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public NVGcolor outerCol;
		// float[2]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		public float[] scissorExt;
		// float[2]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		public float[] scissorScale;
		// float[2]
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		public float[] extent;

		public float radius;
		public float feather;
		public float strokeMult;
		public float strokeThr;

		float texType_;

		public int texType
		{
			get { return (int)texType_; }
			set { texType_ = value; }
		}

		float type_;

		public int type
		{
			get { return (int)type_; }
			set { type_ = value; }
		}

		public GLNVGfragUniforms()
		{
			scissorMat = new float[12];
			paintMat = new float[12];
			innerCol = new NVGcolor();
			outerCol = new NVGcolor();
			scissorExt = new float[2];
			scissorScale = new float[2];
			extent = new float[2];
		}

		public float[] GetFloats
		{
			get
			{
				int size = (int)GLNVGfragUniforms.GetSize;
				int felements = (int)Math.Ceiling((float)(size / sizeof(float)));
				float[] farr = new float[felements];

				IntPtr ptr = Marshal.AllocHGlobal(size);
				Marshal.StructureToPtr(this, ptr, true);
				Marshal.Copy(ptr, farr, 0, felements);
				Marshal.FreeHGlobal(ptr);
				return farr;
			}
		}

		/// <summary>
		/// Gets the size of the <see cref="GLNVGfragUniforms"/> in bytes.
		/// </summary>
		/// <value>The size of the GLNVGfragUniforms struct.</value>
		public static uint GetSize
		{
			get
			{
				// 176 bytes
				//return (uint)(12 + 12 + 4 + 4 + 2 + 2 + 2 + 6) * 4;
				return (uint)Marshal.SizeOf(typeof(GLNVGfragUniforms));
			}
		}
	}

}

