using System;
using System.Collections.Generic;
using System.Drawing;

namespace SubtitleParser.VobSub
{
    /// <summary>
    /// Subtitle Picture - see http://www.mpucoder.com/DVD/spu.html for more info
    /// </summary>
    public class SPU
    {
        public int? StreamNum { get; set; }
        public TimeSpan StartTime { get; set; }

        public byte[] Data { get; set; }
        public List<SPDCSQ> SPDCSQs { get; set; } = new List<SPDCSQ>();

        /// <summary>
        /// Sub-Picture Display Control SeQuence
        /// </summary>
        public class SPDCSQ
        {
            /// <summary>
            /// Start Display Delay Time. usual: 0　
            /// </summary>
            public TimeSpan Start { get; set; }

            /// <summary>
            /// Stop Display Delay Time.
            /// </summary>
            public TimeSpan Stop { get; set; }

            /// <summary>
            /// Image Size
            /// </summary>
            public Size ImgSize { get; set; }

            /// <summary>
            /// PXDtf
            /// address of pixel data for the top field 
            /// </summary>
            public int PXDtfOffset { get; set; }

            /// <summary>
            /// PXDbf
            /// address of pixel data for the bottom field 
            /// </summary>
            public int PXDbfOffset { get; set; }

            /// <summary>
            /// subtitle display four colors
            /// backgroud,pattern,emphasis 1,emphasis 2
            /// </summary>
            public FourColor fourColor { get; set; }
        }

        /// <summary>
        /// four color 
        /// </summary>
        public class FourColor
        {
            /// <summary>
            /// 
            /// </summary>
            public int? Background { get; set; }
            public int? Pattern { get; set; }
            public int? Emphasis1 { get; set; }
            public int? Emphasis2 { get; set; }

            public int? ContrBG { get; set; }
            public int? ContrPtn { get; set; }
            public int? ContrEm1 { get; set; }
            public int? ContrEm2 { get; set; }
        }
    }
}
