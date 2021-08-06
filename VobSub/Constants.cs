namespace SubtitleParser.VobSub
{
    public class Constants
    {
        public const int PESPackSize = 0x800;

        public const int Mpeg2PackHeaderLength = 14;

        /// <summary>
        /// PES Header Length
        /// </summary>
        public const int PESHeaderLength = 6;

        /// <summary>
        /// MPEG-2 extension length
        /// </summary>
        public const int MPEG2ExtLength = 3;


        public enum DCC
        {
            ForcedStartDisplay = 0,
            StartDisplay = 1,
            StopDisplay = 2,
            SetColor = 3,
            SetContrast = 4,
            SetDisplayArea = 5,
            SetPixelDataAddress = 6,
            ChangeColorAndContrast = 7,
            End = 0xFF,
        }

        
    }
}
