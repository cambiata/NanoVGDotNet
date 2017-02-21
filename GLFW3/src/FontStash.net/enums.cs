
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
 *  along with FontStash.net.  If not, see <http://www.gnu.org/licenses/>. See
 *  the file lgpl-3.0.txt for more details.
*/

using System;

namespace FontStashDotNet
{
	public enum FONSerrorCode
	{
		// Font atlas is full.
		FONS_ATLAS_FULL = 1,
		// Scratch memory used to render glyphs is full, requested size reported in 'val', you may need to bump up FONS_SCRATCH_BUF_SIZE.
		FONS_SCRATCH_FULL = 2,
		// Calls to fonsPushState has craeted too large stack, if you need deep state stack bump up FONS_MAX_STATES.
		FONS_STATES_OVERFLOW = 3,
		// Trying to pop too many states fonsPopState().
		FONS_STATES_UNDERFLOW = 4,
	}

	public enum FONSalign
	{
		// Horizontal align

		// Default
		FONS_ALIGN_LEFT = 1 << 0,
		FONS_ALIGN_CENTER = 1 << 1,
		FONS_ALIGN_RIGHT = 1 << 2,
		// Vertical align
		FONS_ALIGN_TOP = 1 << 3,
		FONS_ALIGN_MIDDLE	= 1 << 4,
		FONS_ALIGN_BOTTOM	= 1 << 5,
		// Default
		FONS_ALIGN_BASELINE	= 1 << 6,
	}

	public enum FONSflags
	{
		FONS_ZERO_TOPLEFT = 1,
		FONS_ZERO_BOTTOMLEFT = 2,
	}

	public enum STBTT_PLATFORM_ID
	{
		// platformID
		STBTT_PLATFORM_ID_UNICODE = 0,
		STBTT_PLATFORM_ID_MAC = 1,
		STBTT_PLATFORM_ID_ISO = 2,
		STBTT_PLATFORM_ID_MICROSOFT = 3
	}

	public enum STBTT_PLATFORM_ID_MICROSOFT
	{
		// encodingID for STBTT_PLATFORM_ID_MICROSOFT
		STBTT_MS_EID_SYMBOL = 0,
		STBTT_MS_EID_UNICODE_BMP = 1,
		STBTT_MS_EID_SHIFTJIS = 2,
		STBTT_MS_EID_UNICODE_FULL = 10
	}

	public enum STBTT_vmove
	{
		STBTT_vmove = 1,
		STBTT_vline,
		STBTT_vcurve
	}
}

