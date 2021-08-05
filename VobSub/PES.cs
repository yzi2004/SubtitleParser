namespace SubtitleParser.VobSub
{
    /// <summary>
    /// http://www.mpucoder.com/DVD/pes-hdr.html
    /// </summary>
    public class PES
    {
        public ulong? DST { get; set; }
        public int? StreamNum { get; set; }

        public bool HasPTSInfo => DST.HasValue;
        public byte[] Data { get; set; }
    }
}
